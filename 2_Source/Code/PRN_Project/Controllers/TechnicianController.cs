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

            // Ghi log lịch sử thay đổi các trường dữ liệu (Audit Trail)
            if (equipment.EquipmentName != vm.EquipmentName.Trim())
            {
                _context.EquipmentStatusLogs.Add(new EquipmentStatusLog
                {
                    EquipmentId = equipment.EquipmentId,
                    FieldChanged = "Tên thiết bị",
                    OldValue = equipment.EquipmentName,
                    NewValue = vm.EquipmentName.Trim(),
                    OldStatus = equipment.Status,
                    NewStatus = equipment.Status,
                    ChangedBy = userId,
                    ChangedAt = DateTime.Now,
                    ChangeReason = "Cập nhật tên thiết bị"
                });
            }

            if (equipment.CategoryId != vm.CategoryId)
            {
                var oldCategory = await _context.EquipmentCategories.FindAsync(equipment.CategoryId);
                var newCategory = await _context.EquipmentCategories.FindAsync(vm.CategoryId);
                _context.EquipmentStatusLogs.Add(new EquipmentStatusLog
                {
                    EquipmentId = equipment.EquipmentId,
                    FieldChanged = "Loại thiết bị",
                    OldValue = oldCategory?.CategoryName ?? "Trống",
                    NewValue = newCategory?.CategoryName ?? "Trống",
                    OldStatus = equipment.Status,
                    NewStatus = equipment.Status,
                    ChangedBy = userId,
                    ChangedAt = DateTime.Now,
                    ChangeReason = "Thay đổi loại danh mục thiết bị"
                });
            }

            if ((equipment.SerialNumber ?? "") != (vm.SerialNumber?.Trim() ?? ""))
            {
                _context.EquipmentStatusLogs.Add(new EquipmentStatusLog
                {
                    EquipmentId = equipment.EquipmentId,
                    FieldChanged = "Số Serial (S/N)",
                    OldValue = string.IsNullOrEmpty(equipment.SerialNumber) ? "Trống" : equipment.SerialNumber,
                    NewValue = string.IsNullOrEmpty(vm.SerialNumber) ? "Trống" : vm.SerialNumber.Trim(),
                    OldStatus = equipment.Status,
                    NewStatus = equipment.Status,
                    ChangedBy = userId,
                    ChangedAt = DateTime.Now,
                    ChangeReason = "Cập nhật số Serial"
                });
            }

            if ((equipment.Manufacturer ?? "") != (vm.Manufacturer?.Trim() ?? ""))
            {
                _context.EquipmentStatusLogs.Add(new EquipmentStatusLog
                {
                    EquipmentId = equipment.EquipmentId,
                    FieldChanged = "Hãng sản xuất",
                    OldValue = string.IsNullOrEmpty(equipment.Manufacturer) ? "Trống" : equipment.Manufacturer,
                    NewValue = string.IsNullOrEmpty(vm.Manufacturer) ? "Trống" : vm.Manufacturer.Trim(),
                    OldStatus = equipment.Status,
                    NewStatus = equipment.Status,
                    ChangedBy = userId,
                    ChangedAt = DateTime.Now,
                    ChangeReason = "Cập nhật hãng sản xuất"
                });
            }

            if ((equipment.Supplier ?? "") != (vm.Supplier?.Trim() ?? ""))
            {
                _context.EquipmentStatusLogs.Add(new EquipmentStatusLog
                {
                    EquipmentId = equipment.EquipmentId,
                    FieldChanged = "Nhà cung cấp",
                    OldValue = string.IsNullOrEmpty(equipment.Supplier) ? "Trống" : equipment.Supplier,
                    NewValue = string.IsNullOrEmpty(vm.Supplier) ? "Trống" : vm.Supplier.Trim(),
                    OldStatus = equipment.Status,
                    NewStatus = equipment.Status,
                    ChangedBy = userId,
                    ChangedAt = DateTime.Now,
                    ChangeReason = "Cập nhật nhà cung cấp"
                });
            }

            if (equipment.PurchaseDate != vm.PurchaseDate)
            {
                _context.EquipmentStatusLogs.Add(new EquipmentStatusLog
                {
                    EquipmentId = equipment.EquipmentId,
                    FieldChanged = "Ngày mua",
                    OldValue = equipment.PurchaseDate?.ToString("dd/MM/yyyy") ?? "Trống",
                    NewValue = vm.PurchaseDate?.ToString("dd/MM/yyyy") ?? "Trống",
                    OldStatus = equipment.Status,
                    NewStatus = equipment.Status,
                    ChangedBy = userId,
                    ChangedAt = DateTime.Now,
                    ChangeReason = "Cập nhật ngày mua thiết bị"
                });
            }

            // Ghi log qua C# thay thế/bổ trợ cho trigger phòng trường hợp trigger bỏ sót thay đổi từ NULL
            if (equipment.WarrantyExpiry != vm.WarrantyExpiry)
            {
                _context.EquipmentStatusLogs.Add(new EquipmentStatusLog
                {
                    EquipmentId = equipment.EquipmentId,
                    FieldChanged = "Hạn bảo hành",
                    OldValue = equipment.WarrantyExpiry?.ToString("dd/MM/yyyy") ?? "Trống",
                    NewValue = vm.WarrantyExpiry?.ToString("dd/MM/yyyy") ?? "Trống",
                    OldStatus = equipment.Status,
                    NewStatus = equipment.Status,
                    ChangedBy = userId,
                    ChangedAt = DateTime.Now,
                    ChangeReason = "Cập nhật hạn bảo hành thiết bị"
                });
            }

            if ((equipment.Notes ?? "") != (vm.Notes?.Trim() ?? ""))
            {
                _context.EquipmentStatusLogs.Add(new EquipmentStatusLog
                {
                    EquipmentId = equipment.EquipmentId,
                    FieldChanged = "Ghi chú",
                    OldValue = string.IsNullOrEmpty(equipment.Notes) ? "Trống" : equipment.Notes,
                    NewValue = string.IsNullOrEmpty(vm.Notes) ? "Trống" : vm.Notes.Trim(),
                    OldStatus = equipment.Status,
                    NewStatus = equipment.Status,
                    ChangedBy = userId,
                    ChangedAt = DateTime.Now,
                    ChangeReason = "Cập nhật ghi chú"
                });
            }

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
        // GET: Technician/ProposeDisposal/5 - Form đề xuất thanh lý
        // ========================================================
        [HttpGet]
        public async Task<IActionResult> ProposeDisposal(int id)
        {
            var e = await _context.Equipments
                .Include(x => x.CurrentRoom)
                .Include(x => x.Category)
                .FirstOrDefaultAsync(x => x.EquipmentId == id && x.IsActive);

            if (e == null) return NotFound();

            if (e.Status == "Disposed")
            {
                TempData["ErrorMessage"] = "Thiết bị này đã được thanh lý từ trước.";
                return RedirectToAction(nameof(Index));
            }

            if (e.Status == "ProposedDisposal")
            {
                TempData["ErrorMessage"] = "Thiết bị này đang trong trạng thái chờ duyệt thanh lý.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Equipment = e;
            return View();
        }

        // ========================================================
        // POST: Technician/ProposeDisposal/5 - Lưu đề xuất thanh lý
        // ========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProposeDisposal(int id, string reason)
        {
            var e = await _context.Equipments.FirstOrDefaultAsync(x => x.EquipmentId == id && x.IsActive);
            if (e == null) return NotFound();

            if (e.Status == "Disposed" || e.Status == "ProposedDisposal")
            {
                TempData["ErrorMessage"] = "Trạng thái thiết bị không hợp lệ để đề xuất thanh lý.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                ModelState.AddModelError("reason", "Vui lòng nhập lý do đề xuất thanh lý thiết bị.");
                // Nạp lại thông tin liên quan
                var orig = await _context.Equipments
                    .Include(x => x.CurrentRoom)
                    .Include(x => x.Category)
                    .FirstOrDefaultAsync(x => x.EquipmentId == id);
                ViewBag.Equipment = orig;
                return View();
            }

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return Challenge();

            var request = new DisposalRequest
            {
                EquipmentId = id,
                ProposedBy = userId,
                Reason = reason.Trim(),
                Status = "Pending",
                ProposedAt = DateTime.Now
            };

            _context.DisposalRequests.Add(request);

            e.Status = "ProposedDisposal";
            e.UpdatedAt = DateTime.Now;
            e.UpdatedBy = userId;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã gửi đề xuất thanh lý thiết bị [{e.AssetCode}] thành công!";
            return RedirectToAction(nameof(Index));
        }

        // ========================================================
        // GET: Technician/Transfer/5 - Form điều chuyển vị trí
        // ========================================================
        [HttpGet]
        public async Task<IActionResult> Transfer(int id)
        {
            var e = await _context.Equipments
                .Include(x => x.CurrentRoom)
                .Include(x => x.Category)
                .FirstOrDefaultAsync(x => x.EquipmentId == id && x.IsActive);

            if (e == null) return NotFound();

            if (e.Status == "Disposed")
            {
                TempData["ErrorMessage"] = "Không thể điều chuyển thiết bị đã thanh lý.";
                return RedirectToAction(nameof(Index));
            }

            var rooms = await _context.Rooms.Where(r => r.IsActive).OrderBy(r => r.RoomName).ToListAsync();
            ViewBag.Rooms = new SelectList(rooms, "RoomId", "RoomName", e.CurrentRoomId);
            ViewBag.Equipment = e;

            return View();
        }

        // ========================================================
        // POST: Technician/Transfer/5 - Thực hiện điều chuyển
        // ========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Transfer(int id, int targetRoomId, string reason)
        {
            var e = await _context.Equipments.FirstOrDefaultAsync(x => x.EquipmentId == id && x.IsActive);
            if (e == null) return NotFound();

            if (e.Status == "Disposed")
            {
                TempData["ErrorMessage"] = "Không thể điều chuyển thiết bị đã thanh lý.";
                return RedirectToAction(nameof(Index));
            }

            if (e.CurrentRoomId == targetRoomId)
            {
                ModelState.AddModelError("targetRoomId", "Thiết bị đang ở phòng này.");
                var rooms = await _context.Rooms.Where(r => r.IsActive).OrderBy(r => r.RoomName).ToListAsync();
                ViewBag.Rooms = new SelectList(rooms, "RoomId", "RoomName", targetRoomId);
                var orig = await _context.Equipments
                    .Include(x => x.CurrentRoom)
                    .Include(x => x.Category)
                    .FirstOrDefaultAsync(x => x.EquipmentId == id);
                ViewBag.Equipment = orig;
                return View();
            }

            var targetRoom = await _context.Rooms.FindAsync(targetRoomId);
            if (targetRoom == null || !targetRoom.IsActive)
            {
                ModelState.AddModelError("targetRoomId", "Phòng đích không hợp lệ hoặc không hoạt động.");
                var rooms = await _context.Rooms.Where(r => r.IsActive).OrderBy(r => r.RoomName).ToListAsync();
                ViewBag.Rooms = new SelectList(rooms, "RoomId", "RoomName", targetRoomId);
                var orig = await _context.Equipments
                    .Include(x => x.CurrentRoom)
                    .Include(x => x.Category)
                    .FirstOrDefaultAsync(x => x.EquipmentId == id);
                ViewBag.Equipment = orig;
                return View();
            }

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return Challenge();

            // Lưu log TransferHistory
            var history = new TransferHistory
            {
                EquipmentId = id,
                FromRoomId = e.CurrentRoomId,
                ToRoomId = targetRoomId,
                TransferredBy = userId,
                TransferDate = DateTime.Now,
                Reason = string.IsNullOrWhiteSpace(reason) ? "Điều chuyển phòng học định kỳ." : reason.Trim()
            };

            _context.TransferHistories.Add(history);

            // Cập nhật vị trí hiện tại
            e.CurrentRoomId = targetRoomId;
            e.UpdatedAt = DateTime.Now;
            e.UpdatedBy = userId;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Điều chuyển thiết bị [{e.AssetCode}] sang phòng [{targetRoom.RoomName}] thành công!";
            return RedirectToAction(nameof(Index));
        }

        // ========================================================
        // GET: Technician/Transfers - Danh sách thiết bị luân chuyển vị trí
        // ========================================================
        [HttpGet]
        public async Task<IActionResult> Transfers(string? search, int? categoryId, string? status)
        {
            ViewBag.Categories = new SelectList(
                await _context.EquipmentCategories.OrderBy(c => c.CategoryName).ToListAsync(),
                "CategoryId", "CategoryName", categoryId);

            ViewBag.Statuses = new SelectList(StatusList(), "Value", "Text", status);

            // Chỉ hiển thị thiết bị chưa thanh lý
            var query = _context.VwEquipmentDetails.Where(e => e.Status != "Disposed");

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
        // GET: Technician/TransferHistory/5 - Lịch sử điều chuyển riêng của thiết bị
        // ========================================================
        [HttpGet]
        public async Task<IActionResult> TransferHistory(int id)
        {
            var e = await _context.Equipments
                .Include(x => x.CurrentRoom)
                .Include(x => x.Category)
                .FirstOrDefaultAsync(x => x.EquipmentId == id && x.IsActive);

            if (e == null) return NotFound();

            var list = await _context.TransferHistories
                .Include(t => t.FromRoom)
                .Include(t => t.ToRoom)
                .Include(t => t.TransferredByNavigation)
                .Where(t => t.EquipmentId == id)
                .OrderByDescending(t => t.TransferDate)
                .ToListAsync();

            ViewBag.Equipment = e;
            return View(list);
        }

        // ========================================================
        // GET: Technician/RepairHistory/5 - Lịch sử sửa chữa/bảo trì riêng của thiết bị
        // ========================================================
        [HttpGet]
        public async Task<IActionResult> RepairHistory(int id)
        {
            var e = await _context.Equipments
                .Include(x => x.CurrentRoom)
                .Include(x => x.Category)
                .FirstOrDefaultAsync(x => x.EquipmentId == id && x.IsActive);

            if (e == null) return NotFound();

            var incidents = await _context.IncidentReports
                .Include(i => i.ReportedByNavigation)
                .Include(i => i.AssignedToNavigation)
                .Include(i => i.Room)
                .Where(i => i.EquipmentId == id)
                .OrderByDescending(i => i.ReportedAt)
                .ToListAsync();

            var tickets = await _context.MaintenanceTickets
                .Include(m => m.CreatedByNavigation)
                .Where(m => m.EquipmentId == id)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            var statusLogs = await _context.EquipmentStatusLogs
                .Include(l => l.ChangedByNavigation)
                .Where(l => l.EquipmentId == id)
                .OrderByDescending(l => l.ChangedAt)
                .ToListAsync();

            ViewBag.Equipment = e;
            ViewBag.Incidents = incidents;
            ViewBag.Tickets = tickets;
            ViewBag.StatusLogs = statusLogs;

            return View();
        }

        // ========================================================
        // GET: Technician/Incidents - Danh sách báo cáo sự cố
        // ========================================================
        [HttpGet]
        public async Task<IActionResult> Incidents(string? search, string? status)
        {
            var query = _context.IncidentReports
                .Include(i => i.Equipment)
                .Include(i => i.Room)
                .Include(i => i.ReportedByNavigation)
                .Include(i => i.AssignedToNavigation)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(i =>
                    i.Equipment.AssetCode.Contains(search) ||
                    i.Equipment.EquipmentName.Contains(search) ||
                    i.Room.RoomName.Contains(search) ||
                    i.Description.Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(i => i.Status == status);
            }

            var list = await query.OrderByDescending(i => i.ReportedAt).ToListAsync();

            ViewBag.Statuses = new SelectList(new List<SelectListItem>
            {
                new() { Value = "Pending", Text = "Chờ xử lý" },
                new() { Value = "InProgress", Text = "Đang xử lý" },
                new() { Value = "Resolved", Text = "Đã khắc phục" }
            }, "Value", "Text", status);

            return View(list);
        }

        // ========================================================
        // POST: Technician/ResolveIncident/5 - Đóng sự cố, thiết bị hoạt động lại
        // ========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResolveIncident(int id, string resolutionNote)
        {
            var incident = await _context.IncidentReports
                .Include(i => i.Equipment)
                .Include(i => i.Room)
                .FirstOrDefaultAsync(i => i.IncidentId == id);

            if (incident == null) return NotFound();

            if (incident.Status == "Resolved")
            {
                TempData["ErrorMessage"] = "Sự cố này đã được giải quyết từ trước.";
                return RedirectToAction(nameof(Incidents));
            }

            if (string.IsNullOrWhiteSpace(resolutionNote))
            {
                TempData["ErrorMessage"] = "Vui lòng cung cấp nội dung giải quyết sự cố.";
                return RedirectToAction(nameof(Incidents));
            }

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return Challenge();

            // Cập nhật sự cố
            incident.Status = "Resolved";
            incident.ResolvedAt = DateTime.Now;
            incident.ResolutionNote = resolutionNote.Trim();
            incident.AssignedTo = userId;

            // Cập nhật thiết bị tương ứng về hoạt động
            var equipment = incident.Equipment;
            if (equipment != null && equipment.Status != "Disposed")
            {
                // Ghi nhận status log cho thiết bị
                _context.EquipmentStatusLogs.Add(new EquipmentStatusLog
                {
                    EquipmentId = equipment.EquipmentId,
                    FieldChanged = "Status",
                    OldStatus = equipment.Status,
                    NewStatus = "InUse",
                    ChangedBy = userId,
                    ChangedAt = DateTime.Now,
                    ChangeReason = $"Khắc phục sự cố hỏng hóc: {resolutionNote.Trim()}"
                });

                equipment.Status = "InUse";
                equipment.UpdatedAt = DateTime.Now;
                equipment.UpdatedBy = userId;
            }

            // Gửi thông báo đến giảng viên báo cáo (UC_SendNotification)
            _context.Notifications.Add(new Notification
            {
                RecipientId = incident.ReportedBy,
                Title = "Báo cáo sự cố đã được xử lý",
                Message = $"Sự cố báo hỏng thiết bị [{equipment?.AssetCode}] tại phòng [{incident.Room.RoomName}] đã được khắc phục. Nội dung giải quyết: {resolutionNote.Trim()}.",
                Type = "System",
                IsRead = false,
                SentAt = DateTime.Now
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã xác nhận giải quyết sự cố cho thiết bị [{equipment?.AssetCode}]!";
            return RedirectToAction(nameof(Incidents));
        }

        // ========================================================
        // GET: Technician/CreateMaintenanceTicket - Form lập phiếu bảo trì ngoài
        // ========================================================
        [HttpGet]
        public async Task<IActionResult> CreateMaintenanceTicket(int incidentId)
        {
            var incident = await _context.IncidentReports
                .Include(i => i.Equipment)
                .Include(i => i.Room)
                .FirstOrDefaultAsync(i => i.IncidentId == incidentId);

            if (incident == null) return NotFound();

            if (incident.Status == "Resolved")
            {
                TempData["ErrorMessage"] = "Sự cố này đã được giải quyết, không thể lập phiếu bảo trì ngoài.";
                return RedirectToAction(nameof(Incidents));
            }

            ViewBag.Incident = incident;

            // Mặc định ngày gửi là hôm nay, hẹn trả sau 7 ngày
            var model = new MaintenanceTicket
            {
                IncidentId = incidentId,
                EquipmentId = incident.EquipmentId,
                SentDate = DateOnly.FromDateTime(DateTime.Today),
                ExpectedReturnDate = DateOnly.FromDateTime(DateTime.Today.AddDays(7))
            };

            return View(model);
        }

        // ========================================================
        // POST: Technician/CreateMaintenanceTicket - Lưu phiếu bảo trì ngoài
        // ========================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMaintenanceTicket(MaintenanceTicket model)
        {
            var incident = await _context.IncidentReports
                .Include(i => i.Equipment)
                .Include(i => i.Room)
                .FirstOrDefaultAsync(i => i.IncidentId == model.IncidentId);

            if (incident == null) return NotFound();

            if (string.IsNullOrWhiteSpace(model.ServiceProvider))
            {
                ModelState.AddModelError(nameof(model.ServiceProvider), "Vui lòng nhập đơn vị sửa chữa/bảo trì.");
            }

            if (model.ExpectedReturnDate < model.SentDate)
            {
                ModelState.AddModelError(nameof(model.ExpectedReturnDate), "Ngày dự kiến hoàn thành phải sau hoặc bằng Ngày gửi sửa.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Incident = incident;
                return model.IncidentId == 0 ? RedirectToAction(nameof(Incidents)) : View(model);
            }

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return Challenge();

            var ticket = new MaintenanceTicket
            {
                IncidentId = model.IncidentId,
                EquipmentId = incident.EquipmentId,
                CreatedBy = userId,
                ServiceProvider = model.ServiceProvider.Trim(),
                EstimatedCost = model.EstimatedCost,
                SentDate = model.SentDate,
                ExpectedReturnDate = model.ExpectedReturnDate,
                Status = "Sent",
                Notes = model.Notes?.Trim(),
                CreatedAt = DateTime.Now
            };

            _context.MaintenanceTickets.Add(ticket);

            // Cập nhật trạng thái sự cố sang InProgress và gán cho kỹ thuật viên
            incident.Status = "InProgress";
            incident.AssignedTo = userId;

            // Cập nhật trạng thái thiết bị sang UnderMaintenance
            var equipment = incident.Equipment;
            if (equipment != null && equipment.Status != "Disposed")
            {
                // Ghi log trạng thái
                _context.EquipmentStatusLogs.Add(new EquipmentStatusLog
                {
                    EquipmentId = equipment.EquipmentId,
                    FieldChanged = "Status",
                    OldStatus = equipment.Status,
                    NewStatus = "UnderMaintenance",
                    ChangedBy = userId,
                    ChangedAt = DateTime.Now,
                    ChangeReason = $"Gửi bảo trì ngoài qua đơn vị: {model.ServiceProvider.Trim()}"
                });

                equipment.Status = "UnderMaintenance";
                equipment.UpdatedAt = DateTime.Now;
                equipment.UpdatedBy = userId;
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã lập phiếu gửi bảo trì ngoài thành công cho thiết bị [{equipment?.AssetCode}]!";
            return RedirectToAction(nameof(Incidents));
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
