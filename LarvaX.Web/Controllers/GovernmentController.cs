using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LarvaX.Core.Entities;
using LarvaX.Core.Interfaces;
using LarvaX.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LarvaX.Web.Controllers
{
    [Authorize(Roles = "GovernmentAuthority,Administrator")]
    public class GovernmentController : Controller
    {
        private readonly IPdfReportService _pdfReportService;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public GovernmentController(
            IPdfReportService pdfReportService,
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _pdfReportService = pdfReportService;
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var now = DateTime.UtcNow;

            // ── Dengue Case Statistics ──────────────────────────────────────
            var allCases = await _context.DengueCases.ToListAsync();
            var totalCases = allCases.Count;
            var confirmedCases = allCases.Count(c => c.Status == CaseStatus.Confirmed);
            var suspectedCases = allCases.Count(c => c.Status == CaseStatus.Suspected);
            var activeCases = allCases.Count(c =>
                c.Status == CaseStatus.Suspected ||
                c.Status == CaseStatus.UnderObservation ||
                c.Status == CaseStatus.Confirmed);
            var recoveringCases = allCases.Count(c => c.Status == CaseStatus.Recovering);
            var deceasedCases = allCases.Count(c => c.Status == CaseStatus.Deceased);

            // ── Risk Zones ──────────────────────────────────────────────────
            var riskZones = await _context.RiskZones.ToListAsync();
            var highRiskZonesCount = riskZones.Count(z => z.RiskLevel == RiskLevel.High);
            var mediumRiskZonesCount = riskZones.Count(z => z.RiskLevel == RiskLevel.Medium);
            var sufficientZones = riskZones.Count(z => z.DataSufficiency == DataSufficiency.Sufficient);
            var partialZones = riskZones.Count(z => z.DataSufficiency == DataSufficiency.PartiallySufficient);
            var insufficientZones = riskZones.Count(z => z.DataSufficiency == DataSufficiency.Insufficient);
            var overallRisk = highRiskZonesCount > 0 ? "High" : mediumRiskZonesCount > 0 ? "Medium" : "Low";
            var lastModelRun = riskZones.Any() ? riskZones.Max(z => z.LastModelRun) : (DateTime?)null;

            // ── Citizen Reports ─────────────────────────────────────────────
            var allReports = await _context.Reports.ToListAsync();
            var recentReports = allReports.Where(r => r.CreatedAt >= now.AddDays(-30)).ToList();
            var verifiedReports = allReports.Count(r => r.Verification == ReportVerification.Verified);
            var pendingReports = allReports.Count(r => r.Verification == ReportVerification.Pending);

            // ── Alerts ──────────────────────────────────────────────────────
            var recentAlerts = await _context.Alerts
                .Include(a => a.Zone)
                .OrderByDescending(a => a.SentAt)
                .Take(10)
                .ToListAsync();

            // ── Daily Case Trend (last 14 days) ────────────────────────────
            var trendStart = now.AddDays(-13).Date;
            var caseTrend = Enumerable.Range(0, 14)
                .Select(i => trendStart.AddDays(i))
                .Select(day => new
                {
                    Date = day.ToString("dd MMM"),
                    Count = allCases.Count(c => c.ReportedDate.Date == day)
                })
                .ToList();

            // Weekly comparison (current vs previous week)
            var thisWeekCases = allCases.Count(c => c.ReportedDate >= now.AddDays(-7));
            var prevWeekCases = allCases.Count(c => c.ReportedDate >= now.AddDays(-14) && c.ReportedDate < now.AddDays(-7));
            var weeklyChange = prevWeekCases > 0
                ? Math.Round(((double)(thisWeekCases - prevWeekCases) / prevWeekCases) * 100, 1)
                : (thisWeekCases > 0 ? 100.0 : 0.0);

            // Monthly comparison
            var thisMonthCases = allCases.Count(c => c.ReportedDate >= now.AddDays(-30));
            var prevMonthCases = allCases.Count(c => c.ReportedDate >= now.AddDays(-60) && c.ReportedDate < now.AddDays(-30));

            // ── Inventory & Resources ───────────────────────────────────────
            var inventoryItems = await _context.InventoryItems.ToListAsync();
            var lowStockCount = inventoryItems.Count(i => i.Quantity <= i.Threshold);
            var donors = await _context.Donors.Where(d => d.IsAvailable).ToListAsync();

            // ── Map data ────────────────────────────────────────────────────
            var mapZones = riskZones.Select(z => new
            {
                region = z.Region,
                riskLevel = z.RiskLevel.ToString(),
                lat = z.Latitude,
                lng = z.Longitude,
                radius = z.RadiusMetres,
                sufficiency = z.DataSufficiency.ToString(),
                confidence = Math.Round(z.ConfidenceScore * 100, 0)
            }).ToList();

            var mapCases = allCases.Select(c => new
            {
                lat = c.Latitude,
                lng = c.Longitude,
                status = c.Status.ToString(),
                severity = c.Severity.ToString(),
                name = c.PatientName,
                address = c.PatientAddress ?? ""
            }).ToList();

            var mapReports = allReports
                .Where(r => r.Verification != ReportVerification.Invalid)
                .Select(r => new
                {
                    lat = r.Latitude,
                    lng = r.Longitude,
                    diseaseType = r.DiseaseType.ToString(),
                    status = r.Status.ToString(),
                    verification = r.Verification.ToString()
                }).ToList();

            // ── Regional breakdown for bar chart ─────────────────────────
            var regionalCases = riskZones.Take(8).Select(z => new
            {
                region = z.Region,
                risk = z.RiskLevel.ToString()
            }).ToList();

            ViewBag.TotalCases = totalCases;
            ViewBag.ConfirmedCases = confirmedCases;
            ViewBag.SuspectedCases = suspectedCases;
            ViewBag.ActiveCases = activeCases;
            ViewBag.RecoveringCases = recoveringCases;
            ViewBag.DeceasedCases = deceasedCases;

            ViewBag.HighRiskZonesCount = highRiskZonesCount;
            ViewBag.MediumRiskZonesCount = mediumRiskZonesCount;
            ViewBag.OverallRisk = overallRisk;
            ViewBag.SufficientZones = sufficientZones;
            ViewBag.PartialZones = partialZones;
            ViewBag.InsufficientZones = insufficientZones;
            ViewBag.TotalZones = riskZones.Count;
            ViewBag.LastModelRun = lastModelRun?.ToString("dd MMM yyyy HH:mm") + " UTC";

            ViewBag.TotalReports = allReports.Count;
            ViewBag.VerifiedReports = verifiedReports;
            ViewBag.PendingReports = pendingReports;

            ViewBag.RecentAlerts = recentAlerts;

            ViewBag.CaseTrendLabels = System.Text.Json.JsonSerializer.Serialize(caseTrend.Select(t => t.Date).ToList());
            ViewBag.CaseTrendData = System.Text.Json.JsonSerializer.Serialize(caseTrend.Select(t => t.Count).ToList());
            ViewBag.ThisWeekCases = thisWeekCases;
            ViewBag.PrevWeekCases = prevWeekCases;
            ViewBag.WeeklyChange = weeklyChange;
            ViewBag.ThisMonthCases = thisMonthCases;
            ViewBag.PrevMonthCases = prevMonthCases;

            ViewBag.LowStockCount = lowStockCount;
            ViewBag.TotalInventoryItems = inventoryItems.Count;
            ViewBag.ActiveDonors = donors.Count;

            ViewBag.MapZones = System.Text.Json.JsonSerializer.Serialize(mapZones);
            ViewBag.MapCases = System.Text.Json.JsonSerializer.Serialize(mapCases);
            ViewBag.MapReports = System.Text.Json.JsonSerializer.Serialize(mapReports);
            ViewBag.RegionalCases = System.Text.Json.JsonSerializer.Serialize(regionalCases);

            ViewBag.DefaultStart = now.AddMonths(-1).ToString("yyyy-MM-dd");
            ViewBag.DefaultEnd = now.ToString("yyyy-MM-dd");

            // Available filter options for PDF
            ViewBag.Divisions = new List<string> {
                "ALL", "Dhaka", "Chattogram", "Rajshahi", "Khulna", "Sylhet", "Barisal", "Rangpur", "Mymensingh"
            };
            ViewBag.CaseStatuses = Enum.GetNames(typeof(CaseStatus)).ToList();
            ViewBag.RiskLevels = Enum.GetNames(typeof(RiskLevel)).ToList();

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ExportPdf(
            string startDate, string endDate,
            string? division = null, string? district = null,
            string? riskLevel = null, string? caseStatus = null)
        {
            if (!DateTime.TryParse(startDate, out var start))
                start = DateTime.UtcNow.AddMonths(-1);
            if (!DateTime.TryParse(endDate, out var end))
                end = DateTime.UtcNow;

            var pdfBytes = await _pdfReportService.GenerateGovernmentReportAsync(
                start, end, division, district, riskLevel, caseStatus);

            var divTag = string.IsNullOrWhiteSpace(division) || division == "ALL" ? "National" : division;
            var fileName = $"LarvaX_DGHS_Report_{divTag}_{start:yyyyMMdd}_{end:yyyyMMdd}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

        [HttpGet]
        public async Task<IActionResult> ExportCsv(string startDate, string endDate)
        {
            if (!DateTime.TryParse(startDate, out var start))
                start = DateTime.UtcNow.AddMonths(-1);
            if (!DateTime.TryParse(endDate, out var end))
                end = DateTime.UtcNow;

            var cases = await _context.DengueCases
                .Where(c => c.ReportedDate >= start && c.ReportedDate <= end)
                .OrderByDescending(c => c.ReportedDate)
                .ToListAsync();

            var csv = new System.Text.StringBuilder();
            csv.AppendLine("ID,PatientName,Age,Gender,Address,Status,Severity,PlateletCount,Hematocrit,ReportedDate,UpdatedAt,IsEscalated");
            foreach (var c in cases)
            {
                csv.AppendLine($"{c.Id},\"{c.PatientName}\",{c.Age},{c.Gender ?? ""},\"{c.PatientAddress ?? ""}\",{c.Status},{c.Severity},{c.PlateletCount},{c.Hematocrit},{c.ReportedDate:yyyy-MM-dd},{c.UpdatedAt:yyyy-MM-dd},{c.IsEscalated}");
            }

            var fileName = $"LarvaX_Cases_{start:yyyyMMdd}_{end:yyyyMMdd}.csv";
            return File(System.Text.Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", fileName);
        }
    }
}
