using LarvaX.Core.Entities;
using LarvaX.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LarvaX.Web.Controllers;

[Authorize(Roles = "GovernmentAuthority,Administrator")]
public class GovernmentController : Controller
{
    private readonly ApplicationDbContext _context;

    public GovernmentController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Dashboard()
    {
        var reports = await _context.Reports
            .OrderByDescending(r => r.CreatedAt)
            .Take(200)
            .ToListAsync();

        var riskZones = await _context.RiskZones
            .OrderByDescending(r => r.LastModelRun)
            .ToListAsync();

        var viewModel = new GovernmentDashboardViewModel
        {
            TotalReports = reports.Count,
            HighRiskZones = riskZones.Count(z => z.RiskLevel == RiskLevel.High),
            InsufficientDataZones = riskZones.Count(z => z.DataSufficiency == DataSufficiency.Insufficient),
            LatestReports = reports,
            RiskZones = riskZones
        };

        return View(viewModel);
    }

    [HttpGet]
    public IActionResult RiskZones()
    {
        var zones = _context.RiskZones
            .OrderByDescending(z => z.LastModelRun)
            .ToList();

        return View(zones);
    }

    [HttpGet]
    public IActionResult ExportCsv()
    {
        var items = _context.Reports
            .Select(r => new
            {
                r.Id,
                r.DiseaseType,
                r.Status,
                r.Verification,
                r.Latitude,
                r.Longitude,
                r.CreatedAt,
                r.UserId
            })
            .ToList();

        var csv = new System.Text.StringBuilder();
        csv.AppendLine("Id,DiseaseType,Status,Verification,Latitude,Longitude,CreatedAt,UserId");
        foreach (var item in items)
        {
            csv.AppendLine($"{item.Id},{item.DiseaseType},{item.Status},{item.Verification},{item.Latitude},{item.Longitude},{item.CreatedAt:O},{item.UserId}");
        }

        return File(System.Text.Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "larvax-reports.csv");
    }

    [HttpGet]
    public IActionResult ExportJson()
    {
        var items = _context.Reports
            .Select(r => new
            {
                r.Id,
                r.DiseaseType,
                r.Status,
                r.Verification,
                r.Latitude,
                r.Longitude,
                r.CreatedAt,
                r.UserId
            })
            .ToList();

        var json = System.Text.Json.JsonSerializer.Serialize(items);
        return File(System.Text.Encoding.UTF8.GetBytes(json), "application/json", "larvax-reports.json");
    }
}

public class GovernmentDashboardViewModel
{
    public int TotalReports { get; set; }
    public int HighRiskZones { get; set; }
    public int InsufficientDataZones { get; set; }
    public List<Report> LatestReports { get; set; } = new();
    public List<RiskZone> RiskZones { get; set; } = new();
}
