using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using UrbanDrive.Data;
using UrbanDrive.Models;
using UrbanDrive.Services;

namespace UrbanDrive.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserService _userService;
        private readonly IEmailService _emailService;

        public AccountController(ApplicationDbContext context, IUserService userService, IEmailService emailService)
        {
            _context = context;
            _userService = userService;
            _emailService = emailService;
        }

        // GET: Login page
        public IActionResult Login()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToDashboard();
            }
            return View();
        }

        // POST: Login
        [HttpPost]
        public async Task<IActionResult> Login(string email, string password, bool rememberMe, string returnUrl = null)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                TempData["ErrorMessage"] = "Email and password are required";
                return View();
            }

            var user = await _userService.AuthenticateAsync(email, password);

            if (user == null)
            {
                TempData["ErrorMessage"] = "Invalid email or password";
                return View();
            }

            if (!user.IsActive)
            {
                TempData["ErrorMessage"] = "Your account is deactivated. Please contact admin.";
                return View();
            }

            // Create authentication cookie
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var identity = new ClaimsIdentity(claims, "CookieAuth");
            var principal = new ClaimsPrincipal(identity);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            };

            await HttpContext.SignInAsync("CookieAuth", principal, authProperties);

            // Update last login
            user.LastLoginAt = DateTime.Now;
            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToDashboard(user.Role);
        }

        // GET: Register page
        public IActionResult Register()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToDashboard();
            }
            return View();
        }

        // POST: Register
        [HttpPost]
        public async Task<IActionResult> Register(string fullName, string email, string phoneNumber, string password, string confirmPassword)
        {
            if (password != confirmPassword)
            {
                TempData["ErrorMessage"] = "Passwords do not match";
                return View();
            }

            // Check if email exists
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (existingUser != null)
            {
                TempData["ErrorMessage"] = "Email already registered";
                return View();
            }

            var user = new User
            {
                FullName = fullName,
                Email = email,
                PhoneNumber = phoneNumber,
                Role = "User",
                IsActive = true,
                IsEmailVerified = true, // Development: auto-verified
                RegisteredAt = DateTime.Now,
                EmailVerificationToken = _userService.GenerateToken()
            };

            var result = await _userService.RegisterUserAsync(user, password);

            if (!result)
            {
                TempData["ErrorMessage"] = "Registration failed. Please try again.";
                return View();
            }

            // Development: Skip verification email
            Console.WriteLine($"[DEV MODE] User registered: {email} (auto-verified)");

            TempData["SuccessMessage"] = "Registration successful! You can now login.";
            return RedirectToAction("Login");
        }

        // GET: Verify Email
        public async Task<IActionResult> VerifyEmail(string email, string token)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null || user.EmailVerificationToken != token)
            {
                ViewBag.Success = false;
                ViewBag.ErrorMessage = "Invalid verification link.";
                return View();
            }

            user.IsEmailVerified = true;
            user.EmailVerificationToken = null;
            await _context.SaveChangesAsync();

            ViewBag.Success = true;
            return View();
        }

        // GET: Forgot Password
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: Forgot Password
        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user != null)
            {
                var token = _userService.GenerateToken();
                user.PasswordResetToken = token;
                user.PasswordResetTokenExpiry = DateTime.Now.AddHours(24);
                await _context.SaveChangesAsync();

                var resetLink = Url.Action("ResetPassword", "Account", new { email = user.Email, token = token }, Request.Scheme);
                var placeholders = new Dictionary<string, string>
                {
                    { "FullName", user.FullName },
                    { "ResetLink", resetLink }
                };
                await _emailService.SendEmailWithTemplateAsync(user.Email, user.FullName, "PasswordReset", placeholders);
            }

            TempData["Message"] = "If an account exists with this email, you will receive a password reset link.";
            return View();
        }

        // GET: Reset Password
        public IActionResult ResetPassword(string email, string token)
        {
            ViewBag.Email = email;
            ViewBag.Token = token;
            return View();
        }

        // POST: Reset Password
        [HttpPost]
        public async Task<IActionResult> ResetPassword(string email, string token, string newPassword, string confirmPassword)
        {
            if (newPassword != confirmPassword)
            {
                TempData["ErrorMessage"] = "Passwords do not match";
                return View();
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null || user.PasswordResetToken != token || user.PasswordResetTokenExpiry < DateTime.Now)
            {
                TempData["ErrorMessage"] = "Invalid or expired reset link.";
                return View();
            }

            user.PasswordHash = _userService.HashPassword(newPassword);
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiry = null;
            user.MustChangePassword = false;
            user.LastPasswordChange = DateTime.Now;
            await _context.SaveChangesAsync();

            // Send confirmation email
            var placeholders = new Dictionary<string, string>
            {
                { "FullName", user.FullName },
                { "ChangeDate", DateTime.Now.ToString("MMM dd, yyyy 'at' hh:mm tt") }
            };
            await _emailService.SendEmailWithTemplateAsync(user.Email, user.FullName, "PasswordChanged", placeholders);

            TempData["SuccessMessage"] = "Password reset successfully. Please login with your new password.";
            return RedirectToAction("Login");
        }

        // GET: Logout
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("CookieAuth");
            return RedirectToAction("Login");
        }

        // GET: Access Denied
        public IActionResult AccessDenied()
        {
            return View();
        }

        private IActionResult RedirectToDashboard(string role = null)
        {
            var userRole = role ?? User.FindFirstValue(ClaimTypes.Role);
            return userRole switch
            {
                "Admin" => RedirectToAction("Dashboard", "Admin"),
                "Driver" => RedirectToAction("Dashboard", "Driver"),
                "User" => RedirectToAction("Dashboard", "User"),
                _ => RedirectToAction("Login")
            };
        }
    }
}