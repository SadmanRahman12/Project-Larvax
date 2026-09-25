using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LarvaX.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using LarvaX.Application.Services;
using LarvaX.Application.Models;
using LarvaX.Infrastructure.Data;

namespace LarvaX.Infrastructure.Services
{
    public class TelemedicineService : ITelemedicineService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TelemedicineService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IEnumerable<ApplicationUser>> GetAvailableDoctorsAsync(string? specialty = null)
        {
            var doctors = await _userManager.GetUsersInRoleAsync("Doctor");
            
            if (!string.IsNullOrWhiteSpace(specialty))
            {
                return doctors.Where(d => string.Equals(d.Specialty, specialty, StringComparison.OrdinalIgnoreCase));
            }
            
            return doctors;
        }

        public async Task<ApplicationUser?> GetDoctorByIdAsync(string doctorId)
        {
            return await _userManager.FindByIdAsync(doctorId);
        }

        public async Task<Appointment> BookAppointmentAsync(string patientId, string doctorId, DateTime scheduledAt, string? notes)
        {
            var appointment = new Appointment
            {
                PatientId = patientId,
                DoctorId = doctorId,
                ScheduledAt = scheduledAt.ToUniversalTime(),
                Status = AppointmentStatus.Booked,
                Notes = notes,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            return appointment;
        }

        public async Task<IEnumerable<Appointment>> GetUserAppointmentsAsync(string userId)
        {
            var asPatient = _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Patient)
                .Where(a => a.PatientId == userId);

            var asDoctor = _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Patient)
                .Where(a => a.DoctorId == userId);

            return await asPatient.Union(asDoctor)
                .OrderByDescending(a => a.ScheduledAt)
                .ToListAsync();
        }

        public async Task<Appointment?> GetAppointmentByIdAsync(int appointmentId)
        {
            return await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);
        }

        public async Task EnsureVideoRoomExistsAsync(Appointment appointment)
        {
            if (string.IsNullOrEmpty(appointment.VideoRoomId))
            {
                appointment.VideoRoomId = Guid.NewGuid().ToString();
                appointment.UpdatedAt = DateTime.UtcNow;
                _context.Appointments.Update(appointment);
                await _context.SaveChangesAsync();
            }
        }

        // ── Doctor Schedule Management ─────────────────────────────────────────

        public async Task<List<DoctorSchedule>> GetDoctorSchedulesAsync(string doctorId)
        {
            return await _context.DoctorSchedules
                .Where(s => s.DoctorId == doctorId)
                .OrderBy(s => s.DayOfWeek)
                .ThenBy(s => s.StartTime)
                .ToListAsync();
        }

        public async Task<DoctorSchedule?> GetDoctorScheduleByIdAsync(int id)
        {
            return await _context.DoctorSchedules.FindAsync(id);
        }

        public async Task AddDoctorScheduleAsync(DoctorSchedule schedule)
        {
            schedule.CreatedAt = DateTime.UtcNow;
            schedule.UpdatedAt = DateTime.UtcNow;
            _context.DoctorSchedules.Add(schedule);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteDoctorScheduleAsync(int id, string doctorId)
        {
            var item = await _context.DoctorSchedules.FirstOrDefaultAsync(s => s.Id == id && s.DoctorId == doctorId);
            if (item != null)
            {
                _context.DoctorSchedules.Remove(item);
                await _context.SaveChangesAsync();
            }
        }

        public async Task ToggleDoctorScheduleAsync(int id, string doctorId)
        {
            var item = await _context.DoctorSchedules.FirstOrDefaultAsync(s => s.Id == id && s.DoctorId == doctorId);
            if (item != null)
            {
                item.IsActive = !item.IsActive;
                item.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task SetQuickPresetScheduleAsync(string doctorId, string presetType)
        {
            if (presetType == "clear_all")
            {
                var existing = await _context.DoctorSchedules.Where(s => s.DoctorId == doctorId).ToListAsync();
                _context.DoctorSchedules.RemoveRange(existing);
                await _context.SaveChangesAsync();
                return;
            }

            // Days for presets
            var days = new List<DayOfWeek>();
            var startTime = TimeSpan.FromHours(9);
            var endTime = TimeSpan.FromHours(13);
            var shiftName = "Morning OPD";
            var slotDuration = 30;

            if (presetType == "weekday_morning")
            {
                // Sunday to Thursday (Bangladesh work week)
                days.AddRange(new[] { DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday });
                startTime = new TimeSpan(9, 0, 0);
                endTime = new TimeSpan(13, 0, 0);
                shiftName = "Morning Consultation";
            }
            else if (presetType == "weekday_evening")
            {
                // Sunday to Thursday evening
                days.AddRange(new[] { DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday });
                startTime = new TimeSpan(17, 0, 0);
                endTime = new TimeSpan(21, 0, 0);
                shiftName = "Evening Tele-clinic";
            }
            else if (presetType == "everyday_morning")
            {
                days.AddRange(new[] {
                    DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday,
                    DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday
                });
                startTime = new TimeSpan(9, 0, 0);
                endTime = new TimeSpan(12, 0, 0);
                shiftName = "Daily Consultation";
            }

            foreach (var day in days)
            {
                // Check if already exists for this day and startTime
                var exists = await _context.DoctorSchedules.AnyAsync(s => s.DoctorId == doctorId && s.DayOfWeek == day && s.StartTime == startTime);
                if (!exists)
                {
                    _context.DoctorSchedules.Add(new DoctorSchedule
                    {
                        DoctorId = doctorId,
                        DayOfWeek = day,
                        StartTime = startTime,
                        EndTime = endTime,
                        SlotDurationMinutes = slotDuration,
                        ShiftName = shiftName,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }

            await _context.SaveChangesAsync();
        }

        // ── Slot Generation & Validation ───────────────────────────────────────

        public async Task<List<DoctorAvailableSlotDto>> GetAvailableSlotsForDateAsync(string doctorId, DateTime date)
        {
            var dayOfWeek = date.DayOfWeek;

            var activeSchedules = await _context.DoctorSchedules
                .Where(s => s.DoctorId == doctorId && s.DayOfWeek == dayOfWeek && s.IsActive)
                .OrderBy(s => s.StartTime)
                .ToListAsync();

            if (!activeSchedules.Any())
            {
                return new List<DoctorAvailableSlotDto>();
            }

            // Get booked appointments for this doctor on this day
            var dayStartUtc = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
            var dayEndUtc = dayStartUtc.AddDays(1);

            var bookedAppointments = await _context.Appointments
                .Where(a => a.DoctorId == doctorId
                            && a.ScheduledAt >= dayStartUtc
                            && a.ScheduledAt < dayEndUtc
                            && a.Status != AppointmentStatus.Cancelled)
                .Select(a => a.ScheduledAt)
                .ToListAsync();

            var now = DateTime.Now;
            var result = new List<DoctorAvailableSlotDto>();

            foreach (var schedule in activeSchedules)
            {
                var durationMinutes = schedule.SlotDurationMinutes > 0 ? schedule.SlotDurationMinutes : 30;
                var duration = TimeSpan.FromMinutes(durationMinutes);

                for (var time = schedule.StartTime; time + duration <= schedule.EndTime; time += duration)
                {
                    var slotLocalDateTime = date.Date.Add(time);
                    var slotUtc = DateTime.SpecifyKind(slotLocalDateTime, DateTimeKind.Local).ToUniversalTime();

                    // Check if already booked
                    var isBooked = bookedAppointments.Any(bookedAt => Math.Abs((bookedAt - slotUtc).TotalMinutes) < (durationMinutes / 2.0));

                    // Check if in the past (only if date is today)
                    var isPast = slotLocalDateTime < now.AddMinutes(5);

                    var isAvailable = !isBooked && !isPast;
                    string? reason = null;
                    if (isBooked) reason = "Already Booked";
                    else if (isPast) reason = "Time has passed";

                    result.Add(new DoctorAvailableSlotDto
                    {
                        SlotDateTime = slotLocalDateTime,
                        TimeFormatted = DateTime.Today.Add(time).ToString("hh:mm tt"),
                        SlotValue = slotLocalDateTime.ToString("yyyy-MM-ddTHH:mm"),
                        IsAvailable = isAvailable,
                        ShiftName = schedule.ShiftName,
                        UnavailabilityReason = reason
                    });
                }
            }

            return result.OrderBy(s => s.SlotDateTime).ToList();
        }

        public async Task<(bool IsValid, string? ErrorMessage)> ValidateAppointmentSlotAsync(string doctorId, DateTime scheduledAt)
        {
            var now = DateTime.Now;
            if (scheduledAt < now.AddMinutes(5))
            {
                return (false, "Appointment time must be in the future.");
            }

            var dayOfWeek = scheduledAt.DayOfWeek;
            var activeSchedules = await _context.DoctorSchedules
                .Where(s => s.DoctorId == doctorId && s.DayOfWeek == dayOfWeek && s.IsActive)
                .ToListAsync();

            if (!activeSchedules.Any())
            {
                return (false, $"Dr. is not available on {dayOfWeek}s. Please choose a day within the doctor's scheduled hours.");
            }

            var timeOfDay = scheduledAt.TimeOfDay;
            bool matchesSlot = false;
            int slotDuration = 30;

            foreach (var schedule in activeSchedules)
            {
                var dur = schedule.SlotDurationMinutes > 0 ? schedule.SlotDurationMinutes : 30;
                var step = TimeSpan.FromMinutes(dur);

                for (var t = schedule.StartTime; t + step <= schedule.EndTime; t += step)
                {
                    if (Math.Abs((timeOfDay - t).TotalMinutes) < 1.0)
                    {
                        matchesSlot = true;
                        slotDuration = dur;
                        break;
                    }
                }
                if (matchesSlot) break;
            }

            if (!matchesSlot)
            {
                return (false, "The selected time does not match any available schedule slot for this doctor.");
            }

            // Check if already booked
            var slotUtc = scheduledAt.ToUniversalTime();
            var windowStart = slotUtc.AddMinutes(-slotDuration + 1);
            var windowEnd = slotUtc.AddMinutes(slotDuration - 1);

            var conflict = await _context.Appointments
                .AnyAsync(a => a.DoctorId == doctorId
                               && a.Status != AppointmentStatus.Cancelled
                               && a.ScheduledAt > windowStart
                               && a.ScheduledAt < windowEnd);

            if (conflict)
            {
                return (false, "This time slot is already booked. Please pick another available time slot.");
            }

            return (true, null);
        }

        public async Task<Dictionary<string, DoctorScheduleSummaryDto>> GetDoctorScheduleSummariesAsync()
        {
            var allSchedules = await _context.DoctorSchedules
                .Where(s => s.IsActive)
                .OrderBy(s => s.DayOfWeek)
                .ThenBy(s => s.StartTime)
                .ToListAsync();

            var grouped = allSchedules.GroupBy(s => s.DoctorId);
            var result = new Dictionary<string, DoctorScheduleSummaryDto>();

            foreach (var group in grouped)
            {
                var days = group.Select(s => s.DayOfWeek.ToString().Substring(0, 3)).Distinct().ToList();
                var earliest = group.Min(s => s.StartTime);
                var latest = group.Max(s => s.EndTime);

                var startStr = DateTime.Today.Add(earliest).ToString("hh:mm tt");
                var endStr = DateTime.Today.Add(latest).ToString("hh:mm tt");

                result[group.Key] = new DoctorScheduleSummaryDto
                {
                    DoctorId = group.Key,
                    HasSchedule = true,
                    AvailableDays = days,
                    SummaryText = $"{string.Join(", ", days)} ({startStr} - {endStr})"
                };
            }

            return result;
        }
    }
}
