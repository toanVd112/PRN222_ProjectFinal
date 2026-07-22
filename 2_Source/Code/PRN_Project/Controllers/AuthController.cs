using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PRN_Project.Helpers;
using PRN_Project.Models;
using System.Security.Claims;

namespace PRN_Project.Controllers
{
    public class AuthController : Controller
    {
        private readonly AppDbContext _context;
        private readonly PRN_Project.Services.IEmailService _emailService;

        public AuthController(AppDbContext context, PRN_Project.Services.IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);

            if (user == null || !user.IsActive)
            {
                ModelState.AddModelError(string.Empty, "Tài khoản không tồn tại hoặc đã bị khóa (IsActive = false).");
                return View(model);
            }

            if (user.LockoutUntil.HasValue && user.LockoutUntil.Value > DateTime.Now)
            {
                var remainingMinutes = (int)Math.Ceiling((user.LockoutUntil.Value - DateTime.Now).TotalMinutes);
                ModelState.AddModelError(string.Empty, $"Tài khoản của bạn đang bị khóa tạm thời do đăng nhập sai nhiều lần. Vui lòng thử lại sau {remainingMinutes} phút.");
                return View(model);
            }

            if (!PasswordHelper.VerifyPassword(model.Password, user.PasswordHash))
            {
                user.FailedLoginCount++;
                
                if (user.FailedLoginCount >= 5)
                {
                    user.LockoutUntil = DateTime.Now.AddMinutes(15);
                    ModelState.AddModelError(string.Empty, "Bạn đã nhập sai mật khẩu 5 lần. Tài khoản bị khóa trong 15 phút.");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, $"Mật khẩu không đúng. Bạn còn {5 - user.FailedLoginCount} lần thử.");
                }

                await _context.SaveChangesAsync();
                return View(model);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("UserCode", user.UserCode)
            };

            var claimsIdentity = new ClaimsIdentity(
                claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(7) : null
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            user.FailedLoginCount = 0;
            user.LockoutUntil = null;
            user.LastLoginAt = DateTime.Now;
            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Auth");
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
            if (user == null)
            {
                // Để bảo mật, không tiết lộ email có tồn tại hay không nếu là hệ thống public
                // Nhưng theo UC, yêu cầu báo lỗi "Email không tồn tại trong hệ thống"
                ModelState.AddModelError(string.Empty, "Email không tồn tại trong hệ thống.");
                return View(model);
            }

            // Kiểm tra giới hạn 3 lần/24h (Spam rule)
            var recentRequests = await _context.PasswordResetTokens
                .Where(t => t.UserId == user.UserId && t.CreatedAt >= DateTime.Now.AddHours(-24))
                .CountAsync();

            if (recentRequests >= 3)
            {
                ModelState.AddModelError(string.Empty, "Bạn đã yêu cầu đặt lại mật khẩu quá số lần cho phép trong 24 giờ. Vui lòng thử lại sau.");
                return View(model);
            }

            // Tạo Token
            var token = Guid.NewGuid().ToString();
            var resetToken = new PasswordResetToken
            {
                UserId = user.UserId,
                Token = token,
                ExpiresAt = DateTime.Now.AddMinutes(15),
                IsUsed = false,
                CreatedAt = DateTime.Now
            };

            _context.PasswordResetTokens.Add(resetToken);
            await _context.SaveChangesAsync();

            // Tạo link khôi phục
            var resetLink = Url.Action("ResetPassword", "Auth", new { token = token, email = model.Email }, Request.Scheme);

            // Gửi email
            var subject = "Khôi phục mật khẩu - CEMS";
            var body = $@"
                <h3>Yêu cầu khôi phục mật khẩu</h3>
                <p>Chào {user.FullName},</p>
                <p>Bạn (hoặc ai đó) vừa yêu cầu đặt lại mật khẩu cho tài khoản CEMS của bạn.</p>
                <p>Vui lòng click vào đường dẫn bên dưới để đặt lại mật khẩu. Link này sẽ hết hạn sau 15 phút.</p>
                <p><a href='{resetLink}'>{resetLink}</a></p>
                <p>Nếu bạn không yêu cầu, vui lòng bỏ qua email này.</p>";

            await _emailService.SendEmailAsync(model.Email, subject, body);

            return RedirectToAction("ForgotPasswordConfirmation");
        }

        [HttpGet]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        [HttpGet]
        public IActionResult ResetPassword(string token, string email)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError(string.Empty, "Thông tin khôi phục không hợp lệ.");
                return View();
            }

            var model = new ResetPasswordViewModel { Token = token, Email = email };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Người dùng không tồn tại.");
                return View(model);
            }

            var resetToken = await _context.PasswordResetTokens
                .FirstOrDefaultAsync(t => t.Token == model.Token && t.UserId == user.UserId && !t.IsUsed);

            if (resetToken == null)
            {
                ModelState.AddModelError(string.Empty, "Mã khôi phục không hợp lệ hoặc đã được sử dụng.");
                return View(model);
            }

            if (resetToken.ExpiresAt < DateTime.Now)
            {
                ModelState.AddModelError(string.Empty, "Mã khôi phục đã hết hạn. Vui lòng yêu cầu lại.");
                return View(model);
            }

            // Đổi mật khẩu
            user.PasswordHash = PasswordHelper.HashPassword(model.Password);
            resetToken.IsUsed = true;
            user.UpdatedAt = DateTime.Now;
            
            // Xóa khóa tạm thời nếu có để user login được ngay sau khi đổi MK thành công
            user.FailedLoginCount = 0;
            user.LockoutUntil = null;

            await _context.SaveChangesAsync();

            return RedirectToAction("ResetPasswordConfirmation");
        }

        [HttpGet]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
