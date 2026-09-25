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
            dataSufficiency = z.DataSufficiency.ToString(),
            lastModelRun = z.LastModelRun.ToString("o"),
            // Use real stored coordinates — populated by RiskCalculationJob (Gap 6 fix)
            lat = z.Latitude,
            lng = z.Longitude,
            radius = z.RadiusMetres
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
