using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PRN_Project.Models;

using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PRN_Project.Models.ViewModels;

namespace PRN_Project.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _context;

        public HomeController(ILogger<HomeController> logger, AppDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }
            return null;
        }

        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            if (userId == null) return RedirectToAction("Login", "Auth");

            var model = new DashboardViewModel();

            if (User.IsInRole("Admin"))
            {
                model.AdminDashboard = new AdminDashboardViewModel
                {
                    TotalActiveEquipments = await _context.Equipments.CountAsync(e => e.IsActive && e.Status == "InUse"),
                    PendingIncidents = await _context.IncidentReports.CountAsync(i => i.Status == "Pending" || i.Status == "InProgress"),
                    UnderMaintenanceEquipments = await _context.Equipments.CountAsync(e => e.IsActive && e.Status == "UnderMaintenance"),
                    ActiveRooms = await _context.Rooms.CountAsync(r => r.IsActive)
                };
                
                var recentIncidents = await _context.IncidentReports
                    .OrderByDescending(i => i.ReportedAt)
                    .Take(3)
                    .Select(i => new ActivityItem
                    {
                        Type = "Incident",
                        EntityId = i.IncidentId,
                        Title = "Sự cố thiết bị",
                        Description = i.Description != null && i.Description.Length > 50 ? i.Description.Substring(0, 50) + "..." : (i.Description ?? ""),
                        TimeAgo = i.ReportedAt.ToString("dd/MM/yyyy HH:mm"),
                        IconClass = "bx-error",
                        ColorClass = "danger"
                    }).ToListAsync();
                    
                model.AdminDashboard.RecentActivities.AddRange(recentIncidents);
            }
            else if (User.IsInRole("Technician"))
            {
                model.TechnicianDashboard = new TechnicianDashboardViewModel
                {
                    AssignedPendingIncidents = await _context.IncidentReports.CountAsync(i => i.AssignedTo == userId && (i.Status == "Pending" || i.Status == "InProgress")),
                    ResolvedIncidents = await _context.IncidentReports.CountAsync(i => i.AssignedTo == userId && i.Status == "Resolved"),
                    TotalTransferred = await _context.TransferHistories.CountAsync(t => t.TransferredBy == userId),
                    PendingDisposals = await _context.DisposalRequests.CountAsync(d => d.ProposedBy == userId && d.Status == "Pending")
                };
                
                var assignedIncidents = await _context.IncidentReports
                    .Where(i => i.AssignedTo == userId && (i.Status == "Pending" || i.Status == "InProgress"))
                    .OrderByDescending(i => i.ReportedAt)
                    .Take(3)
                    .Select(i => new ActivityItem
                    {
                        Type = "Incident",
                        EntityId = i.IncidentId,
                        Title = "Sự cố được phân công",
                        Description = i.Description != null && i.Description.Length > 50 ? i.Description.Substring(0, 50) + "..." : (i.Description ?? ""),
                        TimeAgo = i.ReportedAt.ToString("dd/MM/yyyy HH:mm"),
                        IconClass = "bx-wrench",
                        ColorClass = "warning"
                    }).ToListAsync();
                    
                model.TechnicianDashboard.RecentActivities.AddRange(assignedIncidents);
            }
            else if (User.IsInRole("Lecturer"))
            {
                model.LecturerDashboard = new LecturerDashboardViewModel
                {
                    AssignedRooms = await _context.LecturerRooms.CountAsync(r => r.UserId == userId),
                    TotalReportedIncidents = await _context.IncidentReports.CountAsync(i => i.ReportedBy == userId),
                    PendingReportedIncidents = await _context.IncidentReports.CountAsync(i => i.ReportedBy == userId && (i.Status == "Pending" || i.Status == "InProgress"))
                };
                
                var reportedIncidents = await _context.IncidentReports
                    .Where(i => i.ReportedBy == userId)
                    .OrderByDescending(i => i.ReportedAt)
                    .Take(3)
                    .Select(i => new ActivityItem
                    {
                        Type = "Incident",
                        EntityId = i.IncidentId,
                        Title = "Sự cố đã báo cáo",
                        Description = i.Description != null && i.Description.Length > 50 ? i.Description.Substring(0, 50) + "..." : (i.Description ?? ""),
                        TimeAgo = i.ReportedAt.ToString("dd/MM/yyyy HH:mm"),
                        IconClass = "bx-info-circle",
                        ColorClass = "info"
                    }).ToListAsync();
                    
                model.LecturerDashboard.RecentActivities.AddRange(reportedIncidents);
            }

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
