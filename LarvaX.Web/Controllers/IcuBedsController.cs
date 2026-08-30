using Microsoft.AspNetCore.Mvc;

namespace LarvaX.Web.Controllers;

public class IcuBedsController : Controller
{
    [HttpGet]
    public IActionResult Index(string? city = null)
    {
        var hospitals = new List<IcuHospital>
        {
            new("Dhaka", "Dhaka Medical College Hospital", "Secretariat Road, Dhaka", "02-55165088", "Government tertiary hospital"),
            new("Dhaka", "Shaheed Suhrawardy Medical College Hospital", "Sher-e-Bangla Nagar, Dhaka", "02-9130800", "Government tertiary hospital"),
            new("Dhaka", "Bangladesh Medical University", "Shahbag, Dhaka", "02-55165760", "Specialist referral hospital"),
            new("Dhaka", "Dhaka Shishu Hospital", "Sher-e-Bangla Nagar, Dhaka", "02-55059051", "Children's critical care"),
            new("Dhaka", "Square Hospitals Ltd.", "Panthapath, Dhaka", "10616", "Private hospital"),
            new("Dhaka", "Evercare Hospital Dhaka", "Bashundhara, Dhaka", "10678", "Private hospital"),
            new("Chattogram", "Chattogram Medical College Hospital", "Panchlaish, Chattogram", "031-619400", "Government tertiary hospital"),
            new("Rajshahi", "Rajshahi Medical College Hospital", "Laxmipur, Rajshahi", "0721-772150", "Government tertiary hospital"),
            new("Sylhet", "Sylhet MAG Osmani Medical College Hospital", "Medical Road, Sylhet", "0821-713667", "Government tertiary hospital"),
            new("Khulna", "Khulna Medical College Hospital", "Sonadanga, Khulna", "041-762945", "Government tertiary hospital")
        };

        var selectedCity = string.IsNullOrWhiteSpace(city) ? "" : city.Trim();
        var filtered = string.IsNullOrEmpty(selectedCity)
            ? hospitals
            : hospitals.Where(h => h.City.Equals(selectedCity, StringComparison.OrdinalIgnoreCase)).ToList();

        var model = new IcuBedsViewModel
        {
            Hospitals = filtered,
            SelectedCity = selectedCity,
            Cities = hospitals.Select(h => h.City).Distinct().OrderBy(c => c).ToList()
        };

        return View(model);
    }
}

public record IcuHospital(string City, string Name, string Address, string Phone, string Type);

public class IcuBedsViewModel
{
    public string SelectedCity { get; set; } = "";
    public List<string> Cities { get; set; } = new();
    public List<IcuHospital> Hospitals { get; set; } = new();
}