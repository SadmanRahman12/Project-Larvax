using Microsoft.AspNetCore.Mvc;

namespace LarvaX.Web.Controllers;

public class IcuBedsController : Controller
{
    private static readonly List<IcuFacility> Facilities = new()
    {
        new()
        {
            Name = "Bangabandhu Sheikh Mujib Medical University (BSMMU)",
            City = "Dhaka",
            Contact = "+880 2-55667788",
            Note = "Large tertiary referral hospital with intensive-care support; check directly before travel.",
            Type = "Public tertiary hospital"
        },
        new()
        {
            Name = "Dhaka Medical College Hospital",
            City = "Dhaka",
            Contact = "+880 2-55165760",
            Note = "Major emergency care centre in Dhaka; ICU status may change rapidly in peak dengue periods.",
            Type = "Teaching hospital"
        },
        new()
        {
            Name = "Apollo Hospitals Dhaka",
            City = "Dhaka",
            Contact = "+880 2-9889999",
            Note = "Private hospital with critical care and emergency admissions; phone first for bed confirmation.",
            Type = "Private hospital"
        },
        new()
        {
            Name = "Square Hospitals Ltd.",
            City = "Dhaka",
            Contact = "+880 2-8144466",
            Note = "Private multi-specialty facility with ICU and emergency support; confirm availability before arrival.",
            Type = "Private hospital"
        },
        new()
        {
            Name = "Chittagong Medical College Hospital",
            City = "Chattogram",
            Contact = "+880 31-619300",
            Note = "Regional tertiary hospital; emergency and ICU bed status should be confirmed by phone.",
            Type = "Regional public hospital"
        },
        new()
        {
            Name = "Sylhet MAG Osmani Medical College Hospital",
            City = "Sylhet",
            Contact = "+880 821-713282",
            Note = "Regional referral centre; ICU availability can change during outbreaks and transport surges.",
            Type = "Regional public hospital"
        }
    };

    public IActionResult Index()
    {
        var model = new IcuBedFinderViewModel
        {
            Facilities = Facilities,
            SearchText = string.Empty,
            LastUpdated = DateTime.Now
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Index(string searchText)
    {
        var filtered = string.IsNullOrWhiteSpace(searchText)
            ? Facilities
            : Facilities.Where(f =>
                f.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase)
                || f.City.Contains(searchText, StringComparison.OrdinalIgnoreCase)
                || f.Type.Contains(searchText, StringComparison.OrdinalIgnoreCase)).ToList();

        var model = new IcuBedFinderViewModel
        {
            Facilities = filtered,
            SearchText = searchText,
            LastUpdated = DateTime.Now
        };

        return View(model);
    }
}

public class IcuFacility
{
    public string Name { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Contact { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}

public class IcuBedFinderViewModel
{
    public List<IcuFacility> Facilities { get; set; } = new();
    public string SearchText { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; }
}
