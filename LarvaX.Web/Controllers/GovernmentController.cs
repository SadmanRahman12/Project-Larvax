using LarvaX.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LarvaX.Web.Controllers
{
    [Authorize(Roles = "GovernmentAuthority,Administrator")]
    public class GovernmentController : Controller
    {
        private readonly IPdfReportService _pdfReportService;
        private readonly IAnalyticsService _analyticsService;

        public GovernmentController(IPdfReportService pdfReportService, IAnalyticsService analyticsService)
        {
            _pdfReportService = pdfReportService;
            _analyticsService = analyticsService;
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var flows = new[] { "CitizenReport", "LabBooking", "BloodDonor", "SymptomCheck" };
            var abandonmentStats = new Dictionary<string, double>();
            var flowStats = new Dictionary<string, Dictionary<string, int>>();

            foreach (var flow in flows)
            {
                abandonmentStats[flow] = await _analyticsService.GetAbandonmentRateAsync(flow);
                flowStats[flow] = await _analyticsService.GetFlowStatsAsync(flow);
            }

            ViewBag.AbandonmentStats = abandonmentStats;
            ViewBag.FlowStats = flowStats;
            ViewBag.DefaultStart = DateTime.UtcNow.AddMonths(-1).ToString("yyyy-MM-dd");
            ViewBag.DefaultEnd = DateTime.UtcNow.ToString("yyyy-MM-dd");

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ExportPdf(string startDate, string endDate)
        {
            if (!DateTime.TryParse(startDate, out var start))
                start = DateTime.UtcNow.AddMonths(-1);
            if (!DateTime.TryParse(endDate, out var end))
                end = DateTime.UtcNow;

            var pdfBytes = await _pdfReportService.GenerateGovernmentReportAsync(start, end);
            var fileName = $"LarvaX_Report_{start:yyyyMMdd}_{end:yyyyMMdd}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }
    }
}
