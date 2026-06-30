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

        public AuthController(AppDbContext context)
        {
            _context = context;
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
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
