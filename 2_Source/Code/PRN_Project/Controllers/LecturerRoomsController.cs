using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PRN_Project.Models;

namespace PRN_Project.Controllers
{
    [Authorize(Roles = "Lecturer")]
    public class LecturerRoomsController : Controller
    {
        private readonly AppDbContext _context;

        public LecturerRoomsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int? roomId)
        {
            var activeRooms = await _context.Rooms
                .AsNoTracking()
                .Where(room => room.IsActive)
                .OrderBy(room => room.RoomCode)
                .Select(room => new RoomOptionViewModel
                {
                    RoomId = room.RoomId,
                    RoomCode = room.RoomCode,
                    RoomName = room.RoomName
                })
                .ToListAsync();

            var model = new RoomEquipmentViewModel
            {
                SelectedRoomId = roomId,
                ActiveRooms = activeRooms
            };

            if (!roomId.HasValue)
            {
                return View(model);
            }

            var selectedRoom = activeRooms.FirstOrDefault(room => room.RoomId == roomId.Value);
            if (selectedRoom == null)
            {
                ModelState.AddModelError(nameof(model.SelectedRoomId), "Phòng học không tồn tại hoặc đang ngừng hoạt động.");
                model.SelectedRoomId = null;
                return View(model);
            }

            model.SelectedRoomName = selectedRoom.DisplayName;
            model.Equipments = await _context.Equipments
                .AsNoTracking()
                .Where(equipment => equipment.IsActive && equipment.CurrentRoomId == roomId.Value)
                .OrderBy(equipment => equipment.AssetCode)
                .Select(equipment => new RoomEquipmentItemViewModel
                {
                    EquipmentId = equipment.EquipmentId,
                    AssetCode = equipment.AssetCode,
                    EquipmentName = equipment.EquipmentName,
                    CategoryName = equipment.Category.CategoryName,
                    Status = equipment.Status,
                    HasPendingIncident = equipment.IncidentReports.Any(incident => incident.Status == "Pending")
                })
                .ToListAsync();

            return View(model);
        }
    }
}
