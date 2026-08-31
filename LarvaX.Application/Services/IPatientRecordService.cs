using System.Collections.Generic;
using System.Threading.Tasks;
using LarvaX.Core.Entities;

namespace LarvaX.Application.Services
{
    public interface IPatientRecordService
    {
        Task<IEnumerable<PatientRecord>> GetPatientRecordsAsync(string patientId);
        Task<PatientRecord?> GetPatientRecordByIdAsync(int id);
        Task<PatientRecord> AddPatientRecordAsync(string patientId, string title, string? description, string recordType, string? fileUrl = null);
        Task DeletePatientRecordAsync(int id);
    }
}
