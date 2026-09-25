using LarvaX.Application.Services;
using LarvaX.Core.Entities;
using LarvaX.Core.Interfaces;
using LarvaX.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LarvaX.Web.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IReportService _reportService;

        public AdminController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IReportService reportService)
        {
            _context = context;
            _userManager = userManager;
            _reportService = reportService;
        }

        public async Task<IActionResult> Index()
        {
            var pendingUsers = await _userManager.Users.CountAsync(u => !u.IsApproved);
            var pendingReports = await _context.Reports.CountAsync(r => r.Verification == ReportVerification.Pending);

            ViewBag.PendingUsersCount = pendingUsers;
            ViewBag.PendingReportsCount = pendingReports;

            return View();
        }

        public async Task<IActionResult> Approvals()
        {
            var pendingUsers = await _userManager.Users
                .Where(u => !u.IsApproved)
                .ToListAsync();
            return View(pendingUsers);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                user.IsApproved = true;
                await _userManager.UpdateAsync(user);
                TempData["SuccessMessage"] = $"User {user.FullName} approved successfully.";
            }
            return RedirectToAction(nameof(Approvals));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectUser(string id, string reason)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                user.RejectionReason = reason;
                user.IsRejected = true;   // permanently blocks login — Gap 4 fix
                user.IsApproved = false;
                await _userManager.UpdateAsync(user);
                TempData["SuccessMessage"] = $"User {user.FullName} rejected and blocked.";
            }
            return RedirectToAction(nameof(Approvals));
        }

        public async Task<IActionResult> Reports(string? tab = "pending")
        {
            var query = _context.Reports.Include(r => r.User).AsQueryable();

            if (tab == "under_review")
            {
                query = query.Where(r => r.Status == ReportStatus.UnderReview);
            }
            else if (tab == "resolved")
            {
                query = query.Where(r => r.Status == ReportStatus.Resolved);
            }
            else
            {
                tab = "pending";
                query = query.Where(r => r.Verification == ReportVerification.Pending);
            }

            ViewBag.ActiveTab = tab;
            var reports = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
            return View(reports);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyReport(int id)
        {
            var report = await _context.Reports.FindAsync(id);
            if (report != null)
            {
                report.Verification = ReportVerification.Verified;
                report.Status = _reportService.DetermineNextStatus(report.Status, ReportVerification.Verified);
                report.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Report #{id} verified. Status updated to {report.Status}.";
            }
            return RedirectToAction(nameof(Reports));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectReport(int id, string reason)
        {
            var report = await _context.Reports.FindAsync(id);
            if (report != null)
            {
                report.Verification = ReportVerification.Invalid;
                report.RejectionReason = reason;
                report.Status = _reportService.DetermineNextStatus(report.Status, ReportVerification.Invalid);
                report.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Report #{id} marked as invalid.";
            }
            return RedirectToAction(nameof(Reports));
        }

        /// <summary>
        /// Moves a verified (UnderReview) report to Resolved — completing the status lifecycle.
        /// Fixes Gap 12: ReportStatus.UnderReview was never transitioned to Resolved.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResolveReport(int id)
        {
            var report = await _context.Reports.FindAsync(id);
            if (report != null && _reportService.CanTransition(report.Status, ReportStatus.Resolved))
            {
                report.Status = ReportStatus.Resolved;
                report.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Report #{id} marked as Resolved.";
            }
            return RedirectToAction(nameof(Reports), new { tab = "under_review" });
        }
    }
}
