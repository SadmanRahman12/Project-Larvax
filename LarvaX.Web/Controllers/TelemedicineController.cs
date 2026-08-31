using Microsoft.AspNetCore.Mvc;
using LarvaX.Application.Services;
using LarvaX.Web.Models;
using System.Security.Claims;
using System.Threading.Tasks;

namespace LarvaX.Web.Controllers
{
    public class TelemedicineController : Controller
    {
        private readonly ITelemedicineService _telemedicineService;

        public TelemedicineController(ITelemedicineService telemedicineService)
        {
            _telemedicineService = telemedicineService;
        }

        // Browse doctors
        public async Task<IActionResult> Index(string? specialty = null)
        {
            var doctors = await _telemedicineService.GetAvailableDoctorsAsync(specialty);
            return View(doctors);
        }

        // GET: Book an appointment with a doctor
        [HttpGet]
        public async Task<IActionResult> Book(string doctorId)
        {
            var doctor = await _telemedicineService.GetDoctorByIdAsync(doctorId);
            if (doctor == null) return NotFound();

            var model = new AppointmentViewModel
            {
                DoctorId = doctorId,
                DoctorName = doctor.FullName ?? doctor.UserName,
                ScheduledAt = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-ddTHH:mm")
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Book(AppointmentViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return RedirectToAction("Login", "Account");

            if (!DateTime.TryParse(model.ScheduledAt, out var dt))
            {
                ModelState.AddModelError(string.Empty, "Invalid date/time");
                return View(model);
            }

            await _telemedicineService.BookAppointmentAsync(userId, model.DoctorId!, dt, model.Notes);

            TempData["SuccessMessage"] = "Appointment booked successfully.";
            return RedirectToAction(nameof(Appointments));
        }

        // List appointments for current user
        public async Task<IActionResult> Appointments()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return RedirectToAction("Login", "Account");

            var appointments = await _telemedicineService.GetUserAppointmentsAsync(userId);
            return View(appointments);
        }

        // Video consultation room
        public async Task<IActionResult> Room(int id)
        {
            var appt = await _telemedicineService.GetAppointmentByIdAsync(id);
            if (appt == null) return NotFound();

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return RedirectToAction("Login", "Account");

            // Only participants may open the room
            if (appt.PatientId != userId && appt.DoctorId != userId && !User.IsInRole("Administrator"))
                return Forbid();

            await _telemedicineService.EnsureVideoRoomExistsAsync(appt);

            ViewBag.Appointment = appt;
            return View();
        }
    }
}
