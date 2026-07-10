using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ParkHub.Data;
using ParkHub.Models;
using System.Security.Claims;

namespace ParkHub.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher<User> _passwordHasher;

        public AccountController(ApplicationDbContext context, IPasswordHasher<User> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(User user)
        {
            if (ModelState.IsValid)
            {
                var plainPassword = user.PasswordHash;
                user.Role = "Customer";
                user.PasswordHash = _passwordHasher.HashPassword(user, plainPassword);

                _context.Users.Add(user);
                _context.SaveChanges();

                return RedirectToAction("Login");
            }

            return View(user);
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            var user = _context.Users
                .FirstOrDefault(u => u.Email == email);

            if (user == null || !IsValidPassword(user, password))
            {
                ViewBag.Error = "Invalid email or password";
                return View();
            }

            if (!user.PasswordHash.StartsWith("AQAAAA"))
            {
                user.PasswordHash = _passwordHasher.HashPassword(user, password);
                _context.SaveChanges();
            }

            var isAdmin = user.Email.Equals("admin@parkhub.com", StringComparison.OrdinalIgnoreCase);
            var role = user.Role ?? (isAdmin ? "Admin" : "User");

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, role)
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal);

            if ((user.Role ?? "Customer") == "Admin")
            {
                return RedirectToAction("Index", "Parking");
            }

            return RedirectToAction("Index", "Home");
        }

        private bool IsValidPassword(User user, string password)
        {
            if (user.PasswordHash.StartsWith("AQAAAA"))
            {
                var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
                return result != PasswordVerificationResult.Failed;
            }

            return user.PasswordHash == password;
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction("Login");
        }
    }
}
