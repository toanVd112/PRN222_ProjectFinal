using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PRN_Project.Models;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace PRN_Project.ViewComponents
{
    public class NotificationViewComponent : ViewComponent
    {
        private readonly AppDbContext _context;

        public NotificationViewComponent(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var userIdClaim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return View(new List<Notification>());
            }

            var notifications = await _context.Notifications
                .Where(n => n.RecipientId == userId)
                .OrderByDescending(n => n.SentAt)
                .Take(10) // Show last 10 notifications
                .ToListAsync();

            return View(notifications);
        }
    }
}
