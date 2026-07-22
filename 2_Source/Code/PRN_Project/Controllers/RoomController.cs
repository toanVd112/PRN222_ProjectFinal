using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PRN_Project.Models;

namespace PRN_Project.Controllers
{
    [Authorize(Roles = "Admin")]
    public class RoomController : Controller
    {
        private readonly AppDbContext _context;

        public RoomController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string searchTerm = null, string statusFilter = null, string typeFilter = null, int page = 1)
        {
            var query = _context.Rooms.AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(r => r.RoomCode.Contains(searchTerm) || r.Location.Contains(searchTerm));
            }

            if (!string.IsNullOrEmpty(statusFilter))
            {
                bool isActive = statusFilter == "Active";
                query = query.Where(r => r.IsActive == isActive);
            }

            if (!string.IsNullOrEmpty(typeFilter))
            {
                query = query.Where(r => r.RoomType == typeFilter);
            }

            int pageSize = 10;
            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            page = Math.Max(1, Math.Min(page, totalPages > 0 ? totalPages : 1));

            var rooms = await query
                .OrderBy(r => r.RoomCode)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var viewModel = new RoomListViewModel
            {
                Rooms = rooms,
                SearchTerm = searchTerm,
                StatusFilter = statusFilter,
                TypeFilter = typeFilter,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalItems = totalItems
            };

            return View(viewModel);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateRoomViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (await _context.Rooms.AnyAsync(r => r.RoomCode == model.RoomCode))
                {
                    ModelState.AddModelError("RoomCode", "Mã phòng này đã tồn tại trong hệ thống.");
                    return View(model);
                }

                if (!IsValidRoomCode(model.RoomCode, model.Location))
                {
                    ModelState.AddModelError("RoomCode", $"Mã phòng phải bắt đầu bằng chữ cái viết tắt của tòa nhà (VD: tòa Alpha bắt đầu bằng AL).");
                    return View(model);
                }

                var room = new Room
                {
                    RoomCode = model.RoomCode.ToUpper(),
                    RoomName = model.RoomCode.ToUpper(),
                    RoomType = model.RoomType,
                    Location = model.Location,
                    Capacity = model.Capacity,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                _context.Rooms.Add(room);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Thêm phòng học mới thành công.";
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room == null)
            {
                return NotFound();
            }

            var model = new EditRoomViewModel
            {
                RoomId = room.RoomId,
                RoomCode = room.RoomCode,
                RoomType = room.RoomType,
                Location = room.Location,
                Capacity = room.Capacity
            };

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditRoomViewModel model)
        {
            if (ModelState.IsValid)
            {
                var room = await _context.Rooms.FindAsync(model.RoomId);
                if (room == null)
                {
                    return NotFound();
                }

                // Check for duplicate room code
                if (await _context.Rooms.AnyAsync(r => r.RoomCode == model.RoomCode && r.RoomId != model.RoomId))
                {
                    ModelState.AddModelError("RoomCode", "Mã phòng này đã tồn tại trong hệ thống.");
                    return View(model);
                }

                if (!IsValidRoomCode(model.RoomCode, model.Location))
                {
                    ModelState.AddModelError("RoomCode", $"Mã phòng phải bắt đầu bằng chữ cái viết tắt của tòa nhà (VD: tòa Alpha bắt đầu bằng AL).");
                    return View(model);
                }

                room.RoomCode = model.RoomCode.ToUpper();
                room.RoomName = model.RoomCode.ToUpper();
                room.RoomType = model.RoomType;
                room.Location = model.Location;
                room.Capacity = model.Capacity;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Cập nhật thông tin phòng học thành công.";
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room == null)
            {
                return NotFound();
            }

            // Nếu đang định khóa (IsActive = true chuyển thành false)
            if (room.IsActive == true)
            {
                // Kiểm tra xem phòng có thiết bị nào không (bao gồm cả thiết bị hỏng, bảo trì)
                var hasEquipments = await _context.Equipments.AnyAsync(e => e.CurrentRoomId == id);
                if (hasEquipments)
                {
                    TempData["ErrorMessage"] = "Không thể khóa phòng học này vì vẫn còn thiết bị bên trong.";
                    return RedirectToAction(nameof(Index));
                }
            }

            room.IsActive = !room.IsActive;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = room.IsActive == true ? "Đã mở khóa phòng học." : "Đã tạm ngưng phòng học.";
            return RedirectToAction(nameof(Index));
        }

        private bool IsValidRoomCode(string roomCode, string location)
        {
            if (string.IsNullOrEmpty(roomCode) || string.IsNullOrEmpty(location)) return false;
            var prefix = location switch
            {
                "Alpha" => "AL",
                "Beta" => "BE",
                "Delta" => "DE",
                "Gamma" => "GA",
                _ => ""
            };
            return roomCode.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Equipments(int id)
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room == null)
            {
                return NotFound();
            }

            var equipments = await _context.Equipments
                .Include(e => e.Category)
                .Where(e => e.CurrentRoomId == id)
                .OrderBy(e => e.Category.CategoryName)
                .ThenBy(e => e.EquipmentName)
                .ToListAsync();

            var model = new RoomEquipmentsViewModel
            {
                Room = room,
                Equipments = equipments
            };

            return View(model);
        }
    }
}
