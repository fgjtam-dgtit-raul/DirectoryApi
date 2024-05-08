using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AuthApi.Data;
using AuthApi.Data.Exceptions;
using AuthApi.Entities;
using AuthApi.Helper;
using AuthApi.Models;
using Microsoft.Extensions.Options;

namespace AuthApi.Services
{
    public class PreregisterService( DirectoryDBContext dbContext, ICryptographyService cryptographyService, ILogger<PreregisterService> logger, PersonService personService, IEmailProvider emailProvider )
    {
        private readonly DirectoryDBContext dbContext = dbContext;
        private readonly ICryptographyService cryptographyService = cryptographyService;
        private readonly ILogger<PreregisterService> logger = logger;
        private readonly PersonService personService = personService;
        private readonly IEmailProvider emailProvider = emailProvider;
        
        /// <summary>
        ///  Create a new preregister record
        /// </summary>
        /// <param name="request"></param>
        /// <returns> People pre-registration id </returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="SimpleValidationException"></exception>
        public async Task<string?> CreatePreregister(PreregistrationRequest request){

            // * Prevenet email diplicated
            if( dbContext.People.Where( p => p.DeletedAt == null && p != null && p.Email == request.Mail ).Any() ){
                var errors = new List<KeyValuePair<string, string>> {
                    new("email", "El correo ya se encuentra almacenado en la base de datos.")
                };
                throw new SimpleValidationException("Can't stored the pre-register", errors );
            }

            // * Verify if a same preregistration is already stored in the database
            Preregistration? preRegister = dbContext.Preregistrations
                .Where(p => p.Mail!.ToLower() == request.Mail.ToLower())
                .OrderByDescending( p => p.CreatedAt )
                .FirstOrDefault();

            
            if( preRegister != null){
                // * Update the password if are diferent
                if( preRegister.Password != request.Password){
                    preRegister.Password = request.Password;
                    dbContext.Preregistrations.Update( preRegister );
                    dbContext.SaveChanges();
                }
            }
            else{
                // * Create a new pre-register record
                preRegister = new Preregistration(){
                    Mail = request.Mail,
                    Password = request.Password
                };

                // * Insert record into db
                dbContext.Preregistrations.Add( preRegister );
                dbContext.SaveChanges();
            }

            // * Update the token
            var validationToken =  PreregisterToken.GenerateToken(preRegister, cryptographyService );
            preRegister.Token = validationToken;
            preRegister.UpdatedAt = DateTime.Now;
            dbContext.Preregistrations.Update(preRegister);
            dbContext.SaveChanges();


            // * Sende the email with the token
            // TODO: make a thread 
            await Task.Run( async ()=>{
                await SendEmail( preRegister, request.Url );
            });

            // * Return the id generated
            return preRegister.Id.ToString();

        }

        public Person? ValidateRegister( Guid preregisterId, ValidateRegisterRequest request){
            
            // Retrive validation enity
            var preregister = this.dbContext.Preregistrations.Find(preregisterId)
                ?? throw new Exception($"Can't find a Preregistration record with ID:{preregisterId}");
            

            // Generate new person request
            var newPersonRequest = new PersonRequest(){
                Rfc = request.Rfc,
                Curp = request.Curp,
                Name = request.Name,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Birthdate = request.Birthdate,
                GenderId = request.GenderId,
                MaritalStatusId = request.MaritalStatusId,
                NationalityId = request.NationalityId,
                OccupationId = request.OccupationId,
                AppName = request.AppName,
                Email = preregister.Mail,
                Password = preregister.Password,
                ConfirmPassword = preregister.Password,
            };


            // Create person record
            var newPerson = personService.StorePerson(newPersonRequest, preregister.Id) ?? throw new Exception("Can't store the new person, null data was returned");

            // Delete the pre-register record
            try{
                this.dbContext.Preregistrations.Remove( preregister);
                dbContext.SaveChanges();
            }catch(Exception err){
                this.logger.LogError("Can't delete the pre-register record; {message}", err.Message );
            }

            return newPerson;
        }

        public Preregistration? GetPreregistrationByToken(string token){
            return this.dbContext.Preregistrations
             .Where(item => item.Token == token)
             .FirstOrDefault();
        }
        

        private async Task SendEmail( Preregistration preregistration, string? url = null){
            
            // TODO: Make url destination
            var _ref = string.Format("{0}?t={1}",
                url ?? "http://auth.fgjtam.gob.mx/person/validate/email",
                preregistration.Token
            );

            // * Make html body
            var _htmlBody = ValidationEmailTemplate().Replace("{{urlRef}}", _ref);

            // * Send email
            try{
                var emailID = await Task.Run<string>( async ()=>{
                    return await this.emailProvider.SendEmail( preregistration.Mail!, "Validacion de correo", _htmlBody );
                });
                logger.LogInformation( "Email ID:{emailID} sending", emailID);
            }catch(Exception err){
                logger.LogError( err, "Error at attempting to send Email for validation to email {mail}; {message}", preregistration.Mail!, err.Message);
            }
        }

        private static string ValidationEmailTemplate(){
            return "<body><table width='100%' border='0' cellspacing='0' cellpadding='0'><tr><td align='center' style='padding: 20px;'><table class='content' width='600' border='0' cellspacing='0' cellpadding='0' style='border-collapse: collapse;'><tr><td class='body' style='padding: 40px; text-align: left; font-size: 16px; line-height: 1.6;'>Hello, All! <br>Lorem odio soluta quae dolores sapiente voluptatibus recusandae aliquam fugit ipsam.</td></tr><tr><td style='padding: 0px 40px 0px 40px; text-align: center;'><table cellspacing='0' cellpadding='0' style='margin: auto;'><tr><td align='center' style='background-color: #345C72; padding: 10px 20px; border-radius: 5px;'><a href='{{urlRef}}' target='_blank' style='color: #ffffff; text-decoration: none; font-weight: bold;'>Validar Correo</a></td></tr></table></td></tr><tr><td class='body' style='padding: 40px; text-align: left; font-size: 16px; line-height: 1.6;'>Lorem ipsum dolor sit amet consectetur adipisicing elit. Veniam corporis sint eum nemo animi velit exercitationem impedit. </td></tr></table></td></tr></table></body>";
        }

    }
}