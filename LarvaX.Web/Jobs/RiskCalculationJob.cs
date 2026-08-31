using LarvaX.Application.Services;
using LarvaX.Core.Entities;
using LarvaX.Infrastructure.Data;
using LarvaX.Web.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace LarvaX.Web.Jobs
{
    public class RiskCalculationJob
    {
        private readonly ApplicationDbContext _context;
        private readonly IRiskAssessmentService _riskAssessmentService;
        private readonly IHubContext<AlertsHub> _alertsHub;
        private readonly ILogger<RiskCalculationJob> _logger;

        public RiskCalculationJob(
            ApplicationDbContext context,
            IRiskAssessmentService riskAssessmentService,
            IHubContext<AlertsHub> alertsHub,
            ILogger<RiskCalculationJob> logger)
        {
            _context = context;
            _riskAssessmentService = riskAssessmentService;
            _alertsHub = alertsHub;
            _logger = logger;
        }

        public async Task ExecuteAsync()
        {
            _logger.LogInformation("Starting Hangfire Risk Calculation and Alert Broadcast job...");

            // Look at verified reports from the last 14 days
            var cutoffDate = DateTime.UtcNow.AddDays(-14);
            var verifiedReports = await _context.Reports
                .Where(r => r.CreatedAt >= cutoffDate && r.Verification == ReportVerification.Verified)
                .ToListAsync();

            int verifiedCount = verifiedReports.Count;
            int totalReports = await _context.Reports.CountAsync(r => r.CreatedAt >= cutoffDate);

            var riskLevel = _riskAssessmentService.CalculateRiskLevel(verifiedCount, totalReports);
            var sufficiency = _riskAssessmentService.DetermineDataSufficiency(totalReports);
            var confidence = _riskAssessmentService.CalculateConfidenceScore(totalReports);

            // Update or create default primary metropolitan zone (Dhaka Metropolitan)
            var zone = await _context.RiskZones.FirstOrDefaultAsync(z => z.Region == "Dhaka Metropolitan Area");
            bool isNewOrElevated = false;

            if (zone == null)
            {
                zone = new RiskZone
                {
                    Region = "Dhaka Metropolitan Area",
                    DiseaseType = DiseaseType.Dengue,
                    RiskLevel = riskLevel,
                    ConfidenceScore = confidence,
                    DataSufficiency = sufficiency,
                    LastModelRun = DateTime.UtcNow
                };
                _context.RiskZones.Add(zone);
                isNewOrElevated = true;
            }
            else
            {
                if (zone.RiskLevel != riskLevel && riskLevel == RiskLevel.High)
                {
                    isNewOrElevated = true;
                }
                zone.RiskLevel = riskLevel;
                zone.ConfidenceScore = confidence;
                zone.DataSufficiency = sufficiency;
                zone.LastModelRun = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            // If elevated or at high risk, dispatch real-time SignalR alert and record Alert entity
            if (isNewOrElevated || riskLevel == RiskLevel.High)
            {
                string alertEn = _riskAssessmentService.GenerateAlertMessage("Dhaka Metropolitan Area", riskLevel, "Dengue", "en");
                string alertBn = _riskAssessmentService.GenerateAlertMessage("Dhaka Metropolitan Area", riskLevel, "Dengue", "bn");

                var alertEntity = new Alert
                {
                    Message = alertEn,
                    Language = "en",
                    ZoneId = zone.Id,
                    RadiusKm = 25.0,
                    SentAt = DateTime.UtcNow,
                    DeliveredCount = 1
                };

                _context.Alerts.Add(alertEntity);
                await _context.SaveChangesAsync();

                // Broadcast live over SignalR to all connected users
                await _alertsHub.Clients.All.SendAsync("ReceiveAlert", new
                {
                    id = alertEntity.Id,
                    message = alertEn,
                    messageBn = alertBn,
                    riskLevel = riskLevel.ToString(),
                    region = "Dhaka Metropolitan Area",
                    sentAt = alertEntity.SentAt.ToString("o")
                });

                _logger.LogInformation("SignalR Alert dispatched for {Region} with Risk Level {RiskLevel}", zone.Region, riskLevel);
            }

            _logger.LogInformation("Hangfire Risk Calculation job completed successfully. Zone: {Region}, Level: {Level}", zone.Region, riskLevel);
        }
    }
}
