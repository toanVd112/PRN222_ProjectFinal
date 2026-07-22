using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using PRN_Project.Controllers;
using PRN_Project.Models;
using PRN_Project.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace PRN_Project.Tests
{
    public class UserControllerTests
    {
        private AppDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
                .Options;

            var context = new AppDbContext(options);

            // Seed mock data
            context.Users.AddRange(new List<User>
            {
                new User { UserId = 1, UserCode = "ADMIN01", FullName = "Admin User", Role = "Admin", Email = "a@cems.com", IsActive = true, PasswordHash = "dummy" },
                new User { UserId = 2, UserCode = "TECH01", FullName = "Tech User 1", Role = "Technician", Email = "t1@cems.com", IsActive = true, PasswordHash = "dummy" },
                new User { UserId = 3, UserCode = "TECH02", FullName = "Tech User 2", Role = "Technician", Email = "t2@cems.com", IsActive = false, PasswordHash = "dummy" },
                new User { UserId = 4, UserCode = "LEC01", FullName = "Lecturer User", Role = "Lecturer", Email = "l1@cems.com", IsActive = true, PasswordHash = "dummy" }
            });
            context.SaveChanges();

            return context;
        }

        [Fact]
        public async Task Index_ReturnsViewResult_WithListOfUsers()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var mockEmailService = new Mock<IEmailService>();
            var controller = new UserController(context, mockEmailService.Object);

            // Act
            var result = await controller.Index(null, null, null, 1);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<UserListViewModel>(viewResult.Model);
            Assert.Equal(4, model.Users.Count()); // Total seeded users
            Assert.Equal(1, model.CurrentPage);
            Assert.Equal(1, model.TotalPages);
        }

        [Fact]
        public async Task Index_WithRoleFilter_ReturnsFilteredUsers()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var mockEmailService = new Mock<IEmailService>();
            var controller = new UserController(context, mockEmailService.Object);

            // Act
            var result = await controller.Index(null, "Technician", null, 1);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<UserListViewModel>(viewResult.Model);
            Assert.Equal(2, model.Users.Count()); // 2 Technicians
        }

        [Fact]
        public async Task Index_WithSearchTerm_ReturnsMatchingUsers()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var mockEmailService = new Mock<IEmailService>();
            var controller = new UserController(context, mockEmailService.Object);

            // Act
            var result = await controller.Index("Tech User 2", null, null, 1);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<UserListViewModel>(viewResult.Model);
            Assert.Single(model.Users);
            Assert.Equal("TECH02", model.Users.First().UserCode);
        }
        [Fact]
        public async Task Index_WithStatusFilter_ReturnsFilteredUsers()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var mockEmailService = new Mock<IEmailService>();
            var controller = new UserController(context, mockEmailService.Object);

            // Act
            var result = await controller.Index(null, null, "Inactive", 1);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<UserListViewModel>(viewResult.Model);
            Assert.Single(model.Users); // Only TECH02 is inactive
            Assert.False(model.Users.First().IsActive);
        }

        [Fact]
        public async Task Create_Post_ValidModel_CreatesUserAndSendsEmail()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var mockEmailService = new Mock<IEmailService>();
            var controller = new UserController(context, mockEmailService.Object);
            controller.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                new Microsoft.AspNetCore.Http.DefaultHttpContext(), 
                Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());

            var model = new CreateUserViewModel
            {
                UserCode = "TECH99",
                FullName = "New User",
                Email = "new@cems.com",
                Role = "Technician"
            };

            // Act
            var result = await controller.Create(model);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            
            var userInDb = context.Users.FirstOrDefault(u => u.Email == "new@cems.com");
            Assert.NotNull(userInDb);
            Assert.Equal("Cems@123", PRN_Project.Helpers.PasswordHelper.VerifyPassword("Cems@123", userInDb.PasswordHash) ? "Cems@123" : "");
            
            mockEmailService.Verify(e => e.SendEmailAsync(
                It.Is<string>(s => s == "new@cems.com"),
                It.IsAny<string>(),
                It.Is<string>(body => body.Contains("Cems@123"))), 
                Times.Once);
        }
        [Fact]
        public async Task Edit_Post_ValidModel_UpdatesUser()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var mockEmailService = new Mock<IEmailService>();
            var controller = new UserController(context, mockEmailService.Object);
            var user = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(new[]
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, "2")
            }));
            var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext { User = user };
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            controller.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                httpContext, 
                Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());

            var model = new EditUserViewModel
            {
                UserId = 1,
                UserCode = "TECH99",
                FullName = null, // Không cập nhật FullName
                Email = "a@cems.com",
                Role = "Technician" // Admins can change role
            };

            // Act
            var result = await controller.Edit(1, model);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            
            var userInDb = context.Users.Find(1);
            // FullName should remain unchanged (Admin User)
            Assert.Equal("Admin User", userInDb.FullName);
            Assert.Equal("Technician", userInDb.Role);
        }

        [Fact]
        public async Task ToggleStatus_NotSelf_TogglesActiveStatus()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var mockEmailService = new Mock<IEmailService>();
            var controller = new UserController(context, mockEmailService.Object);
            
            // Mock ClaimsPrincipal to simulate current user is UserId=1
            var user = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(new[]
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, "1")
            }));
            var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext { User = user };
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            controller.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                httpContext, 
                Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());

            // Act: Toggle user 2 (currently IsActive = true)
            var result = await controller.ToggleStatus(2);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            var user2 = context.Users.Find(2);
            Assert.False(user2.IsActive);
        }

        [Fact]
        public async Task ToggleStatus_Self_CannotToggle()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var mockEmailService = new Mock<IEmailService>();
            var controller = new UserController(context, mockEmailService.Object);
            
            var user = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(new[]
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, "1")
            }));
            var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext { User = user };
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            controller.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                httpContext, 
                Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());

            // Act: Toggle self (UserId=1)
            var result = await controller.ToggleStatus(1);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            var user1 = context.Users.Find(1);
            Assert.True(user1.IsActive); // Status should remain unchanged
        }
    }
}
