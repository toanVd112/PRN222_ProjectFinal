using System;
using System.Collections.Generic;
using System.Linq;
using PRN_Project.Helpers;

namespace PRN_Project.Models
{
    public static class DbSeeder
    {
        public static void Seed(AppDbContext context)
        {
            context.Database.EnsureCreated();

            if (!context.Users.Any())
            {
                var users = new List<User>
                {
                    new User
                    {
                        UserCode = "ADMIN001",
                        FullName = "System Administrator",
                        Email = "admin@cems.com",
                        PasswordHash = PasswordHelper.HashPassword("Admin@123"),
                        Role = "Admin",
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    },
                    new User
                    {
                        UserCode = "TECH001",
                        FullName = "Tech Support",
                        Email = "tech@cems.com",
                        PasswordHash = PasswordHelper.HashPassword("Tech@123"),
                        Role = "Technician",
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    },
                    new User
                    {
                        UserCode = "LEC001",
                        FullName = "Lecturer Nguyen",
                        Email = "lecturer@cems.com",
                        PasswordHash = PasswordHelper.HashPassword("Lecturer@123"),
                        Role = "Lecturer",
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    }
                };

                context.Users.AddRange(users);
                context.SaveChanges();
            }
        }
    }
}
