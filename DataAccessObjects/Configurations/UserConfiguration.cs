using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BusinessObjects.Entities;

namespace DataAccessObjects.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.HasKey(u => u.Id);
            builder.Property(u => u.FullName).IsRequired().HasMaxLength(100);
            builder.Property(u => u.Email).IsRequired().HasMaxLength(150);
            builder.Property(u => u.PasswordHash).IsRequired().HasMaxLength(255);

            // Unique email index
            builder.HasIndex(u => u.Email).IsUnique();

            // Set Relationship
            builder.HasOne(u => u.Role)
                   .WithMany(r => r.Users)
                   .HasForeignKey(u => u.RoleId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Seed Users with hashed passwords
            builder.HasData(
                new User 
                { 
                    Id = 1, 
                    FullName = "Admin User", 
                    Email = "admin@hotel.com", 
                    PasswordHash = "$2a$11$/LMRtOKzu0S3y3Wy61vsPeGaBR.YvvmkgijRmvbobvp2RNSlrkD3e", 
                    IsActive = true, 
                    RoleId = 1 
                },
                new User 
                { 
                    Id = 2, 
                    FullName = "Manager User", 
                    Email = "manager@hotel.com", 
                    PasswordHash = "$2a$11$T0pLWQ97vlvtz6TZar.kpeCGQeYKu3ojVW/99TcqzI5n3FWPEbPma", 
                    IsActive = true, 
                    RoleId = 2 
                },
                new User 
                { 
                    Id = 3, 
                    FullName = "Receptionist User", 
                    Email = "receptionist@hotel.com", 
                    PasswordHash = "$2a$11$1J39Dq0KNZJB5wIWzBxdAOOZx8hsLsvUFsdlq3f4sCTAMvdJNw7l6", 
                    IsActive = true, 
                    RoleId = 3 
                },
                new User 
                { 
                    Id = 4, 
                    FullName = "Service Staff", 
                    Email = "service@hotel.com", 
                    PasswordHash = "$2a$11$A4f9RZSr6u.ePghf680l8eU5FJos.cO0eqaadtofI4wY4pu5D/Gb.", 
                    IsActive = true, 
                    RoleId = 4 
                }
            );
        }
    }
}
