using CMCSystem.Data;
using CMCSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CMCSystem.Controllers
{
    public class ClaimController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ClaimController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ================= LECTURER DASHBOARD ====================
        public async Task<IActionResult> Dashboard()
        {
            var role = HttpContext.Session.GetString("UserRole")?.ToLower();
            var email = HttpContext.Session.GetString("UserEmail");

            // Debug session
            Console.WriteLine($"Dashboard - Role: {role}, Email: {email}");

            if (role != "lecturer")
            {
                Console.WriteLine("Redirecting to login from Dashboard");
                return RedirectToAction("Login", "Account");
            }

            // Get lecturer and their current rate
            var lecturer = await _context.Lecturers.FirstOrDefaultAsync(l => l.Email == email);
            if (lecturer != null)
            {
                ViewBag.CurrentRate = lecturer.HourlyRate;
            }

            return View();
        }

        // ================= CREATE CLAIM ====================
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var role = HttpContext.Session.GetString("UserRole")?.ToLower();
            var email = HttpContext.Session.GetString("UserEmail");

            // Debug session
            Console.WriteLine($"Create GET - Role: {role}, Email: {email}");

            if (role != "lecturer")
            {
                Console.WriteLine("Redirecting to login from Create GET");
                return RedirectToAction("Login", "Account");
            }

            // Get lecturer info to pre-fill the hourly rate
            var lecturer = await _context.Lecturers.FirstOrDefaultAsync(l => l.Email == email);
            if (lecturer == null)
            {
                Console.WriteLine("Lecturer not found in database");
                return RedirectToAction("Login", "Account");
            }

            // Use the lecturer's current HourlyRate from the database
            var claim = new Claim
            {
                LecturerName = lecturer.FullName,
                HourlyRate = lecturer.HourlyRate
            };

            Console.WriteLine($"Creating claim for {lecturer.FullName} with rate: R{lecturer.HourlyRate}");

            return View(claim);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Claim claim, IFormFile supportingDocument)
        {
            var role = HttpContext.Session.GetString("UserRole")?.ToLower();
            var email = HttpContext.Session.GetString("UserEmail");

            // Debug session
            Console.WriteLine($"Create POST - Role: {role}, Email: {email}");

            if (role != "lecturer")
            {
                Console.WriteLine("Redirecting to login from Create POST");
                return RedirectToAction("Login", "Account");
            }

            var lecturer = await _context.Lecturers.FirstOrDefaultAsync(l => l.Email == email);
            if (lecturer == null)
            {
                Console.WriteLine("Lecturer not found in database");
                return RedirectToAction("Login", "Account");
            }

            // ALWAYS use the lecturer's current rate from the database
            claim.HourlyRate = lecturer.HourlyRate;

            // Upload supporting document
            if (supportingDocument != null && supportingDocument.Length > 0)
            {
                // Ensure uploads directory exists
                var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                if (!Directory.Exists(uploadsDir))
                    Directory.CreateDirectory(uploadsDir);

                var fileName = Guid.NewGuid() + Path.GetExtension(supportingDocument.FileName);
                var filePath = Path.Combine(uploadsDir, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await supportingDocument.CopyToAsync(stream);
                }

                claim.SupportingDocumentPath = "/uploads/" + fileName;
            }

            claim.LecturerId = lecturer.LecturerId;
            claim.LecturerName = lecturer.FullName;
            claim.TotalAmount = claim.HourlyRate * claim.HoursWorked;
            claim.DateSubmitted = DateTime.Now;
            claim.Status = ClaimStatus.Pending;

            _context.Claims.Add(claim);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Claim submitted successfully! Total amount: R{claim.TotalAmount:F2}";
            return RedirectToAction("MyClaims");
        }

        // ================= VIEW MY CLAIMS ====================
        public async Task<IActionResult> MyClaims()
        {
            var role = HttpContext.Session.GetString("UserRole")?.ToLower();
            var email = HttpContext.Session.GetString("UserEmail");

            // Debug session
            Console.WriteLine($"MyClaims - Role: {role}, Email: {email}");

            if (role != "lecturer")
            {
                Console.WriteLine("Redirecting to login from MyClaims");
                return RedirectToAction("Login", "Account");
            }

            var lecturer = await _context.Lecturers.FirstOrDefaultAsync(l => l.Email == email);
            if (lecturer == null)
            {
                Console.WriteLine("Lecturer not found in database");
                return RedirectToAction("Login", "Account");
            }

            var claims = await _context.Claims
                .Where(c => c.LecturerId == lecturer.LecturerId)
                .OrderByDescending(c => c.DateSubmitted)
                .ToListAsync();

            return View(claims);
        }

        // ================= EDIT CLAIM ====================
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var role = HttpContext.Session.GetString("UserRole")?.ToLower();
            var email = HttpContext.Session.GetString("UserEmail");

            if (role != "lecturer") return RedirectToAction("Login", "Account");

            var lecturer = await _context.Lecturers.FirstOrDefaultAsync(l => l.Email == email);
            if (lecturer == null) return RedirectToAction("Login", "Account");

            var claim = await _context.Claims
                .FirstOrDefaultAsync(c => c.ClaimId == id && c.LecturerId == lecturer.LecturerId);

            if (claim == null) return NotFound();

            // Only allow editing of pending claims
            if (claim.Status != ClaimStatus.Pending)
            {
                TempData["Error"] = "You can only edit pending claims.";
                return RedirectToAction("MyClaims");
            }

            return View(claim);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Claim updatedClaim, IFormFile supportingDocument)
        {
            var role = HttpContext.Session.GetString("UserRole")?.ToLower();
            if (role != "lecturer") return RedirectToAction("Login", "Account");

            var email = HttpContext.Session.GetString("UserEmail");
            var lecturer = await _context.Lecturers.FirstOrDefaultAsync(l => l.Email == email);
            if (lecturer == null) return RedirectToAction("Login", "Account");

            var existingClaim = await _context.Claims
                .FirstOrDefaultAsync(c => c.ClaimId == updatedClaim.ClaimId && c.LecturerId == lecturer.LecturerId);

            if (existingClaim == null) return NotFound();

            // Only allow editing of pending claims
            if (existingClaim.Status != ClaimStatus.Pending)
            {
                TempData["Error"] = "You can only edit pending claims.";
                return RedirectToAction("MyClaims");
            }

            // Update fields - but keep the original hourly rate
            existingClaim.HoursWorked = updatedClaim.HoursWorked;
            existingClaim.TotalAmount = existingClaim.HourlyRate * updatedClaim.HoursWorked;

            // Handle new document upload
            if (supportingDocument != null && supportingDocument.Length > 0)
            {
                var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                if (!Directory.Exists(uploadsDir))
                    Directory.CreateDirectory(uploadsDir);

                var fileName = Guid.NewGuid() + Path.GetExtension(supportingDocument.FileName);
                var filePath = Path.Combine(uploadsDir, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await supportingDocument.CopyToAsync(stream);
                }

                existingClaim.SupportingDocumentPath = "/uploads/" + fileName;
            }

            _context.Claims.Update(existingClaim);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Claim updated successfully!";
            return RedirectToAction("MyClaims");
        }

        // ================= HR CLAIM VIEW ====================
        public async Task<IActionResult> Index()
        {
            var role = HttpContext.Session.GetString("UserRole")?.ToLower();
            if (role != "hr") return RedirectToAction("Login", "Account");

            var claims = await _context.Claims.Include(c => c.Lecturer).ToListAsync();
            return View(claims);
        }

        // ================= DOWNLOAD DOCUMENT ====================
        public IActionResult DownloadDocument(int id)
        {
            var claim = _context.Claims.FirstOrDefault(c => c.ClaimId == id);
            if (claim == null || string.IsNullOrEmpty(claim.SupportingDocumentPath))
                return NotFound();

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", claim.SupportingDocumentPath.TrimStart('/'));

            if (!System.IO.File.Exists(filePath))
                return NotFound();

            var fileName = Path.GetFileName(filePath);
            return PhysicalFile(filePath, "application/octet-stream", fileName);
        }
    }
}