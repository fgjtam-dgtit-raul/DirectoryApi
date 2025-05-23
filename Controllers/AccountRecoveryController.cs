using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Data.Common;
using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AuthApi.Data;
using AuthApi.Models;
using AuthApi.Models.Responses;
using AuthApi.Helper;
using AuthApi.Entities;
using AuthApi.Data.Exceptions;
using AuthApi.Validators.AccountRecovery;
using AuthApi.Services;

namespace AuthApi.Controllers
{

    [Authorize]
    [ApiController]
    [Route("api/[Controller]")]
    public class AccountRecoveryController(ILogger<AccountRecoveryController> logger, RecoveryAccountService ras, MinioService ms, CatalogService cs, DirectoryDBContext c, PersonService ps) : ControllerBase
    {
        private readonly ILogger<AccountRecoveryController> _logger = logger;
        private readonly RecoveryAccountService recoveryAccountService = ras;
        private readonly MinioService minioService = ms;
        private readonly CatalogService catalogService = cs;
        private readonly DirectoryDBContext context = c;
        private readonly PersonService personService = ps;

        /// <summary>
        /// List the accounts recovery request
        /// </summary>
        /// <param name="orderBy"> propertie name used for ordering by default 'createdAt' posibles ["id", "name", "folio", "denunciaId","status","area", "createdAt"] </param>
        /// <param name="ascending">Ordering mode</param>
        /// <param name="excludeConcluded">Ignore the request finished</param>
        /// <param name="excludeDeleted">Ignore the request deleted</param>
        /// <param name="excludePending">Ignore the request pending</param>
        /// <param name="take">Ammount of record to return</param>
        /// <param name="offset"></param>
        /// <returns></returns>
        /// <response code="200">Get the list of recovery request</response>
        [HttpGet]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<IActionResult> ListAccountRecoveryRequest(
            [FromQuery] string orderBy = "createdAt",
            [FromQuery] bool ascending = false,
            [FromQuery] bool excludeConcluded = false,
            [FromQuery] bool excludeDeleted = false,
            [FromQuery] bool excludePending = false,
            [FromQuery] int take = 5,
            [FromQuery] int offset = 0
        )
        {
            // * get data
            var records = this.recoveryAccountService.GetAllRecords(out int totalRecords, take, offset, orderBy, ascending, excludeConcluded, excludeDeleted, excludePending);

            // * make pagination
            var paginator = new
            {
                Total = totalRecords,
                Data = records,
                Filters = new
                {
                    Take = take,
                    Offset = offset,
                    OrderBy = orderBy,
                    Ascending = ascending,
                    ExcludeConcluded = excludeConcluded,
                    ExcludeDeleted = excludeDeleted,
                    ExcludePending = excludePending
                }
            };

            await Task.CompletedTask;
            return Ok(paginator);
        }


        /// <summary>
        /// Get the recovery request with temporally url of the files
        /// </summary>
        /// <returns></returns>
        /// <response code="200">Get the data</response>
        /// <response code="400">The request is not valid</response>
        [HttpGet("{accountRecoveryUUID}")]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<AccountRecoveryResponse>> GetWithDocs([FromRoute] string accountRecoveryUUID)
        {
            var parseCorrect = Guid.TryParse(accountRecoveryUUID, out Guid requestUUID);
            if(!parseCorrect){
                return BadRequest( new {
                    Title = "El formato del accountRecoveryUUID es incorrecto",
                    Message = "El formato del accountRecoveryUUID es incorrecto, se espera un UUID"
                });
            }

            // * get data
            var cdata = await this.recoveryAccountService.GetRequestWithFiles(requestUUID);
            return cdata!;
        }


        /// <summary>
        /// Register a new account recovery request, only one request per person is allowed
        /// </summary>
        /// <returns></returns>
        /// <response code="201">Stored the recovery request</response>
        /// <response code="400">The person already has a recovery request</response>
        /// <response code="401">Auth token is not valid or is not present</response>
        /// <response code="409">Some error at attempt to store the request</response>
        /// <response code="422">The request params are not valid</response>
        [HttpPost]
        [Consumes(MediaTypeNames.Application.Json)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(UnprocesableResponse), StatusCodes.Status422UnprocessableEntity)]
        public IActionResult RegisterNewRequest([FromBody] AccountRecoveryRequest request)
        {
            // * validate the request
            var validationResults = new NewAccountRecoberyValidator().Validate(request);
            if(!validationResults.IsValid){
                return UnprocessableEntity( new UnprocesableResponse(validationResults.Errors));
            }

            // * check if already exist a request
            if(ExistAPendingRequest(request))
            {
                var nationalityMX = context.Nationality.Where(item => EF.Functions.Like(item.Name, "mexicana")).FirstOrDefault();
                if(request.NationalityId == nationalityMX?.Id )
                {
                    return BadRequest( new {
                        Title = "Ya existe una petición de recuperación de cuenta registrada.",
                        Errors = new Dictionary<string,string>
                        {
                            { "curp","Ya existe una petición de recuperación de cuenta registrada, espere que concluya o comuníquese con el administrador."}
                        }
                    });
                }
                else
                {
                    return BadRequest( new {
                        Title = "Ya existe una petición de recuperación de cuenta registrada.",
                        Errors = new Dictionary<string,string>
                        {
                            { "email","Ya existe una petición de recuperación de cuenta registrada, espere que concluya o comuníquese con el administrador."}
                        }
                    });
                }
            }


            // * store the request
            try {
                var recoveryRequest = this.recoveryAccountService.RegisterRecoveryAccountRequest(request);
                return StatusCode(201, recoveryRequest);
            }catch(SimpleValidationException sve){
                return UnprocessableEntity( new UnprocesableResponse(sve.ValidationErrors.ToDictionary()));
            }catch(Exception err){
                return Conflict( new {
                    Title = "Error al registrar la peticion",
                    err.Message
                });
            }
        }


        /// <summary>
        /// Upload a file for the account recovery
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST /api/AccountRecovery/{accountRecoveryUUID}/file
        ///     Content-Type: multipart/form-data
        ///     Authorization: Bearer {auth-token}
        ///
        /// **Body Parameters:**
        /// - **documentTypeId**: (int, required)
        /// - **file**: (file, required, size:max:10MB, content-type:pdf,jpg,png)
        ///
        /// </remarks>
        /// <returns></returns>
        /// <response code="201">The file was uploaded</response>
        /// <response code="400">The request is not valid</response>
        /// <response code="401">Auth token is not valid or is not present</response>
        /// <response code="404">The request id was not found on the system</response>
        [HttpPost("{accountRecoveryUUID}/file")]
        [Consumes(MediaTypeNames.Multipart.FormData)]
        [Produces(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(AccountRecoveryFile), StatusCodes.Status201Created)]
        [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB
        public async Task<ActionResult<AccountRecoveryFileResponse>> UploadFile([FromRoute] string accountRecoveryUUID, [FromForm] AccountRecoveryFileRequest fileRequest)
        {
            var parseCorrect = Guid.TryParse(accountRecoveryUUID, out Guid requestUUID);
            if(!parseCorrect){
                return BadRequest( new {
                    Title = "El formato del accountRecoveryUUID es incorrecto",
                    Message = "El formato del accountRecoveryUUID es incorrecto, se espera un UUID"
                });
            }

            // * validate the file
            var validator = new AccountRecoveryUploadFileValidator().Validate(fileRequest);
            if(!validator.IsValid)
            {
                return UnprocessableEntity(
                    new UnprocesableResponse(validator.Errors)
                );
            }


            // * attempt to get the recovery request
            var recoveryRequest = this.recoveryAccountService.GetByID(requestUUID);
            if(recoveryRequest == null)
            {
                return NotFound(new {
                    Title = "No se econtro el registro en la base de datos.",
                    Message = "No se econtro el registro en la base de datos."
                });
            }

            // * get the documentType
            DocumentType? documentType1 = null;
            try
            {
                documentType1 = catalogService.GetDocumentTypes().FirstOrDefault(item => item.Id == fileRequest.DocumentTypeId);
                if(documentType1 == null){
                    var errors = new Dictionary<string,string> { {"documentTypeId", "No se encontro el tipo de documento"}};
                    return UnprocessableEntity(new UnprocesableResponse(errors));
                }
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error at attempting to get the document types catalog: {message}", ex.Message);
                return Conflict( new {
                    Title = "Error al conectarse al servidor",
                    Message = "Error al conectarse al servidor, " + ex.Message,
                });
            }

            // * upload the file
            var uuidFile = Guid.NewGuid();
            string filePath = "";
            using(var stream = fileRequest.File!.OpenReadStream())
            {
                filePath = await minioService.UploadFile( fileRequest.File!.FileName, stream, uuidFile, $"accountRecoveryFiles/{accountRecoveryUUID.ToLower()}/");
            }
            if(string.IsNullOrEmpty(filePath))
            {
                throw new Exception("Fail at uplaod the file");
            }

            // * make the file record
            var recoveryAccountFile = new AccountRecoveryFile()
            {
                Id = uuidFile,
                FileName = fileRequest.File!.FileName,
                FilePath = filePath,
                FileType = fileRequest.File!.ContentType,
                FileSize = fileRequest.File!.Length,
                DocumentType = documentType1
            };
            this.recoveryAccountService.AttachFile(recoveryRequest, recoveryAccountFile);

            // * return response
            return StatusCode(201, AccountRecoveryFileResponse.FromEntity(recoveryAccountFile));
        }


        /// <summary>
        /// updated the request and set the AttendingAt datetime
        /// </summary>
        /// <param name="accountRecoveryUUID"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <response code="20">Request updated</response>
        /// <response code="400">The request is not valid</response>
        /// <response code="401">Auth token is not valid or is not present</response>
        /// <response code="404">The request id was not found on the system</response>
        /// <response code="409">Internal error</response>S
        [HttpPatch("{accountRecoveryUUID}")]
        public async Task<IActionResult> UpdateRequest([FromRoute] string accountRecoveryUUID, [FromBody] AccountRecoveryUpdateRequest request)
        {
            var parseCorrect = Guid.TryParse(accountRecoveryUUID, out Guid requestUUID);
            if(!parseCorrect){
                return BadRequest( new {
                    Title = "El formato del accountRecoveryUUID es incorrecto",
                    Message = "El formato del accountRecoveryUUID es incorrecto, se espera un UUID"
                });
            }

            // * attempt to get the recovery request
            var recoveryRequest = this.recoveryAccountService.GetByID(requestUUID);
            if(recoveryRequest == null)
            {
                return NotFound(new {
                    Title = "No se econtro el registro en la base de datos.",
                    Message = "No se econtro el registro en la base de datos."
                });
            }


            // * retrive the template
            if(!Enum.IsDefined(typeof(RecoveryAccountTemplate), request.TemplateId))
            {
                return BadRequest( new {
                    Title = "El TemplateId seleccionado es incorrecto.",
                    Message = "El TemplateId ingresado no existe."
                });
            }
            RecoveryAccountTemplate recoveryAccountTemplate = (RecoveryAccountTemplate)request.TemplateId;

            try
            {
                // * updated the request
                var authenticatedUser = this.GetCurrentUser();
                recoveryRequest.ResponseComments = request.ResponseComments;
                recoveryRequest.AttendingAt = DateTime.Now;
                recoveryRequest.AttendingBy = authenticatedUser.Id;
                context.AccountRecoveryRequests.Update(recoveryRequest);
                context.SaveChanges();

                // * attemp to send the email
                if(request.NotifyEmail == 1)
                {
                    var personName = string.Join(" ", [recoveryRequest.Name, recoveryRequest.FirstName, recoveryRequest.LastName]);
                    if(recoveryRequest.ContactEmail != null)
                    {
                        var emailResponse = await this.recoveryAccountService.SendEmail(personName, recoveryRequest.ContactEmail.Trim(), request.ResponseComments ?? string.Empty, recoveryAccountTemplate);
                        // * save the email response
                        recoveryRequest.NotificationEmailResponse = emailResponse.Response;
                        recoveryRequest.NotificationEmailContent = emailResponse.Body;
                        context.AccountRecoveryRequests.Update(recoveryRequest);
                        context.SaveChanges();
                    }
                }
                return Ok();
            }
            catch(ArgumentNullException ane)
            {
                if( ane.ParamName == "userId" || ane.ParamName == "user")
                {
                    return Unauthorized(
                        new {
                            Message = $"Cant access to the authenticated user: {ane.Message}"
                        }
                    );
                }

                return Conflict( new
                {
                    Message = ane.Message
                });
            }
            catch (System.Exception err)
            {
                return Conflict(new {
                    Message = err.Message
                });
            }
        }


        [HttpGet("templates")]
        public IActionResult GetTemplates()
        {
            var templates = new List<dynamic>()
            {
                new { Id = 1, Name = "Finished", Label = "Solicitud finalizada" },
                new { Id = 2, Name = "Incompleted", Label = "Solicitud incompleta" },
                new { Id = 3, Name = "NotFound", Label = "Sin Conincidencia" },
                new { Id = 4, Name = "Custom", Label = "Respuesta personalizada" }
            };
            return Ok(templates);
        }


        /// <summary>
        /// Retorna el listado de las peticiones de recuperacion de cuentas de la persona
        /// </summary>
        /// <param name="personId">Id de la persona en formato UUID</param>
        /// <param name="take"></param>
        /// <param name="offset"></param>
        /// <response code="200">return the data</response>
        /// <response code="400">The request is not valid ore some error are present</response>
        /// <response code="404">La persona no se encuentro en el sistema</response>
        [HttpGet]
        [Route("people/{personId}")]
        public ActionResult<PagedResponse<AccountRecoveryResponse>> GetRequestFromPerson([FromRoute] Guid personId, [FromQuery] int take = 5, [FromQuery] int offset = 0)
        {
            // * attempt to get the person
            var person = this.personService.GetPeople().FirstOrDefault(item => item.Id == personId);
            if(person == null)
            {
                return NotFound( new {
                    Title = "La persona no existe",
                    Message = "No se encontro la persona en el sistema"
                });
            }

            // * prepare the query
            var queryRecords = this.context.AccountRecoveryRequests
                .Where(item => item.PersonId == person.Id)
                .Include( p => p.Files!)
                    .ThenInclude( f => f.DocumentType)
                .AsQueryable();
            
            var totalRecords = queryRecords.Count();
            
            // * retrive the data
            var records = queryRecords.OrderByDescending(item => item.CreatedAt)
                .Skip(offset)
                .Take(take)
                .Select(item => AccountRecoveryResponse.FromEntity(item))
                .ToList();

            var response = new PagedResponse<AccountRecoveryResponse>()
            {
                Items = records,
                TotalItems = totalRecords,
                PageSize = take,
                PageNumber = (offset / take) + 1
            };

            // * return the data
            return response;
        }


        #region Private functions
        /// <summary>
        /// returns the current user authenticated
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Can find the userId or the user itself</exception>
        private User GetCurrentUser()
        {
            var userIdValue = (HttpContext.User?.FindFirst("userId")?.Value) ?? throw new ArgumentNullException("userId", "UserId value of the authenticated user not found.");
            return context.Users.FirstOrDefault(item => item.Id == Convert.ToInt32(userIdValue)) ?? throw new ArgumentNullException("user", "User not foun on the system.");
        }
        
        private bool ExistAPendingRequest(AccountRecoveryRequest request)
        {
            AccountRecovery? accountRecovery = null;

            var nationalityMX = context.Nationality.Where(item => EF.Functions.Like(item.Name, "mexicana")).FirstOrDefault();
            if(request.NationalityId == nationalityMX?.Id )
            {
                accountRecovery = context.AccountRecoveryRequests
                    .Where(item => EF.Functions.Like(item.Curp!, request.Curp))
                    .Where(item => item.AttendingAt == null || item.DeletedAt == null)
                    .OrderByDescending( item => item.CreatedAt)
                    .FirstOrDefault();
            }
            else
            {
                accountRecovery = context.AccountRecoveryRequests
                    .Where(item => EF.Functions.Like(item.ContactEmail!, request.ContactEmail))
                    .Where(item => item.AttendingAt == null || item.DeletedAt == null)
                    .OrderByDescending( item => item.CreatedAt)
                    .FirstOrDefault();
            }

            return accountRecovery != null;
        }
        #endregion

    }
}