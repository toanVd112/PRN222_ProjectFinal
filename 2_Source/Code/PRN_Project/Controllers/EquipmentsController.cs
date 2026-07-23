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
        public async Task<IActionResult> Index(string? search, int? categoryId, int? roomId, string? status, int page = 1)
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
                if (roomId.Value == 0)
                {
                    query = query.Where(e => e.CurrentRoomId == null);
                }
                else
                {
                    query = query.Where(e => e.CurrentRoomId == roomId.Value);
                }
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(e => e.Status == status);
            }

            int pageSize = 10;
            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            if (page > totalPages && totalPages > 0) page = totalPages;
            if (page < 1) page = 1;

            var equipments = await query
                .OrderBy(e => 
                    e.Status == "PendingRepair" ? 1 :
                    e.Status == "ProposedDisposal" ? 2 :
                    e.Status == "InUse" ? 3 :
                    e.Status == "Disposed" ? 4 : 5)
                .ThenBy(e => e.AssetCode)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(e => new EquipmentListItemViewModel
                {
                    EquipmentId = e.EquipmentId,
                    AssetCode = e.AssetCode,
                    EquipmentName = e.EquipmentName,
                    CategoryName = e.Category.CategoryName,
                    RoomDisplayName = e.CurrentRoom != null ? e.CurrentRoom.RoomCode == e.CurrentRoom.Location ? e.CurrentRoom.RoomCode : $"{e.CurrentRoom.RoomCode} - {e.CurrentRoom.Location}" : "Chưa gán / Lưu kho",
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
                CurrentPage = page,
                TotalPages = totalPages,
                PageSize = pageSize,
                Equipments = equipments,
                Categories = await _context.EquipmentCategories
                    .OrderBy(c => c.CategoryName)
                    .Select(c => new SelectListItem { Value = c.CategoryId.ToString(), Text = c.CategoryName })
                    .ToListAsync(),
                Rooms = await _context.Rooms
                    .Where(r => r.IsActive)
                    .OrderBy(r => r.RoomCode)
                    .Select(r => new SelectListItem { Value = r.RoomId.ToString(), Text = r.RoomCode == r.Location ? r.RoomCode : $"{r.RoomCode} - {r.Location}" })
                    .ToListAsync(),
                Statuses = new List<SelectListItem>
                {
                    new() { Value = "InUse", Text = "Đang sử dụng" },
                    new() { Value = "PendingRepair", Text = "Chờ sửa chữa" },
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
        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin")]
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

                var statusLog = new EquipmentStatusLog
                {
                    EquipmentId = equipment.EquipmentId,
                    ChangedBy = userId.Value,
                    OldStatus = null,
                    NewStatus = equipment.Status,
                    FieldChanged = "Status",
                    OldValue = null,
                    NewValue = equipment.Status,
                    ChangeReason = "Thêm mới thiết bị vào hệ thống.",
                    ChangedAt = DateTime.Now
                };
                _context.EquipmentStatusLogs.Add(statusLog);
                await _context.SaveChangesAsync();

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
        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin")]
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
                RoomDisplayName = equipment.CurrentRoom != null ? equipment.CurrentRoom.RoomCode == equipment.CurrentRoom.Location ? equipment.CurrentRoom.RoomCode : $"{equipment.CurrentRoom.RoomCode} - {equipment.CurrentRoom.Location}" : "Chưa gán / Lưu kho",
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
                var isAdmin = User.IsInRole("Admin");

                if (isAdmin)
                {
                    equipment.Status = "Disposed";
                    equipment.CurrentRoomId = null; // Remove from room
                    equipment.UpdatedBy = userId.Value;
                    equipment.UpdatedAt = DateTime.Now;

                    _context.EquipmentStatusLogs.Add(new EquipmentStatusLog
                    {
                        EquipmentId = equipment.EquipmentId,
                        ChangedBy = userId.Value,
                        OldStatus = originalStatus,
                        NewStatus = "Disposed",
                        FieldChanged = "Status",
                        OldValue = originalStatus,
                        NewValue = "Disposed",
                        ChangeReason = model.Reason.Trim(),
                        ChangedAt = DateTime.Now
                    });
                    
                    await _context.SaveChangesAsync();
                }
                else
                {
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
                    
                    _context.EquipmentStatusLogs.Add(new EquipmentStatusLog
                    {
                        EquipmentId = equipment.EquipmentId,
                        ChangedBy = userId.Value,
                        OldStatus = originalStatus,
                        NewStatus = "ProposedDisposal",
                        FieldChanged = "Status",
                        OldValue = originalStatus,
                        NewValue = "ProposedDisposal",
                        ChangeReason = model.Reason.Trim(),
                        ChangedAt = DateTime.Now
                    });

                    await _context.SaveChangesAsync(); // Generates RequestId

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
                }
                
                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError(string.Empty, "Có lỗi xảy ra khi xử lý. Vui lòng thử lại.");
                return View(model);
            }

            if (User.IsInRole("Admin"))
            {
                TempData["SuccessMessage"] = $"Đã thanh lý thiết bị {equipment.AssetCode} thành công.";
            }
            else
            {
                TempData["SuccessMessage"] = $"Đã gửi đề xuất thanh lý thiết bị {equipment.AssetCode} cho Ban quản lý.";
            }
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

        // POST: Equipments/ReviewDisposalRequest
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ReviewDisposalRequest(ReviewDisposalRequestViewModel model)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Auth");

            var request = await _context.DisposalRequests
                .Include(r => r.Equipment)
                .FirstOrDefaultAsync(r => r.DisposalId == model.DisposalId);

            if (request == null)
            {
                return NotFound();
            }

            if (request.Status != "Pending")
            {
                TempData["ErrorMessage"] = "Yêu cầu này đã được xử lý trước đó.";
                return RedirectToAction(nameof(Disposals));
            }

            var originalStatus = request.Equipment.Status;

            if (model.Action == "Approve")
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    request.Status = "Approved";
                    request.ApprovedBy = userId;
                    request.DecidedAt = DateTime.Now;
                    request.AdminNote = model.AdminNote?.Trim();

                    request.Equipment.Status = "Disposed";
                    request.Equipment.CurrentRoomId = null; // Remove from room
                    request.Equipment.UpdatedBy = userId.Value;
                    request.Equipment.UpdatedAt = DateTime.Now;

                    _context.EquipmentStatusLogs.Add(new EquipmentStatusLog
                    {
                        EquipmentId = request.EquipmentId,
                        ChangedBy = userId.Value,
                        OldStatus = originalStatus,
                        NewStatus = "Disposed",
                        FieldChanged = "Status",
                        OldValue = originalStatus,
                        NewValue = "Disposed",
                        ChangeReason = request.Reason != null && request.Reason.Length > 450 ? request.Reason.Substring(0, 450) + "..." : request.Reason,
                        ChangedAt = DateTime.Now
                    });

                    _context.Notifications.Add(new Notification
                    {
                        RecipientId = request.ProposedBy,
                        Title = "Đề xuất thanh lý được duyệt",
                        Message = $"Đề xuất thanh lý thiết bị {request.Equipment.AssetCode} của bạn đã được duyệt.",
                        Type = "DisposalDecided",
                        IsRead = false,
                        SentAt = DateTime.Now,
                        RelatedEntityType = "DisposalRequest",
                        RelatedEntityId = request.DisposalId
                    });

                    // Resolve any active incidents for this equipment
                    var activeIncidents = await _context.IncidentReports
                        .Where(i => i.EquipmentId == request.EquipmentId && (i.Status == "Pending" || i.Status == "InProgress"))
                        .ToListAsync();
                    
                    foreach (var incident in activeIncidents)
                    {
                        incident.Status = "Resolved";
                        incident.ResolvedAt = DateTime.Now;
                        incident.ResolutionNote = "Thiết bị đã được thanh lý.";

                        _context.Notifications.Add(new Notification
                        {
                            RecipientId = incident.ReportedBy,
                            Title = "Sự cố phòng học đã kết thúc",
                            Message = $"Báo cáo sự cố cho thiết bị {request.Equipment.AssetCode} đã đóng do thiết bị được thanh lý.",
                            Type = "IncidentResolved",
                            IsRead = false,
                            SentAt = DateTime.Now,
                            RelatedEntityType = "IncidentReport",
                            RelatedEntityId = incident.IncidentId
                        });
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["SuccessMessage"] = "Đã duyệt yêu cầu thanh lý thành công.";
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = "Có lỗi xảy ra khi duyệt yêu cầu: " + ex.Message + (ex.InnerException != null ? " - " + ex.InnerException.Message : "");
                }
            }
            else if (model.Action == "Reject")
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    request.Status = "Rejected";
                    request.ApprovedBy = userId;
                    request.DecidedAt = DateTime.Now;
                    request.AdminNote = model.AdminNote?.Trim();

                    request.Equipment.Status = "InUse"; // Reset back to InUse
                    request.Equipment.UpdatedBy = userId.Value;
                    request.Equipment.UpdatedAt = DateTime.Now;
                    
                    _context.EquipmentStatusLogs.Add(new EquipmentStatusLog
                    {
                        EquipmentId = request.EquipmentId,
                        ChangedBy = userId.Value,
                        OldStatus = originalStatus,
                        NewStatus = "InUse",
                        FieldChanged = "Status",
                        OldValue = originalStatus,
                        NewValue = "InUse",
                        ChangeReason = "Từ chối đề xuất thanh lý",
                        ChangedAt = DateTime.Now
                    });

                    _context.Notifications.Add(new Notification
                    {
                        RecipientId = request.ProposedBy,
                        Title = "Đề xuất thanh lý bị từ chối",
                        Message = $"Đề xuất thanh lý thiết bị {request.Equipment.AssetCode} của bạn đã bị từ chối.",
                        Type = "DisposalDecided",
                        IsRead = false,
                        SentAt = DateTime.Now,
                        RelatedEntityType = "DisposalRequest",
                        RelatedEntityId = request.DisposalId
                    });

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["ErrorMessage"] = "Đã từ chối yêu cầu thanh lý.";
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = "Có lỗi xảy ra khi từ chối yêu cầu.";
                }
            }

            return RedirectToAction(nameof(Disposals));
        }

        // GET: Equipments/Transfers
        [HttpGet]
        [Authorize(Roles = "Technician")]
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
                    RoomDisplayName = e.CurrentRoom != null ? e.CurrentRoom.RoomCode == e.CurrentRoom.Location ? e.CurrentRoom.RoomCode : $"{e.CurrentRoom.RoomCode} - {e.CurrentRoom.Location}" : "Chưa gán / Lưu kho",
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

        // GET: Equipments/ProposeTransfer/5
        [HttpGet]
        [Authorize(Roles = "Technician")]
        public async Task<IActionResult> ProposeTransfer(int id)
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

            var roomsList = await _context.Rooms
                .Where(r => r.IsActive && r.RoomId != equipment.CurrentRoomId)
                .OrderBy(r => r.RoomCode)
                .Select(r => new SelectListItem { Value = r.RoomId.ToString(), Text = r.RoomCode == r.Location ? r.RoomCode : $"{r.RoomCode} - {r.Location}" })
                .ToListAsync();
            
            if (equipment.CurrentRoomId != null)
            {
                roomsList.Insert(0, new SelectListItem { Value = "", Text = "Kho / Chờ xử lý (Không chọn phòng đích)" });
            }
            
            var model = new ProposeTransferViewModel
            {
                EquipmentId = equipment.EquipmentId,
                AssetCode = equipment.AssetCode,
                EquipmentName = equipment.EquipmentName,
                CurrentRoomId = equipment.CurrentRoomId,
                CurrentRoomDisplayName = equipment.CurrentRoom != null ? equipment.CurrentRoom.RoomCode == equipment.CurrentRoom.Location ? equipment.CurrentRoom.RoomCode : $"{equipment.CurrentRoom.RoomCode} - {equipment.CurrentRoom.Location}" : "Chưa gán / Lưu kho",
                Rooms = roomsList
            };

            return View(model);
        }

        // POST: Equipments/ProposeTransfer/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Technician")]
        public async Task<IActionResult> ProposeTransfer(ProposeTransferViewModel model)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            if (!ModelState.IsValid)
            {
                var roomsList = await _context.Rooms
                    .Where(r => r.IsActive && r.RoomId != model.CurrentRoomId)
                    .OrderBy(r => r.RoomCode)
                    .Select(r => new SelectListItem { Value = r.RoomId.ToString(), Text = r.RoomCode == r.Location ? r.RoomCode : $"{r.RoomCode} - {r.Location}" })
                    .ToListAsync();

                if (model.CurrentRoomId != null)
                {
                    roomsList.Insert(0, new SelectListItem { Value = "", Text = "Kho / Chờ xử lý (Không chọn phòng đích)" });
                }
                
                model.Rooms = roomsList;
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
                var roomsList = await _context.Rooms
                    .Where(r => r.IsActive && r.RoomId != model.CurrentRoomId)
                    .OrderBy(r => r.RoomCode)
                    .Select(r => new SelectListItem { Value = r.RoomId.ToString(), Text = r.RoomCode == r.Location ? r.RoomCode : $"{r.RoomCode} - {r.Location}" })
                    .ToListAsync();

                if (model.CurrentRoomId != null)
                {
                    roomsList.Insert(0, new SelectListItem { Value = "", Text = "Kho / Chờ xử lý (Không chọn phòng đích)" });
                }
                
                model.Rooms = roomsList;
                return View(model);
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var request = new TransferRequest
                {
                    EquipmentId = equipment.EquipmentId,
                    FromRoomId = equipment.CurrentRoomId,
                    ToRoomId = model.ToRoomId,
                    ProposedBy = userId.Value,
                    Reason = model.Reason.Trim(),
                    Status = "Pending",
                    ProposedAt = DateTime.Now
                };

                _context.TransferRequests.Add(request);
                await _context.SaveChangesAsync();

                // Create Notifications for Admins
                var admins = await _context.Users.Where(u => u.IsActive && u.Role == "Admin").ToListAsync();
                var technicianName = User.Identity?.Name ?? "Kỹ thuật viên";
                var title = "Yêu cầu luân chuyển thiết bị mới";
                var message = $"{technicianName} đã đề xuất luân chuyển thiết bị {equipment.AssetCode} sang phòng khác.";

                foreach (var admin in admins)
                {
                    _context.Notifications.Add(new Notification
                    {
                        RecipientId = admin.UserId,
                        Title = title,
                        Message = message,
                        Type = "TransferProposed",
                        IsRead = false,
                        SentAt = DateTime.Now,
                        RelatedEntityType = "TransferRequest",
                        RelatedEntityId = request.RequestId
                    });
                }
                
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
                    .Select(r => new SelectListItem { Value = r.RoomId.ToString(), Text = r.RoomCode == r.Location ? r.RoomCode : $"{r.RoomCode} - {r.Location}" })
                    .ToListAsync();
                return View(model);
            }

            TempData["SuccessMessage"] = $"Đã gửi đề xuất luân chuyển thiết bị {equipment.AssetCode} thành công. Vui lòng chờ phê duyệt.";
            return RedirectToAction(nameof(Transfers));
        }

        // GET: Equipments/TransferRequests
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> TransferRequests(string? status)
        {
            var query = _context.TransferRequests
                .Include(r => r.Equipment)
                .Include(r => r.FromRoom)
                .Include(r => r.ToRoom)
                .Include(r => r.ProposedByNavigation)
                .Include(r => r.ApprovedByNavigation)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(r => r.Status == status);
            }

            var requests = await query
                .OrderByDescending(r => r.ProposedAt)
                .Select(r => new TransferRequestListItemViewModel
                {
                    RequestId = r.RequestId,
                    EquipmentId = r.EquipmentId,
                    AssetCode = r.Equipment.AssetCode,
                    EquipmentName = r.Equipment.EquipmentName,
                    FromRoomDisplayName = r.FromRoom != null ? r.FromRoom.RoomCode == r.FromRoom.Location ? r.FromRoom.RoomCode : $"{r.FromRoom.RoomCode} - {r.FromRoom.Location}" : "Chưa gán / Lưu kho",
                    ToRoomDisplayName = r.ToRoom != null ? r.ToRoom.RoomCode == r.ToRoom.Location ? r.ToRoom.RoomCode : $"{r.ToRoom.RoomCode} - {r.ToRoom.Location}" : "Kho / Chờ xử lý",
                    ProposedBy = r.ProposedByNavigation.FullName,
                    ProposedAt = r.ProposedAt,
                    Reason = r.Reason,
                    Status = r.Status,
                    AdminNote = r.AdminNote,
                    DecidedAt = r.DecidedAt,
                    ApprovedBy = r.ApprovedByNavigation != null ? r.ApprovedByNavigation.FullName : null
                })
                .ToListAsync();

            var model = new TransferRequestListViewModel
            {
                Requests = requests,
                StatusFilter = status
            };

            return View(model);
        }

        // POST: Equipments/ReviewTransferRequest
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ReviewTransferRequest(ReviewTransferRequestViewModel model)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Auth");

            var request = await _context.TransferRequests
                .Include(r => r.Equipment)
                .FirstOrDefaultAsync(r => r.RequestId == model.RequestId);

            if (request == null || request.Status != "Pending")
            {
                TempData["ErrorMessage"] = "Yêu cầu không tồn tại hoặc đã được xử lý.";
                return RedirectToAction(nameof(TransferRequests));
            }

            if (model.Action == "Approve")
            {
                if (request.Equipment.Status == "Disposed" || request.Equipment.Status == "ProposedDisposal")
                {
                    TempData["ErrorMessage"] = "Không thể duyệt vì thiết bị đang chờ thanh lý hoặc đã bị thanh lý.";
                    return RedirectToAction(nameof(TransferRequests));
                }

                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    request.Status = "Approved";
                    request.ApprovedBy = userId;
                    request.DecidedAt = DateTime.Now;
                    request.AdminNote = model.AdminNote?.Trim();

                    var oldRoomId = request.Equipment.CurrentRoomId;
                    request.Equipment.CurrentRoomId = request.ToRoomId;
                    request.Equipment.UpdatedBy = userId.Value;
                    request.Equipment.UpdatedAt = DateTime.Now;

                    var transfer = new TransferHistory
                    {
                        EquipmentId = request.EquipmentId,
                        FromRoomId = oldRoomId,
                        ToRoomId = request.ToRoomId,
                        TransferredBy = userId.Value,
                        TransferDate = DateTime.Now,
                        Reason = request.Reason
                    };

                    _context.TransferHistories.Add(transfer);
                    
                    _context.Notifications.Add(new Notification
                    {
                        RecipientId = request.ProposedBy,
                        Title = "Đề xuất luân chuyển được duyệt",
                        Message = $"Đề xuất luân chuyển thiết bị {request.Equipment.AssetCode} của bạn đã được duyệt.",
                        Type = "TransferApproved",
                        IsRead = false,
                        SentAt = DateTime.Now,
                        RelatedEntityType = "TransferRequest",
                        RelatedEntityId = request.RequestId
                    });

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["SuccessMessage"] = "Đã duyệt yêu cầu luân chuyển thành công.";
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = "Có lỗi xảy ra khi duyệt yêu cầu.";
                }
            }
            else if (model.Action == "Reject")
            {
                request.Status = "Rejected";
                request.ApprovedBy = userId;
                request.DecidedAt = DateTime.Now;
                request.AdminNote = model.AdminNote?.Trim();
                
                _context.Notifications.Add(new Notification
                {
                    RecipientId = request.ProposedBy,
                    Title = "Đề xuất luân chuyển bị từ chối",
                    Message = $"Đề xuất luân chuyển thiết bị {request.Equipment.AssetCode} của bạn đã bị từ chối.",
                    Type = "TransferRejected",
                    IsRead = false,
                    SentAt = DateTime.Now,
                    RelatedEntityType = "TransferRequest",
                    RelatedEntityId = request.RequestId
                });

                await _context.SaveChangesAsync();
                TempData["ErrorMessage"] = "Đã từ chối yêu cầu luân chuyển.";
            }

            return RedirectToAction(nameof(TransferRequests));
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
                .Select(r => new SelectListItem { Value = r.RoomId.ToString(), Text = $"{r.RoomCode} - {r.Location}" })
                .ToListAsync();
        }

        private int? GetCurrentUserId()
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdValue, out var userId) ? userId : null;
        }
    }
}
