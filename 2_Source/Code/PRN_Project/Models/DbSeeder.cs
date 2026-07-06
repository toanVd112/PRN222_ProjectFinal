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

            SeedRoomEquipmentSampleData(context);
        }

        private static void SeedRoomEquipmentSampleData(AppDbContext context)
        {
            var now = DateTime.Now;

            EnsureCategory(context, "Máy chiếu", "Thiết bị trình chiếu trong phòng học", now);
            EnsureCategory(context, "Âm thanh", "Thiết bị âm thanh phục vụ giảng dạy", now);
            EnsureCategory(context, "Máy tính", "Máy tính phục vụ phòng học và thực hành", now);
            context.SaveChanges();

            EnsureRoom(context, "P101", "Phòng học P101", "Tầng 1 - Khu A", 60, "Lý thuyết", true, now);
            EnsureRoom(context, "P202", "Phòng học P202", "Tầng 2 - Khu A", 45, "Lý thuyết", true, now);
            EnsureRoom(context, "LAB301", "Phòng thực hành LAB301", "Tầng 3 - Khu B", 35, "Thực hành", true, now);
            EnsureRoom(context, "P404", "Phòng bảo trì P404", "Tầng 4 - Khu C", 20, "Khác", false, now);
            context.SaveChanges();

            var adminUser = context.Users.FirstOrDefault(user => user.UserCode == "ADMIN001")
                ?? context.Users.OrderBy(user => user.UserId).FirstOrDefault();

            if (adminUser == null)
            {
                return;
            }

            var projectorCategory = context.EquipmentCategories.First(category => category.CategoryName == "Máy chiếu");
            var audioCategory = context.EquipmentCategories.First(category => category.CategoryName == "Âm thanh");
            var computerCategory = context.EquipmentCategories.First(category => category.CategoryName == "Máy tính");

            var roomP101 = context.Rooms.First(room => room.RoomCode == "P101");
            var roomP202 = context.Rooms.First(room => room.RoomCode == "P202");
            var roomLab301 = context.Rooms.First(room => room.RoomCode == "LAB301");

            EnsureEquipment(context, "EQ-P101-PRJ-001", "Máy chiếu Epson EB-X49", projectorCategory.CategoryId, roomP101.RoomId, "EPX49-P101-001", "Epson", "InUse", adminUser.UserId, now);
            EnsureEquipment(context, "EQ-P101-SPK-001", "Loa treo tường TOA BS-1030", audioCategory.CategoryId, roomP101.RoomId, "TOA-P101-001", "TOA", "InUse", adminUser.UserId, now);
            EnsureEquipment(context, "EQ-P101-MIC-001", "Micro không dây Shure BLX24", audioCategory.CategoryId, roomP101.RoomId, "SHURE-P101-001", "Shure", "InUse", adminUser.UserId, now);

            EnsureEquipment(context, "EQ-P202-PRJ-001", "Máy chiếu Sony VPL-EX575", projectorCategory.CategoryId, roomP202.RoomId, "SONY-P202-001", "Sony", "InUse", adminUser.UserId, now);
            EnsureEquipment(context, "EQ-P202-PC-001", "Máy tính giảng viên Dell OptiPlex", computerCategory.CategoryId, roomP202.RoomId, "DELL-P202-001", "Dell", "InUse", adminUser.UserId, now);

            EnsureEquipment(context, "EQ-LAB301-PC-001", "Máy tính thực hành 01", computerCategory.CategoryId, roomLab301.RoomId, "LAB301-PC-001", "HP", "InUse", adminUser.UserId, now);
            EnsureEquipment(context, "EQ-LAB301-PC-002", "Máy tính thực hành 02", computerCategory.CategoryId, roomLab301.RoomId, "LAB301-PC-002", "HP", "InUse", adminUser.UserId, now);
            EnsureEquipment(context, "EQ-LAB301-PRJ-001", "Máy chiếu BenQ MW560", projectorCategory.CategoryId, roomLab301.RoomId, "BENQ-LAB301-001", "BenQ", "InUse", adminUser.UserId, now);

            context.SaveChanges();
        }

        private static void EnsureCategory(AppDbContext context, string name, string description, DateTime createdAt)
        {
            if (context.EquipmentCategories.Any(category => category.CategoryName == name))
            {
                return;
            }

            context.EquipmentCategories.Add(new EquipmentCategory
            {
                CategoryName = name,
                Description = description,
                CreatedAt = createdAt
            });
        }

        private static void EnsureRoom(
            AppDbContext context,
            string roomCode,
            string roomName,
            string location,
            int capacity,
            string roomType,
            bool isActive,
            DateTime createdAt)
        {
            if (context.Rooms.Any(room => room.RoomCode == roomCode))
            {
                return;
            }

            context.Rooms.Add(new Room
            {
                RoomCode = roomCode,
                RoomName = roomName,
                Location = location,
                Capacity = capacity,
                RoomType = roomType,
                IsActive = isActive,
                CreatedAt = createdAt
            });
        }

        private static void EnsureEquipment(
            AppDbContext context,
            string assetCode,
            string equipmentName,
            int categoryId,
            int roomId,
            string serialNumber,
            string manufacturer,
            string status,
            int createdBy,
            DateTime createdAt)
        {
            if (context.Equipments.Any(equipment => equipment.AssetCode == assetCode))
            {
                return;
            }

            context.Equipments.Add(new Equipment
            {
                AssetCode = assetCode,
                EquipmentName = equipmentName,
                CategoryId = categoryId,
                CurrentRoomId = roomId,
                SerialNumber = serialNumber,
                Manufacturer = manufacturer,
                Status = status,
                IsActive = true,
                CreatedBy = createdBy,
                CreatedAt = createdAt
            });
        }
    }
}
