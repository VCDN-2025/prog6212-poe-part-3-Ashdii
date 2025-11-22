using CMCSystem.Data;
using CMCSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Text;

namespace CMCSystem.Controllers
{
    public class HRController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HRController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ================= HR DASHBOARD ====================
        public async Task<IActionResult> Index()
        {
            var role = HttpContext.Session.GetString("UserRole")?.ToLower();
            if (role != "hr")
                return RedirectToAction("Login", "Account");

            var dashboardStats = new
            {
                TotalLecturers = await _context.Lecturers.CountAsync(),
                TotalClaims = await _context.Claims.CountAsync(),
                PendingClaims = await _context.Claims.CountAsync(c => c.Status == ClaimStatus.Pending),
                ApprovedClaims = await _context.Claims.CountAsync(c => c.Status == ClaimStatus.ApprovedByManager)
            };

            ViewBag.DashboardStats = dashboardStats;
            var lecturers = await _context.Lecturers.ToListAsync();
            return View(lecturers);
        }

        // ================= LECTURER MANAGEMENT ====================

        // CREATE LECTURER
        public IActionResult CreateLecturer()
        {
            var role = HttpContext.Session.GetString("UserRole")?.ToLower();
            if (role != "hr")
                return RedirectToAction("Login", "Account");

            return View();
        }

        // DELETE LECTURER
        
        public async Task<IActionResult> DeleteLecturer(int id)
        {
            var role = HttpContext.Session.GetString("UserRole")?.ToLower();
            if (role != "hr")
                return RedirectToAction("Login", "Account");

            var lecturer = await _context.Lecturers.FindAsync(id);
            if (lecturer == null)
                return NotFound();

            // Count claims for warning message (but don't prevent deletion)
            var claimCount = await _context.Claims.CountAsync(c => c.LecturerId == id);
            ViewBag.ClaimCount = claimCount;

            return View("Delete", lecturer); // Specify the view name "Delete"
        }

        //Reference List
        //Title: Entity Framework Core in Action
        //Author: Jon P Smith
        //Date: 2018
        //Edition: 1st ed
        //Publisher: Manning Publications

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteLecturerConfirmed(int id, string confirmationText)
        {
            var role = HttpContext.Session.GetString("UserRole")?.ToLower();
            if (role != "hr")
                return RedirectToAction("Login", "Account");

            // Check confirmation text
            if (confirmationText?.ToUpper() != "DELETE")
            {
                TempData["Error"] = "Confirmation text 'DELETE' was not entered correctly.";
                return RedirectToAction("DeleteLecturer", new { id });
            }

            try
            {
                var lecturer = await _context.Lecturers.FindAsync(id);
                if (lecturer == null)
                {
                    TempData["Error"] = "Lecturer not found.";
                    return RedirectToAction("Index");
                }

                // Get claim count for messaging
                var claimCount = await _context.Claims.CountAsync(c => c.LecturerId == id);

                // Use transaction for data consistency
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // 1️⃣ DELETE CLAIMS FIRST
                    var claims = await _context.Claims
                        .Where(c => c.LecturerId == id)
                        .ToListAsync();

                    if (claims.Any())
                        _context.Claims.RemoveRange(claims);

                    // 2️⃣ DELETE LECTURER RATES
                    var lecturerRates = await _context.LecturerRates
                        .Where(r => r.LecturerName == lecturer.FullName)
                        .ToListAsync();

                    if (lecturerRates.Any())
                        _context.LecturerRates.RemoveRange(lecturerRates);

                    // 3️⃣ DELETE LECTURER LAST
                    _context.Lecturers.Remove(lecturer);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // Success message
                    var message = $"Lecturer {lecturer.FullName} deleted successfully!";
                    if (claimCount > 0)
                        message += $" {claimCount} claim(s) have been deleted.";

                    TempData["Success"] = message;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (DbUpdateException dbEx)
            {
                Console.WriteLine($"Database error deleting lecturer: {dbEx.Message}");
                TempData["Error"] = "Database error occurred while deleting lecturer. Please try again.";
                return RedirectToAction("DeleteLecturer", new { id });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting lecturer: {ex.Message}");
                TempData["Error"] = $"Error deleting lecturer: {ex.Message}";
                return RedirectToAction("DeleteLecturer", new { id });
            }

            return RedirectToAction("Index");
        }


        // ================= SET GLOBAL RATE ====================
        public IActionResult SetRate()
        {
            var role = HttpContext.Session.GetString("UserRole")?.ToLower();
            if (role != "hr")
                return RedirectToAction("Login", "Account");

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SetRate(LecturerRate rate  )
        {
            var role = HttpContext.Session.GetString("UserRole")?.ToLower();
            if (role != "hr")
                return RedirectToAction("Login", "Account");

            try
            {
                // Update ALL lecturers to R40 per hour
                var allLecturers = await _context.Lecturers.ToListAsync();
                int updatedCount = 0;

                foreach (var lecturer in allLecturers)
                {
                    // Only update if the rate is different
                    if (lecturer.HourlyRate != 40.00m)
                    {
                        lecturer.HourlyRate = 40.00m;
                        _context.Lecturers.Update(lecturer);
                        updatedCount++;
                    }

                    // Also update LecturerRate table for each lecturer
                    var existingRate = await _context.LecturerRates
                        .FirstOrDefaultAsync(r => r.LecturerName == lecturer.FullName);

                    if (existingRate != null)
                    {
                        existingRate.Rate = 40.00m;
                        _context.LecturerRates.Update(existingRate);
                    }
                    else
                    {
                        var newRate = new LecturerRate
                        {
                            LecturerName = lecturer.FullName,
                            Rate = 40.00m
                        };
                        _context.LecturerRates.Add(newRate);
                    }
                }

                await _context.SaveChangesAsync();

                TempData["Success"] = $"Successfully updated {updatedCount} lecturers to R40.00 per hour!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error updating rates: {ex.Message}";
                return RedirectToAction("SetRate");
            }
        }

        // USER MANAGEMENT - VIEW ALL USERS
        public async Task<IActionResult> UserManagement()
        {
            var role = HttpContext.Session.GetString("UserRole")?.ToLower();
            if (role != "hr")
                return RedirectToAction("Login", "Account");

            var users = await _context.Users.OrderBy(u => u.Name).ToListAsync();
            return View(users);
        }
        
        // ================= CLAIMS REPORTS WITH FILTERS ====================

        public async Task<IActionResult> Reports(
            string lecturerName = null,
            string status = null,
            decimal? minAmount = null,
            decimal? maxAmount = null,
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            var role = HttpContext.Session.GetString("UserRole")?.ToLower();
            if (role != "hr")
                return RedirectToAction("Login", "Account");

            var claimsQuery = _context.Claims.Include(c => c.Lecturer).AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(lecturerName))
                claimsQuery = claimsQuery.Where(c => c.LecturerName.Contains(lecturerName));

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<ClaimStatus>(status, out var statusEnum))
                claimsQuery = claimsQuery.Where(c => c.Status == statusEnum);

            if (minAmount.HasValue)
                claimsQuery = claimsQuery.Where(c => c.TotalAmount >= minAmount.Value);

            if (maxAmount.HasValue)
                claimsQuery = claimsQuery.Where(c => c.TotalAmount <= maxAmount.Value);

            if (fromDate.HasValue)
                claimsQuery = claimsQuery.Where(c => c.DateSubmitted >= fromDate.Value);

            if (toDate.HasValue)
                claimsQuery = claimsQuery.Where(c => c.DateSubmitted <= toDate.Value.AddDays(1)); // Include the entire day

            var claims = await claimsQuery.OrderByDescending(c => c.DateSubmitted).ToListAsync();

            // Get all approval logs separately for the view
            var approvalLogs = await _context.ApprovalLogs.ToListAsync();
            ViewBag.ApprovalLogs = approvalLogs;

            // Pass filter values to view to maintain state
            ViewBag.LecturerName = lecturerName;
            ViewBag.Status = status;
            ViewBag.MinAmount = minAmount;
            ViewBag.MaxAmount = maxAmount;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

            return View(claims);
        }

        // ================= EXPORT FUNCTIONALITY ====================

        [HttpGet]
        public async Task<IActionResult> ExportClaims(
            string lecturerName = null,
            string status = null,
            decimal? minAmount = null,
            decimal? maxAmount = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string type = "csv")
        {
            var role = HttpContext.Session.GetString("UserRole")?.ToLower();
            if (role != "hr")
                return RedirectToAction("Login", "Account");

            // Apply the same filters as Reports action
            var claimsQuery = _context.Claims.Include(c => c.Lecturer).AsQueryable();

            if (!string.IsNullOrEmpty(lecturerName))
                claimsQuery = claimsQuery.Where(c => c.LecturerName.Contains(lecturerName));

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<ClaimStatus>(status, out var statusEnum))
                claimsQuery = claimsQuery.Where(c => c.Status == statusEnum);

            if (minAmount.HasValue)
                claimsQuery = claimsQuery.Where(c => c.TotalAmount >= minAmount.Value);

            if (maxAmount.HasValue)
                claimsQuery = claimsQuery.Where(c => c.TotalAmount <= maxAmount.Value);

            if (fromDate.HasValue)
                claimsQuery = claimsQuery.Where(c => c.DateSubmitted >= fromDate.Value);

            if (toDate.HasValue)
                claimsQuery = claimsQuery.Where(c => c.DateSubmitted <= toDate.Value.AddDays(1));

            var claims = await claimsQuery.OrderByDescending(c => c.DateSubmitted).ToListAsync();

            if (type == "pdf")
            {
                QuestPDF.Settings.License = LicenseType.Community;

                var pdf = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(20);
                        page.Header().Column(col =>
                        {
                            col.Item().Text("CMC System - Claims Report").FontSize(20).Bold();
                            col.Item().Text($"Generated on: {DateTime.Now:yyyy-MM-dd HH:mm}").FontSize(10);
                        });


                        page.Content().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn();
                                c.RelativeColumn();
                                c.RelativeColumn();
                                c.RelativeColumn();
                                c.RelativeColumn();
                                c.RelativeColumn();
                            });

                            table.Header(h =>
                            {
                                h.Cell().Text("Claim ID").Bold();
                                h.Cell().Text("Lecturer").Bold();
                                h.Cell().Text("Hours").Bold();
                                h.Cell().Text("Rate (R)").Bold();
                                h.Cell().Text("Total (R)").Bold();
                                h.Cell().Text("Status").Bold();
                            });

                            foreach (var c in claims)
                            {
                                table.Cell().Text(c.ClaimId.ToString());
                                table.Cell().Text(c.LecturerName);
                                table.Cell().Text(c.HoursWorked.ToString("F2"));
                                table.Cell().Text($"R{c.HourlyRate:F2}");
                                table.Cell().Text($"R{c.TotalAmount:F2}");
                                table.Cell().Text(c.Status.ToString());
                            }
                        });
                    });
                });

                return File(pdf.GeneratePdf(), "application/pdf", $"ClaimsReport_{DateTime.Now:yyyyMMddHHmm}.pdf");
            }

            // Default CSV
            var sb = new StringBuilder();
            sb.AppendLine("ClaimID,Lecturer,HoursWorked,HourlyRate,TotalAmount,Status,DateSubmitted");

            foreach (var c in claims)
                sb.AppendLine($"{c.ClaimId},{c.LecturerName},{c.HoursWorked},{c.HourlyRate},{c.TotalAmount},{c.Status},{c.DateSubmitted:yyyy-MM-dd}");

            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"ClaimsReport_{DateTime.Now:yyyyMMddHHmm}.csv");
        }

        // ================= APPROVAL HISTORY ====================
        public async Task<IActionResult> ClaimApprovalHistory(int id)
        {
            var role = HttpContext.Session.GetString("UserRole")?.ToLower();
            if (role != "hr")
                return RedirectToAction("Login", "Account");

            var claim = await _context.Claims
                .Include(c => c.Lecturer)
                .FirstOrDefaultAsync(c => c.ClaimId == id);

            if (claim == null) return NotFound();

            var approvalLogs = await _context.ApprovalLogs
                .Where(log => log.ClaimId == id)
                .OrderBy(log => log.ActionAt)
                .ToListAsync();

            ViewBag.Claim = claim;
            return View(approvalLogs);
        }
    }
}