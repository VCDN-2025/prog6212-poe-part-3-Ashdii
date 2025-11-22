using CMCSystem.Data;
using CMCSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CMCSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ================= LOGIN =================
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password, string? returnUrl = null)
        {
            try
            {
                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                {
                    ModelState.AddModelError("", "Email and password are required.");
                    return View();
                }

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == email && u.Password == password && u.IsActive);

                if (user == null)
                {
                    ModelState.AddModelError("", "Invalid login credentials.");
                    return View();
                }

                // Clear any existing session
                HttpContext.Session.Clear();

                // Store session (all lowercase for roles)
                HttpContext.Session.SetString("UserId", user.Id.ToString());
                HttpContext.Session.SetString("UserEmail", user.Email);
                HttpContext.Session.SetString("UserName", user.Name);
                HttpContext.Session.SetString("UserRole", user.Role.ToLower().Trim());

                // Debug info
                Console.WriteLine($"User logged in: {user.Name}, Role: {user.Role.ToLower().Trim()}");

                // Redirect by role
                switch (user.Role.ToLower().Trim())
                {
                    case "hr":
                        return RedirectToAction("Index", "HR");
                    case "lecturer":
                        return RedirectToAction("Dashboard", "Claim");
                    case "programmecoordinator":
                        return RedirectToAction("Index", "ProgrammeCoordinator");
                    case "academicmanager":
                        return RedirectToAction("Index", "AcademicManager");
                    default:
                        return RedirectToAction("Index", "Home");
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Login error: {ex.Message}");
                ModelState.AddModelError("", "An error occurred during login.");
                return View();
            }
        }

        // ================= LOGOUT =================
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        // ================= REGISTER =================
        [HttpGet]
        public IActionResult Register() => View();


        [HttpPost]
        public async Task<IActionResult> Register(string name, string email, string password, string role)
        {
            // Preserve form values in case of error
            ViewData["Name"] = name;
            ViewData["Email"] = email;
            ViewData["Role"] = role;

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(email) ||
                string.IsNullOrEmpty(password) || string.IsNullOrEmpty(role))
            {
                ModelState.AddModelError("", "All fields are required.");
                return View();
            }

            // Check if email exists in Users table
            if (await _context.Users.AnyAsync(u => u.Email == email))
            {
                ModelState.AddModelError("", "Email already exists in Users.");
                return View();
            }

            var normalizedRole = role.ToLower().Trim();

            // Create User
            var user = new User
            {
                Name = name,
                Email = email,
                Password = password,
                Role = normalizedRole,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync(); // Save to get the User ID

            // If Lecturer, also add to Lecturers table
            if (normalizedRole == "lecturer")
            {
                // Check if lecturer already exists
                if (!await _context.Lecturers.AnyAsync(l => l.Email == email))
                {
                    var lecturer = new Lecturer
                    {
                        FullName = name,
                        Email = email,
                        Password = password,
                        HourlyRate = 0 // default, can be updated by HR
                    };
                    _context.Lecturers.Add(lecturer);
                    await _context.SaveChangesAsync();
                }
            }

            // Set session after registration
            HttpContext.Session.SetString("UserId", user.Id.ToString());
            HttpContext.Session.SetString("UserEmail", user.Email);
            HttpContext.Session.SetString("UserName", user.Name);
            HttpContext.Session.SetString("UserRole", user.Role);

            // Redirect by role
            switch (normalizedRole)
            {
                case "hr":
                    return RedirectToAction("Index", "HR");
                case "lecturer":
                    return RedirectToAction("Dashboard", "Claim");
                case "programmecoordinator":
                    return RedirectToAction("Index", "ProgrammeCoordinator");
                case "academicmanager":
                    return RedirectToAction("Index", "AcademicManager");
                default:
                    return RedirectToAction("Index", "Home");
            }
        }

    }
}
