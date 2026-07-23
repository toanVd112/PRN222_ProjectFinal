using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PRN_Project.Controllers;
using PRN_Project.Models;
using System.Threading.Tasks;
using Xunit;

namespace PRN_Project.Tests
{
    public class EquipmentCategoryControllerTests
    {
        private AppDbContext GetContextWithData()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
                .Options;

            var context = new AppDbContext(options);

            context.EquipmentCategories.AddRange(
                new EquipmentCategory { CategoryId = 1, CategoryName = "Máy chiếu" },
                new EquipmentCategory { CategoryId = 2, CategoryName = "Tivi" }
            );

            // Thêm 1 equipment thuộc category 1 để test chặn xóa
            context.Equipments.Add(
                new Equipment { EquipmentId = 1, CategoryId = 1, AssetCode = "MC01", EquipmentName = "Máy chiếu Panasonic", Status = "InUse" }
            );

            context.SaveChanges();
            return context;
        }

        [Fact]
        public async Task Index_ReturnsViewResult_WithListOfCategories()
        {
            using var context = GetContextWithData();
            var controller = new EquipmentCategoryController(context);

            var result = await controller.Index();

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<System.Collections.Generic.IEnumerable<CategoryListViewModel>>(viewResult.Model);
            Assert.Equal(2, System.Linq.Enumerable.Count(model));
        }

        [Fact]
        public async Task Create_ReturnsError_WhenNameExists()
        {
            using var context = GetContextWithData();
            var controller = new EquipmentCategoryController(context);
            var newCategory = new CreateCategoryViewModel { CategoryName = "Máy chiếu" }; // Tên trùng

            var result = await controller.Create(newCategory);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
            Assert.True(controller.ModelState.ContainsKey("CategoryName"));
        }

        [Fact]
        public async Task Create_AddsNewCategory_WhenValid()
        {
            using var context = GetContextWithData();
            var controller = new EquipmentCategoryController(context);
            controller.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(new Microsoft.AspNetCore.Http.DefaultHttpContext(), Moq.Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());
            var newCategory = new CreateCategoryViewModel { CategoryName = "Điều hòa" };

            var result = await controller.Create(newCategory);

            Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(3, await context.EquipmentCategories.CountAsync());
        }

        [Fact]
        public async Task Delete_ReturnsError_WhenCategoryHasEquipments()
        {
            using var context = GetContextWithData();
            var controller = new EquipmentCategoryController(context);
            controller.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(new Microsoft.AspNetCore.Http.DefaultHttpContext(), Moq.Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());

            // Category 1 đang có thiết bị
            var result = await controller.Delete(1);

            Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Không thể xóa danh mục này vì đang có thiết bị thuộc về nó.", controller.TempData["ErrorMessage"]);
            // Đảm bảo không bị xóa
            Assert.NotNull(await context.EquipmentCategories.FindAsync(1));
        }

        [Fact]
        public async Task Delete_Success_WhenCategoryIsEmpty()
        {
            using var context = GetContextWithData();
            var controller = new EquipmentCategoryController(context);
            controller.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(new Microsoft.AspNetCore.Http.DefaultHttpContext(), Moq.Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());

            // Category 2 không có thiết bị
            var result = await controller.Delete(2);

            Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Đã xóa danh mục thiết bị thành công.", controller.TempData["SuccessMessage"]);
            // Đảm bảo đã bị xóa
            Assert.Null(await context.EquipmentCategories.FindAsync(2));
        }
    }
}
