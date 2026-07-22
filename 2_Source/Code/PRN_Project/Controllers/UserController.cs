using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PRN_Project.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PRN_Project.Services;

namespace PRN_Project.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UserController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;

        public UserController(AppDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public async Task<IActionResult> Index(string? searchTerm, string? roleFilter, string? statusFilter, int page = 1)
        {
            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(u => u.UserCode.Contains(searchTerm) || u.FullName.Contains(searchTerm) || u.Email.Contains(searchTerm));
            }

            if (!string.IsNullOrEmpty(roleFilter))
            {
                query = query.Where(u => u.Role == roleFilter);
            }

            if (!string.IsNullOrEmpty(statusFilter))
            {
                bool isActive = statusFilter == "Active";
                query = query.Where(u => u.IsActive == isActive);
            }

            int pageSize = 10;
            int totalItems = await query.CountAsync();
            int totalPages = (int)System.Math.Ceiling(totalItems / (double)pageSize);
            
            // Đảm bảo không quá trang cuối
            if (page > totalPages && totalPages > 0) page = totalPages;
            if (page < 1) page = 1;

            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var model = new UserListViewModel
            {
                Users = users,
                SearchTerm = searchTerm,
                RoleFilter = roleFilter,
                StatusFilter = statusFilter,
                CurrentPage = page,
                TotalPages = totalPages,
                PageSize = pageSize
            };

            return View(model);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Xử lý khoảng trắng và chữ hoa cho UserCode
                if (!string.IsNullOrEmpty(model.UserCode))
                {
                    model.UserCode = model.UserCode.Replace(" ", "").ToUpper();
                }

                // Validate định danh theo Role
                if (model.Role == "Lecturer" && !System.Text.RegularExpressions.Regex.IsMatch(model.UserCode, @"^LEC\d+$"))
                {
                    ModelState.AddModelError("UserCode", "Mã Giảng viên phải bắt đầu bằng 'LEC' và theo sau là các chữ số (Ví dụ: LEC01).");
                    return View(model);
                }
                else if (model.Role == "Technician" && !System.Text.RegularExpressions.Regex.IsMatch(model.UserCode, @"^TECH\d+$"))
                {
                    ModelState.AddModelError("UserCode", "Mã Nhân viên phải bắt đầu bằng 'TECH' và theo sau là các chữ số (Ví dụ: TECH01).");
                    return View(model);
                }

                // Kiểm tra trùng lặp
                if (await _context.Users.AnyAsync(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "Email này đã được sử dụng.");
                    return View(model);
                }

                if (await _context.Users.AnyAsync(u => u.UserCode == model.UserCode))
                {
                    ModelState.AddModelError("UserCode", "Mã người dùng này đã tồn tại.");
                    return View(model);
                }

                string defaultPassword = "Cems@123";
                var newUser = new User
                {
                    UserCode = model.UserCode,
                    FullName = model.FullName,
                    Email = model.Email,
                    Role = model.Role,
                    PasswordHash = PRN_Project.Helpers.PasswordHelper.HashPassword(defaultPassword),
                    IsActive = true
                };

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                // Gửi email thông báo
                string subject = "Tài khoản CEMS của bạn đã được tạo";
                string body = $@"
                    <h3>Xin chào {model.FullName},</h3>
                    <p>Tài khoản truy cập hệ thống Quản lý thiết bị phòng học (CEMS) của bạn đã được khởi tạo thành công.</p>
                    <p><strong>Thông tin đăng nhập:</strong></p>
                    <ul>
                        <li>Email: {model.Email}</li>
                        <li>Mật khẩu mặc định: <b>{defaultPassword}</b></li>
                        <li>Vai trò: {model.Role}</li>
                    </ul>
                    <p>Vui lòng đăng nhập và đổi mật khẩu trong lần truy cập đầu tiên để bảo mật tài khoản.</p>
                    <p>Trân trọng,<br>Ban quản trị CEMS.</p>
                ";

                await _emailService.SendEmailAsync(model.Email, subject, body);

                TempData["SuccessMessage"] = "Đã tạo tài khoản và gửi email thông báo thành công.";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var model = new EditUserViewModel
            {
                UserId = user.UserId,
                UserCode = user.UserCode,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditUserViewModel model)
        {
            if (id != model.UserId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound();
                }

                // Xử lý khoảng trắng và chữ hoa cho UserCode
                if (!string.IsNullOrEmpty(model.UserCode))
                {
                    model.UserCode = model.UserCode.Replace(" ", "").ToUpper();
                }

                // Validate định danh theo Role
                if (model.Role == "Lecturer" && !System.Text.RegularExpressions.Regex.IsMatch(model.UserCode, @"^LEC\d+$"))
                {
                    ModelState.AddModelError("UserCode", "Mã Giảng viên phải bắt đầu bằng 'LEC' và theo sau là các chữ số (Ví dụ: LEC01).");
                    return View(model);
                }
                else if (model.Role == "Technician" && !System.Text.RegularExpressions.Regex.IsMatch(model.UserCode, @"^TECH\d+$"))
                {
                    ModelState.AddModelError("UserCode", "Mã Nhân viên phải bắt đầu bằng 'TECH' và theo sau là các chữ số (Ví dụ: TECH01).");
                    return View(model);
                }

                // Kiểm tra trùng UserCode nếu thay đổi
                if (user.UserCode != model.UserCode && await _context.Users.AnyAsync(u => u.UserCode == model.UserCode))
                {
                    ModelState.AddModelError("UserCode", "Mã người dùng này đã tồn tại.");
                    return View(model);
                }

                // Xử lý chặn tự đổi Role của chính mình
                var currentUserIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (currentUserIdStr != null && int.TryParse(currentUserIdStr, out int currentUserId))
                {
                    if (currentUserId == id && user.Role != model.Role)
                    {
                        TempData["ErrorMessage"] = "Bạn không thể tự thay đổi vai trò của chính mình.";
                        return RedirectToAction(nameof(Index));
                    }
                }

                user.UserCode = model.UserCode;
                // Không cho phép đổi FullName ở đây nữa (theo yêu cầu)
                user.Role = model.Role;
                user.UpdatedAt = System.DateTime.Now;

                _context.Update(user);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Cập nhật tài khoản thành công.";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var currentUserIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (currentUserIdStr != null && int.TryParse(currentUserIdStr, out int currentUserId))
            {
                if (currentUserId == id)
                {
                    TempData["ErrorMessage"] = "Bạn không thể tự khóa/mở khóa tài khoản của chính mình.";
                    return RedirectToAction(nameof(Index));
                }
            }

            user.IsActive = !user.IsActive;
            user.UpdatedAt = System.DateTime.Now;
            _context.Update(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = user.IsActive ? "Đã mở khóa tài khoản." : "Đã khóa tài khoản.";
            return RedirectToAction(nameof(Index));
        }
    }
}
