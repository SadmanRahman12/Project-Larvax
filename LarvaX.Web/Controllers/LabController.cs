using Microsoft.AspNetCore.Mvc;
using LarvaX.Application.Services;
using System.Security.Claims;
using System.Threading.Tasks;
using System;

namespace LarvaX.Web.Controllers
{
    public class LabController : Controller
    {
        private readonly ILabService _labService;

        public LabController(ILabService labService)
        {
            _labService = labService;
        }

        public async Task<IActionResult> Index()
        {
            var tests = await _labService.GetAvailableLabTestsAsync();
            return View(tests);
        }

        [HttpGet]
        public async Task<IActionResult> Book(int id)
        {
            var test = await _labService.GetLabTestByIdAsync(id);
            if (test == null) return NotFound();

            var model = new LabBookingViewModel
            {
                LabTestId = id,
                TestName = test.Name,
                Cost = test.Cost,
                ScheduledAt = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-ddTHH:mm")
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Book(LabBookingViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return RedirectToAction("Login", "Account");

            if (!DateTime.TryParse(model.ScheduledAt, out var dt))
            {
                ModelState.AddModelError(string.Empty, "Invalid date/time");
                return View(model);
            }

            await _labService.BookLabTestAsync(userId, model.LabTestId, dt);

            TempData["SuccessMessage"] = "Lab test booked successfully.";
            return RedirectToAction(nameof(Bookings));
        }

        public async Task<IActionResult> Bookings()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return RedirectToAction("Login", "Account");

            var bookings = await _labService.GetUserLabBookingsAsync(userId);
            return View(bookings);
        }
    }

    public class LabBookingViewModel
    {
        public int LabTestId { get; set; }
        public string TestName { get; set; } = string.Empty;
        public decimal Cost { get; set; }
        public string ScheduledAt { get; set; } = string.Empty;
    }
}
