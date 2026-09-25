using LarvaX.Core.Entities;
using LarvaX.Application.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LarvaX.Application.Services
{
    public interface ITelemedicineService
    {
        Task<IEnumerable<ApplicationUser>> GetAvailableDoctorsAsync(string? specialty = null);
        Task<ApplicationUser?> GetDoctorByIdAsync(string doctorId);
        Task<Appointment> BookAppointmentAsync(string patientId, string doctorId, DateTime scheduledAt, string? notes);
        Task<IEnumerable<Appointment>> GetUserAppointmentsAsync(string userId);
        Task<Appointment?> GetAppointmentByIdAsync(int appointmentId);
        Task EnsureVideoRoomExistsAsync(Appointment appointment);

        // Schedule Management
        Task<List<DoctorSchedule>> GetDoctorSchedulesAsync(string doctorId);
        Task<DoctorSchedule?> GetDoctorScheduleByIdAsync(int id);
        Task AddDoctorScheduleAsync(DoctorSchedule schedule);
        Task DeleteDoctorScheduleAsync(int id, string doctorId);
        Task ToggleDoctorScheduleAsync(int id, string doctorId);
        Task SetQuickPresetScheduleAsync(string doctorId, string presetType);

        // Schedule & Slots for Booking
        Task<List<DoctorAvailableSlotDto>> GetAvailableSlotsForDateAsync(string doctorId, DateTime date);
        Task<(bool IsValid, string? ErrorMessage)> ValidateAppointmentSlotAsync(string doctorId, DateTime scheduledAt);
        Task<Dictionary<string, DoctorScheduleSummaryDto>> GetDoctorScheduleSummariesAsync();
    }
}
