using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PRN_Project.Models;
using System.Security.Claims;

namespace PRN_Project.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProfileController(AppDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var model = new ProfileViewModel
            {
                UserCode = user.UserCode,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role,
                PhoneNumber = user.PhoneNumber,
                AvatarUrl = user.AvatarUrl
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(ProfileViewModel model)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            if (!ModelState.IsValid)
            {
                // Re-populate readonly fields
                var u = await _context.Users.FindAsync(userId);
                if (u != null)
                {
                    model.UserCode = u.UserCode;
                    model.Email = u.Email;
                    model.Role = u.Role;
                    model.AvatarUrl = u.AvatarUrl;
                }
                return View("Index", model);
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            // Handle Avatar Upload
            if (model.AvatarFile != null)
            {
                // Validate file extension
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                var extension = Path.GetExtension(model.AvatarFile.FileName).ToLowerInvariant();
                
                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("AvatarFile", "Chỉ chấp nhận định dạng ảnh (.jpg, .jpeg, .png).");
                    model.UserCode = user.UserCode;
                    model.Email = user.Email;
                    model.Role = user.Role;
                    model.AvatarUrl = user.AvatarUrl;
                    return View("Index", model);
                }

                // Validate file size (max 2MB)
                if (model.AvatarFile.Length > 2 * 1024 * 1024)
                {
                    ModelState.AddModelError("AvatarFile", "Kích thước file không được vượt quá 2MB.");
                    model.UserCode = user.UserCode;
                    model.Email = user.Email;
                    model.Role = user.Role;
                    model.AvatarUrl = user.AvatarUrl;
                    return View("Index", model);
                }

                // Save file
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "avatars");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string uniqueFileName = Guid.NewGuid().ToString() + "_" + model.AvatarFile.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await model.AvatarFile.CopyToAsync(fileStream);
                }

                // Delete old avatar if exists
                if (!string.IsNullOrEmpty(user.AvatarUrl))
                {
                    string oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, user.AvatarUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                }

                user.AvatarUrl = "/uploads/avatars/" + uniqueFileName;
            }

            if (!string.IsNullOrEmpty(model.FullName))
            {
                user.FullName = System.Text.RegularExpressions.Regex.Replace(model.FullName.Trim(), @"\s+", " ");
            }
            else
            {
                user.FullName = string.Empty;
            }
            user.PhoneNumber = model.PhoneNumber?.Trim();
            
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Cập nhật hồ sơ thành công!";
            return RedirectToAction("Index");
        }
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            // Verify old password
            if (!PRN_Project.Helpers.PasswordHelper.VerifyPassword(model.OldPassword, user.PasswordHash))
            {
                ModelState.AddModelError("OldPassword", "Mật khẩu hiện tại không đúng.");
                return View(model);
            }

            // Check if new password is the same as the old password
            if (PRN_Project.Helpers.PasswordHelper.VerifyPassword(model.NewPassword, user.PasswordHash))
            {
                ModelState.AddModelError("NewPassword", "Mật khẩu mới không được trùng với mật khẩu hiện tại.");
                return View(model);
            }

            // Hash and save new password
            user.PasswordHash = PRN_Project.Helpers.PasswordHelper.HashPassword(model.NewPassword);
            await _context.SaveChangesAsync();

            // Sign out the user
            await HttpContext.SignOutAsync(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);

            TempData["SuccessMessage"] = "Đổi mật khẩu thành công. Vui lòng đăng nhập lại.";
            return RedirectToAction("Login", "Auth");
        }
    }
}
