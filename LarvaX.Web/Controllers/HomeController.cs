using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using LarvaX.Web.Models;
using LarvaX.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;

namespace LarvaX.Web.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;

    public HomeController(ApplicationDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> MapData()
    {
        var zones = await _context.RiskZones.ToListAsync();
        
        var mapData = zones.Select(z => new
        {
            region = z.Region,
            riskLevel = z.RiskLevel.ToString(),
            confidence = z.ConfidenceScore,
            // Assign dummy coordinates for known regions (in real app this would be in DB)
            lat = z.Region == "Dhaka Metropolitan Area" ? 23.8103 : 23.6850,
            lng = z.Region == "Dhaka Metropolitan Area" ? 90.4125 : 90.3563,
            radius = z.RiskLevel == Core.Entities.RiskLevel.High ? 5000 : 3000
        });

        return Json(mapData);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
