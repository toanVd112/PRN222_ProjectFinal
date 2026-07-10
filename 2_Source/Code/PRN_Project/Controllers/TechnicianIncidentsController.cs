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
    public class TechnicianIncidentsController : Controller
    {
        private readonly AppDbContext _context;

        public TechnicianIncidentsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: TechnicianIncidents
        [HttpGet]
        public async Task<IActionResult> Index(string? search, string? status, int? roomId)
        {
            var query = _context.IncidentReports
                .Include(i => i.Equipment)
                .Include(i => i.Room)
                .Include(i => i.ReportedByNavigation)
                .Include(i => i.AssignedToNavigation)
                .AsQueryable();

            // Apply search
            if (!string.IsNullOrWhiteSpace(search))
            {
                var cleanSearch = search.Trim();
                query = query.Where(i => i.Equipment.EquipmentName.Contains(cleanSearch) || 
                                         i.Equipment.AssetCode.Contains(cleanSearch) || 
                                         i.Room.RoomCode.Contains(cleanSearch) || 
                                         i.Description.Contains(cleanSearch));
            }

            // Apply filters
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(i => i.Status == status);
            }

            if (roomId.HasValue)
            {
                query = query.Where(i => i.RoomId == roomId.Value);
            }

            var incidents = await query
                .OrderByDescending(i => i.ReportedAt)
                .ToListAsync();

            // AJAX request check
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_IncidentTableBody", incidents);
            }

            var model = new TechnicianIncidentListViewModel
            {
                Search = search,
                Status = status,
                RoomId = roomId,
                Incidents = incidents,
                Rooms = await _context.Rooms
                    .Where(r => r.IsActive)
                    .OrderBy(r => r.RoomCode)
                    .Select(r => new SelectListItem { Value = r.RoomId.ToString(), Text = $"{r.RoomCode} - {r.RoomName}" })
                    .ToListAsync(),
                Statuses = new List<SelectListItem>
                {
                    new() { Value = "Pending", Text = "Chờ tiếp nhận (Pending)" },
                    new() { Value = "InProgress", Text = "Đang xử lý (InProgress)" },
                    new() { Value = "Resolved", Text = "Đã giải quyết (Resolved)" }
                }
            };

            return View(model);
        }

        // GET: TechnicianIncidents/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var incident = await _context.IncidentReports
                .Include(i => i.Equipment)
                    .ThenInclude(e => e.Category)
                .Include(i => i.Room)
                .Include(i => i.ReportedByNavigation)
                .Include(i => i.AssignedToNavigation)
                .Include(i => i.MaintenanceTicket)
                    .ThenInclude(t => t.CreatedByNavigation)
                .FirstOrDefaultAsync(i => i.IncidentId == id);

            if (incident == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy báo cáo sự cố.";
                return RedirectToAction(nameof(Index));
            }

            return View(incident);
        }

        // POST: TechnicianIncidents/Accept/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Accept(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var incident = await _context.IncidentReports.FindAsync(id);
            if (incident == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy báo cáo sự cố.";
                return RedirectToAction(nameof(Index));
            }

            if (incident.Status != "Pending")
            {
                TempData["WarningMessage"] = "Sự cố này đã được tiếp nhận từ trước.";
                return RedirectToAction(nameof(Details), new { id = incident.IncidentId });
            }

            incident.Status = "InProgress";
            incident.AssignedTo = userId.Value;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã tiếp nhận sự cố. Vui lòng tiến hành kiểm tra thiết bị.";
            return RedirectToAction(nameof(Details), new { id = incident.IncidentId });
        }

        // POST: TechnicianIncidents/Resolve/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Resolve(int id, string resolutionNote)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            if (string.IsNullOrWhiteSpace(resolutionNote))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập mô tả kết quả xử lý sự cố.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var incident = await _context.IncidentReports
                .Include(i => i.Equipment)
                .Include(i => i.Room)
                .FirstOrDefaultAsync(i => i.IncidentId == id);

            if (incident == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy báo cáo sự cố.";
                return RedirectToAction(nameof(Index));
            }

            if (incident.Status != "InProgress")
            {
                TempData["WarningMessage"] = "Chỉ có thể giải quyết các sự cố đang trong quá trình xử lý.";
                return RedirectToAction(nameof(Details), new { id = incident.IncidentId });
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var originalStatus = incident.Equipment.Status;

                // Update Incident
                incident.Status = "Resolved";
                incident.ResolvedAt = DateTime.Now;
                incident.ResolutionNote = resolutionNote.Trim();

                // Update Equipment back to InUse
                incident.Equipment.Status = "InUse";
                incident.Equipment.UpdatedBy = userId.Value;
                incident.Equipment.UpdatedAt = DateTime.Now;

                // Add Status log for equipment
                _context.EquipmentStatusLogs.Add(new EquipmentStatusLog
                {
                    EquipmentId = incident.EquipmentId,
                    ChangedBy = userId.Value,
                    OldStatus = originalStatus,
                    NewStatus = "InUse",
                    FieldChanged = "Status",
                    OldValue = originalStatus,
                    NewValue = "InUse",
                    ChangeReason = $"Kỹ thuật viên giải quyết sự cố #{incident.IncidentId}: {resolutionNote.Trim()}",
                    ChangedAt = DateTime.Now
                });

                // Send notification to the Lecturer who reported
                _context.Notifications.Add(new Notification
                {
                    RecipientId = incident.ReportedBy,
                    Title = "Sự cố phòng học đã xử lý xong",
                    Message = $"Báo cáo sự cố cho thiết bị {incident.Equipment.AssetCode} tại phòng {incident.Room.RoomCode} đã hoàn thành: {resolutionNote.Trim()}",
                    Type = "IncidentResolved",
                    IsRead = false,
                    SentAt = DateTime.Now,
                    RelatedEntityType = "IncidentReport",
                    RelatedEntityId = incident.IncidentId
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi cập nhật kết quả xử lý. Vui lòng thử lại.";
                return RedirectToAction(nameof(Details), new { id });
            }

            TempData["SuccessMessage"] = "Đã hoàn thành xử lý sự cố và bàn giao lại thiết bị.";
            return RedirectToAction(nameof(Details), new { id = incident.IncidentId });
        }

        // GET: TechnicianIncidents/CreateTicket/5
        [HttpGet]
        public async Task<IActionResult> CreateTicket(int id)
        {
            var incident = await _context.IncidentReports
                .Include(i => i.Equipment)
                .FirstOrDefaultAsync(i => i.IncidentId == id);

            if (incident == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy báo cáo sự cố.";
                return RedirectToAction(nameof(Index));
            }

            if (incident.Status != "InProgress")
            {
                TempData["WarningMessage"] = "Phải tiếp nhận sự cố trước khi lập phiếu bảo trì ngoại.";
                return RedirectToAction(nameof(Details), new { id = incident.IncidentId });
            }

            if (incident.Equipment.Status == "UnderMaintenance")
            {
                TempData["WarningMessage"] = "Thiết bị này hiện đã nằm trong phiếu bảo trì khác.";
                return RedirectToAction(nameof(Details), new { id = incident.IncidentId });
            }

            var model = new MaintenanceTicketViewModel
            {
                IncidentId = incident.IncidentId,
                EquipmentId = incident.EquipmentId,
                AssetCode = incident.Equipment.AssetCode,
                EquipmentName = incident.Equipment.EquipmentName,
                ExpectedReturnDate = DateOnly.FromDateTime(DateTime.Today.AddDays(7)) // default to 7 days later
            };

            return View(model);
        }

        // POST: TechnicianIncidents/CreateTicket/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTicket(MaintenanceTicketViewModel model)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            if (model.ExpectedReturnDate <= DateOnly.FromDateTime(DateTime.Today))
            {
                ModelState.AddModelError(nameof(model.ExpectedReturnDate), "Ngày dự kiến trả máy phải ở trong tương lai.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var incident = await _context.IncidentReports
                .Include(i => i.Equipment)
                .FirstOrDefaultAsync(i => i.IncidentId == model.IncidentId);

            if (incident == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy báo cáo sự cố.";
                return RedirectToAction(nameof(Index));
            }

            if (incident.Status != "InProgress")
            {
                TempData["ErrorMessage"] = "Hành động không hợp lệ. Trạng thái sự cố không ở tiến trình xử lý.";
                return RedirectToAction(nameof(Details), new { id = incident.IncidentId });
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var originalStatus = incident.Equipment.Status;

                // Create Maintenance Ticket
                var ticket = new MaintenanceTicket
                {
                    IncidentId = model.IncidentId,
                    EquipmentId = model.EquipmentId,
                    CreatedBy = userId.Value,
                    ServiceProvider = model.ServiceProvider.Trim(),
                    EstimatedCost = model.EstimatedCost,
                    SentDate = DateOnly.FromDateTime(DateTime.Today),
                    ExpectedReturnDate = model.ExpectedReturnDate,
                    Status = "UnderMaintenance",
                    CreatedAt = DateTime.Now
                };

                _context.MaintenanceTickets.Add(ticket);

                // Update Equipment Status to UnderMaintenance
                incident.Equipment.Status = "UnderMaintenance";
                incident.Equipment.UpdatedBy = userId.Value;
                incident.Equipment.UpdatedAt = DateTime.Now;

                // Add Status log for equipment
                _context.EquipmentStatusLogs.Add(new EquipmentStatusLog
                {
                    EquipmentId = incident.EquipmentId,
                    ChangedBy = userId.Value,
                    OldStatus = originalStatus,
                    NewStatus = "UnderMaintenance",
                    FieldChanged = "Status",
                    OldValue = originalStatus,
                    NewValue = "UnderMaintenance",
                    ChangeReason = $"Thiết bị được gửi đi sửa chữa ngoài (Đơn vị: {model.ServiceProvider.Trim()}). Phiếu sửa chữa #{ticket.TicketId}",
                    ChangedAt = DateTime.Now
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError(string.Empty, "Có lỗi xảy ra khi tạo phiếu bảo trì. Vui lòng thử lại.");
                return View(model);
            }

            TempData["SuccessMessage"] = $"Lập phiếu bảo trì và gửi thiết bị đi sửa chữa tại {model.ServiceProvider} thành công.";
            return RedirectToAction(nameof(Details), new { id = model.IncidentId });
        }

        // POST: TechnicianIncidents/CompleteTicket/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteTicket(int incidentId, DateOnly actualReturnDate, decimal actualCost, string? notes)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var incident = await _context.IncidentReports
                .Include(i => i.Equipment)
                .Include(i => i.Room)
                .Include(i => i.MaintenanceTicket)
                .FirstOrDefaultAsync(i => i.IncidentId == incidentId);

            if (incident == null || incident.MaintenanceTicket == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin phiếu bảo trì tương ứng.";
                return RedirectToAction(nameof(Index));
            }

            if (incident.MaintenanceTicket.Status != "UnderMaintenance")
            {
                TempData["WarningMessage"] = "Thiết bị này đã được xác nhận hoàn thành bảo trì từ trước.";
                return RedirectToAction(nameof(Details), new { id = incident.IncidentId });
            }

            if (actualReturnDate < incident.MaintenanceTicket.SentDate)
            {
                TempData["ErrorMessage"] = $"Ngày nhận lại máy không được trước ngày gửi đi ({incident.MaintenanceTicket.SentDate:dd/MM/yyyy}).";
                return RedirectToAction(nameof(Details), new { id = incident.IncidentId });
            }

            if (actualCost < 0)
            {
                TempData["ErrorMessage"] = "Chi phí thực tế không được âm.";
                return RedirectToAction(nameof(Details), new { id = incident.IncidentId });
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var originalStatus = incident.Equipment.Status;

                // Update Maintenance Ticket
                incident.MaintenanceTicket.ActualReturnDate = actualReturnDate;
                incident.MaintenanceTicket.ActualCost = actualCost;
                incident.MaintenanceTicket.Status = "Returned";
                incident.MaintenanceTicket.Notes = notes?.Trim();

                // Update Equipment to InUse
                incident.Equipment.Status = "InUse";
                incident.Equipment.UpdatedBy = userId.Value;
                incident.Equipment.UpdatedAt = DateTime.Now;

                // Add Status log for equipment
                _context.EquipmentStatusLogs.Add(new EquipmentStatusLog
                {
                    EquipmentId = incident.EquipmentId,
                    ChangedBy = userId.Value,
                    OldStatus = originalStatus,
                    NewStatus = "InUse",
                    FieldChanged = "Status",
                    OldValue = originalStatus,
                    NewValue = "InUse",
                    ChangeReason = $"Nhận lại thiết bị từ đơn vị bảo trì {incident.MaintenanceTicket.ServiceProvider}. Chi phí: {actualCost:N0} VNĐ.",
                    ChangedAt = DateTime.Now
                });

                // Resolve the Incident Report
                incident.Status = "Resolved";
                incident.ResolvedAt = DateTime.Now;
                incident.ResolutionNote = $"Nhận máy hoàn thành sửa chữa ngoài từ đơn vị {incident.MaintenanceTicket.ServiceProvider}. Chi phí thực tế: {actualCost:N0} VNĐ. Ghi chú bảo trì: {notes}";

                // Send notification to the Lecturer who reported
                _context.Notifications.Add(new Notification
                {
                    RecipientId = incident.ReportedBy,
                    Title = "Sự cố phòng học đã xử lý xong (Bảo trì ngoài)",
                    Message = $"Thiết bị {incident.Equipment.AssetCode} tại phòng {incident.Room.RoomCode} đã được bảo trì xong và đưa trở lại hoạt động.",
                    Type = "IncidentResolved",
                    IsRead = false,
                    SentAt = DateTime.Now,
                    RelatedEntityType = "IncidentReport",
                    RelatedEntityId = incident.IncidentId
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi hoàn tất bảo trì. Vui lòng thử lại.";
                return RedirectToAction(nameof(Details), new { id = incident.IncidentId });
            }

            TempData["SuccessMessage"] = "Đã nhận thiết bị trả về và hoàn tất xử lý sự cố.";
            return RedirectToAction(nameof(Details), new { id = incident.IncidentId });
        }

        private int? GetCurrentUserId()
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdValue, out var userId) ? userId : null;
        }
    }
}
