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

            var userId = GetCurrentUserId();
            if (User.IsInRole("Technician") && !User.IsInRole("Admin") && userId.HasValue)
            {
                query = query.Where(i => i.AssignedTo == userId.Value);
            }

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
                .OrderBy(i => i.IncidentId)
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
                    .Select(r => new SelectListItem { Value = r.RoomId.ToString(), Text = $"{r.RoomCode} - {r.Location}" })
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

            if (User.IsInRole("Admin") && incident.Status == "Pending")
            {
                // Retrieve busy technicians who are currently handling an InProgress incident
                var busyTechnicianIds = await _context.IncidentReports
                    .Where(i => i.Status == "InProgress" && i.AssignedTo != null)
                    .Select(i => i.AssignedTo.Value)
                    .Distinct()
                    .ToListAsync();

                // Get available technicians
                var availableTechnicians = await _context.Users
                    .Where(u => u.IsActive && u.Role == "Technician" && !busyTechnicianIds.Contains(u.UserId))
                    .OrderBy(u => u.FullName)
                    .Select(u => new SelectListItem
                    {
                        Value = u.UserId.ToString(),
                        Text = u.FullName
                    })
                    .ToListAsync();

                ViewBag.AvailableTechnicians = availableTechnicians;
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

        // POST: TechnicianIncidents/Assign/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Assign(int id, int technicianId)
        {
            var incident = await _context.IncidentReports.FindAsync(id);
            if (incident == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy báo cáo sự cố.";
                return RedirectToAction(nameof(Index));
            }

            if (incident.Status != "Pending")
            {
                TempData["WarningMessage"] = "Sự cố này đã được tiếp nhận hoặc đã giải quyết, không thể phân công.";
                return RedirectToAction(nameof(Details), new { id = incident.IncidentId });
            }

            var technician = await _context.Users.FirstOrDefaultAsync(u => u.UserId == technicianId && u.IsActive && u.Role == "Technician");
            if (technician == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy Kỹ thuật viên hợp lệ.";
                return RedirectToAction(nameof(Details), new { id = incident.IncidentId });
            }

            // Assign the technician, keep status Pending
            incident.AssignedTo = technicianId;
            await _context.SaveChangesAsync();

            // Send notification
            _context.Notifications.Add(new Notification
            {
                RecipientId = technicianId,
                Title = "Phân công xử lý sự cố mới",
                Message = $"Bạn đã được phân công xử lý sự cố INC-#{incident.IncidentId}. Vui lòng kiểm tra và tiếp nhận yêu cầu.",
                Type = "IncidentAssigned",
                IsRead = false,
                SentAt = DateTime.Now,
                RelatedEntityType = "IncidentReport",
                RelatedEntityId = incident.IncidentId
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã phân công sự cố cho {technician.FullName} thành công.";
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

                // Send notification to Admins
                var admins = await _context.Users.Where(u => u.IsActive && u.Role == "Admin").ToListAsync();
                foreach (var admin in admins)
                {
                    _context.Notifications.Add(new Notification
                    {
                        RecipientId = admin.UserId,
                        Title = "Kỹ thuật viên đã xử lý xong sự cố",
                        Message = $"Kỹ thuật viên đã giải quyết xong sự cố cho thiết bị {incident.Equipment.AssetCode} tại phòng {incident.Room.RoomCode}.",
                        Type = "IncidentResolved",
                        IsRead = false,
                        SentAt = DateTime.Now,
                        RelatedEntityType = "IncidentReport",
                        RelatedEntityId = incident.IncidentId
                    });
                }

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


        private int? GetCurrentUserId()
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdValue, out var userId) ? userId : null;
        }
    }
}
