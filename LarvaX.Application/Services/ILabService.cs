using System.Collections.Generic;
using System.Threading.Tasks;
using LarvaX.Core.Entities;

namespace LarvaX.Application.Services
{
    public interface ILabService
    {
        Task<IEnumerable<LabTest>> GetAvailableLabTestsAsync();
        Task<IEnumerable<LabTest>> GetAllLabTestsAsync();
        Task<LabTest?> GetLabTestByIdAsync(int id);
        Task<LabBooking> BookLabTestAsync(string patientId, int labTestId, DateTime scheduledAt);
        Task<IEnumerable<LabBooking>> GetUserLabBookingsAsync(string userId);
        Task<IEnumerable<LabBooking>> GetAllBookingsAsync();
        Task<LabBooking?> GetLabBookingByIdAsync(int bookingId);
        Task UpdateBookingStatusAsync(int bookingId, string status, string? resultLink = null);
        Task UpdateSampleStatusAsync(int bookingId, string sampleStatus, string? rejectionReason = null);
        Task<LabBooking> SaveTestResultsAsync(int bookingId, int? plateletCount, int? wbcCount, double? hematocrit, string? dengueNs1, string? dengueIgm, string? dengueIgg, string? testNotes, string? resultLink, string? staffId);
        Task VerifyBookingResultAsync(int bookingId, string verifiedBy);
        Task<LabTest> SaveLabTestAsync(LabTest labTest);
        Task ToggleTestAvailabilityAsync(int id);
    }
}
