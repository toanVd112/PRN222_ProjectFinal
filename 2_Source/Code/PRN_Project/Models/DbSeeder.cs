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

            if (!context.EquipmentCategories.Any())
            {
                var categories = new List<EquipmentCategory>
                {
                    new EquipmentCategory { CategoryName = "Máy chiếu", Description = "Máy chiếu treo tường và máy chiếu di động" },
                    new EquipmentCategory { CategoryName = "Điều hòa", Description = "Hệ thống máy lạnh điều hòa nhiệt độ" },
                    new EquipmentCategory { CategoryName = "Máy tính", Description = "Máy tính để bàn phục vụ thực hành" },
                    new EquipmentCategory { CategoryName = "Tivi", Description = "Tivi thông minh truyền hình trình chiếu" },
                    new EquipmentCategory { CategoryName = "Loa & Micro", Description = "Thiết bị âm thanh giảng đường" }
                };
                context.EquipmentCategories.AddRange(categories);
                context.SaveChanges();
            }

            if (!context.Rooms.Any())
            {
                var rooms = new List<Room>
                {
                    new Room { RoomCode = "R101", RoomName = "Phòng Lý thuyết 101", Location = "Tầng 1 - Nhà A", Capacity = 40, RoomType = "Lý thuyết", IsActive = true, CreatedAt = DateTime.Now },
                    new Room { RoomCode = "R102", RoomName = "Phòng Thực hành 102", Location = "Tầng 1 - Nhà A", Capacity = 30, RoomType = "Thực hành", IsActive = true, CreatedAt = DateTime.Now },
                    new Room { RoomCode = "R201", RoomName = "Phòng Lý thuyết 201", Location = "Tầng 2 - Nhà A", Capacity = 45, RoomType = "Lý thuyết", IsActive = true, CreatedAt = DateTime.Now },
                    new Room { RoomCode = "HTA", RoomName = "Hội trường A", Location = "Tầng 1 - Nhà B", Capacity = 150, RoomType = "Hội trường", IsActive = true, CreatedAt = DateTime.Now }
                };
                context.Rooms.AddRange(rooms);
                context.SaveChanges();
            }
        }
    }
}
