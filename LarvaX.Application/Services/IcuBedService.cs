using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LarvaX.Application.Services
{
    public class IcuBedService : IIcuBedService
    {
        // Mock data, simulating a database or external API with freshness and cost.
        private readonly List<IcuHospitalDto> _hospitals = new()
        {
            new("Dhaka", "Dhaka Medical College Hospital", "Secretariat Road, Dhaka", "02-55165088", "Government tertiary hospital", DateTime.UtcNow.AddHours(-2), 500m),
            new("Dhaka", "Shaheed Suhrawardy Medical College Hospital", "Sher-e-Bangla Nagar, Dhaka", "02-9130800", "Government tertiary hospital", DateTime.UtcNow.AddHours(-5), 500m),
            new("Dhaka", "Bangladesh Medical University", "Shahbag, Dhaka", "02-55165760", "Specialist referral hospital", DateTime.UtcNow.AddMinutes(-30), 1000m),
            new("Dhaka", "Dhaka Shishu Hospital", "Sher-e-Bangla Nagar, Dhaka", "02-55059051", "Children's critical care", DateTime.UtcNow.AddHours(-1), 800m),
            new("Dhaka", "Square Hospitals Ltd.", "Panthapath, Dhaka", "10616", "Private hospital", DateTime.UtcNow.AddMinutes(-10), 15000m),
            new("Dhaka", "Evercare Hospital Dhaka", "Bashundhara, Dhaka", "10678", "Private hospital", DateTime.UtcNow.AddHours(-3), 20000m),
            new("Chattogram", "Chattogram Medical College Hospital", "Panchlaish, Chattogram", "031-619400", "Government tertiary hospital", DateTime.UtcNow.AddDays(-1), 500m),
            new("Rajshahi", "Rajshahi Medical College Hospital", "Laxmipur, Rajshahi", "0721-772150", "Government tertiary hospital", DateTime.UtcNow.AddHours(-12), 500m),
            new("Sylhet", "Sylhet MAG Osmani Medical College Hospital", "Medical Road, Sylhet", "0821-713667", "Government tertiary hospital", DateTime.UtcNow.AddHours(-8), 500m),
            new("Khulna", "Khulna Medical College Hospital", "Sonadanga, Khulna", "041-762945", "Government tertiary hospital", DateTime.UtcNow.AddDays(-2), 500m)
        };

        public Task<IEnumerable<IcuHospitalDto>> GetIcuHospitalsAsync(string? city = null)
        {
            var selectedCity = string.IsNullOrWhiteSpace(city) ? "" : city.Trim();
            
            var filtered = string.IsNullOrEmpty(selectedCity)
                ? _hospitals
                : _hospitals.Where(h => h.City.Equals(selectedCity, StringComparison.OrdinalIgnoreCase));
                
            return Task.FromResult(filtered.OrderByDescending(h => h.LastUpdated).AsEnumerable());
        }

        public Task<IEnumerable<string>> GetAvailableCitiesAsync()
        {
            var cities = _hospitals.Select(h => h.City).Distinct().OrderBy(c => c).AsEnumerable();
            return Task.FromResult(cities);
        }
    }
}
