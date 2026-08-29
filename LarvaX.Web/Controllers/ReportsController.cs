using System.Security.Claims;
using LarvaX.Core.Entities;
using LarvaX.Infrastructure.Data;
using LarvaX.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LarvaX.Web.Controllers
{
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public ReportsController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: /Reports
        public async Task<IActionResult> Index()
        {
            var reports = await _context.Reports
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(reports);
        }

        // GET: /Reports/Create
        public IActionResult Create()
        {
            return View(new ReportCreateViewModel());
        }

        // POST: /Reports/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ReportCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Check if logged in, otherwise require login or use fallback guest ID
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                // Redirect unauthenticated users to Login with returnUrl
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Create", "Reports") });
            }

            string? photoUrl = null;
            if (model.Photo != null && model.Photo.Length > 0)
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(model.Photo.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.Photo.CopyToAsync(stream);
                }

                photoUrl = "/uploads/" + uniqueFileName;
            }

            var report = new Report
            {
                UserId = userId,
                Latitude = model.Latitude,
                Longitude = model.Longitude,
                Description = model.Description,
                DiseaseType = model.DiseaseType,
                PhotoUrl = photoUrl,
                Status = ReportStatus.Received,
                Verification = ReportVerification.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Reports.Add(report);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Breeding site report submitted successfully! Status: Received.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Reports/MyReports
        [Authorize]
        public async Task<IActionResult> MyReports()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var myReports = await _context.Reports
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View("Index", myReports);
        }
    }
}
