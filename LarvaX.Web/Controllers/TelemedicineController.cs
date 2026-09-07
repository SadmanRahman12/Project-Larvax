using Microsoft.AspNetCore.Mvc;
using LarvaX.Infrastructure.Data;
using LarvaX.Core.Entities;
using Microsoft.AspNetCore.Identity;
using LarvaX.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace LarvaX.Web.Controllers
{
    public class TelemedicineController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TelemedicineController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Browse doctors
        public async Task<IActionResult> Index(string? specialty = null)
        {
            // Assumption: doctors are users in role "Doctor". Specialty is not modeled yet; show all doctors.
            var doctors = await _userManager.GetUsersInRoleAsync("Doctor");
            return View(doctors);
        }

        // GET: Book an appointment with a doctor
        [HttpGet]
        public async Task<IActionResult> Book(string doctorId)
        {
            var doctor = await _userManager.FindByIdAsync(doctorId);
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

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return RedirectToAction("Login", "Account");

            if (!DateTime.TryParse(model.ScheduledAt, out var dt))
            {
                ModelState.AddModelError(string.Empty, "Invalid date/time");
                return View(model);
            }

            var appointment = new Appointment
            {
                PatientId = userId,
                DoctorId = model.DoctorId!,
                ScheduledAt = dt.ToUniversalTime(),
                Status = AppointmentStatus.Booked,
                Notes = model.Notes,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Appointment booked successfully.";
            return RedirectToAction(nameof(Appointments));
        }

        // List appointments for current user (patient or doctor)
        public async Task<IActionResult> Appointments()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return RedirectToAction("Login", "Account");

            var asPatient = _context.Appointments
                .Include(a => a.Doctor)
                .Where(a => a.PatientId == userId);

            var asDoctor = _context.Appointments
                .Include(a => a.Patient)
                .Where(a => a.DoctorId == userId);

            var appointments = await asPatient.Union(asDoctor).OrderByDescending(a => a.ScheduledAt).ToListAsync();
            return View(appointments);
        }

        // Video consultation room
        public async Task<IActionResult> Room(int id)
        {
            var appt = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appt == null) return NotFound();

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return RedirectToAction("Login", "Account");

            // Only participants may open the room
            if (appt.PatientId != userId && appt.DoctorId != userId && !User.IsInRole("Administrator"))
                return Forbid();

            // Ensure VideoRoomId exists
            if (string.IsNullOrEmpty(appt.VideoRoomId))
            {
                appt.VideoRoomId = Guid.NewGuid().ToString();
                appt.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            ViewBag.Appointment = appt;
            return View();
        }
    }
}
