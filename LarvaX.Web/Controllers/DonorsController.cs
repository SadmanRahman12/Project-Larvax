using LarvaX.Core.Entities;
using LarvaX.Infrastructure.Data;
using LarvaX.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LarvaX.Web.Controllers
{
    public class DonorsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DonorsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? bloodGroup = null, string? location = null)
        {
            var query = _context.Donors.Include(d => d.User).AsQueryable();

            if (!string.IsNullOrEmpty(bloodGroup) && Enum.TryParse<BloodGroup>(bloodGroup, out var bg))
            {
                query = query.Where(d => d.BloodGroup == bg);
            }

            if (!string.IsNullOrEmpty(location))
            {
                query = query.Where(d => d.Location != null && d.Location.Contains(location));
            }

            // Always show available donors first, then order by last confirmed
            var donors = await query
                .Where(d => d.IsAvailable)
                .OrderByDescending(d => d.LastConfirmedAvailable)
                .ToListAsync();

            ViewBag.SelectedBloodGroup = bloodGroup;
            ViewBag.SelectedLocation = location;

            return View(donors);
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(DonorRegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return RedirectToAction("Login", "Account");

            var donor = new Donor
            {
                UserId = userId,
                BloodGroup = model.BloodGroup,
                Latitude = model.Latitude,
                Longitude = model.Longitude,
                Location = model.Location,
                ContactNumber = model.ContactNumber,
                LastConfirmedAvailable = DateTime.UtcNow,
                IsAvailable = true
            };

            _context.Donors.Add(donor);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "You are now registered as a blood donor!";
            return RedirectToAction(nameof(Index));
        }
    }
}
