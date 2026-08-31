using System.Collections.Generic;
using System.Threading.Tasks;

namespace LarvaX.Application.Services
{
    public record IcuHospitalDto(string City, string Name, string Address, string Phone, string Type, DateTime LastUpdated, decimal EstimatedCostPerDay);

    public interface IIcuBedService
    {
        Task<IEnumerable<IcuHospitalDto>> GetIcuHospitalsAsync(string? city = null);
        Task<IEnumerable<string>> GetAvailableCitiesAsync();
    }
}
