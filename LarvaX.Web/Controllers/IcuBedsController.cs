using Microsoft.AspNetCore.Mvc;
using LarvaX.Application.Services;
using System.Threading.Tasks;
using System.Linq;

namespace LarvaX.Web.Controllers;

public class IcuBedsController : Controller
{
    private readonly IIcuBedService _icuBedService;

    public IcuBedsController(IIcuBedService icuBedService)
    {
        _icuBedService = icuBedService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? city = null)
    {
        var hospitals = await _icuBedService.GetIcuHospitalsAsync(city);
        var cities = await _icuBedService.GetAvailableCitiesAsync();

        var model = new IcuBedsViewModel
        {
            Hospitals = hospitals.ToList(),
            SelectedCity = city ?? "",
            Cities = cities.ToList()
        };

        return View(model);
    }
}

public class IcuBedsViewModel
{
    public string SelectedCity { get; set; } = "";
    public List<string> Cities { get; set; } = new();
    public List<IcuHospitalDto> Hospitals { get; set; } = new();
}