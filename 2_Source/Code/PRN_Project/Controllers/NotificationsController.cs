using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PRN_Project.Models;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PRN_Project.Controllers
{
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly AppDbContext _context;

        public NotificationsController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Read(int id)
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.NotificationId == id && n.RecipientId == userId);

            if (notification == null)
            {
                return RedirectToAction("Index", "Home");
            }

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }

            // Redirect based on RelatedEntityType
            if (!string.IsNullOrEmpty(notification.RelatedEntityType) && notification.RelatedEntityId.HasValue)
            {
                switch (notification.RelatedEntityType)
                {
                    case "IncidentReport":
                        if (User.IsInRole("Admin") || User.IsInRole("Technician"))
                            return RedirectToAction("Details", "TechnicianIncidents", new { id = notification.RelatedEntityId.Value });
                        if (User.IsInRole("Lecturer"))
                            return RedirectToAction("Details", "IncidentReports", new { id = notification.RelatedEntityId.Value });
                        break;
                        
                    case "DisposalRequest":
                        return RedirectToAction("Disposals", "Equipments");
                        
                    case "TransferRequest":
                        return RedirectToAction("TransferRequests", "Equipments");
                }
            }

            return RedirectToAction("Index", "Home");
        }
    }
}
