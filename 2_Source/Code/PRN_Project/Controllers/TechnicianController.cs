using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PRN_Project.Models;
using System.Security.Claims;

namespace PRN_Project.Controllers
{
    [Authorize(Roles = "Technician,Admin")]
    public class TechnicianController : Controller
    {
        private readonly AppDbContext _context;

        public TechnicianController(AppDbContext context)
        {
            _context = context;
        }

        // ========================================================
        // GET: Technician/Index - Danh sách thiết bị
        // ========================================================
        [HttpGet]
        public async Task<IActionResult> Index(string? search, int? categoryId, string? status)
        {
            ViewBag.Categories = new SelectList(
                await _context.EquipmentCategories.OrderBy(c => c.CategoryName).ToListAsync(),
                "CategoryId", "CategoryName", categoryId);

            ViewBag.Statuses = new SelectList(StatusList(), "Value", "Text", status);

            var query = _context.VwEquipmentDetails.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(e =>
                    e.AssetCode.Contains(search) ||
                    e.EquipmentName.Contains(search) ||
                    (e.RoomName != null && e.RoomName.Contains(search)));
            }

            if (categoryId.HasValue)
            {
                var cat = await _context.EquipmentCategories.FindAsync(categoryId.Value);
                if (cat != null)
                    query = query.Where(e => e.CategoryName == cat.CategoryName);
            }

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(e => e.Status == status);

            var list = await query.OrderByDescending(e => e.EquipmentId).ToListAsync();
            return View(list);
        }

        // ========================================================
        // GET: Technician/Create - Form thêm mới
        // ========================================================
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await PopulateDropdowns();
            return View(new EquipmentFormViewModel());
        }

        // ========================================================
        // POST: Technician/Create - Lưu thiết bị mới
        // ========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EquipmentFormViewModel vm)
        {
            // Kiểm tra AssetCode trùng lặp (ngoài DataAnnotations)
            if (ModelState.IsValid)
            {
                var exists = await _context.Equipments
                    .AnyAsync(e => e.AssetCode == vm.AssetCode.Trim());

                if (exists)
                    ModelState.AddModelError(nameof(vm.AssetCode),
                        "Mã tài sản này đã tồn tại trong hệ thống. Vui lòng nhập mã khác.");
            }

            // Kiểm tra WarrantyExpiry >= PurchaseDate
            if (vm.PurchaseDate.HasValue && vm.WarrantyExpiry.HasValue
                && vm.WarrantyExpiry.Value < vm.PurchaseDate.Value)
            {
                ModelState.AddModelError(nameof(vm.WarrantyExpiry),
                    "Ngày hết hạn bảo hành phải sau hoặc bằng Ngày mua.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(vm.CategoryId, vm.CurrentRoomId);
                return View(vm);
            }

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
                return Challenge();

            var equipment = new Equipment
            {
                AssetCode      = vm.AssetCode.Trim().ToUpper(),
                EquipmentName  = vm.EquipmentName.Trim(),
                CategoryId     = vm.CategoryId,
                CurrentRoomId  = vm.CurrentRoomId,
                SerialNumber   = vm.SerialNumber?.Trim(),
                Manufacturer   = vm.Manufacturer?.Trim(),
                Supplier       = vm.Supplier?.Trim(),
                PurchaseDate   = vm.PurchaseDate,
                WarrantyExpiry = vm.WarrantyExpiry,
                Notes          = vm.Notes?.Trim(),
                Status         = "InUse",
                IsActive       = true,
                CreatedBy      = userId,
                CreatedAt      = DateTime.Now
            };

            _context.Equipments.Add(equipment);
            await _context.SaveChangesAsync();

            // Ghi lịch sử gán phòng đầu tiên nếu có (UC18 logic)
            if (equipment.CurrentRoomId.HasValue)
            {
                _context.TransferHistories.Add(new TransferHistory
                {
                    EquipmentId  = equipment.EquipmentId,
                    FromRoomId   = null,
                    ToRoomId     = equipment.CurrentRoomId.Value,
                    TransferredBy = userId,
                    TransferDate = DateTime.Now,
                    Reason       = "Gán phòng học lần đầu khi nhập kho thiết bị."
                });
                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = $"Thêm mới thiết bị [{equipment.AssetCode}] thành công!";
            return RedirectToAction(nameof(Index));
        }

        // ========================================================
        // GET: Technician/Edit/5 - Form chỉnh sửa
        // ========================================================
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var e = await _context.Equipments
                .Include(x => x.CreatedByNavigation)
                .FirstOrDefaultAsync(x => x.EquipmentId == id && x.IsActive);

            if (e == null) return NotFound();

            var vm = new EquipmentFormViewModel
            {
                EquipmentId    = e.EquipmentId,
                AssetCode      = e.AssetCode,
                EquipmentName  = e.EquipmentName,
                CategoryId     = e.CategoryId,
                CurrentRoomId  = e.CurrentRoomId,
                SerialNumber   = e.SerialNumber,
                Manufacturer   = e.Manufacturer,
                Supplier       = e.Supplier,
                PurchaseDate   = e.PurchaseDate,
                WarrantyExpiry = e.WarrantyExpiry,
                Notes          = e.Notes,
                Status         = e.Status,
                CreatedByName  = e.CreatedByNavigation?.FullName,
                CreatedAt      = e.CreatedAt
            };

            await PopulateDropdowns(e.CategoryId, e.CurrentRoomId);
            return View(vm);
        }

        // ========================================================
        // POST: Technician/Edit/5 - Lưu chỉnh sửa
        // ========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EquipmentFormViewModel vm)
        {
            if (id != vm.EquipmentId) return BadRequest();

            // AssetCode là readonly nên không validate RegEx cho phần này
            ModelState.Remove(nameof(vm.AssetCode));

            // Kiểm tra WarrantyExpiry >= PurchaseDate
            if (vm.PurchaseDate.HasValue && vm.WarrantyExpiry.HasValue
                && vm.WarrantyExpiry.Value < vm.PurchaseDate.Value)
            {
                ModelState.AddModelError(nameof(vm.WarrantyExpiry),
                    "Ngày hết hạn bảo hành phải sau hoặc bằng Ngày mua.");
            }

            if (!ModelState.IsValid)
            {
                // Lấy lại thông tin readonly để hiển thị
                var orig = await _context.Equipments
                    .Include(x => x.CreatedByNavigation)
                    .FirstOrDefaultAsync(x => x.EquipmentId == id);
                if (orig != null)
                {
                    vm.AssetCode     = orig.AssetCode;
                    vm.Status        = orig.Status;
                    vm.CreatedByName = orig.CreatedByNavigation?.FullName;
                    vm.CreatedAt     = orig.CreatedAt;
                }
                await PopulateDropdowns(vm.CategoryId, vm.CurrentRoomId);
                return View(vm);
            }

            var equipment = await _context.Equipments.FindAsync(id);
            if (equipment == null || !equipment.IsActive) return NotFound();

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return Challenge();

            equipment.EquipmentName  = vm.EquipmentName.Trim();
            equipment.CategoryId     = vm.CategoryId;
            equipment.SerialNumber   = vm.SerialNumber?.Trim();
            equipment.Manufacturer   = vm.Manufacturer?.Trim();
            equipment.Supplier       = vm.Supplier?.Trim();
            equipment.PurchaseDate   = vm.PurchaseDate;
            equipment.WarrantyExpiry = vm.WarrantyExpiry;
            equipment.Notes          = vm.Notes?.Trim();
            equipment.UpdatedBy      = userId;
            equipment.UpdatedAt      = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Cập nhật thiết bị [{equipment.AssetCode}] thành công!";
            return RedirectToAction(nameof(Index));
        }

        // ========================================================
        // Helpers
        // ========================================================
        private async Task PopulateDropdowns(int? selectedCategoryId = null, int? selectedRoomId = null)
        {
            ViewBag.Categories = new SelectList(
                await _context.EquipmentCategories.OrderBy(c => c.CategoryName).ToListAsync(),
                "CategoryId", "CategoryName", selectedCategoryId);

            ViewBag.Rooms = new SelectList(
                await _context.Rooms.Where(r => r.IsActive).OrderBy(r => r.RoomName).ToListAsync(),
                "RoomId", "RoomName", selectedRoomId);
        }

        private static List<SelectListItem> StatusList() => new()
        {
            new() { Value = "InUse",            Text = "Đang sử dụng" },
            new() { Value = "PendingRepair",     Text = "Đang chờ sửa" },
            new() { Value = "UnderMaintenance",  Text = "Đang bảo trì" },
            new() { Value = "ProposedDisposal",  Text = "Đề xuất thanh lý" },
            new() { Value = "Disposed",          Text = "Đã thanh lý" }
        };
    }
}
