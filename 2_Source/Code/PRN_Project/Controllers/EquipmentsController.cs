using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PRN_Project.Models;

namespace PRN_Project.Controllers
{
    [Authorize(Roles = "Technician,Admin")]
    public class EquipmentsController : Controller
    {
        private readonly AppDbContext _context;

        public EquipmentsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Equipments
        [HttpGet]
        public async Task<IActionResult> Index(string? search, int? categoryId, int? roomId, string? status)
        {
            var query = _context.Equipments
                .Include(e => e.Category)
                .Include(e => e.CurrentRoom)
                .Where(e => e.IsActive)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(search))
            {
                var cleanSearch = search.Trim();
                query = query.Where(e => e.EquipmentName.Contains(cleanSearch) || e.AssetCode.Contains(cleanSearch) || (e.SerialNumber != null && e.SerialNumber.Contains(cleanSearch)));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(e => e.CategoryId == categoryId.Value);
            }

            if (roomId.HasValue)
            {
                query = query.Where(e => e.CurrentRoomId == roomId.Value);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(e => e.Status == status);
            }

            var equipments = await query
                .OrderBy(e => e.AssetCode)
                .Select(e => new EquipmentListItemViewModel
                {
                    EquipmentId = e.EquipmentId,
                    AssetCode = e.AssetCode,
                    EquipmentName = e.EquipmentName,
                    CategoryName = e.Category.CategoryName,
                    RoomDisplayName = e.CurrentRoom != null ? $"{e.CurrentRoom.RoomCode} - {e.CurrentRoom.RoomName}" : "Chưa gán / Lưu kho",
                    Status = e.Status,
                    WarrantyExpiry = e.WarrantyExpiry
                })
                .ToListAsync();

            // Check if AJAX request (auto-search)
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_EquipmentTableBody", equipments);
            }

            // Populate view model with filter lists
            var model = new EquipmentListViewModel
            {
                Search = search,
                CategoryId = categoryId,
                RoomId = roomId,
                Status = status,
                Equipments = equipments,
                Categories = await _context.EquipmentCategories
                    .OrderBy(c => c.CategoryName)
                    .Select(c => new SelectListItem { Value = c.CategoryId.ToString(), Text = c.CategoryName })
                    .ToListAsync(),
                Rooms = await _context.Rooms
                    .Where(r => r.IsActive)
                    .OrderBy(r => r.RoomCode)
                    .Select(r => new SelectListItem { Value = r.RoomId.ToString(), Text = $"{r.RoomCode} - {r.RoomName}" })
                    .ToListAsync(),
                Statuses = new List<SelectListItem>
                {
                    new() { Value = "InUse", Text = "Đang sử dụng" },
                    new() { Value = "PendingRepair", Text = "Chờ sửa chữa" },
                    new() { Value = "UnderMaintenance", Text = "Đang bảo trì ngoài" },
                    new() { Value = "ProposedDisposal", Text = "Đề xuất thanh lý" },
                    new() { Value = "Disposed", Text = "Đã thanh lý" }
                }
            };

            return View(model);
        }

        // GET: Equipments/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var equipment = await _context.Equipments
                .Include(e => e.Category)
                .Include(e => e.CurrentRoom)
                .Include(e => e.CreatedByNavigation)
                .Include(e => e.UpdatedByNavigation)
                .Include(e => e.TransferHistories)
                    .ThenInclude(th => th.FromRoom)
                .Include(e => e.TransferHistories)
                    .ThenInclude(th => th.ToRoom)
                .Include(e => e.TransferHistories)
                    .ThenInclude(th => th.TransferredByNavigation)
                .Include(e => e.EquipmentStatusLogs)
                    .ThenInclude(esl => esl.ChangedByNavigation)
                .FirstOrDefaultAsync(e => e.EquipmentId == id && e.IsActive);

            if (equipment == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thiết bị hoặc thiết bị đã bị vô hiệu hóa.";
                return RedirectToAction(nameof(Index));
            }

            return View(equipment);
        }

        // GET: Equipments/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new EquipmentCreateViewModel
            {
                Categories = await GetCategorySelectList(),
                Rooms = await GetRoomSelectList()
            };
            return View(model);
        }

        // POST: Equipments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EquipmentCreateViewModel model)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            // Check if AssetCode is unique
            if (await _context.Equipments.AnyAsync(e => e.AssetCode == model.AssetCode))
            {
                ModelState.AddModelError(nameof(model.AssetCode), "Mã tài sản này đã tồn tại trong hệ thống.");
            }

            if (!ModelState.IsValid)
            {
                model.Categories = await GetCategorySelectList();
                model.Rooms = await GetRoomSelectList();
                return View(model);
            }

            var equipment = new Equipment
            {
                AssetCode = model.AssetCode.Trim(),
                EquipmentName = model.EquipmentName.Trim(),
                CategoryId = model.CategoryId,
                CurrentRoomId = model.CurrentRoomId,
                SerialNumber = model.SerialNumber?.Trim(),
                Manufacturer = model.Manufacturer?.Trim(),
                Supplier = model.Supplier?.Trim(),
                PurchaseDate = model.PurchaseDate,
                WarrantyExpiry = model.WarrantyExpiry,
                Status = "InUse",
                IsActive = true,
                Notes = model.Notes?.Trim(),
                CreatedBy = userId.Value,
                CreatedAt = DateTime.Now
            };

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Equipments.Add(equipment);
                await _context.SaveChangesAsync();

                // If room is assigned, record initial transfer history
                if (equipment.CurrentRoomId.HasValue)
                {
                    var transfer = new TransferHistory
                    {
                        EquipmentId = equipment.EquipmentId,
                        FromRoomId = null,
                        ToRoomId = equipment.CurrentRoomId.Value,
                        TransferredBy = userId.Value,
                        TransferDate = DateTime.Now,
                        Reason = "Khởi tạo thiết bị mới và bàn giao phòng học."
                    };
                    _context.TransferHistories.Add(transfer);
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError(string.Empty, "Có lỗi xảy ra khi lưu dữ liệu. Vui lòng thử lại.");
                model.Categories = await GetCategorySelectList();
                model.Rooms = await GetRoomSelectList();
                return View(model);
            }

            TempData["SuccessMessage"] = $"Thêm mới thiết bị {equipment.AssetCode} thành công.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Equipments/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var equipment = await _context.Equipments
                .Include(e => e.Category)
                .FirstOrDefaultAsync(e => e.EquipmentId == id && e.IsActive);

            if (equipment == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thiết bị hoặc thiết bị đã bị vô hiệu hóa.";
                return RedirectToAction(nameof(Index));
            }

            var model = new EquipmentEditViewModel
            {
                EquipmentId = equipment.EquipmentId,
                AssetCode = equipment.AssetCode,
                EquipmentName = equipment.EquipmentName,
                CategoryId = equipment.CategoryId,
                SerialNumber = equipment.SerialNumber,
                Manufacturer = equipment.Manufacturer,
                Supplier = equipment.Supplier,
                PurchaseDate = equipment.PurchaseDate,
                WarrantyExpiry = equipment.WarrantyExpiry,
                Status = equipment.Status,
                Notes = equipment.Notes,
                Categories = await GetCategorySelectList(),
                Statuses = new List<SelectListItem>
                {
                    new() { Value = "InUse", Text = "Đang sử dụng" },
                    new() { Value = "PendingRepair", Text = "Chờ sửa chữa" },
                    new() { Value = "UnderMaintenance", Text = "Đang bảo trì ngoài" },
                    new() { Value = "ProposedDisposal", Text = "Đề xuất thanh lý" }
                }
            };

            return View(model);
        }

        // POST: Equipments/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EquipmentEditViewModel model)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            if (!ModelState.IsValid)
            {
                model.Categories = await GetCategorySelectList();
                model.Statuses = new List<SelectListItem>
                {
                    new() { Value = "InUse", Text = "Đang sử dụng" },
                    new() { Value = "PendingRepair", Text = "Chờ sửa chữa" },
                    new() { Value = "UnderMaintenance", Text = "Đang bảo trì ngoài" },
                    new() { Value = "ProposedDisposal", Text = "Đề xuất thanh lý" }
                };
                return View(model);
            }

            var equipment = await _context.Equipments.FirstOrDefaultAsync(e => e.EquipmentId == model.EquipmentId && e.IsActive);
            if (equipment == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thiết bị hoặc thiết bị đã bị vô hiệu hóa.";
                return RedirectToAction(nameof(Index));
            }

            // Generate audit logs for changed fields
            var changeTime = DateTime.Now;
            var logs = new List<EquipmentStatusLog>();

            if (equipment.EquipmentName != model.EquipmentName.Trim())
            {
                logs.Add(new EquipmentStatusLog
                {
                    EquipmentId = equipment.EquipmentId,
                    ChangedBy = userId.Value,
                    OldStatus = equipment.Status,
                    NewStatus = model.Status,
                    FieldChanged = "Tên thiết bị",
                    OldValue = equipment.EquipmentName,
                    NewValue = model.EquipmentName.Trim(),
                    ChangeReason = "Chỉnh sửa thông tin thiết bị",
                    ChangedAt = changeTime
                });
            }

            if (equipment.CategoryId != model.CategoryId)
            {
                var oldCategory = await _context.EquipmentCategories.FindAsync(equipment.CategoryId);
                var newCategory = await _context.EquipmentCategories.FindAsync(model.CategoryId);
                logs.Add(new EquipmentStatusLog
                {
                    EquipmentId = equipment.EquipmentId,
                    ChangedBy = userId.Value,
                    OldStatus = equipment.Status,
                    NewStatus = model.Status,
                    FieldChanged = "Loại thiết bị",
                    OldValue = oldCategory?.CategoryName ?? equipment.CategoryId.ToString(),
                    NewValue = newCategory?.CategoryName ?? model.CategoryId.ToString(),
                    ChangeReason = "Chỉnh sửa thông tin thiết bị",
                    ChangedAt = changeTime
                });
            }

            if ((equipment.SerialNumber ?? "") != (model.SerialNumber?.Trim() ?? ""))
            {
                logs.Add(new EquipmentStatusLog
                {
                    EquipmentId = equipment.EquipmentId,
                    ChangedBy = userId.Value,
                    OldStatus = equipment.Status,
                    NewStatus = model.Status,
                    FieldChanged = "Số Serial",
                    OldValue = string.IsNullOrEmpty(equipment.SerialNumber) ? "Trống" : equipment.SerialNumber,
                    NewValue = string.IsNullOrEmpty(model.SerialNumber) ? "Trống" : model.SerialNumber.Trim(),
                    ChangeReason = "Chỉnh sửa thông tin thiết bị",
                    ChangedAt = changeTime
                });
            }

            if ((equipment.Manufacturer ?? "") != (model.Manufacturer?.Trim() ?? ""))
            {
                logs.Add(new EquipmentStatusLog
                {
                    EquipmentId = equipment.EquipmentId,
                    ChangedBy = userId.Value,
                    OldStatus = equipment.Status,
                    NewStatus = model.Status,
                    FieldChanged = "Hãng sản xuất",
                    OldValue = string.IsNullOrEmpty(equipment.Manufacturer) ? "Trống" : equipment.Manufacturer,
                    NewValue = string.IsNullOrEmpty(model.Manufacturer) ? "Trống" : model.Manufacturer.Trim(),
                    ChangeReason = "Chỉnh sửa thông tin thiết bị",
                    ChangedAt = changeTime
                });
            }

            if ((equipment.Supplier ?? "") != (model.Supplier?.Trim() ?? ""))
            {
                logs.Add(new EquipmentStatusLog
                {
                    EquipmentId = equipment.EquipmentId,
                    ChangedBy = userId.Value,
                    OldStatus = equipment.Status,
                    NewStatus = model.Status,
                    FieldChanged = "Nhà cung cấp",
                    OldValue = string.IsNullOrEmpty(equipment.Supplier) ? "Trống" : equipment.Supplier,
                    NewValue = string.IsNullOrEmpty(model.Supplier) ? "Trống" : model.Supplier.Trim(),
                    ChangeReason = "Chỉnh sửa thông tin thiết bị",
                    ChangedAt = changeTime
                });
            }

            if (equipment.PurchaseDate != model.PurchaseDate)
            {
                logs.Add(new EquipmentStatusLog
                {
                    EquipmentId = equipment.EquipmentId,
                    ChangedBy = userId.Value,
                    OldStatus = equipment.Status,
                    NewStatus = model.Status,
                    FieldChanged = "Ngày mua",
                    OldValue = equipment.PurchaseDate?.ToString("dd/MM/yyyy") ?? "Trống",
                    NewValue = model.PurchaseDate?.ToString("dd/MM/yyyy") ?? "Trống",
                    ChangeReason = "Chỉnh sửa thông tin thiết bị",
                    ChangedAt = changeTime
                });
            }

            if ((equipment.Notes ?? "") != (model.Notes?.Trim() ?? ""))
            {
                logs.Add(new EquipmentStatusLog
                {
                    EquipmentId = equipment.EquipmentId,
                    ChangedBy = userId.Value,
                    OldStatus = equipment.Status,
                    NewStatus = model.Status,
                    FieldChanged = "Ghi chú",
                    OldValue = string.IsNullOrEmpty(equipment.Notes) ? "Trống" : equipment.Notes,
                    NewValue = string.IsNullOrEmpty(model.Notes) ? "Trống" : model.Notes.Trim(),
                    ChangeReason = "Chỉnh sửa thông tin thiết bị",
                    ChangedAt = changeTime
                });
            }

            equipment.EquipmentName = model.EquipmentName.Trim();
            equipment.CategoryId = model.CategoryId;
            equipment.SerialNumber = model.SerialNumber?.Trim();
            equipment.Manufacturer = model.Manufacturer?.Trim();
            equipment.Supplier = model.Supplier?.Trim();
            equipment.PurchaseDate = model.PurchaseDate;
            equipment.WarrantyExpiry = model.WarrantyExpiry;
            equipment.Status = model.Status;
            equipment.Notes = model.Notes?.Trim();
            equipment.UpdatedBy = userId.Value;
            equipment.UpdatedAt = changeTime;

            if (logs.Any())
            {
                _context.EquipmentStatusLogs.AddRange(logs);
            }

            try
            {
                _context.Update(equipment);
                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "Có lỗi xảy ra khi cập nhật thiết bị. Vui lòng thử lại.");
                model.Categories = await GetCategorySelectList();
                model.Statuses = new List<SelectListItem>
                {
                    new() { Value = "InUse", Text = "Đang sử dụng" },
                    new() { Value = "PendingRepair", Text = "Chờ sửa chữa" },
                    new() { Value = "UnderMaintenance", Text = "Đang bảo trì ngoài" },
                    new() { Value = "ProposedDisposal", Text = "Đề xuất thanh lý" }
                };
                return View(model);
            }

            TempData["SuccessMessage"] = $"Cập nhật thiết bị {equipment.AssetCode} thành công.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Equipments/ProposeDisposal/5
        [HttpGet]
        public async Task<IActionResult> ProposeDisposal(int id)
        {
            var equipment = await _context.Equipments
                .Include(e => e.CurrentRoom)
                .FirstOrDefaultAsync(e => e.EquipmentId == id && e.IsActive);

            if (equipment == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thiết bị hoặc thiết bị đã bị vô hiệu hóa.";
                return RedirectToAction(nameof(Index));
            }

            if (equipment.Status == "Disposed")
            {
                TempData["ErrorMessage"] = "Thiết bị này đã được thanh lý từ trước.";
                return RedirectToAction(nameof(Details), new { id = equipment.EquipmentId });
            }

            if (equipment.Status == "ProposedDisposal")
            {
                TempData["WarningMessage"] = "Thiết bị này đang trong trạng thái chờ phê duyệt thanh lý.";
                return RedirectToAction(nameof(Details), new { id = equipment.EquipmentId });
            }

            var model = new EquipmentProposeDisposalViewModel
            {
                EquipmentId = equipment.EquipmentId,
                AssetCode = equipment.AssetCode,
                EquipmentName = equipment.EquipmentName,
                RoomDisplayName = equipment.CurrentRoom != null ? $"{equipment.CurrentRoom.RoomCode} - {equipment.CurrentRoom.RoomName}" : "Chưa gán / Lưu kho",
                Status = equipment.Status
            };

            return View(model);
        }

        // POST: Equipments/ProposeDisposal/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProposeDisposal(EquipmentProposeDisposalViewModel model)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var equipment = await _context.Equipments.FirstOrDefaultAsync(e => e.EquipmentId == model.EquipmentId && e.IsActive);
            if (equipment == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thiết bị hoặc thiết bị đã bị vô hiệu hóa.";
                return RedirectToAction(nameof(Index));
            }

            if (equipment.Status == "Disposed" || equipment.Status == "ProposedDisposal")
            {
                TempData["ErrorMessage"] = "Thiết bị đã ở trạng thái đề xuất thanh lý hoặc đã thanh lý.";
                return RedirectToAction(nameof(Details), new { id = equipment.EquipmentId });
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var originalStatus = equipment.Status;

                // Update Equipment Status
                equipment.Status = "ProposedDisposal";
                equipment.UpdatedBy = userId.Value;
                equipment.UpdatedAt = DateTime.Now;

                // Create Disposal Request
                var request = new DisposalRequest
                {
                    EquipmentId = equipment.EquipmentId,
                    ProposedBy = userId.Value,
                    Reason = model.Reason.Trim(),
                    Status = "Pending",
                    ProposedAt = DateTime.Now
                };

                _context.DisposalRequests.Add(request);
                await _context.SaveChangesAsync(); // Generates RequestId

                // Create manual status log detail if needed or let trigger handle it.
                // The trigger trg_Equipment_AfterUpdate will log the change to 'ProposedDisposal' automatically!
                // Wait, trigger logs: OldStatus = originalStatus, NewStatus = 'ProposedDisposal'

                // Create Notifications for Admins
                var admins = await _context.Users.Where(u => u.IsActive && u.Role == "Admin").ToListAsync();
                var technicianName = User.Identity?.Name ?? "Kỹ thuật viên";
                var title = "Yêu cầu phê duyệt thanh lý";
                var message = $"{technicianName} đã đề xuất thanh lý thiết bị {equipment.AssetCode} - {equipment.EquipmentName} với lý do: {model.Reason.Trim()}";

                foreach (var admin in admins)
                {
                    _context.Notifications.Add(new Notification
                    {
                        RecipientId = admin.UserId,
                        Title = title,
                        Message = message,
                        Type = "DisposalProposed",
                        IsRead = false,
                        SentAt = DateTime.Now,
                        RelatedEntityType = "DisposalRequest",
                        RelatedEntityId = request.DisposalId
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError(string.Empty, "Có lỗi xảy ra khi xử lý đề xuất. Vui lòng thử lại.");
                return View(model);
            }

            TempData["SuccessMessage"] = $"Đã gửi đề xuất thanh lý thiết bị {equipment.AssetCode} cho Ban quản lý.";
            return RedirectToAction(nameof(Details), new { id = equipment.EquipmentId });
        }

        // GET: Equipments/Disposals
        [HttpGet]
        public async Task<IActionResult> Disposals(string? status)
        {
            var query = _context.DisposalRequests
                .Include(d => d.Equipment)
                .Include(d => d.ProposedByNavigation)
                .Include(d => d.ApprovedByNavigation)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(d => d.Status == status);
            }

            var disposals = await query
                .OrderByDescending(d => d.ProposedAt)
                .ToListAsync();

            ViewData["SelectedStatus"] = status;
            return View(disposals);
        }

        // GET: Equipments/Transfers
        [HttpGet]
        public async Task<IActionResult> Transfers(string? search, int? categoryId, int? roomId)
        {
            var query = _context.Equipments
                .Include(e => e.Category)
                .Include(e => e.CurrentRoom)
                .Where(e => e.IsActive && e.Status != "Disposed" && e.Status != "ProposedDisposal")
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var cleanSearch = search.Trim();
                query = query.Where(e => e.EquipmentName.Contains(cleanSearch) || e.AssetCode.Contains(cleanSearch) || (e.SerialNumber != null && e.SerialNumber.Contains(cleanSearch)));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(e => e.CategoryId == categoryId.Value);
            }

            if (roomId.HasValue)
            {
                query = query.Where(e => e.CurrentRoomId == roomId.Value);
            }

            var equipments = await query
                .OrderBy(e => e.AssetCode)
                .Select(e => new EquipmentListItemViewModel
                {
                    EquipmentId = e.EquipmentId,
                    AssetCode = e.AssetCode,
                    EquipmentName = e.EquipmentName,
                    CategoryName = e.Category.CategoryName,
                    RoomDisplayName = e.CurrentRoom != null ? $"{e.CurrentRoom.RoomCode} - {e.CurrentRoom.RoomName}" : "Chưa gán / Lưu kho",
                    Status = e.Status,
                    WarrantyExpiry = e.WarrantyExpiry
                })
                .ToListAsync();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_TransferTableBody", equipments);
            }

            var model = new EquipmentListViewModel
            {
                Search = search,
                CategoryId = categoryId,
                RoomId = roomId,
                Equipments = equipments,
                Categories = await GetCategorySelectList(),
                Rooms = await GetRoomSelectList()
            };

            return View(model);
        }

        // GET: Equipments/Transfer/5
        [HttpGet]
        public async Task<IActionResult> Transfer(int id)
        {
            var equipment = await _context.Equipments
                .Include(e => e.CurrentRoom)
                .FirstOrDefaultAsync(e => e.EquipmentId == id && e.IsActive);

            if (equipment == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thiết bị hoặc thiết bị đã bị vô hiệu hóa.";
                return RedirectToAction(nameof(Transfers));
            }

            if (equipment.Status == "Disposed")
            {
                TempData["ErrorMessage"] = "Thiết bị này đã bị thanh lý, không thể điều chuyển.";
                return RedirectToAction(nameof(Transfers));
            }

            if (equipment.Status == "ProposedDisposal")
            {
                TempData["ErrorMessage"] = "Thiết bị này đang chờ duyệt thanh lý, không thể điều chuyển.";
                return RedirectToAction(nameof(Transfers));
            }

            var model = new EquipmentTransferViewModel
            {
                EquipmentId = equipment.EquipmentId,
                AssetCode = equipment.AssetCode,
                EquipmentName = equipment.EquipmentName,
                CurrentRoomId = equipment.CurrentRoomId,
                CurrentRoomDisplayName = equipment.CurrentRoom != null ? $"{equipment.CurrentRoom.RoomCode} - {equipment.CurrentRoom.RoomName}" : "Chưa gán / Lưu kho",
                Rooms = await _context.Rooms
                    .Where(r => r.IsActive && r.RoomId != equipment.CurrentRoomId)
                    .OrderBy(r => r.RoomCode)
                    .Select(r => new SelectListItem { Value = r.RoomId.ToString(), Text = $"{r.RoomCode} - {r.RoomName}" })
                    .ToListAsync()
            };

            return View(model);
        }

        // POST: Equipments/Transfer/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Transfer(EquipmentTransferViewModel model)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            if (!ModelState.IsValid)
            {
                model.Rooms = await _context.Rooms
                    .Where(r => r.IsActive && r.RoomId != model.CurrentRoomId)
                    .OrderBy(r => r.RoomCode)
                    .Select(r => new SelectListItem { Value = r.RoomId.ToString(), Text = $"{r.RoomCode} - {r.RoomName}" })
                    .ToListAsync();
                return View(model);
            }

            var equipment = await _context.Equipments.FirstOrDefaultAsync(e => e.EquipmentId == model.EquipmentId && e.IsActive);
            if (equipment == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thiết bị hoặc thiết bị đã bị vô hiệu hóa.";
                return RedirectToAction(nameof(Transfers));
            }

            if (equipment.Status == "Disposed" || equipment.Status == "ProposedDisposal")
            {
                TempData["ErrorMessage"] = "Thiết bị đang chờ thanh lý hoặc đã bị thanh lý, không thể điều chuyển.";
                return RedirectToAction(nameof(Transfers));
            }

            if (equipment.CurrentRoomId == model.ToRoomId)
            {
                ModelState.AddModelError(nameof(model.ToRoomId), "Phòng đích không được trùng với phòng học hiện tại.");
                model.Rooms = await _context.Rooms
                    .Where(r => r.IsActive && r.RoomId != model.CurrentRoomId)
                    .OrderBy(r => r.RoomCode)
                    .Select(r => new SelectListItem { Value = r.RoomId.ToString(), Text = $"{r.RoomCode} - {r.RoomName}" })
                    .ToListAsync();
                return View(model);
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var oldRoomId = equipment.CurrentRoomId;

                // Update Room ID
                equipment.CurrentRoomId = model.ToRoomId;
                equipment.UpdatedBy = userId.Value;
                equipment.UpdatedAt = DateTime.Now;

                // Create Transfer History
                var transfer = new TransferHistory
                {
                    EquipmentId = equipment.EquipmentId,
                    FromRoomId = oldRoomId,
                    ToRoomId = model.ToRoomId,
                    TransferredBy = userId.Value,
                    TransferDate = DateTime.Now,
                    Reason = model.Reason.Trim()
                };

                _context.TransferHistories.Add(transfer);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError(string.Empty, "Có lỗi xảy ra trong quá trình điều chuyển. Vui lòng thử lại.");
                model.Rooms = await _context.Rooms
                    .Where(r => r.IsActive && r.RoomId != model.CurrentRoomId)
                    .OrderBy(r => r.RoomCode)
                    .Select(r => new SelectListItem { Value = r.RoomId.ToString(), Text = $"{r.RoomCode} - {r.RoomName}" })
                    .ToListAsync();
                return View(model);
            }

            TempData["SuccessMessage"] = $"Điều chuyển thiết bị {equipment.AssetCode} thành công.";
            return RedirectToAction(nameof(Transfers));
        }

        private async Task<List<SelectListItem>> GetCategorySelectList()
        {
            return await _context.EquipmentCategories
                .OrderBy(c => c.CategoryName)
                .Select(c => new SelectListItem { Value = c.CategoryId.ToString(), Text = c.CategoryName })
                .ToListAsync();
        }

        private async Task<List<SelectListItem>> GetRoomSelectList()
        {
            return await _context.Rooms
                .Where(r => r.IsActive)
                .OrderBy(r => r.RoomCode)
                .Select(r => new SelectListItem { Value = r.RoomId.ToString(), Text = $"{r.RoomCode} - {r.RoomName}" })
                .ToListAsync();
        }

        private int? GetCurrentUserId()
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdValue, out var userId) ? userId : null;
        }
    }
}
