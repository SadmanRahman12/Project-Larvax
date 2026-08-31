using LarvaX.Core.Entities;
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
    }
}
