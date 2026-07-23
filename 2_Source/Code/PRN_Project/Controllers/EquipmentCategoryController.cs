using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PRN_Project.Models;
using System.Linq;
using System.Threading.Tasks;

namespace PRN_Project.Controllers
{
    [Authorize(Roles = "Admin")]
    public class EquipmentCategoryController : Controller
    {
        private readonly AppDbContext _context;

        public EquipmentCategoryController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var categories = await _context.EquipmentCategories
                .Select(c => new CategoryListViewModel
                {
                    CategoryId = c.CategoryId,
                    CategoryName = c.CategoryName,
                    CreatedAt = c.CreatedAt,
                    EquipmentCount = c.Equipment.Count
                })
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return View(categories);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateCategoryViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (await _context.EquipmentCategories.AnyAsync(c => c.CategoryName == model.CategoryName))
                {
                    ModelState.AddModelError("CategoryName", "Tên danh mục này đã tồn tại.");
                    return View(model);
                }

                var category = new EquipmentCategory
                {
                    CategoryName = model.CategoryName,
                    CreatedAt = System.DateTime.Now
                };

                _context.EquipmentCategories.Add(category);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đã thêm danh mục thiết bị thành công.";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var category = await _context.EquipmentCategories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            var model = new EditCategoryViewModel
            {
                CategoryId = category.CategoryId,
                CategoryName = category.CategoryName
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditCategoryViewModel model)
        {
            if (id != model.CategoryId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var category = await _context.EquipmentCategories.FindAsync(id);
                if (category == null)
                {
                    return NotFound();
                }

                if (category.CategoryName != model.CategoryName && await _context.EquipmentCategories.AnyAsync(c => c.CategoryName == model.CategoryName))
                {
                    ModelState.AddModelError("CategoryName", "Tên danh mục này đã tồn tại.");
                    return View(model);
                }

                category.CategoryName = model.CategoryName;
                _context.Update(category);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Cập nhật danh mục thiết bị thành công.";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.EquipmentCategories
                .Include(c => c.Equipment)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
            {
                return NotFound();
            }

            if (category.Equipment.Any())
            {
                TempData["ErrorMessage"] = "Không thể xóa danh mục này vì đang có thiết bị thuộc về nó.";
                return RedirectToAction(nameof(Index));
            }

            _context.EquipmentCategories.Remove(category);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã xóa danh mục thiết bị thành công.";
            return RedirectToAction(nameof(Index));
        }
    }
}
