using CMCSystem.Data;
using CMCSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CMCSystem.Controllers
{
    public class ProgrammeCoordinatorController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProgrammeCoordinatorController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var role = HttpContext.Session.GetString("UserRole")?.ToLower();
            if (role != "programmecoordinator")
                return RedirectToAction("Login", "Account");

            // Show claims that are pending (either coordinator or manager can approve)
            var claims = await _context.Claims
                .Include(c => c.Lecturer)
                .Where(c => c.Status == ClaimStatus.Pending)
                .OrderBy(c => c.DateSubmitted)
                .ToListAsync();

            return View(claims);
        }

        public async Task<IActionResult> Details(int id)
        {
            var role = HttpContext.Session.GetString("UserRole")?.ToLower();
            if (role != "programmecoordinator")
                return RedirectToAction("Login", "Account");

            var claim = await _context.Claims
                .Include(c => c.Lecturer)
                .FirstOrDefaultAsync(c => c.ClaimId == id);

            if (claim == null) return NotFound();

            return View(claim);
        }

        [HttpPost]
        public async Task<IActionResult> Approve(int id)
        {
            var role = HttpContext.Session.GetString("UserRole")?.ToLower();
            if (role != "programmecoordinator")
                return RedirectToAction("Login", "Account");

            var claim = await _context.Claims.FindAsync(id);
            if (claim == null) return NotFound();

            // Either coordinator or manager can approve - whoever does it first
            claim.Status = ClaimStatus.ApprovedByManager; // Use Manager status since both can approve
            claim.ApprovedAt = DateTime.Now;

            // Create approval log
            var approvalLog = new ApprovalLog
            {
                ClaimId = id,
                ApproverId = HttpContext.Session.GetString("UserId") ?? "Unknown",
                ApproverName = HttpContext.Session.GetString("UserName") ?? "Programme Coordinator",
                Action = "Approved",
                Comments = "Approved by Programme Coordinator",
                ActionAt = DateTime.Now
            };

            _context.ApprovalLogs.Add(approvalLog);
            _context.Claims.Update(claim);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Claim approved successfully!";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Reject(int id, string comments)
        {
            var role = HttpContext.Session.GetString("UserRole")?.ToLower();
            if (role != "programmecoordinator")
                return RedirectToAction("Login", "Account");

            var claim = await _context.Claims.FindAsync(id);
            if (claim == null) return NotFound();

            claim.Status = ClaimStatus.RejectedByCoordinator;
            claim.RejectionComments = comments;
            claim.RejectedAt = DateTime.Now;

            // Create rejection log
            var approvalLog = new ApprovalLog
            {
                ClaimId = id,
                ApproverId = HttpContext.Session.GetString("UserId") ?? "Unknown",
                ApproverName = HttpContext.Session.GetString("UserName") ?? "Programme Coordinator",
                Action = "Rejected",
                Comments = comments,
                ActionAt = DateTime.Now
            };

            _context.ApprovalLogs.Add(approvalLog);
            _context.Claims.Update(claim);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Claim rejected successfully!";
            return RedirectToAction("Index");
        }

        // View all approved claims
        public async Task<IActionResult> ApprovedClaims()
        {
            var role = HttpContext.Session.GetString("UserRole")?.ToLower();
            if (role != "programmecoordinator")
                return RedirectToAction("Login", "Account");

            var claims = await _context.Claims
                .Include(c => c.Lecturer)
                .Where(c => c.Status == ClaimStatus.ApprovedByManager)
                .OrderByDescending(c => c.ApprovedAt)
                .ToListAsync();

            return View(claims);
        }

        // View all rejected claims
        public async Task<IActionResult> RejectedClaims()
        {
            var role = HttpContext.Session.GetString("UserRole")?.ToLower();
            if (role != "programmecoordinator")
                return RedirectToAction("Login", "Account");

            var claims = await _context.Claims
                .Include(c => c.Lecturer)
                .Where(c => c.Status == ClaimStatus.RejectedByCoordinator || c.Status == ClaimStatus.RejectedByManager)
                .OrderByDescending(c => c.RejectedAt)
                .ToListAsync();

            return View(claims);
        }
    }
}