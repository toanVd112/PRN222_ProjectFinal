using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using PRN_Project.Controllers;
using PRN_Project.Models;
using Xunit;

namespace PRN_Project.Tests
{
    public class RoomControllerTests
    {
        private AppDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
                .Options;

            var context = new AppDbContext(options);

            // Seed data
            context.Rooms.AddRange(
                new Room { RoomId = 1, RoomCode = "AL101", RoomName = "AL101", RoomType = "Phòng học (P)", Location = "Alpha", Capacity = 35, IsActive = true },
                new Room { RoomId = 2, RoomCode = "BE01", RoomName = "BE01", RoomType = "Phòng LAB", Location = "Beta", Capacity = 30, IsActive = true },
                new Room { RoomId = 3, RoomCode = "DE01", RoomName = "DE01", RoomType = "Nhà vệ sinh (WC)", Location = "Delta", Capacity = 10, IsActive = false }
            );
            context.SaveChanges();

            return context;
        }

        private RoomController CreateController(AppDbContext context, string role = "Admin")
        {
            var controller = new RoomController(context);
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Name, "TestUser"),
                new Claim(ClaimTypes.Role, role)
            }, "mock"));

            var httpContext = new DefaultHttpContext { User = user };
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
            return controller;
        }

        [Fact]
        public async Task Index_ReturnsViewResult_WithListOfRooms()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var controller = CreateController(context);

            // Act
            var result = await controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<RoomListViewModel>(viewResult.ViewData.Model);
            Assert.Equal(3, model.Rooms.Count); // Including inactive rooms for Admin
        }

        [Fact]
        public async Task Create_Post_ValidModel_CreatesRoom()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var controller = CreateController(context);
            var model = new CreateRoomViewModel
            {
                RoomCode = "GA101",
                RoomType = "Phòng hội trường",
                Location = "Gamma",
                Capacity = 90
            };

            // Act
            var result = await controller.Create(model);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            
            var roomInDb = context.Rooms.FirstOrDefault(r => r.RoomCode == "GA101");
            Assert.NotNull(roomInDb);
            Assert.Equal("GA101", roomInDb.RoomName);
        }

        [Fact]
        public async Task Create_Post_InvalidPrefix_ReturnsViewWithModelError()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var controller = CreateController(context);
            var model = new CreateRoomViewModel
            {
                RoomCode = "BE101", // Sai tiền tố vì Location là Alpha
                RoomType = "Phòng học (P)",
                Location = "Alpha"
            };

            // Act
            var result = await controller.Create(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
            Assert.True(controller.ModelState.ContainsKey("RoomCode"));
        }

        [Fact]
        public async Task Create_Post_InvalidCapacityForStudyRoom_ReturnsViewWithModelError()
        {
            var context = GetInMemoryDbContext();
            var controller = CreateController(context);
            var model = new CreateRoomViewModel
            {
                RoomCode = "AL202",
                RoomType = "Phòng học (P)",
                Location = "Alpha",
                Capacity = 36 // Vượt quá 35
            };
            
            // Trigger IValidatableObject logic
            var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(model);
            var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
            System.ComponentModel.DataAnnotations.Validator.TryValidateObject(model, validationContext, validationResults, true);
            foreach(var validationResult in validationResults)
            {
                controller.ModelState.AddModelError(validationResult.MemberNames.First(), validationResult.ErrorMessage);
            }

            var result = await controller.Create(model);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
            Assert.True(controller.ModelState.ContainsKey("Capacity"));
        }

        [Fact]
        public async Task Create_Post_InvalidCapacityForAuditorium_ReturnsViewWithModelError()
        {
            var context = GetInMemoryDbContext();
            var controller = CreateController(context);
            var model = new CreateRoomViewModel
            {
                RoomCode = "AL203",
                RoomType = "Phòng hội trường",
                Location = "Alpha",
                Capacity = 101 // Vượt quá 100
            };
            
            var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(model);
            var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
            System.ComponentModel.DataAnnotations.Validator.TryValidateObject(model, validationContext, validationResults, true);
            foreach(var validationResult in validationResults)
            {
                controller.ModelState.AddModelError(validationResult.MemberNames.First(), validationResult.ErrorMessage);
            }

            var result = await controller.Create(model);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
            Assert.True(controller.ModelState.ContainsKey("Capacity"));
        }

        [Fact]
        public async Task Create_Post_DuplicateRoomCode_ReturnsViewWithModelError()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var controller = CreateController(context);
            var model = new CreateRoomViewModel
            {
                RoomCode = "AL101", // Already seeded
                RoomType = "Phòng học (P)",
                Location = "Alpha"
            };

            // Act
            var result = await controller.Create(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
            Assert.True(controller.ModelState.ContainsKey("RoomCode"));
        }

        [Fact]
        public async Task Edit_Get_ValidId_ReturnsViewWithModel()
        {
            var context = GetInMemoryDbContext();
            var controller = CreateController(context);

            var result = await controller.Edit(1);

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<EditRoomViewModel>(viewResult.ViewData.Model);
            Assert.Equal("AL101", model.RoomCode);
        }

        [Fact]
        public async Task Edit_Post_ValidModel_UpdatesRoom()
        {
            var context = GetInMemoryDbContext();
            var controller = CreateController(context);
            var model = new EditRoomViewModel
            {
                RoomId = 1,
                RoomCode = "AL101",
                RoomType = "Phòng học (P)",
                Location = "Alpha",
                Capacity = 35
            };

            var result = await controller.Edit(model);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);

            var roomInDb = context.Rooms.Find(1);
            Assert.Equal("AL101", roomInDb.RoomName);
            Assert.Equal(35, roomInDb.Capacity);
        }

        [Fact]
        public async Task ToggleStatus_RoomHasEquipment_ReturnsError()
        {
            var context = GetInMemoryDbContext();
            // Thêm thiết bị vào phòng 1 để test ràng buộc
            context.Equipments.Add(new Equipment { EquipmentId = 1, AssetCode = "EQ01", EquipmentName = "Máy chiếu", CategoryId = 1, CurrentRoomId = 1, Status = "InUse" });
            context.SaveChanges();
            
            var controller = CreateController(context);

            var result = await controller.ToggleStatus(1);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            
            // Trạng thái vẫn là true
            var roomInDb = context.Rooms.Find(1);
            Assert.True(roomInDb.IsActive);
        }
        
        [Fact]
        public async Task ToggleStatus_RoomHasNoEquipment_TogglesSuccessfully()
        {
            var context = GetInMemoryDbContext();
            var controller = CreateController(context);

            // Phòng 2 đang IsActive = true và không có thiết bị
            var result = await controller.ToggleStatus(2);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            
            var roomInDb = context.Rooms.Find(2);
            Assert.False(roomInDb.IsActive);
        }
    }
}
