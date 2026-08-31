using System.Collections.Generic;
using System.Threading.Tasks;
using LarvaX.Core.Entities;

namespace LarvaX.Application.Services
{
    public interface ILabService
    {
        Task<IEnumerable<LabTest>> GetAvailableLabTestsAsync();
        Task<LabTest?> GetLabTestByIdAsync(int id);
        Task<LabBooking> BookLabTestAsync(string patientId, int labTestId, DateTime scheduledAt);
        Task<IEnumerable<LabBooking>> GetUserLabBookingsAsync(string userId);
        Task<IEnumerable<LabBooking>> GetAllBookingsAsync();
        Task<LabBooking?> GetLabBookingByIdAsync(int bookingId);
        Task UpdateBookingStatusAsync(int bookingId, string status, string? resultLink = null);
    }
}
