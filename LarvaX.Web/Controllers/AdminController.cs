using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using LarvaX.Application.Services;
using LarvaX.Core.Entities;
using LarvaX.Core.Interfaces;
using LarvaX.Infrastructure.Data;
using LarvaX.Web.Hubs;
using LarvaX.Web.Jobs;
using LarvaX.Web.Models;
using LarvaX.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace LarvaX.Web.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IReportService _reportService;
        private readonly IHubContext<AlertsHub> _alertsHub;
        private readonly RiskCalculationJob _riskCalculationJob;
        private readonly IAnalyticsService _analyticsService;
        private readonly IAdminSettingsService _settingsService;

        public AdminController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IReportService reportService,
            IHubContext<AlertsHub> alertsHub,
            RiskCalculationJob riskCalculationJob,
            IAnalyticsService analyticsService,
            IAdminSettingsService settingsService)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _reportService = reportService;
            _alertsHub = alertsHub;
            _riskCalculationJob = riskCalculationJob;
            _analyticsService = analyticsService;
            _settingsService = settingsService;
        }

        // ==========================================
        // 1. DASHBOARD OVERVIEW
        // ==========================================
        public async Task<IActionResult> Index()
        {
            var now = DateTime.UtcNow;

            var totalUsers = await _userManager.Users.CountAsync();
            var pendingApprovalsList = await _userManager.Users
                .Where(u => !u.IsApproved && !u.IsRejected)
                .OrderByDescending(u => u.Id)
                .Take(5)
                .ToListAsync();
            var pendingApprovalsCount = await _userManager.Users.CountAsync(u => !u.IsApproved && !u.IsRejected);

            var allReports = await _context.Reports.Include(r => r.User).ToListAsync();
            var pendingReportsCount = allReports.Count(r => r.Verification == ReportVerification.Pending);
            var verifiedReportsCount = allReports.Count(r => r.Verification == ReportVerification.Verified);
            var rejectedReportsCount = allReports.Count(r => r.Verification == ReportVerification.Invalid);
            var pendingReportsList = allReports
                .Where(r => r.Verification == ReportVerification.Pending)
                .OrderByDescending(r => r.CreatedAt)
                .Take(5)
                .ToList();

            var riskZones = await _context.RiskZones.ToListAsync();
            var highRiskZonesCount = riskZones.Count(z => z.RiskLevel == RiskLevel.High);

            var allAlerts = await _context.Alerts.Include(a => a.Zone).OrderByDescending(a => a.SentAt).ToListAsync();
            var activeAlertsCount = allAlerts.Count(a => a.SentAt >= now.AddDays(-7));
            var recentAlertsList = allAlerts.Take(5).ToList();

            var totalCases = await _context.DengueCases.CountAsync();

            // Map overlays
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

            var mapReports = allReports
                .Where(r => r.Verification != ReportVerification.Invalid)
                .Take(50)
                .Select(r => new
                {
                    id = r.Id,
                    lat = r.Latitude,
                    lng = r.Longitude,
                    diseaseType = r.DiseaseType.ToString(),
                    status = r.Status.ToString(),
                    verification = r.Verification.ToString(),
                    desc = r.Description ?? "No description"
                }).ToList();

            var mapCases = await _context.DengueCases
                .Take(50)
                .Select(c => new
                {
                    lat = c.Latitude,
                    lng = c.Longitude,
                    status = c.Status.ToString(),
                    severity = c.Severity.ToString(),
                    name = c.PatientName
                }).ToListAsync();

            // Analytics summary figures
            var usersByRole = new Dictionary<string, int>();
            var roles = new[] { "Citizen", "Doctor", "HealthWorker", "LabStaff", "Administrator", "GovernmentAuthority" };
            foreach (var r in roles)
            {
                var usersInRole = await _userManager.GetUsersInRoleAsync(r);
                usersByRole[r] = usersInRole.Count;
            }

            var reportsByStatus = allReports
                .GroupBy(r => r.Status.ToString())
                .ToDictionary(g => g.Key, g => g.Count());

            var flows = new[] { "CitizenReport", "SymptomChecker", "TelemedicineBooking", "LabBooking" };
            var abandonmentRates = new Dictionary<string, double>();
            foreach (var f in flows)
            {
                abandonmentRates[f] = await _analyticsService.GetAbandonmentRateAsync(f);
            }

            // Synthesize recent activities
            var activities = await GenerateRecentActivitiesAsync(10);

            var viewModel = new AdminDashboardOverviewViewModel
            {
                TotalUsersCount = totalUsers,
                PendingApprovalsCount = pendingApprovalsCount,
                PendingReportsCount = pendingReportsCount,
                VerifiedReportsCount = verifiedReportsCount,
                RejectedReportsCount = rejectedReportsCount,
                ActiveAlertsCount = activeAlertsCount,
                HighRiskZonesCount = highRiskZonesCount,
                TotalDengueCasesCount = totalCases,
                PendingApprovals = pendingApprovalsList,
                PendingReports = pendingReportsList,
                RecentAlerts = recentAlertsList,
                RecentActivities = activities,
                MapZonesJson = JsonSerializer.Serialize(mapZones),
                MapReportsJson = JsonSerializer.Serialize(mapReports),
                MapCasesJson = JsonSerializer.Serialize(mapCases),
                UsersByRole = usersByRole,
                ReportsByStatus = reportsByStatus,
                AbandonmentRates = abandonmentRates
            };

            return View(viewModel);
        }

        // ==========================================
        // 2. USER MANAGEMENT
        // ==========================================
        public async Task<IActionResult> Users(string? search, string? role, string? status)
        {
            var users = await _userManager.Users.ToListAsync();
            var userList = new List<AdminUserListItemViewModel>();

            var allRoles = await _roleManager.Roles.Select(r => r.Name!).ToListAsync();
            var roleCounts = allRoles.ToDictionary(r => r, _ => 0);

            foreach (var u in users)
            {
                var userRoles = await _userManager.GetRolesAsync(u);
                foreach (var r in userRoles)
                {
                    if (roleCounts.ContainsKey(r)) roleCounts[r]++;
                }

                userList.Add(new AdminUserListItemViewModel
                {
                    Id = u.Id,
                    FullName = u.FullName ?? u.UserName,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    Roles = userRoles,
                    IsApproved = u.IsApproved,
                    IsRejected = u.IsRejected,
                    RejectionReason = u.RejectionReason,
                    ModePreference = u.ModePreference ?? "Citizen",
                    Specialty = u.Specialty,
                    Address = u.Address,
                    LockoutEnabled = u.LockoutEnabled,
                    LockoutEnd = u.LockoutEnd,
                    EmailConfirmed = u.EmailConfirmed
                });
            }

            // Apply Filters
            var filtered = userList.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLowerInvariant();
                filtered = filtered.Where(u =>
                    (u.FullName != null && u.FullName.ToLowerInvariant().Contains(term)) ||
                    (u.Email != null && u.Email.ToLowerInvariant().Contains(term)) ||
                    (u.PhoneNumber != null && u.PhoneNumber.Contains(term)) ||
                    (u.Specialty != null && u.Specialty.ToLowerInvariant().Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(role) && role != "All")
            {
                filtered = filtered.Where(u => u.Roles.Contains(role));
            }

            if (!string.IsNullOrWhiteSpace(status) && status != "All")
            {
                if (status == "Approved")
                    filtered = filtered.Where(u => u.IsApproved && !u.IsRejected && !u.IsLockedOut);
                else if (status == "Pending")
                    filtered = filtered.Where(u => !u.IsApproved && !u.IsRejected);
                else if (status == "Rejected")
                    filtered = filtered.Where(u => u.IsRejected);
                else if (status == "Locked")
                    filtered = filtered.Where(u => u.IsLockedOut);
            }

            var vm = new AdminUserManagementViewModel
            {
                Users = filtered.OrderBy(u => u.FullName).ToList(),
                SearchTerm = search,
                SelectedRole = role ?? "All",
                SelectedStatus = status ?? "All",
                RoleCounts = roleCounts,
                AvailableRoles = allRoles
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserLockout(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(Users));
            }

            var isCurrentlyLocked = await _userManager.IsLockedOutAsync(user);
            if (isCurrentlyLocked)
            {
                await _userManager.SetLockoutEndDateAsync(user, null);
                TempData["SuccessMessage"] = $"Account for {user.FullName ?? user.Email} has been activated/unlocked.";
            }
            else
            {
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(50));
                TempData["SuccessMessage"] = $"Account for {user.FullName ?? user.Email} has been suspended/locked out.";
            }

            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeUserRole(string id, string newRole)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(Users));
            }

            if (!await _roleManager.RoleExistsAsync(newRole))
            {
                TempData["ErrorMessage"] = $"Role '{newRole}' does not exist.";
                return RedirectToAction(nameof(Users));
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, newRole);

            user.ModePreference = (newRole == "Citizen") ? "Citizen" : "Professional";
            await _userManager.UpdateAsync(user);

            TempData["SuccessMessage"] = $"Role for {user.FullName ?? user.Email} successfully updated to '{newRole}'.";
            return RedirectToAction(nameof(Users));
        }

        // ==========================================
        // 2b. PROFESSIONAL APPROVALS
        // ==========================================
        public async Task<IActionResult> Approvals()
        {
            var pendingUsers = await _userManager.Users
                .Where(u => !u.IsApproved && !u.IsRejected)
                .OrderByDescending(u => u.Id)
                .ToListAsync();
            return View(pendingUsers);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                user.IsApproved = true;
                user.IsRejected = false;
                user.RejectionReason = null;
                await _userManager.UpdateAsync(user);
                TempData["SuccessMessage"] = $"User {user.FullName ?? user.Email} approved successfully.";
            }
            return RedirectToAction(nameof(Approvals));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectUser(string id, string reason)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                user.RejectionReason = reason;
                user.IsRejected = true;
                user.IsApproved = false;
                await _userManager.UpdateAsync(user);
                TempData["SuccessMessage"] = $"User {user.FullName ?? user.Email} rejected and blocked.";
            }
            return RedirectToAction(nameof(Approvals));
        }

        // ==========================================
        // 3. REPORT VERIFICATION QUEUE
        // ==========================================
        public async Task<IActionResult> Reports(string? tab = "pending", string? disease = null, string? search = null)
        {
            var query = _context.Reports.Include(r => r.User).AsQueryable();

            if (tab == "under_review")
            {
                query = query.Where(r => r.Status == ReportStatus.UnderReview);
            }
            else if (tab == "verified")
            {
                query = query.Where(r => r.Verification == ReportVerification.Verified && r.Status != ReportStatus.UnderReview);
            }
            else if (tab == "invalid")
            {
                query = query.Where(r => r.Verification == ReportVerification.Invalid);
            }
            else
            {
                tab = "pending";
                query = query.Where(r => r.Verification == ReportVerification.Pending);
            }

            if (!string.IsNullOrWhiteSpace(disease) && Enum.TryParse<DiseaseType>(disease, true, out var dt))
            {
                query = query.Where(r => r.DiseaseType == dt);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLowerInvariant();
                query = query.Where(r =>
                    (r.Description != null && r.Description.ToLower().Contains(term)) ||
                    (r.FieldNotes != null && r.FieldNotes.ToLower().Contains(term)) ||
                    (r.User.FullName != null && r.User.FullName.ToLower().Contains(term)));
            }

            ViewBag.ActiveTab = tab;
            ViewBag.SelectedDisease = disease;
            ViewBag.SearchTerm = search;

            // Summary counts for tabs
            ViewBag.PendingCount = await _context.Reports.CountAsync(r => r.Verification == ReportVerification.Pending);
            ViewBag.UnderReviewCount = await _context.Reports.CountAsync(r => r.Status == ReportStatus.UnderReview);
            ViewBag.VerifiedCount = await _context.Reports.CountAsync(r => r.Verification == ReportVerification.Verified && r.Status != ReportStatus.UnderReview);
            ViewBag.InvalidCount = await _context.Reports.CountAsync(r => r.Verification == ReportVerification.Invalid);

            var reports = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
            return View(reports);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyReport(int id)
        {
            var report = await _context.Reports.FindAsync(id);
            if (report != null)
            {
                report.Verification = ReportVerification.Verified;
                report.Status = _reportService.DetermineNextStatus(report.Status, ReportVerification.Verified);
                report.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Report #{id} verified. Status transitioned to {report.Status}.";
            }
            return RedirectToAction(nameof(Reports));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectReport(int id, string reason)
        {
            var report = await _context.Reports.FindAsync(id);
            if (report != null)
            {
                report.Verification = ReportVerification.Invalid;
                report.RejectionReason = reason;
                report.Status = _reportService.DetermineNextStatus(report.Status, ReportVerification.Invalid);
                report.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Report #{id} marked as invalid.";
            }
            return RedirectToAction(nameof(Reports));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkUnderReview(int id)
        {
            var report = await _context.Reports.FindAsync(id);
            if (report != null)
            {
                report.Status = ReportStatus.UnderReview;
                report.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Report #{id} moved to Under Review.";
            }
            return RedirectToAction(nameof(Reports), new { tab = "under_review" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResolveReport(int id)
        {
            var report = await _context.Reports.FindAsync(id);
            if (report != null && _reportService.CanTransition(report.Status, ReportStatus.Resolved))
            {
                report.Status = ReportStatus.Resolved;
                report.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Report #{id} marked as Resolved.";
            }
            return RedirectToAction(nameof(Reports), new { tab = "under_review" });
        }

        // ==========================================
        // 4. DENGUE RISK MONITORING
        // ==========================================
        public async Task<IActionResult> RiskMonitoring()
        {
            var riskZones = await _context.RiskZones.ToListAsync();
            var allReports = await _context.Reports.Where(r => r.Verification == ReportVerification.Verified).ToListAsync();
            var allCases = await _context.DengueCases.ToListAsync();

            var highCount = riskZones.Count(z => z.RiskLevel == RiskLevel.High);
            var mediumCount = riskZones.Count(z => z.RiskLevel == RiskLevel.Medium);
            var lowCount = riskZones.Count(z => z.RiskLevel == RiskLevel.Low);

            var sufficientCount = riskZones.Count(z => z.DataSufficiency == DataSufficiency.Sufficient);
            var partialCount = riskZones.Count(z => z.DataSufficiency == DataSufficiency.PartiallySufficient);
            var insufficientCount = riskZones.Count(z => z.DataSufficiency == DataSufficiency.Insufficient);

            var lastRun = riskZones.Any() ? riskZones.Max(z => z.LastModelRun) : (DateTime?)null;

            var mapZones = riskZones.Select(z => new
            {
                region = z.Region,
                riskLevel = z.RiskLevel.ToString(),
                lat = z.Latitude,
                lng = z.Longitude,
                radius = z.RadiusMetres,
                sufficiency = z.DataSufficiency.ToString(),
                confidence = Math.Round(z.ConfidenceScore * 100, 0),
                lastRun = z.LastModelRun.ToString("dd MMM yyyy HH:mm")
            }).ToList();

            var mapReports = allReports.Select(r => new
            {
                id = r.Id,
                lat = r.Latitude,
                lng = r.Longitude,
                diseaseType = r.DiseaseType.ToString(),
                status = r.Status.ToString(),
                desc = r.Description ?? "No description"
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

            var vm = new AdminRiskMonitoringViewModel
            {
                RiskZones = riskZones,
                HighRiskZonesCount = highCount,
                MediumRiskZonesCount = mediumCount,
                LowRiskZonesCount = lowCount,
                SufficientCount = sufficientCount,
                PartialCount = partialCount,
                InsufficientCount = insufficientCount,
                LastModelRun = lastRun,
                MapZonesJson = JsonSerializer.Serialize(mapZones),
                MapReportsJson = JsonSerializer.Serialize(mapReports),
                MapCasesJson = JsonSerializer.Serialize(mapCases)
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TriggerRiskCalculation()
        {
            await _riskCalculationJob.ExecuteAsync();
            TempData["SuccessMessage"] = "Risk recalculation successfully executed across all monitored zones. High-risk alerts broadcasted.";
            return RedirectToAction(nameof(RiskMonitoring));
        }

        // ==========================================
        // 5. ALERT MANAGEMENT
        // ==========================================
        public async Task<IActionResult> Alerts()
        {
            var now = DateTime.UtcNow;
            var alerts = await _context.Alerts.Include(a => a.Zone).OrderByDescending(a => a.SentAt).ToListAsync();
            var zones = await _context.RiskZones.OrderBy(z => z.Region).ToListAsync();

            var totalDelivered = alerts.Sum(a => a.DeliveredCount);
            var totalOpened = alerts.Sum(a => a.OpenedCount);
            var totalFailed = alerts.Sum(a => a.FailedCount);
            var activeToday = alerts.Count(a => a.SentAt >= now.Date);

            var vm = new AdminAlertManagementViewModel
            {
                Alerts = alerts,
                RiskZones = zones,
                TotalDeliveredCount = totalDelivered,
                TotalOpenedCount = totalOpened,
                TotalFailedCount = totalFailed,
                ActiveTodayCount = activeToday
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BroadcastAlert(BroadcastAlertFormModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Message))
            {
                TempData["ErrorMessage"] = "Alert message cannot be empty.";
                return RedirectToAction(nameof(Alerts));
            }

            RiskZone? targetZone = null;
            if (model.ZoneId.HasValue && model.ZoneId.Value > 0)
            {
                targetZone = await _context.RiskZones.FindAsync(model.ZoneId.Value);
            }

            var alertEntity = new Alert
            {
                Message = model.Message.Trim(),
                Language = model.Language ?? "en",
                ZoneId = targetZone?.Id,
                RadiusKm = model.RadiusKm > 0 ? model.RadiusKm : (targetZone != null ? targetZone.RadiusMetres / 1000.0 : 5.0),
                SentAt = DateTime.UtcNow,
                DeliveredCount = 1
            };

            _context.Alerts.Add(alertEntity);
            await _context.SaveChangesAsync();

            // Broadcast via SignalR AlertsHub in real-time
            var alertPayload = new
            {
                id = alertEntity.Id,
                message = alertEntity.Message,
                messageBn = !string.IsNullOrWhiteSpace(model.MessageBn) ? model.MessageBn : alertEntity.Message,
                riskLevel = targetZone?.RiskLevel.ToString() ?? "High",
                region = targetZone?.Region ?? "National / All Regions",
                radiusKm = alertEntity.RadiusKm,
                sentAt = alertEntity.SentAt.ToString("o")
            };

            await _alertsHub.Clients.All.SendAsync("ReceiveAlert", alertPayload);

            TempData["SuccessMessage"] = $"Emergency Dengue Alert #{alertEntity.Id} successfully broadcasted via SignalR to all active clients.";
            return RedirectToAction(nameof(Alerts));
        }

        // ==========================================
        // 6. ANALYTICS
        // ==========================================
        public async Task<IActionResult> Analytics()
        {
            var now = DateTime.UtcNow;

            var totalUsers = await _userManager.Users.CountAsync();
            var totalReports = await _context.Reports.CountAsync();
            var totalCases = await _context.DengueCases.CountAsync();
            var totalAlerts = await _context.Alerts.CountAsync();

            // 1. User Role Distribution
            var roleNames = new[] { "Citizen", "Doctor", "HealthWorker", "LabStaff", "Administrator", "GovernmentAuthority" };
            var roleLabels = new List<string>();
            var roleValues = new List<int>();
            foreach (var r in roleNames)
            {
                var count = (await _userManager.GetUsersInRoleAsync(r)).Count;
                roleLabels.Add(r);
                roleValues.Add(count);
            }

            // 2. Reports by Disease Type
            var reports = await _context.Reports.ToListAsync();
            var diseaseGroups = reports.GroupBy(r => r.DiseaseType.ToString()).ToDictionary(g => g.Key, g => g.Count());
            var diseaseLabels = diseaseGroups.Keys.ToList();
            var diseaseValues = diseaseGroups.Values.ToList();

            // 3. Reports by Verification Status
            var statusGroups = reports.GroupBy(r => r.Verification.ToString()).ToDictionary(g => g.Key, g => g.Count());
            var statusLabels = statusGroups.Keys.ToList();
            var statusValues = statusGroups.Values.ToList();

            // 4. Daily Report Trend (Past 14 Days)
            var trendStart = now.AddDays(-13).Date;
            var dailyLabels = new List<string>();
            var dailyValues = new List<int>();
            for (int i = 0; i < 14; i++)
            {
                var day = trendStart.AddDays(i);
                dailyLabels.Add(day.ToString("dd MMM"));
                dailyValues.Add(reports.Count(r => r.CreatedAt.Date == day));
            }

            // 5. Flow Analytics (Funnel & Abandonment)
            var trackedFlows = new[] { "CitizenReport", "SymptomChecker", "TelemedicineBooking", "LabBooking" };
            var flowStepCounts = new Dictionary<string, Dictionary<string, int>>();
            var flowAbandonmentRates = new Dictionary<string, double>();

            foreach (var flow in trackedFlows)
            {
                var stats = await _analyticsService.GetFlowStatsAsync(flow);
                flowStepCounts[flow] = stats;
                flowAbandonmentRates[flow] = await _analyticsService.GetAbandonmentRateAsync(flow);
            }

            var vm = new AdminAnalyticsViewModel
            {
                TotalUsers = totalUsers,
                TotalReports = totalReports,
                TotalCases = totalCases,
                TotalAlerts = totalAlerts,
                UserRoleLabelsJson = JsonSerializer.Serialize(roleLabels),
                UserRoleValuesJson = JsonSerializer.Serialize(roleValues),
                DiseaseLabelsJson = JsonSerializer.Serialize(diseaseLabels),
                DiseaseValuesJson = JsonSerializer.Serialize(diseaseValues),
                ReportStatusLabelsJson = JsonSerializer.Serialize(statusLabels),
                ReportStatusValuesJson = JsonSerializer.Serialize(statusValues),
                DailyReportTrendLabelsJson = JsonSerializer.Serialize(dailyLabels),
                DailyReportTrendValuesJson = JsonSerializer.Serialize(dailyValues),
                FlowStepCounts = flowStepCounts,
                FlowAbandonmentRates = flowAbandonmentRates
            };

            return View(vm);
        }

        // ==========================================
        // 7. SYSTEM ACTIVITY / AUDIT LOG
        // ==========================================
        public async Task<IActionResult> ActivityLog(string? category, string? search)
        {
            var activities = await GenerateRecentActivitiesAsync(50);

            var filtered = activities.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(category) && category != "All")
            {
                filtered = filtered.Where(a => a.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLowerInvariant();
                filtered = filtered.Where(a =>
                    a.Action.ToLowerInvariant().Contains(term) ||
                    a.Description.ToLowerInvariant().Contains(term) ||
                    a.Actor.ToLowerInvariant().Contains(term));
            }

            var vm = new AdminActivityLogViewModel
            {
                Activities = filtered.OrderByDescending(a => a.Timestamp).ToList(),
                FilterCategory = category ?? "All",
                FilterSearch = search
            };

            return View(vm);
        }

        // ==========================================
        // 8. DATA AND REPORT MANAGEMENT
        // ==========================================
        public async Task<IActionResult> DataReports()
        {
            var reports = await _context.Reports.Include(r => r.User).OrderByDescending(r => r.CreatedAt).Take(25).ToListAsync();
            var alerts = await _context.Alerts.Include(a => a.Zone).OrderByDescending(a => a.SentAt).Take(25).ToListAsync();

            var vm = new AdminDataReportsViewModel
            {
                TotalReports = await _context.Reports.CountAsync(),
                TotalVerifiedReports = await _context.Reports.CountAsync(r => r.Verification == ReportVerification.Verified),
                TotalPendingReports = await _context.Reports.CountAsync(r => r.Verification == ReportVerification.Pending),
                TotalInvalidReports = await _context.Reports.CountAsync(r => r.Verification == ReportVerification.Invalid),
                TotalUsers = await _userManager.Users.CountAsync(),
                TotalAlerts = await _context.Alerts.CountAsync(),
                TotalCases = await _context.DengueCases.CountAsync(),
                RecentReports = reports,
                RecentAlerts = alerts
            };

            return View(vm);
        }

        public async Task<IActionResult> ExportReportsCsv()
        {
            var reports = await _context.Reports.Include(r => r.User).OrderByDescending(r => r.CreatedAt).ToListAsync();
            var csv = new StringBuilder();
            csv.AppendLine("ID,DiseaseType,Latitude,Longitude,Status,Verification,User,Description,FieldNotes,CreatedAt,UpdatedAt");
            foreach (var r in reports)
            {
                csv.AppendLine($"{r.Id},{r.DiseaseType},{r.Latitude},{r.Longitude},{r.Status},{r.Verification},\"{r.User?.FullName ?? r.User?.Email ?? ""}\",\"{r.Description?.Replace("\"", "\"\"") ?? ""}\",\"{r.FieldNotes?.Replace("\"", "\"\"") ?? ""}\",{r.CreatedAt:yyyy-MM-dd HH:mm},{r.UpdatedAt:yyyy-MM-dd HH:mm}");
            }
            return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", $"LarvaX_Reports_{DateTime.UtcNow:yyyyMMdd_HHmm}.csv");
        }

        public async Task<IActionResult> ExportUsersCsv()
        {
            var users = await _userManager.Users.ToListAsync();
            var csv = new StringBuilder();
            csv.AppendLine("ID,FullName,Email,PhoneNumber,ModePreference,Specialty,IsApproved,IsRejected,EmailConfirmed");
            foreach (var u in users)
            {
                csv.AppendLine($"\"{u.Id}\",\"{u.FullName ?? ""}\",\"{u.Email ?? ""}\",\"{u.PhoneNumber ?? ""}\",\"{u.ModePreference ?? ""}\",\"{u.Specialty ?? ""}\",{u.IsApproved},{u.IsRejected},{u.EmailConfirmed}");
            }
            return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", $"LarvaX_Users_{DateTime.UtcNow:yyyyMMdd_HHmm}.csv");
        }

        public async Task<IActionResult> ExportAlertsCsv()
        {
            var alerts = await _context.Alerts.Include(a => a.Zone).OrderByDescending(a => a.SentAt).ToListAsync();
            var csv = new StringBuilder();
            csv.AppendLine("ID,Message,Region,RadiusKm,Language,DeliveredCount,OpenedCount,FailedCount,SentAt");
            foreach (var a in alerts)
            {
                csv.AppendLine($"{a.Id},\"{a.Message.Replace("\"", "\"\"")}\",\"{a.Zone?.Region ?? "National"}\",{a.RadiusKm ?? 0},{a.Language},{a.DeliveredCount},{a.OpenedCount},{a.FailedCount},{a.SentAt:yyyy-MM-dd HH:mm}");
            }
            return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", $"LarvaX_Alerts_{DateTime.UtcNow:yyyyMMdd_HHmm}.csv");
        }

        // ==========================================
        // 9. SYSTEM SETTINGS
        // ==========================================
        public IActionResult Settings()
        {
            var currentSettings = _settingsService.GetSettings();
            return View(currentSettings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SaveSettings(AdminSettingsViewModel model)
        {
            _settingsService.UpdateSettings(model);
            TempData["SuccessMessage"] = "System configuration settings updated and saved successfully.";
            return RedirectToAction(nameof(Settings));
        }

        // ==========================================
        // HELPER METHODS
        // ==========================================
        private async Task<List<AdminActivityItem>> GenerateRecentActivitiesAsync(int limit)
        {
            var list = new List<AdminActivityItem>();

            // 1. Report Activities
            var recentReports = await _context.Reports
                .Include(r => r.User)
                .OrderByDescending(r => r.UpdatedAt)
                .Take(limit)
                .ToListAsync();

            foreach (var r in recentReports)
            {
                if (r.Verification == ReportVerification.Verified)
                {
                    list.Add(new AdminActivityItem
                    {
                        Timestamp = r.UpdatedAt,
                        Category = "Reports",
                        Action = "Report Verified",
                        Description = $"Report #{r.Id} ({r.DiseaseType}) verified and moved to {r.Status}.",
                        Actor = "Administrator",
                        BadgeClass = "bg-success",
                        Icon = "bi-patch-check-fill"
                    });
                }
                else if (r.Verification == ReportVerification.Invalid)
                {
                    list.Add(new AdminActivityItem
                    {
                        Timestamp = r.UpdatedAt,
                        Category = "Reports",
                        Action = "Report Invalidated",
                        Description = $"Report #{r.Id} rejected with reason: {r.RejectionReason ?? "N/A"}",
                        Actor = "Administrator",
                        BadgeClass = "bg-danger",
                        Icon = "bi-x-octagon-fill"
                    });
                }
                else
                {
                    list.Add(new AdminActivityItem
                    {
                        Timestamp = r.CreatedAt,
                        Category = "Reports",
                        Action = "New Citizen Report",
                        Description = $"Citizen {r.User?.FullName ?? "User"} submitted a new {r.DiseaseType} hazard report.",
                        Actor = r.User?.FullName ?? "Citizen",
                        BadgeClass = "bg-warning text-dark",
                        Icon = "bi-file-earmark-medical"
                    });
                }
            }

            // 2. Alert Activities
            var recentAlerts = await _context.Alerts
                .Include(a => a.Zone)
                .OrderByDescending(a => a.SentAt)
                .Take(limit)
                .ToListAsync();

            foreach (var a in recentAlerts)
            {
                list.Add(new AdminActivityItem
                {
                    Timestamp = a.SentAt,
                    Category = "Alerts",
                    Action = "Emergency Alert Dispatched",
                    Description = $"Broadcasted to {(a.Zone != null ? a.Zone.Region : "National")} (Radius: {a.RadiusKm ?? 5}km): {a.Message}",
                    Actor = "System Sentinel",
                    BadgeClass = "bg-danger",
                    Icon = "bi-broadcast-pin"
                });
            }

            // 3. User Approval Activities
            var users = await _userManager.Users
                .OrderByDescending(u => u.Id)
                .Take(limit)
                .ToListAsync();

            foreach (var u in users)
            {
                if (u.IsApproved)
                {
                    list.Add(new AdminActivityItem
                    {
                        Timestamp = DateTime.UtcNow.AddHours(-3),
                        Category = "Users",
                        Action = "User Approved",
                        Description = $"Professional account for {u.FullName ?? u.Email} was verified and activated.",
                        Actor = "Administrator",
                        BadgeClass = "bg-info text-dark",
                        Icon = "bi-person-check-fill"
                    });
                }
                else if (u.IsRejected)
                {
                    list.Add(new AdminActivityItem
                    {
                        Timestamp = DateTime.UtcNow.AddHours(-5),
                        Category = "Users",
                        Action = "User Rejected",
                        Description = $"Account request for {u.FullName ?? u.Email} was rejected.",
                        Actor = "Administrator",
                        BadgeClass = "bg-dark",
                        Icon = "bi-person-x-fill"
                    });
                }
            }

            // 4. Risk engine runs
            var zones = await _context.RiskZones.ToListAsync();
            if (zones.Any())
            {
                var maxRun = zones.Max(z => z.LastModelRun);
                list.Add(new AdminActivityItem
                {
                    Timestamp = maxRun,
                    Category = "Risk",
                    Action = "Risk Recalculation Run",
                    Description = $"Multi-zone risk engine completed recalculation across {zones.Count} regions.",
                    Actor = "Hangfire Risk Engine",
                    BadgeClass = "bg-primary",
                    Icon = "bi-cpu-fill"
                });
            }

            return list.OrderByDescending(a => a.Timestamp).Take(limit).ToList();
        }
    }
}
