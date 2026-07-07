using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PRN_Project.Models;

namespace PRN_Project.Controllers
{
    [Authorize(Roles = "Lecturer")]
    public class IncidentReportsController : Controller
    {
        private const string EquipmentStatusInUse = "InUse";
        private const string EquipmentStatusPendingRepair = "PendingRepair";
        private const string IncidentStatusPending = "Pending";

        private readonly AppDbContext _context;

        public IncidentReportsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var model = new LecturerIncidentHistoryViewModel
            {
                Incidents = await _context.IncidentReports
                    .AsNoTracking()
                    .Where(incident => incident.ReportedBy == userId.Value)
                    .OrderByDescending(incident => incident.ReportedAt)
                    .Select(incident => new LecturerIncidentHistoryItemViewModel
                    {
                        IncidentId = incident.IncidentId,
                        AssetCode = incident.Equipment.AssetCode,
                        EquipmentName = incident.Equipment.EquipmentName,
                        RoomDisplayName = incident.Room.RoomCode + " - " + incident.Room.RoomName,
                        Description = incident.Description,
                        Status = incident.Status,
                        ReportedAt = incident.ReportedAt,
                        ResolvedAt = incident.ResolvedAt
                    })
                    .ToListAsync()
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int equipmentId)
        {
            var equipment = await GetReportableEquipment(equipmentId);
            if (equipment == null)
            {
                TempData["ErrorMessage"] = "Thiết bị không tồn tại hoặc không còn nằm trong phòng học.";
                return RedirectToAction("Index", "LecturerRooms");
            }

            if (await HasPendingIncident(equipment.EquipmentId))
            {
                TempData["WarningMessage"] = "Thiết bị này đã có báo cáo sự cố đang chờ xử lý.";
                return RedirectToAction("Index", "LecturerRooms", new { roomId = equipment.CurrentRoomId });
            }

            if (equipment.Status != EquipmentStatusInUse)
            {
                TempData["WarningMessage"] = "Chỉ có thể báo hỏng thiết bị đang sử dụng bình thường.";
                return RedirectToAction("Index", "LecturerRooms", new { roomId = equipment.CurrentRoomId });
            }

            return View(CreateViewModel(equipment));
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var incident = await _context.IncidentReports
                .AsNoTracking()
                .Where(item => item.IncidentId == id && item.ReportedBy == userId.Value)
                .Select(item => new LecturerIncidentDetailViewModel
                {
                    IncidentId = item.IncidentId,
                    RoomId = item.RoomId,
                    AssetCode = item.Equipment.AssetCode,
                    EquipmentName = item.Equipment.EquipmentName,
                    CategoryName = item.Equipment.Category.CategoryName,
                    RoomDisplayName = item.Room.RoomCode + " - " + item.Room.RoomName,
                    Description = item.Description,
                    Status = item.Status,
                    ReportedAt = item.ReportedAt,
                    ResolvedAt = item.ResolvedAt,
                    ResolutionNote = item.ResolutionNote
                })
                .FirstOrDefaultAsync();

            if (incident == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy báo cáo sự cố hoặc bạn không có quyền xem báo cáo này.";
                return RedirectToAction("Index");
            }

            return View(incident);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SubmitIncidentViewModel model)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var equipment = await GetReportableEquipment(model.EquipmentId);
            if (equipment == null || equipment.CurrentRoomId != model.RoomId)
            {
                TempData["ErrorMessage"] = "Thiết bị không tồn tại hoặc không còn nằm trong phòng học.";
                return RedirectToAction("Index", "LecturerRooms");
            }

            ApplyEquipmentDetails(model, equipment);
            model.Description = model.Description?.Trim() ?? string.Empty;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (await HasPendingIncident(equipment.EquipmentId))
            {
                ModelState.AddModelError(string.Empty, "Thiết bị này đã có báo cáo sự cố đang chờ xử lý.");
                return View(model);
            }

            if (equipment.Status != EquipmentStatusInUse)
            {
                ModelState.AddModelError(string.Empty, "Chỉ có thể báo hỏng thiết bị đang sử dụng bình thường.");
                return View(model);
            }

            var incident = new IncidentReport
            {
                EquipmentId = equipment.EquipmentId,
                RoomId = equipment.CurrentRoomId!.Value,
                ReportedBy = userId.Value,
                Description = model.Description,
                Status = IncidentStatusPending,
                ReportedAt = DateTime.Now,
                IsOverdue = false
            };

            var oldStatus = equipment.Status;
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                equipment.Status = EquipmentStatusPendingRepair;
                equipment.UpdatedBy = userId.Value;
                equipment.UpdatedAt = DateTime.Now;

                _context.IncidentReports.Add(incident);
                _context.EquipmentStatusLogs.Add(new EquipmentStatusLog
                {
                    EquipmentId = equipment.EquipmentId,
                    ChangedBy = userId.Value,
                    OldStatus = oldStatus,
                    NewStatus = EquipmentStatusPendingRepair,
                    FieldChanged = "Status",
                    OldValue = oldStatus,
                    NewValue = EquipmentStatusPendingRepair,
                    ChangeReason = "Lecturer submitted an incident report.",
                    ChangedAt = DateTime.Now
                });

                await _context.SaveChangesAsync();
                AddTechnicianNotifications(incident, equipment);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            TempData["SuccessMessage"] = "Báo cáo sự cố đã được gửi cho bộ phận kỹ thuật.";
            return RedirectToAction("Index", "LecturerRooms", new { roomId = equipment.CurrentRoomId });
        }

        private int? GetCurrentUserId()
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdValue, out var userId) ? userId : null;
        }

        private Task<Equipment?> GetReportableEquipment(int equipmentId)
        {
            return _context.Equipments
                .Include(equipment => equipment.Category)
                .Include(equipment => equipment.CurrentRoom)
                .FirstOrDefaultAsync(equipment =>
                    equipment.EquipmentId == equipmentId &&
                    equipment.IsActive &&
                    equipment.CurrentRoomId != null &&
                    equipment.CurrentRoom != null &&
                    equipment.CurrentRoom.IsActive);
        }

        private Task<bool> HasPendingIncident(int equipmentId)
        {
            return _context.IncidentReports
                .AnyAsync(incident => incident.EquipmentId == equipmentId && incident.Status == IncidentStatusPending);
        }

        private SubmitIncidentViewModel CreateViewModel(Equipment equipment)
        {
            var model = new SubmitIncidentViewModel();
            ApplyEquipmentDetails(model, equipment);
            return model;
        }

        private static void ApplyEquipmentDetails(SubmitIncidentViewModel model, Equipment equipment)
        {
            model.EquipmentId = equipment.EquipmentId;
            model.RoomId = equipment.CurrentRoomId!.Value;
            model.AssetCode = equipment.AssetCode;
            model.EquipmentName = equipment.EquipmentName;
            model.CategoryName = equipment.Category.CategoryName;
            model.RoomDisplayName = $"{equipment.CurrentRoom!.RoomCode} - {equipment.CurrentRoom.RoomName}";
        }

        private void AddTechnicianNotifications(IncidentReport incident, Equipment equipment)
        {
            var technicianIds = _context.Users
                .Where(user => user.IsActive && user.Role == "Technician")
                .Select(user => user.UserId)
                .ToList();

            if (!technicianIds.Any())
            {
                return;
            }

            var lecturerName = User.Identity?.Name ?? "Giảng viên";
            var title = "Báo cáo sự cố mới";
            var message = $"{lecturerName} đã báo hỏng {equipment.AssetCode} - {equipment.EquipmentName} tại {equipment.CurrentRoom!.RoomCode}.";

            foreach (var technicianId in technicianIds)
            {
                _context.Notifications.Add(new Notification
                {
                    RecipientId = technicianId,
                    Title = title,
                    Message = message,
                    Type = "IncidentReported",
                    IsRead = false,
                    SentAt = DateTime.Now,
                    RelatedEntityType = "IncidentReport",
                    RelatedEntityId = incident.IncidentId
                });
            }
        }
    }
}
