#pragma warning disable CS8602 
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using AuthApi.Data;
using AuthApi.Entities;
using AuthApi.Helper;
using AuthApi.Models;
using AuthApi.Models.Responses;

namespace AuthApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/catalog")]
    public class CatalogController(ILogger<CatalogController> logger, DirectoryDBContext context) : ControllerBase
    {
        private readonly ILogger<CatalogController> _logger = logger;
        private readonly DirectoryDBContext dbContext = context;

        private readonly int mexicoCountryId = 138;

        /// <summary>
        ///  Get ocupations catalog
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("occupations")]
        public ActionResult<IEnumerable<Occupation>> GetOccupations()
        {
            return Ok( dbContext.Occupation.OrderBy( item => item.Name).ToArray());
        }

        /// <summary>
        ///  Get genders catalog
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("genders")]
        public ActionResult<IEnumerable<Gender>> GetGenders()
        {
            return Ok( dbContext.Gender.OrderBy( item => item.Name).ToArray());
        }

        /// <summary>
        ///  Get nationalities catalog
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("nationalities")]
        public ActionResult<IEnumerable<Nationality>> GetNationalities()
        {
            return Ok( dbContext.Nationality.OrderBy( item => item.Name).ToArray());
        }

        /// <summary>
        ///  Get marital status catalog
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("marital-statuses")]
        public ActionResult<IEnumerable<MaritalStatus>> GetMaritalStatuses()
        {
            return Ok( dbContext.MaritalStatus.OrderBy(item => item.Name).ToArray() );
        }

        /// <summary>
        ///  Get contact types catalog
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("contact-types")]
        public ActionResult<IEnumerable<ContactType>> GetContactTypes()
        {
            return Ok( dbContext.ContactTypes.OrderBy(item =>item.Name).ToArray() );
        }

        /// <summary>
        ///  Get countries catalog
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("countries")]
        public ActionResult<IEnumerable<Country>> GetCountries()
        {
            return Ok( dbContext.Countries.OrderBy(item =>item.Name).ToArray() );
        }

        #region Catalogo Estados
        /// <summary>
        ///  Get states catalog
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("states")]
        public ActionResult<IEnumerable<State>> GetStates([FromQuery] int country_id = 138, [FromQuery] string? search = null)
        {
            this._logger.LogCritical("CountryId: {id}, Search:{search}", country_id, search);
            return Ok(dbContext.States
                .Include(c => c.Country)
                .Where(item => country_id == 0 || item.Country!.Id == country_id)
                .Where(item => string.IsNullOrEmpty(search) || EF.Functions.Like(item.Name, $"%{search}%"))
                .OrderBy(item => item.Name)
                .ToList()
            );
        }

        /// <summary>
        ///  Get state info
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("states/{stateId}")]
        public ActionResult<State> GetStateById([FromRoute] int stateId)
        {
            try
            {
                var stateData = dbContext.States
                    .Include( c => c.Country )
                    .FirstOrDefault( item => item.Id == stateId) ?? throw new KeyNotFoundException();
                return Ok(stateData);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new {
                    Message = "El estado no se encuentra registrado en el sistema."
                });
            }
        }

        /// <summary>
        ///  Store a new state
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("states")]
        public ActionResult<IEnumerable<State>> StoreStates(NewStateRequest request)
        {
            if(!ModelState.IsValid)
            {
                return UnprocessableEntity(ModelState);
            }

            var country = dbContext.Countries.FirstOrDefault(item =>item.Id == request.CountryId);
            if(country == null)
            {
                var errors = new Dictionary<string,string>{{"countryId", "The country was not found"}};
                return UnprocessableEntity( new UnprocesableResponse(errors));
            }

            try
            {
                var newState = new State {
                    Country = country,
                    Name = request.Name!.Trim().ToUpper()
                };
                this.dbContext.States.Add( newState);
                this.dbContext.SaveChanges();
                return Ok();
            }
            catch (System.Exception err)
            {
                this._logger.LogError(err, "Fail at store the new state");
                return Conflict( new {
                    err.Message
                });
            }
        }

        /// <summary>
        ///  Actualiza el Estado
        /// </summary>
        /// <returns></returns>
        /// <response code="200">Estado actualizado correctamente</response>
        /// <response code="404">Estado no encontrado</response>
        /// <response code="422">Datos inválidos</response>
        /// <response code="409">Conflicto al actualizar el Estado</response>
        [HttpPatch("states/{stateId:int}")]
        public IActionResult UpdateState([FromRoute] int stateId, UdpateCatalogRequest request)
        {
            if (string.IsNullOrEmpty(request.Name))
            {
                var errors = new Dictionary<string, string> { { "Name", "El campo es requerido" } };
                return UnprocessableEntity(new UnprocesableResponse(errors));
            }

            // * retrieve the municipality
            var state = this.dbContext.States.Find(stateId);
            if (state == null)
            {
                return NotFound(new
                {
                    Title = $"El Estado no se encontro",
                    Message = $"El Estado con id '{stateId}' no se encuentra en el sistema.",
                });
            }

            try
            {
                // * update the entity
                state.Name = request.Name!.ToUpper().Trim();
                this.dbContext.States.Update(state);
                this.dbContext.SaveChanges();

                return Ok(new
                {
                    Title = "Estado actualizado",
                    Message = "El Estado se actualizó correctamente."
                });
            }
            catch (Exception err)
            {
                this._logger.LogError(err, "Error al actualizar el estado '{id}': {message}", stateId, err.Message);
                return Conflict(new
                {
                    Title = "Error al actualizar el Estado",
                    Message = "Ocurrió un error al intentar actualizar el estado. Por favor, intente nuevamente más tarde."
                });
            }
        }
        #endregion

        #region Catalogo Municipios
        /// <summary>
        ///  Get municipalities catalog
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("municipalities")]
        public ActionResult<IEnumerable<Municipality>> GetMunicipalities([FromQuery] int state_id = 28, [FromQuery] string? search = null)
        {
            return Ok(dbContext.Municipalities
                .Include(c => c.State)
                    .ThenInclude(s => s.Country)
                .Where(c => state_id == 0 || c.State!.Id == state_id)
                .Where(c => string.IsNullOrEmpty(search) || EF.Functions.Like(c.Name, $"%{search}%"))
                .OrderBy(item => item.Name)
                .ToArray()
            );
        }

        /// <summary>
        ///  Store a new municipality
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("municipalities")]
        public ActionResult<IEnumerable<Municipality>> StoreMunicipality(NewMunicipalityRequest request)
        {
            if(!ModelState.IsValid)
            {
                return UnprocessableEntity(ModelState);
            }

            var country = dbContext.Countries.FirstOrDefault(item =>item.Id == request.CountryId);
            if(country == null)
            {
                var errors = new Dictionary<string,string>{{"countryId", "The country was not found"}};
                return UnprocessableEntity( new UnprocesableResponse(errors));
            }

            var state = dbContext.States.FirstOrDefault(item =>item.Id == request.StateId);
            if(state == null)
            {
                var errors = new Dictionary<string,string>{{"StateId", "The state was not found"}};
                return UnprocessableEntity( new UnprocesableResponse(errors));
            }

            try
            {
                var newModel = new Municipality {
                    State = state,
                    Name = request.Name!.Trim().ToUpper()
                };
                this.dbContext.Municipalities.Add(newModel);
                this.dbContext.SaveChanges();
                return Ok();
            }
            catch (System.Exception err)
            {
                this._logger.LogError(err, "Fail at store the new municipality");
                return Conflict( new {
                    err.Message
                });
            }
        }
        
        /// <summary>
        ///  Get municipality info
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("municipalities/{municipalityId}")]
        public ActionResult<Municipality> GetMunicipalityById([FromRoute] int municipalityId)
        {
            try
            {
                var municipality = dbContext.Municipalities
                    .Include( c => c.State )
                    .FirstOrDefault( m => m.Id == municipalityId) ?? throw new KeyNotFoundException();
                return Ok(municipality);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new {
                    Message = "El municipio no se encuentra registrado en el sistema."
                });
            }
        }

        /// <summary>
        ///  Actualiza el municipio
        /// </summary>
        /// <returns></returns>
        /// <response code="200">Municipio actualizado correctamente</response>
        /// <response code="404">Municipio no encontrado</response>
        /// <response code="422">Datos inválidos</response>
        /// <response code="409">Conflicto al actualizar el Municipio</response>
        [HttpPatch("municipalities/{municipalityId:int}")]
        public IActionResult UpdateMunicipality([FromRoute] int municipalityId, UdpateCatalogRequest request)
        {
            if (string.IsNullOrEmpty(request.Name))
            {
                var errors = new Dictionary<string, string> { { "Name", "El campo es requerido" } };
                return UnprocessableEntity(new UnprocesableResponse(errors));
            }

            // * retrieve the municipality
            var municipality = this.dbContext.Municipalities.Find(municipalityId);
            if (municipality == null)
            {
                return NotFound(new
                {
                    Title = $"El municipio no se encontro",
                    Message = $"El municipio con id '{municipalityId}' no se encuentra en el sistema.",
                });
            }

            try
            {
                // * update the entity
                municipality.Name = request.Name!.ToUpper().Trim();
                this.dbContext.Municipalities.Update(municipality);
                this.dbContext.SaveChanges();

                return Ok(new
                {
                    Title = "Municipio actualizado",
                    Message = "El municipio se actualizó correctamente."
                });
            }
            catch (Exception err)
            {
                this._logger.LogError(err, "Error al actualizar el municipio '{id}': {message}", municipalityId, err.Message);
                return Conflict(new
                {
                    Title = "Error al actualizar el Municipio",
                    Message = "Ocurrió un error al intentar actualizar el municipio. Por favor, intente nuevamente más tarde."
                });
            }
        }
        #endregion

        #region Catalogo Colonias
        /// <summary>
        ///  Get colonies catalog
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("colonies")]
        public ActionResult<IEnumerable<Colony>> GetColonies([FromQuery] int municipality_id = 41, [FromQuery] string? search = null)
        {
            return Ok(dbContext.Colonies
                .Include(c => c.Municipality)
                    .ThenInclude(m => m.State)
                        .ThenInclude(s => s.Country)
                .Where(item => municipality_id == 0 || item.Municipality!.Id == municipality_id)
                .Where(item => string.IsNullOrEmpty(search) || EF.Functions.Like(item.Name, $"%{search}%") || EF.Functions.Like(item.ZipCode, $"{search}%"))
                .OrderBy(item =>item.Name)
                .ToArray()
            );
        }

        /// <summary>
        ///  Store a new colony
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("colonies")]
        public ActionResult<IEnumerable<Colony>> StoreColony(NewColonyRequest request)
        {
            if(!ModelState.IsValid)
            {
                return UnprocessableEntity(ModelState);
            }

            var country = dbContext.Countries.FirstOrDefault(item =>item.Id == request.CountryId);
            if(country == null)
            {
                var errors = new Dictionary<string,string>{{"countryId", "The country was not found"}};
                return UnprocessableEntity( new UnprocesableResponse(errors));
            }

            var state = dbContext.States.FirstOrDefault(item =>item.Id == request.StateId);
            if(state == null)
            {
                var errors = new Dictionary<string,string>{{"StateId", "The state was not found"}};
                return UnprocessableEntity( new UnprocesableResponse(errors));
            }

            var municipality = dbContext.Municipalities.FirstOrDefault(item =>item.Id == request.MunicipalityId);
            if(municipality == null)
            {
                var errors = new Dictionary<string,string>{{"MunicipalityId", "The municipality was not found"}};
                return UnprocessableEntity( new UnprocesableResponse(errors));
            }

            try
            {
                var newModel = new Colony {
                    Municipality = municipality,
                    Name = request.Name!.Trim().ToUpper(),
                    ZipCode = request.ZipCode ?? "0"
                };
                this.dbContext.Colonies.Add(newModel);
                this.dbContext.SaveChanges();
                return Ok();
            }
            catch (System.Exception err)
            {
                this._logger.LogError(err, "Fail at store the new colony");
                return Conflict( new {
                    err.Message
                });
            }
        }

        /// <summary>
        ///  Actualiza la colonia
        /// </summary>
        /// <returns></returns>
        /// <response code="200">Colonia actualizada correctamente</response>
        /// <response code="404">Colonia no encontrada</response>
        /// <response code="422">Datos inválidos</response>
        /// <response code="409">Conflicto al actualizar la colonia</response>
        [HttpPatch("colonies/{colonyId:int}")]
        public IActionResult UpdateColony([FromRoute] int colonyId, UdpateCatalogRequest request)
        {
            if (string.IsNullOrEmpty(request.Name))
            {
                var errors = new Dictionary<string,string>{{"Name", "El campo es requerido"}};
                return UnprocessableEntity( new UnprocesableResponse(errors));
            }

            // * retrive the colony
            var colony = this.dbContext.Colonies.Find(colonyId);
            if (colony == null)
            {
                return NotFound( new {
                    Title = $"Colonia no encontrada",
                    Message = $"La colonia con id '{colonyId}' no se encuentra en el sistema.",
                });
            }

            try
            {
                // * update the entity
                colony.Name = request.Name!.ToUpper().Trim();
                if (!string.IsNullOrEmpty(request.ZipCode))
                {
                    colony.ZipCode = request.ZipCode!.ToUpper().Trim();
                }
                this.dbContext.Colonies.Update(colony);
                this.dbContext.SaveChanges();

                return Ok(new
                {
                    Title = "Colonia actualizada",
                    Message = "La colonia se actualizó correctamente."
                });
            }
            catch (Exception err)
            {
                this._logger.LogError(err, "Error al actualizar la colonia '{id}': {message}", colonyId, err.Message);
                return Conflict(new
                {
                    Title = "Error al actualizar la colonia",
                    Message = "Ocurrió un error al intentar actualizar la colonia. Por favor, intente nuevamente más tarde."
                });
            }
        }

        /// <summary>
        ///  Get colony info
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("colonies/{colonyId}")]
        public ActionResult<Colony> GetColoniesById([FromRoute] int colonyId)
        {
            try
            {
                var colony = dbContext.Colonies
                    .Include( c => c.Municipality )
                    .FirstOrDefault( m => m.Id == colonyId) ?? throw new KeyNotFoundException();
                return Ok(colony);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new {
                    Message = "La colonia no se encuentra registrado en el sistema."
                });
            }
        }
        #endregion

        [HttpGet("/api/zipcode/search")]
        public ActionResult<Object> SearchZipCode([FromQuery] string zipcode)
        {
            // * search the data on the db
            var resultsList = new List<ZipcodeSearchResult>();
            try
            {
                using( var connection = dbContext.Database.GetDbConnection())
                {
                    connection.Open();

                    using var command = connection.CreateCommand();
                    command.CommandText = @"
                        Select top 100
                            c.zipCode,
                            cc.id as countryId,
                            cc.[name] as countryName,
                            s.id as stateId,
                            s.[name] as [state],
                            m.id as [municipalityId],
                            m.[name] as municipality,
                            c.id as colonyId,
                            c.[name] as colonyName
                        From [dbo].[Colonies] c
                        Inner Join [dbo].[Municipalities] m on c.municipalityId = m.id
                        Inner Join [dbo].[States] s on s.id = m.stateId
                        Inner Join [dbo].[Countries] cc on cc.id = s.countryId
                        Where zipCode like @zipcodeSearch
                        Order by cc.[name], s.[name], m.[name], c.[name]";
                    command.CommandType = System.Data.CommandType.Text;
                    var param = command.CreateParameter();
                    param.ParameterName = "@zipcodeSearch";
                    param.Value = "%" + zipcode + "%";
                    param.DbType = System.Data.DbType.String;
                    command.Parameters.Add(param);

                    using(var reader = command.ExecuteReader())
                    {
                        while(reader.Read())
                        {
                            resultsList.Add(ZipcodeSearchResult.FromDataReader(reader));
                        }
                    }
                    connection.Close();
                }
            }
            catch(Exception err)
            {
                this._logger.LogError(err, "Fail at search the zipcode: {message}", err.Message);
                return Conflict( new {
                    err.Message
                });
            }

            if(!resultsList.Any())
            {
                return NotFound( new {
                    Title = $"Zip code {zipcode} not found",
                    Message = $"Colonies with zip code {zipcode} not found",
                });
            }

            // * group the data
            var groupedData = resultsList
            // .GroupBy(c => new { c.ZipCode, c.CountryId, c.CountryName, c.StateId, c.State })
            .GroupBy(c => new { c.CountryId, c.CountryName })
            .Select(group => new
            {
                CountryId = group.Key.CountryId,
                CountryName = group.Key.CountryName,
                States = group.GroupBy( d => new {d.StateId, d.State})
                    .Select(groupd => new {
                        StateId = groupd.Key.StateId,
                        StateName = groupd.Key.State,
                        Data = groupd.ToArray()
                    }).ToArray()
            })
            .ToList();

            // * return the data
            return new {
                Zipcode = zipcode,
                results = groupedData
            };
        }

        /// <summary>
        ///  Return the colonies by zipcode
        /// </summary>
        /// <param name="zipcode"></param>
        /// <param name="country_id"></param>
        /// <returns></returns>
        /// <response code="200">Return the colonies and citys</response>
        /// <response code="404">The zipcode was not found</response>
        /// <response code="406">The country selected has no data</response>
        /// <response code="401">Auth token is not valid or is not present</response>
        [HttpGet]
        [Route("/api/zipcode/{zipcode}")]
        public ActionResult<Object> GetColoniesByZipCode(string zipcode, [FromQuery] string? country_id)
        {

            if(!dbContext.Colonies.Where( item => item.ZipCode == zipcode).Any())
            {
                return NotFound( new {
                    Title = $"Zip code {zipcode} not found",
                    Message = $"Colonies with zip code {zipcode} not found",
                });
            }

            try
            {
                // * get the country
                int countryId = int.TryParse(country_id, out int _countryId) ? _countryId : mexicoCountryId;
                var currentCountry = dbContext.Countries.FirstOrDefault(item => item.Id == countryId);
                if(currentCountry == null)
                {
                    return NotFound( new {
                        Title = $"Coutnry '{country_id}' not found",
                        Message = $"The country id '{country_id}' is not found",
                    });
                }
                
                int[] statesId = dbContext.States
                    .Where(item => item.Country.Id == currentCountry.Id)
                    .Select(s => s.Id)
                    .ToArray();

                int[] municipalitiesId = dbContext.Municipalities
                    .Where(item => statesId.Contains(item.State.Id))
                    .Select( mun => mun.Id)
                    .ToArray();
                
                // * Validate if the country has data
                if( !statesId.Any() || !municipalitiesId.Any() )
                {
                    return StatusCode(406, new {
                        Message = "The Country selected has not states or municipalities"
                    });
                }

                // * get the colonies catalog
                var colonies = dbContext.Colonies
                    .Where( c => c.ZipCode == zipcode && municipalitiesId.Contains(c.Municipality.Id) )
                    .OrderBy( item => item.Name)
                    .Select( c => new {
                        c.Id,
                        c.Name,
                        c.ZipCode})
                    .ToArray();

                // * get the defatul colony
                var defaultColonyId = colonies.First().Id;
                var defaultColony = dbContext.Colonies
                    .Include(c => c.Municipality)
                        .ThenInclude(m => m.State)
                    .FirstOrDefault(c => c.Id == defaultColonyId && c.Municipality != null);

                var defaultMunicipalityId = defaultColony.Municipality?.Id ?? 0;
                var defaultStateId = defaultColony.Municipality?.State?.Id ?? 0;

                // * get the municipalities catalog
                var municipalities = dbContext.Municipalities
                    .Where(m => m.State.Id == defaultStateId)
                    .Select( m => new {
                        m.Id,
                        m.Name,
                        Default = m.Id == defaultMunicipalityId
                    })
                    .OrderBy( item => item.Name)
                    .ToArray();

                // * get the states catalog
                var states = dbContext.States
                    .Where( s => s.Country.Id == countryId)
                    .Select( s => new {
                        s.Id,
                        s.Name,
                        Default = s.Id == defaultStateId
                    })
                    .OrderBy( item => item.Name)
                    .ToArray();

                // * return the data
                return new {
                    Zipcode = zipcode,
                    colonies,
                    municipalities,
                    states,
                    Country = new {
                        currentCountry.Id,
                        currentCountry.Name,
                        currentCountry.ISO
                    }
                };
            }
            catch(Exception err)
            {
                this._logger.LogError(err, "Fail at get the colonies by zipcode: {message}", err.Message);
                return Conflict( new {
                    err.Message
                });
            }
        }


        /// <summary>
        /// Get a catalog of document types
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("document-types")]
        public ActionResult<IEnumerable<DocumentType>> GetDocumentType([FromQuery] int municipality_id = 41 )
        {
            return Ok(dbContext.DocumentTypes
                .Where(item => item.DeletedAt == null)
                .OrderBy(item =>item.Name)
                .ToArray()
            );
        }

        /// <summary>
        /// Store a new document type
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("document-types")]
        public ActionResult<IEnumerable<DocumentType>> StoreDocumentType(NewDocumentTypeRequest request)
        {
            if(!ModelState.IsValid)
            {
                return UnprocessableEntity(ModelState);
            }

            // * check if the name is not taken
            var _model = this.dbContext.DocumentTypes.Where(item => item.DeletedAt == null).FirstOrDefault(item => EF.Functions.Like(item.Name, request.Name));
            if(_model != null)
            {
                return UnprocessableEntity(
                    new UnprocesableResponse("El tipo de documento ya existe", new Dictionary<string,string>{
                        {"name", "El nombre del tipo de documento ya existe."}
                    })
                );
            }

            // * register the new model
            try
            {
                var newModel = new DocumentType
                {
                    Name = request.Name!
                };
                this.dbContext.DocumentTypes.Add(newModel);
                this.dbContext.SaveChanges();
                return StatusCode(201, newModel);
            }
            catch (System.Exception err)
            {
                this._logger.LogError(err, "Fail at store the new document type");
                return Conflict( new {
                    err.Message
                });
            }
        }

        /// <summary>
        /// Delete a document type
        /// </summary>
        /// <param name="documentTypeId"></param>
        /// <returns></returns>
        [HttpDelete("document-types/{documentTypeId}")]
        public ActionResult<IEnumerable<DocumentType>> DeleteDocumentType([FromRoute] int documentTypeId)
        {
            // * check if the name is not taken
            var _model = this.dbContext.DocumentTypes.Where(item => item.DeletedAt == null).FirstOrDefault(item => item.Id == documentTypeId);
            if(_model != null)
            {

                // * check if the document type has records linked
                var total = 0;
                total += this.dbContext.PersonFiles.Where(item => item.DeletedAt == null && item.DocumentTypeId == _model.Id).Count();
                total += this.dbContext.AccountRecoveryFiles.Where(item => item.DeletedAt == null && item.DocumentType.Id == _model.Id).Count();
                if(total > 0)
                {
                    return BadRequest( new {
                        Title = "No se puede eliminar el tipo de documento",
                        Errors = new Dictionary<string,string>{
                            {"documentType", "El tipo de documento no se puede eliminar, continene registros ligados."}
                        }
                    });
                }

                _model.DeletedAt = DateTime.Now;
                this.dbContext.DocumentTypes.Update(_model);
                this.dbContext.SaveChanges();
                this._logger.LogInformation("Document type '{id}|{name}' was removed", documentTypeId, _model.Name);
                return Ok();
            }

            return Ok();
        }

        /// <summary>
        /// Update a new document type
        /// </summary>
        /// <param name="request"></param>
        /// <param name="documentTypeId"></param>
        /// <returns></returns>
        [HttpPatch("document-types/{documentTypeId}")]
        public ActionResult<IEnumerable<DocumentType>> UpdateDocumentType([FromRoute] int documentTypeId, [FromBody] NewDocumentTypeRequest request)
        {
            if(!ModelState.IsValid)
            {
                return UnprocessableEntity(ModelState);
            }

            // * check if the name is not taken
            var _model = this.dbContext.DocumentTypes.Where(item => item.DeletedAt == null).FirstOrDefault(item => item.Id == documentTypeId);
            if(_model == null)
            {
                return NotFound();
            }


            // * check if the name is not taken
            var _model2 = this.dbContext.DocumentTypes.Where(item => item.DeletedAt == null).FirstOrDefault(item => EF.Functions.Like(item.Name, request.Name) && item.Id != _model.Id);
            if(_model2 != null)
            {
                return UnprocessableEntity(
                    new UnprocesableResponse("El tipo de documento ya existe", new Dictionary<string,string>{
                        {"name", "El nombre del tipo de documento ya existe."}
                    })
                );
            }

            // Update the model
            _model.Name = request.Name.Trim();
            this.dbContext.DocumentTypes.Update(_model);
            this.dbContext.SaveChanges();
            this._logger.LogInformation("The document type '{id}|{name}' was updated", _model.Id, _model.Name);
            return Ok();
        }

    }
}
#pragma warning restore CS8602