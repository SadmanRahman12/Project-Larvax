using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LarvaX.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using LarvaX.Application.Services;
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
    }
}
