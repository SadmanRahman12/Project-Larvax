using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LarvaX.Application.Services;
using LarvaX.Web.Models;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace LarvaX.Web.Controllers
{
    /// <summary>
    /// Telemedicine portal — doctor browse is public; booking, appointments, and video room
    /// require an authenticated user. Appointments can only be booked during a doctor's fixed schedule.
    /// </summary>
    [Authorize]
    public class TelemedicineController : Controller
    {
        private readonly ITelemedicineService _telemedicineService;

        public TelemedicineController(ITelemedicineService telemedicineService)
        {
            _telemedicineService = telemedicineService;
        }

        // Browse doctors — intentionally public so anyone can see available doctors and schedules
        [AllowAnonymous]
        public async Task<IActionResult> Index(string? specialty = null)
        {
            var doctors = await _telemedicineService.GetAvailableDoctorsAsync(specialty);

            // Exclude the currently logged-in doctor so they do not see themselves in the booking directory
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(currentUserId))
            {
                doctors = doctors.Where(d => d.Id != currentUserId);
            }

            var scheduleSummaries = await _telemedicineService.GetDoctorScheduleSummariesAsync();
            ViewBag.ScheduleSummaries = scheduleSummaries;
            return View(doctors);
        }

        // GET: Book an appointment with a doctor
        [HttpGet]
        public async Task<IActionResult> Book(string doctorId, string? date = null)
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(currentUserId) && currentUserId == doctorId)
            {
                TempData["ErrorMessage"] = "You cannot book a consultation appointment with yourself.";
                return RedirectToAction(nameof(Index));
            }

            var doctor = await _telemedicineService.GetDoctorByIdAsync(doctorId);
            if (doctor == null) return NotFound();

            var schedules = await _telemedicineService.GetDoctorSchedulesAsync(doctorId);
            var activeSchedules = schedules.Where(s => s.IsActive).ToList();

            var model = new AppointmentViewModel
            {
                DoctorId = doctorId,
                DoctorName = doctor.FullName ?? doctor.UserName,
                DoctorSpecialty = doctor.Specialty,
                DoctorEmail = doctor.Email,
                HasSchedule = activeSchedules.Any(),
                Schedules = activeSchedules,
                WorkingDaysSummary = activeSchedules
                    .Select(s => s.DayOfWeek.ToString().Substring(0, 3))
                    .Distinct()
                    .ToList()
            };

            if (!activeSchedules.Any())
            {
                // Doctor has not configured a schedule yet
                return View(model);
            }

            // Determine default selected date: either parsed date or next available day of week
            DateTime targetDate;
            if (!string.IsNullOrEmpty(date) && DateTime.TryParse(date, out var parsedDate) && parsedDate.Date >= DateTime.Today)
            {
                targetDate = parsedDate.Date;
            }
            else
            {
                // Find nearest day that the doctor has an active schedule for
                targetDate = DateTime.Today;
                var scheduledDays = activeSchedules.Select(s => s.DayOfWeek).ToHashSet();
                
                // If today is a scheduled day, check if any slots are left today
                var slotsToday = await _telemedicineService.GetAvailableSlotsForDateAsync(doctorId, targetDate);
                if (!scheduledDays.Contains(targetDate.DayOfWeek) || !slotsToday.Any(s => s.IsAvailable))
                {
                    // Look for the next upcoming scheduled day (up to 14 days)
                    for (int i = 1; i <= 14; i++)
                    {
                        var candidate = DateTime.Today.AddDays(i);
                        if (scheduledDays.Contains(candidate.DayOfWeek))
                        {
                            targetDate = candidate;
                            break;
                        }
                    }
                }
            }

            model.SelectedDate = targetDate.ToString("yyyy-MM-dd");
            model.AvailableSlots = await _telemedicineService.GetAvailableSlotsForDateAsync(doctorId, targetDate);

            return View(model);
        }

        // AJAX endpoint to get available slots for any date
        [HttpGet]
        public async Task<IActionResult> GetSlots(string doctorId, string date)
        {
            if (string.IsNullOrEmpty(doctorId) || !DateTime.TryParse(date, out var parsedDate))
            {
                return BadRequest("Invalid doctor or date parameter.");
            }

            var slots = await _telemedicineService.GetAvailableSlotsForDateAsync(doctorId, parsedDate.Date);
            var schedules = await _telemedicineService.GetDoctorSchedulesAsync(doctorId);
            var worksOnThisDay = schedules.Any(s => s.IsActive && s.DayOfWeek == parsedDate.DayOfWeek);

            return Json(new
            {
                date = parsedDate.ToString("yyyy-MM-dd"),
                dayName = parsedDate.DayOfWeek.ToString(),
                worksOnThisDay = worksOnThisDay,
                slots = slots
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Book(AppointmentViewModel model)
        {
            var doctor = await _telemedicineService.GetDoctorByIdAsync(model.DoctorId ?? string.Empty);
            if (doctor == null) return NotFound();

            var schedules = await _telemedicineService.GetDoctorSchedulesAsync(model.DoctorId!);
            var activeSchedules = schedules.Where(s => s.IsActive).ToList();

            model.DoctorName = doctor.FullName ?? doctor.UserName;
            model.DoctorSpecialty = doctor.Specialty;
            model.DoctorEmail = doctor.Email;
            model.HasSchedule = activeSchedules.Any();
            model.Schedules = activeSchedules;
            model.WorkingDaysSummary = activeSchedules
                .Select(s => s.DayOfWeek.ToString().Substring(0, 3))
                .Distinct()
                .ToList();

            if (!activeSchedules.Any())
            {
                ModelState.AddModelError(string.Empty, "This doctor has not fixed their schedule yet. Consultations cannot be booked.");
                return View(model);
            }

            if (string.IsNullOrWhiteSpace(model.ScheduledAt) || !DateTime.TryParse(model.ScheduledAt, out var dt))
            {
                ModelState.AddModelError(nameof(model.ScheduledAt), "Please select an available appointment time slot.");
                if (DateTime.TryParse(model.SelectedDate, out var selDate))
                {
                    model.AvailableSlots = await _telemedicineService.GetAvailableSlotsForDateAsync(model.DoctorId!, selDate);
                }
                return View(model);
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return RedirectToAction("Login", "Account");

            if (userId == model.DoctorId)
            {
                ModelState.AddModelError(string.Empty, "You cannot book a consultation appointment with yourself.");
                if (DateTime.TryParse(model.SelectedDate, out var sDate))
                {
                    model.AvailableSlots = await _telemedicineService.GetAvailableSlotsForDateAsync(model.DoctorId!, sDate);
                }
                return View(model);
            }

            // Server-side strict schedule slot validation
            var (isValid, errorMessage) = await _telemedicineService.ValidateAppointmentSlotAsync(model.DoctorId!, dt);
            if (!isValid)
            {
                ModelState.AddModelError(string.Empty, errorMessage ?? "The selected time is not within the doctor's fixed schedule or is already taken.");
                model.AvailableSlots = await _telemedicineService.GetAvailableSlotsForDateAsync(model.DoctorId!, dt.Date);
                model.SelectedDate = dt.ToString("yyyy-MM-dd");
                return View(model);
            }

            await _telemedicineService.BookAppointmentAsync(userId, model.DoctorId!, dt, model.Notes);

            TempData["SuccessMessage"] = $"Appointment booked successfully with Dr. {model.DoctorName} for {dt:f}.";
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
