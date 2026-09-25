using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LarvaX.Core.Entities;
using LarvaX.Infrastructure.Data;
using LarvaX.Application.Services;
using LarvaX.Application.Models;
using LarvaX.Web.Models;

namespace LarvaX.Web.Controllers
{
    [Authorize(Roles = "Doctor,Administrator")]
    public class DoctorDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITelemedicineService _telemedicineService;

        public DoctorDashboardController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ITelemedicineService telemedicineService)
        {
            _context = context;
            _userManager = userManager;
            _telemedicineService = telemedicineService;
        }

        public async Task<IActionResult> Index()
        {
            var doctor = await _userManager.GetUserAsync(User);
            if (doctor == null)
                return RedirectToAction("Login", "Account");

            var doctorId = doctor.Id;
            var roles = await _userManager.GetRolesAsync(doctor);
            var todayUtc = DateTime.UtcNow.Date;

            // ── Appointments ────────────────────────────────────────────────
            var allDoctorAppointments = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Where(a => a.DoctorId == doctorId)
                .OrderBy(a => a.ScheduledAt)
                .ToListAsync();

            var todayAppointments = allDoctorAppointments
                .Where(a => a.ScheduledAt.Date == todayUtc)
                .ToList();

            var pendingRequests = allDoctorAppointments
                .Where(a => a.Status == AppointmentStatus.Booked)
                .Take(10)
                .ToList();

            var upcomingAppointments = allDoctorAppointments
                .Where(a => a.ScheduledAt >= DateTime.UtcNow &&
                            a.Status != AppointmentStatus.Cancelled &&
                            a.Status != AppointmentStatus.Completed)
                .Take(10)
                .ToList();

            var activeConsultations = allDoctorAppointments
                .Count(a => a.Status == AppointmentStatus.Confirmed);

            // ── Patients (distinct patients this doctor has had) ─────────────
            var patientIds = allDoctorAppointments
                .Select(a => a.PatientId)
                .Distinct()
                .ToList();

            // ── Patient Records (records of doctor's patients) ───────────────
            var recentPatientRecords = await _context.PatientRecords
                .Include(r => r.Patient)
                .Where(r => patientIds.Contains(r.PatientId))
                .OrderByDescending(r => r.Date)
                .Take(10)
                .ToListAsync();

            // ── Lab Results (lab bookings of doctor's patients) ──────────────
            var recentLabResults = await _context.LabBookings
                .Include(b => b.LabTest)
                .Include(b => b.Patient)
                .Where(b => patientIds.Contains(b.PatientId))
                .OrderByDescending(b => b.ScheduledAt)
                .Take(10)
                .ToListAsync();

            // ── Dengue Risk ──────────────────────────────────────────────────
            var highRiskZones = await _context.RiskZones
                .Where(z => z.RiskLevel == RiskLevel.High)
                .OrderByDescending(z => z.ConfidenceScore)
                .Take(5)
                .ToListAsync();

            // ── Recent Alerts ─────────────────────────────────────────────────
            var recentAlerts = await _context.Alerts
                .OrderByDescending(a => a.SentAt)
                .Take(8)
                .ToListAsync();

            var vm = new DoctorDashboardViewModel
            {
                Doctor = doctor,
                Roles = roles,

                TodayAppointmentsCount = todayAppointments.Count,
                PendingRequestsCount = pendingRequests.Count,
                ActiveConsultationsCount = activeConsultations,
                TotalPatientsCount = patientIds.Count,
                HighRiskPatientCount = 0, // placeholder — extend with real patient risk data

                TodayAppointments = todayAppointments,
                UpcomingAppointments = upcomingAppointments,
                PendingRequests = pendingRequests,

                RecentPatientRecords = recentPatientRecords,
                RecentLabResults = recentLabResults,

                ActiveHighRiskZonesCount = highRiskZones.Count,
                HighRiskZones = highRiskZones,
                RecentAlerts = recentAlerts,

                // Doctor Schedules
                Schedules = await _telemedicineService.GetDoctorSchedulesAsync(doctorId),
            };

            return View(vm);
        }

        // ── Doctor Schedule Actions ───────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSchedule(DoctorScheduleInputModel model)
        {
            var doctorId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(doctorId)) return RedirectToAction("Login", "Account");

            if (!TimeSpan.TryParse(model.StartTime, out var startTime) ||
                !TimeSpan.TryParse(model.EndTime, out var endTime))
            {
                TempData["ErrorMessage"] = "Invalid start or end time format.";
                return RedirectToAction(nameof(Index), new { fragment = "section-schedule" });
            }

            if (startTime >= endTime)
            {
                TempData["ErrorMessage"] = "Start time must be before end time.";
                return RedirectToAction(nameof(Index), new { fragment = "section-schedule" });
            }

            var schedule = new DoctorSchedule
            {
                DoctorId = doctorId,
                DayOfWeek = model.DayOfWeek,
                StartTime = startTime,
                EndTime = endTime,
                SlotDurationMinutes = model.SlotDurationMinutes > 0 ? model.SlotDurationMinutes : 30,
                ShiftName = string.IsNullOrWhiteSpace(model.ShiftName) ? $"{model.DayOfWeek} Session" : model.ShiftName.Trim(),
                IsActive = true
            };

            await _telemedicineService.AddDoctorScheduleAsync(schedule);
            TempData["SuccessMessage"] = $"Schedule added for {model.DayOfWeek} ({startTime:hh\\:mm} - {endTime:hh\\:mm}).";

            return Redirect(Url.Action(nameof(Index)) + "#section-schedule");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSchedule(int id)
        {
            var doctorId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(doctorId)) return RedirectToAction("Login", "Account");

            await _telemedicineService.DeleteDoctorScheduleAsync(id, doctorId);
            TempData["SuccessMessage"] = "Schedule slot deleted.";
            return Redirect(Url.Action(nameof(Index)) + "#section-schedule");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleSchedule(int id)
        {
            var doctorId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(doctorId)) return RedirectToAction("Login", "Account");

            await _telemedicineService.ToggleDoctorScheduleAsync(id, doctorId);
            TempData["SuccessMessage"] = "Schedule status updated.";
            return Redirect(Url.Action(nameof(Index)) + "#section-schedule");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickSchedulePreset(string presetType)
        {
            var doctorId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(doctorId)) return RedirectToAction("Login", "Account");

            await _telemedicineService.SetQuickPresetScheduleAsync(doctorId, presetType);
            TempData["SuccessMessage"] = presetType == "clear_all" 
                ? "All schedules cleared." 
                : "Schedule preset applied successfully!";
            return Redirect(Url.Action(nameof(Index)) + "#section-schedule");
        }

        // ── Appointment Actions ───────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmAppointment(int id)
        {
            var appt = await _context.Appointments.FindAsync(id);
            if (appt == null) return NotFound();

            var doctorId = _userManager.GetUserId(User);
            if (appt.DoctorId != doctorId && !User.IsInRole("Administrator"))
                return Forbid();

            appt.Status = AppointmentStatus.Confirmed;
            appt.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Appointment confirmed.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectAppointment(int id)
        {
            var appt = await _context.Appointments.FindAsync(id);
            if (appt == null) return NotFound();

            var doctorId = _userManager.GetUserId(User);
            if (appt.DoctorId != doctorId && !User.IsInRole("Administrator"))
                return Forbid();

            appt.Status = AppointmentStatus.Cancelled;
            appt.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Appointment rejected.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteAppointment(int id)
        {
            var appt = await _context.Appointments.FindAsync(id);
            if (appt == null) return NotFound();

            var doctorId = _userManager.GetUserId(User);
            if (appt.DoctorId != doctorId && !User.IsInRole("Administrator"))
                return Forbid();

            appt.Status = AppointmentStatus.Completed;
            appt.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Consultation marked as completed.";
            return RedirectToAction(nameof(Index));
        }
    }
}
