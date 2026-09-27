using LarvaX.Core.Entities;
using LarvaX.Core.Interfaces;
using LarvaX.Infrastructure.Data;
using LarvaX.Web.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace LarvaX.Web.Jobs
{
    /// <summary>
    /// Hangfire recurring job — runs hourly.
    /// Gap 5 fix: now processes ALL distinct regions found in recent reports,
    /// creating or updating a RiskZone per region instead of only "Dhaka Metropolitan Area".
    /// When no reports exist for a region the zone defaults to Low / Insufficient.
    /// </summary>
    public class RiskCalculationJob
    {
        private readonly ApplicationDbContext _context;
        private readonly IRiskAssessmentService _riskAssessmentService;
        private readonly IHubContext<AlertsHub> _alertsHub;
        private readonly ILogger<RiskCalculationJob> _logger;

        // Fallback seed zones with real coordinates for Dhaka divisions.
        // These are used when no report carries an explicit region tag yet.
        private static readonly Dictionary<string, (double Lat, double Lng, double Radius)> KnownRegions = new()
        {
            ["Dhaka Metropolitan Area"]    = (23.8103, 90.4125, 20000),
            ["Mirpur"]                     = (23.8223, 90.3654, 5000),
            ["Uttara"]                     = (23.8759, 90.3795, 5000),
            ["Mohammadpur"]                = (23.7630, 90.3580, 5000),
            ["Demra"]                      = (23.7253, 90.4623, 5000),
            ["Narayanganj"]                = (23.6238, 90.4987, 8000),
        };

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
            _logger.LogInformation("RiskCalculationJob starting — multi-zone run.");

            var cutoffDate = DateTime.UtcNow.AddDays(-14);

            // Pull recent verified reports
            var verifiedReports = await _context.Reports
                .Where(r => r.CreatedAt >= cutoffDate && r.Verification == ReportVerification.Verified)
                .ToListAsync();

            var allRecentReports = await _context.Reports
                .Where(r => r.CreatedAt >= cutoffDate)
                .ToListAsync();

            // Attribute each report to its nearest known region so one city's activity
            // does not incorrectly paint every region with the same risk level.
            var reportsByRegion = allRecentReports
                .Where(report => report.Latitude != 0 || report.Longitude != 0)
                .GroupBy(report => FindNearestRegion(report.Latitude, report.Longitude))
                .ToDictionary(group => group.Key, group => group.ToList());

            // Ensure every known region has a zone entry, including quiet green regions.
            foreach (var kvp in KnownRegions)
            {
                var regionName = kvp.Key;
                var (lat, lng, radius) = kvp.Value;

                reportsByRegion.TryGetValue(regionName, out var regionReports);
                regionReports ??= new List<Core.Entities.Report>();

                int verifiedCount = regionReports.Count(report => report.Verification == ReportVerification.Verified);
                int totalCount    = regionReports.Count;

                var riskLevel   = _riskAssessmentService.CalculateRiskLevel(verifiedCount, totalCount);
                var sufficiency = _riskAssessmentService.DetermineDataSufficiency(totalCount);
                var confidence  = _riskAssessmentService.CalculateConfidenceScore(totalCount);

                var zone = await _context.RiskZones.FirstOrDefaultAsync(z => z.Region == regionName);
                bool isNewOrElevated = false;

                if (zone == null)
                {
                    zone = new RiskZone
                    {
                        Region          = regionName,
                        DiseaseType     = DiseaseType.Dengue,
                        RiskLevel       = riskLevel,
                        ConfidenceScore = confidence,
                        DataSufficiency = sufficiency,
                        LastModelRun    = DateTime.UtcNow,
                        Latitude        = lat,
                        Longitude       = lng,
                        RadiusMetres    = radius
                    };
                    _context.RiskZones.Add(zone);
                    isNewOrElevated = true;
                }
                else
                {
                    if (zone.RiskLevel != riskLevel && riskLevel == RiskLevel.High)
                        isNewOrElevated = true;

                    zone.RiskLevel       = riskLevel;
                    zone.ConfidenceScore = confidence;
                    zone.DataSufficiency = sufficiency;
                    zone.LastModelRun    = DateTime.UtcNow;
                    // Keep coordinates from DB (admin may have refined them)
                    if (zone.Latitude == 0 && zone.Longitude == 0)
                    {
                        zone.Latitude     = lat;
                        zone.Longitude    = lng;
                        zone.RadiusMetres = radius;
                    }
                }

                await _context.SaveChangesAsync();

                // Broadcast SignalR alert only for High-risk elevation events
                if (isNewOrElevated || riskLevel == RiskLevel.High)
                {
                    await DispatchAlertAsync(zone, riskLevel);
                }
            }

            _logger.LogInformation("RiskCalculationJob completed — {Count} zones processed.", KnownRegions.Count);
        }

        private static string FindNearestRegion(double latitude, double longitude)
        {
            return KnownRegions
                .OrderBy(region => DistanceSquared(latitude, longitude, region.Value.Lat, region.Value.Lng))
                .Select(region => region.Key)
                .First();
        }

        private static double DistanceSquared(double latitude, double longitude, double regionLatitude, double regionLongitude)
        {
            var latitudeDelta = latitude - regionLatitude;
            var longitudeDelta = (longitude - regionLongitude) * Math.Cos(latitude * Math.PI / 180d);
            return (latitudeDelta * latitudeDelta) + (longitudeDelta * longitudeDelta);
        }

        private async Task DispatchAlertAsync(RiskZone zone, RiskLevel riskLevel)
        {
            string alertEn = _riskAssessmentService.GenerateAlertMessage(zone.Region, riskLevel, "Dengue", "en");
            string alertBn = _riskAssessmentService.GenerateAlertMessage(zone.Region, riskLevel, "Dengue", "bn");

            var alertEntity = new Alert
            {
                Message        = alertEn,
                Language       = "en",
                ZoneId         = zone.Id,
                RadiusKm       = zone.RadiusMetres / 1000.0,
                SentAt         = DateTime.UtcNow,
                DeliveredCount = 1
            };

            _context.Alerts.Add(alertEntity);
            await _context.SaveChangesAsync();

            await _alertsHub.Clients.All.SendAsync("ReceiveAlert", new
            {
                id        = alertEntity.Id,
                message   = alertEn,
                messageBn = alertBn,
                riskLevel = riskLevel.ToString(),
                region    = zone.Region,
                sentAt    = alertEntity.SentAt.ToString("o")
            });

            _logger.LogInformation("Alert dispatched for {Region} — {Level}", zone.Region, riskLevel);
        }
    }
}
