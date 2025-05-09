using System;
using Microsoft.EntityFrameworkCore;
using AuthApi.Entities;
using AuthApi.Data.Utils;

namespace AuthApi.Data
{
    
    public class DirectoryDBContext : DbContext {
        public DbSet<Preregistration> Preregistrations { get; set; } = default!;
        public DbSet<Person> People { get; set; } = default!;
        public DbSet<Gender> Gender { get; set; } = default!;
        public DbSet<MaritalStatus> MaritalStatus { get; set; } = default!;
        public DbSet<Nationality> Nationality { get; set; } = default!;
        public DbSet<Occupation> Occupation { get; set; } = default!;
        public DbSet<ContactType> ContactTypes { get; set; } = default!;
        public DbSet<Colony> Colonies { get; set; } = default!;
        public DbSet<Municipality> Municipalities { get; set; } = default!;
        public DbSet<State> States { get; set; } = default!;
        public DbSet<Country> Countries { get; set; } = default!;
        public DbSet<Address> Addresses { get; set; } = default!;
        public DbSet<ContactInformation> ContactInformations { get; set; } = default!;
        public DbSet<User> Users {get;set;} = default!;
        public DbSet<Session> Sessions {get;set;} = default!;
        public DbSet<Area> Area {get;set;} = default!;
        public DbSet<ProceedingStatus> ProceedingStatus {get;set;} = default!;
        public DbSet<Proceeding> Proceeding {get;set;} = default!;
        public DbSet<ProceedingFile> ProceedingFiles {get;set;} = default!;
        public DbSet<DocumentType> DocumentTypes {get;set;} = default!;
        public DbSet<AccountRecoveryFile> AccountRecoveryFiles {get;set;} = default!;
        public DbSet<AccountRecovery> AccountRecoveryRequests {get;set;} = default!;
        public DbSet<Role> Roles {get;set;} = default!;
        public DbSet<UserRole> UserRoles {get;set;} = default!;
        public DbSet<UserClaim> UserClaims {get;set;} = default!;
        public DbSet<PersonFile> PersonFiles {get;set;} = default!;
        public DbSet<PersonBanHistory> PersonBanHistories {get;set;} = default!;

        private readonly ICryptographyService cryptographyService;

        public DirectoryDBContext(DbContextOptions options, ICryptographyService cryptographyService ) : base(options)
        {
            this.cryptographyService = cryptographyService;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // * Convert all columns in comel case
            foreach( var entity in modelBuilder.Model.GetEntityTypes() )
            {
                foreach( var property in entity.GetProperties() )
                {
                    var _propertyName = property.Name;
                    property.SetColumnName(  Char.ToLowerInvariant(_propertyName[0]) + _propertyName.Substring(1) );
                }
            }

            // * Person entity
            var personEntity = modelBuilder.Entity<Person>();
            personEntity.Property( p => p.Curp).HasConversion(
                v => cryptographyService.EncryptData(v??""),
                v => cryptographyService.DecryptData(v)
            );
            personEntity.Property( p => p.Rfc).HasConversion(
                v => cryptographyService.EncryptData(v??""),
                v => cryptographyService.DecryptData(v)
            );
            personEntity.Property( p => p.Name).HasConversion(
                v => cryptographyService.EncryptData(v??""),
                v => cryptographyService.DecryptData(v)
            );
            personEntity.Property( p => p.FirstName).HasConversion(
                v => cryptographyService.EncryptData(v??""),
                v => cryptographyService.DecryptData(v)
            );
            personEntity.Property( p => p.LastName).HasConversion(
                v => cryptographyService.EncryptData(v??""),
                v => cryptographyService.DecryptData(v)
            );
            personEntity.Property( p => p.CreatedAt)
                .HasDefaultValueSql("getDate()")
                .HasColumnType("datetime");
            personEntity.Property( p => p.UpdatedAt)
                .HasDefaultValueSql("getDate()")
                .HasColumnType("datetime")
                .ValueGeneratedOnAddOrUpdate();
                
            // * Address entity
            var addressEntity = modelBuilder.Entity<Address>();
            addressEntity.Property( b => b.CreatedAt)
                .HasDefaultValueSql("getDate()")
                .HasColumnType("datetime");
            addressEntity.Property( b => b.UpdatedAt)
                .HasDefaultValueSql("getDate()")
                .HasColumnType("datetime")
                .ValueGeneratedOnAddOrUpdate();
            addressEntity.Navigation(n => n.Country).AutoInclude();
            addressEntity.Navigation(n => n.State).AutoInclude();
            addressEntity.Navigation(n => n.Municipality).AutoInclude();
            addressEntity.Navigation(n => n.Colony).AutoInclude();

            // * Contact information entity
            var contactInformation = modelBuilder.Entity<ContactInformation>();
            contactInformation.Property( p => p.Value).HasConversion(
                v => cryptographyService.EncryptData(v??""),
                v => cryptographyService.DecryptData(v)
            );
            contactInformation.Property( b => b.CreatedAt)
                .HasDefaultValueSql("getDate()")
                .HasColumnType("datetime");
            contactInformation.Property( b => b.UpdatedAt)
                .HasDefaultValueSql("getDate()")
                .HasColumnType("datetime")
                .ValueGeneratedOnAddOrUpdate();
            contactInformation.Navigation( n => n.ContactType ).AutoInclude();

            // * Pre-Registration entity
            var preRegister = modelBuilder.Entity<Preregistration>();
            preRegister.Property( p => p.Password).HasConversion(
                v => cryptographyService.EncryptData(v??""),
                v => cryptographyService.DecryptData(v)
            );

            // * Session entity
            var sessionEntity = modelBuilder.Entity<Session>();
            sessionEntity.Property( b => b.BegginAt)
                .HasDefaultValueSql("getDate()")
                .ValueGeneratedOnAdd();
            sessionEntity.Navigation(n => n.Person).AutoInclude();

            // * Procedding Entity
            var proccedingEntity = modelBuilder.Entity<Proceeding>();
            proccedingEntity.Property( b => b.CreatedAt)
                .HasDefaultValueSql("getDate()")
                .HasColumnType("datetime");
            proccedingEntity.Property( b => b.UpdatedAt)
                .HasDefaultValueSql("getDate()")
                .HasColumnType("datetime")
                .ValueGeneratedOnAddOrUpdate();
            proccedingEntity.HasMany(p => p.Files)
                .WithOne( f => f.Proceeding)
                .HasForeignKey(f => f.ProceedingId);

            // * Procedding File Entity
            var proceddingFileEntity = modelBuilder.Entity<ProceedingFile>();
            proceddingFileEntity.Property( b => b.CreatedAt)
                .HasDefaultValueSql("getDate()")
                .HasColumnType("datetime");
            proceddingFileEntity.Property( b => b.UpdatedAt)
                .HasDefaultValueSql("getDate()")
                .HasColumnType("datetime")
                .ValueGeneratedOnAddOrUpdate();
            
            // * Roles Entity
            var roleEntity = modelBuilder.Entity<Role>( entity => {
                entity.HasKey( r => r.Id);
                entity.HasData(
                    new Role{ Id = 1, Name = "Admin", Description = "Has access to all system features." },
                    new Role{ Id = 2, Name = "User", Description = "Can view the people but not modify it" },
                    new Role{ Id = 3, Name = "Manager", Description = "Can view and modify it the people." }
                );
            });


            // * User Roles Entity
            var userRoleEntity = modelBuilder.Entity<UserRole>( entity => {
                entity.HasOne(ur => ur.User)
                    .WithMany(u => u.UserRoles)
                    .HasForeignKey(ur => ur.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(ur => ur.Role)
                    .WithMany(r => r.UserRoles)
                    .HasForeignKey(ur => ur.RoleId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property( ur => ur.Key).IsRequired();
            });


            // * User Claim Entity
            var userClaimEnity = modelBuilder.Entity<UserClaim>( entity => {
                entity.HasKey( uc => uc.Id);

                entity.HasOne(uc => uc.User)
                    .WithMany(u => u.UserClaims)
                    .HasForeignKey(uc => uc.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property( uc => uc.Id).IsRequired();
            });

            // * Document Types
            var documentTypeEntity = modelBuilder.Entity<DocumentType>();
            documentTypeEntity.Property(b => b.CreatedAt)
                .HasDefaultValueSql("getDate()")
                .HasColumnType("datetime2");
            documentTypeEntity.Property(b => b.UpdatedAt)
                .HasDefaultValueSql("getDate()")
                .HasColumnType("datetime2");
            documentTypeEntity.HasData(
                new DocumentType {Id = 1, Name = "INE" },
                new DocumentType {Id = 2, Name = "CURP" },
                new DocumentType {Id = 3, Name = "Acta de nacimiento" },
                new DocumentType {Id = 4, Name = "Pasaporte" }
            );

            // * Account Recovery Files
            var accountRecoveryFile = modelBuilder.Entity<AccountRecoveryFile>();
            accountRecoveryFile.Property(b => b.CreatedAt)
                .HasDefaultValueSql("getDate()")
                .HasColumnType("datetime2");

            // * Account Recovery Requests
            var accountRecovery = modelBuilder.Entity<AccountRecovery>();
            accountRecovery.Property(b => b.CreatedAt)
                .HasDefaultValueSql("getDate()")
                .HasColumnType("datetime2");
            accountRecovery.HasOne(ar => ar.UserAttended)
                .WithMany()
                .HasForeignKey( ar => ar.AttendingBy)
                .OnDelete(DeleteBehavior.Restrict);
            accountRecovery.HasOne(ar => ar.UserDeleted)
                .WithMany()
                .HasForeignKey( ar => ar.DeletedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // * Seed DB
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    FirstName = "System",
                    LastName = "",
                    Email = "system@email.com",
                    Password = cryptographyService.HashData("system")
                }
            );

            modelBuilder.Entity<PersonFile>(entity => {
                entity.HasOne(pf => pf.Person)
                    .WithMany()
                    .HasForeignKey(pf => pf.PersonId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(pf => pf.DocumentType)
                    .WithMany()
                    .HasForeignKey(pf => pf.DocumentTypeId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                entity.Property(pf => pf.CreatedAt)
                    .HasDefaultValueSql("getDate()")
                    .HasColumnType("datetime2");
                
                entity.Property(pf => pf.Validation)
                    .HasColumnType("date");
            });

            modelBuilder.Entity<PersonBanHistory>(entity => {
                entity.HasOne(pf => pf.Person)
                    .WithMany()
                    .HasForeignKey(pf => pf.PersonId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(pf => pf.CreatedAt)
                    .HasDefaultValueSql("getDate()")
                    .HasColumnType("datetime2");
            });

            base.OnModelCreating(modelBuilder);
        }

    }
}