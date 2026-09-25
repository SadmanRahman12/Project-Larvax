using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using LarvaX.Core.Entities;
using LarvaX.Core.Interfaces;
using LarvaX.Infrastructure.Data;
using LarvaX.Web.Hubs;
using LarvaX.Web.Models;

namespace LarvaX.Web.Controllers
{
    [Authorize(Roles = "HealthWorker,Administrator")]
    public class HealthWorkerDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IInventoryService _inventoryService;
        private readonly IDonorService _donorService;
        private readonly IEducationService _educationService;
        private readonly IHubContext<AlertsHub> _hubContext;

        public HealthWorkerDashboardController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IInventoryService inventoryService,
            IDonorService donorService,
            IEducationService educationService,
            IHubContext<AlertsHub> hubContext)
        {
            _context = context;
            _userManager = userManager;
            _inventoryService = inventoryService;
            _donorService = donorService;
            _educationService = educationService;
            _hubContext = hubContext;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var worker = await _userManager.GetUserAsync(User);
            if (worker == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var roles = await _userManager.GetRolesAsync(worker);
            var lang = CultureInfo.CurrentUICulture.Name.StartsWith("bn") ? "bn" : "en";

            // ── 1. Dengue Cases ─────────────────────────────────────────────────
            var cases = await _context.DengueCases
                .OrderByDescending(c => c.Severity == CaseSeverity.Critical || c.Severity == CaseSeverity.Severe)
                .ThenByDescending(c => c.UpdatedAt)
                .ToListAsync();

            // ── 2. Citizen Reports ──────────────────────────────────────────────
            var citizenReports = await _context.Reports
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            // ── 3. Risk Zones ───────────────────────────────────────────────────
            var riskZones = await _context.RiskZones
                .OrderByDescending(z => z.RiskLevel == RiskLevel.High)
                .ThenByDescending(z => z.ConfidenceScore)
                .ToListAsync();

            // ── 4. Inventory ───────────────────────────────────────────────────
            var inventoryItems = (await _inventoryService.GetAllItemsAsync()).ToList();
            var recentTransactions = await _context.InventoryTransactions
                .Include(t => t.InventoryItem)
                .Include(t => t.User)
                .OrderByDescending(t => t.Date)
                .Take(12)
                .ToListAsync();

            // ── 5. Patient Records ──────────────────────────────────────────────
            var patientRecords = await _context.PatientRecords
                .Include(p => p.Patient)
                .OrderByDescending(p => p.Date)
                .Take(15)
                .ToListAsync();

            // ── 6. Referrals ───────────────────────────────────────────────────
            var referrals = await _context.CaseReferrals
                .Include(r => r.DengueCase)
                .Include(r => r.ReferredBy)
                .OrderByDescending(r => r.Urgency == ReferralUrgency.Emergency)
                .ThenByDescending(r => r.CreatedAt)
                .ToListAsync();

            // ── 7. Blood Donors ─────────────────────────────────────────────────
            var availableDonors = await _context.Donors
                .Include(d => d.User)
                .Where(d => d.IsAvailable)
                .OrderByDescending(d => d.LastConfirmedAvailable)
                .Take(20)
                .ToListAsync();

            var freshnessLabels = availableDonors.ToDictionary(
                d => d.Id,
                d => _donorService.GetFreshnessLabel(d.LastConfirmedAvailable, language: lang));

            var freshnessFlags = availableDonors.ToDictionary(
                d => d.Id,
                d => _donorService.IsConsideredFresh(d.LastConfirmedAvailable));

            // ── 8. Tasks ───────────────────────────────────────────────────────
            var tasks = await _context.HealthWorkerTasks
                .OrderBy(t => t.IsCompleted)
                .ThenByDescending(t => t.Priority == TaskPriority.Urgent)
                .ThenByDescending(t => t.Priority == TaskPriority.High)
                .ThenBy(t => t.DueDate)
                .ToListAsync();

            // ── 9. Alerts ──────────────────────────────────────────────────────
            var alerts = await _context.Alerts
                .OrderByDescending(a => a.SentAt)
                .Take(15)
                .ToListAsync();

            // ── 10. Education Articles & Quizzes ────────────────────────────────
            var articles = (await _educationService.GetArticlesAsync(lang)).ToList();
            var quizzes = (await _educationService.GetQuizzesAsync(lang)).ToList();

            // ── Aggregate Summary Stats ─────────────────────────────────────────
            var highRiskCasesCount = cases.Count(c => c.Severity == CaseSeverity.Critical || c.Severity == CaseSeverity.Severe || c.IsEscalated);
            var pendingReportsCount = citizenReports.Count(r => r.Status == ReportStatus.Received || r.Status == ReportStatus.UnderReview);
            var activeAlertsCount = alerts.Count(a => a.SentAt >= DateTime.UtcNow.AddDays(-7));
            var lowStockItemsCount = inventoryItems.Count(i => i.Quantity <= i.Threshold);
            var followUpCount = cases.Count(c => c.Status == CaseStatus.UnderObservation || c.Status == CaseStatus.Suspected || (c.NextFollowUpDate.HasValue && c.NextFollowUpDate <= DateTime.UtcNow.AddDays(1)));
            var assignedTasksCount = tasks.Count(t => !t.IsCompleted);

            var vm = new HealthWorkerDashboardViewModel
            {
                Worker = worker,
                Roles = roles,
                HighRiskCasesCount = highRiskCasesCount,
                PendingReportsCount = pendingReportsCount,
                ActiveAlertsCount = activeAlertsCount,
                LowStockItemsCount = lowStockItemsCount,
                CasesRequiringFollowUpCount = followUpCount,
                AssignedTasksCount = assignedTasksCount,
                DengueCases = cases,
                CitizenReports = citizenReports,
                RiskZones = riskZones,
                InventoryItems = inventoryItems,
                RecentTransactions = recentTransactions,
                PatientRecords = patientRecords,
                Referrals = referrals,
                AvailableDonors = availableDonors,
                DonorFreshnessLabels = freshnessLabels,
                DonorFreshnessFlags = freshnessFlags,
                EducationArticles = articles,
                Quizzes = quizzes,
                Tasks = tasks,
                RecentAlerts = alerts,
                CurrentLanguage = lang
            };

            return View(vm);
        }

        // ══════════════════════════════════════════════════════════════════════
        // ── 2. Dengue Case Management Actions ─────────────────────────────────
        // ══════════════════════════════════════════════════════════════════════

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCase(AddDengueCaseInputModel model)
        {
            var workerId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(model.PatientName))
            {
                TempData["ErrorMessage"] = "Patient name is required.";
                return Redirect(Url.Action(nameof(Index)) + "#section-cases");
            }

            var dengueCase = new DengueCase
            {
                PatientName = model.PatientName.Trim(),
                PatientPhone = model.PatientPhone?.Trim(),
                PatientAddress = model.PatientAddress?.Trim(),
                Latitude = model.Latitude != 0 ? model.Latitude : 23.7500, // Default to Dhaka latitude if not set
                Longitude = model.Longitude != 0 ? model.Longitude : 90.3800,
                Age = model.Age > 0 ? model.Age : 25,
                Gender = model.Gender,
                Status = model.Status,
                Severity = model.Severity,
                PlateletCount = model.PlateletCount,
                Hematocrit = model.Hematocrit,
                Symptoms = model.Symptoms,
                FieldNotes = model.FieldNotes,
                NextFollowUpDate = model.NextFollowUpDate,
                IsEscalated = model.IsEscalated,
                EscalationReason = model.EscalationReason,
                AssignedWorkerId = workerId,
                ReportedDate = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.DengueCases.Add(dengueCase);
            await _context.SaveChangesAsync();

            // If marked as escalated, automatically generate referral
            if (model.IsEscalated)
            {
                var referral = new CaseReferral
                {
                    DengueCaseId = dengueCase.Id,
                    PatientName = dengueCase.PatientName,
                    PatientPhone = dengueCase.PatientPhone,
                    Target = ReferralTarget.Hospital,
                    Urgency = ReferralUrgency.Emergency,
                    Status = ReferralStatus.Pending,
                    Reason = string.IsNullOrWhiteSpace(model.EscalationReason) ? "Urgent clinical escalation from field worker." : model.EscalationReason,
                    ClinicalNotes = $"Platelet: {model.PlateletCount ?? 0} /uL, Hematocrit: {model.Hematocrit ?? 0}%. Symptoms: {model.Symptoms}",
                    ReferredById = workerId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.CaseReferrals.Add(referral);
                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = $"Case for {dengueCase.PatientName} registered successfully (Status: {dengueCase.Status}).";
            return Redirect(Url.Action(nameof(Index)) + "#section-cases");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCaseStatus(UpdateCaseStatusInputModel model)
        {
            var dengueCase = await _context.DengueCases.FindAsync(model.CaseId);
            if (dengueCase == null) return NotFound();

            dengueCase.Status = model.Status;
            dengueCase.Severity = model.Severity;
            if (model.PlateletCount.HasValue) dengueCase.PlateletCount = model.PlateletCount;
            if (model.Hematocrit.HasValue) dengueCase.Hematocrit = model.Hematocrit;
            if (model.NextFollowUpDate.HasValue) dengueCase.NextFollowUpDate = model.NextFollowUpDate;
            if (model.IsEscalated)
            {
                dengueCase.IsEscalated = true;
                dengueCase.EscalationReason = model.EscalationReason ?? "Escalated by health worker during follow-up";
            }

            if (!string.IsNullOrWhiteSpace(model.FollowUpNotes))
            {
                dengueCase.FieldNotes = string.IsNullOrWhiteSpace(dengueCase.FieldNotes)
                    ? $"[{DateTime.UtcNow:dd MMM HH:mm}] {model.FollowUpNotes}"
                    : $"{dengueCase.FieldNotes}\n[{DateTime.UtcNow:dd MMM HH:mm}] {model.FollowUpNotes}";
            }

            dengueCase.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Case #{dengueCase.Id} ({dengueCase.PatientName}) updated to {dengueCase.Status}.";
            return Redirect(Url.Action(nameof(Index)) + "#section-cases");
        }

        // ══════════════════════════════════════════════════════════════════════
        // ── 3. Citizen Report Queue Actions ───────────────────────────────────
        // ══════════════════════════════════════════════════════════════════════

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCitizenReport(UpdateCitizenReportInputModel model)
        {
            var report = await _context.Reports.FindAsync(model.ReportId);
            if (report == null) return NotFound();

            report.Status = model.Status;
            report.Verification = model.Verification;
            if (!string.IsNullOrWhiteSpace(model.FieldNotes))
            {
                report.FieldNotes = string.IsNullOrWhiteSpace(report.FieldNotes)
                    ? $"[{DateTime.UtcNow:dd MMM HH:mm}] {model.FieldNotes}"
                    : $"{report.FieldNotes}\n[{DateTime.UtcNow:dd MMM HH:mm}] {model.FieldNotes}";
            }
            if (!string.IsNullOrWhiteSpace(model.RejectionReason))
            {
                report.RejectionReason = model.RejectionReason;
            }
            report.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Citizen Report #{report.Id} updated to {report.Status} (Verification: {report.Verification}).";
            return Redirect(Url.Action(nameof(Index)) + "#section-reports");
        }

        // ══════════════════════════════════════════════════════════════════════
        // ── 6. Inventory Actions ──────────────────────────────────────────────
        // ══════════════════════════════════════════════════════════════════════

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdjustInventory(InventoryAdjustmentInputModel model)
        {
            var workerId = _userManager.GetUserId(User)!;
            try
            {
                await _inventoryService.RecordTransactionAsync(model.ItemId, model.QuantityChange, model.Reason, workerId);
                var item = await _inventoryService.GetItemByIdAsync(model.ItemId);
                var actionWord = model.QuantityChange > 0 ? "restocked" : "dispensed";
                TempData["SuccessMessage"] = $"{Math.Abs(model.QuantityChange)} units of {item?.Name} {actionWord} successfully. Current stock: {item?.Quantity}.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            return Redirect(Url.Action(nameof(Index)) + "#section-inventory");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddInventoryItem(string name, int quantity, int threshold, string location)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["ErrorMessage"] = "Item name is required.";
                return Redirect(Url.Action(nameof(Index)) + "#section-inventory");
            }

            var item = new InventoryItem
            {
                Name = name.Trim(),
                Quantity = quantity >= 0 ? quantity : 0,
                Threshold = threshold >= 0 ? threshold : 10,
                Location = string.IsNullOrWhiteSpace(location) ? "Main Depot" : location.Trim()
            };

            await _inventoryService.AddItemAsync(item);
            TempData["SuccessMessage"] = $"Item '{item.Name}' added to inventory.";
            return Redirect(Url.Action(nameof(Index)) + "#section-inventory");
        }

        // ══════════════════════════════════════════════════════════════════════
        // ── 8. Referral & Escalation Actions ──────────────────────────────────
        // ══════════════════════════════════════════════════════════════════════

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateReferral(CreateReferralInputModel model)
        {
            var workerId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(model.PatientName) || string.IsNullOrWhiteSpace(model.Reason))
            {
                TempData["ErrorMessage"] = "Patient name and referral reason are required.";
                return Redirect(Url.Action(nameof(Index)) + "#section-referrals");
            }

            var referral = new CaseReferral
            {
                DengueCaseId = model.DengueCaseId,
                PatientName = model.PatientName.Trim(),
                PatientPhone = model.PatientPhone?.Trim(),
                Target = model.Target,
                Urgency = model.Urgency,
                Reason = model.Reason.Trim(),
                ClinicalNotes = model.ClinicalNotes?.Trim(),
                Status = ReferralStatus.Pending,
                ReferredById = workerId,
                CreatedAt = DateTime.UtcNow
            };

            _context.CaseReferrals.Add(referral);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Referral created for {referral.PatientName} targeting {referral.Target} (Urgency: {referral.Urgency}).";
            return Redirect(Url.Action(nameof(Index)) + "#section-referrals");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateReferralStatus(int referralId, ReferralStatus status)
        {
            var referral = await _context.CaseReferrals.FindAsync(referralId);
            if (referral == null) return NotFound();

            referral.Status = status;
            if (status == ReferralStatus.Completed || status == ReferralStatus.Accepted)
            {
                referral.ResolvedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Referral for {referral.PatientName} updated to {referral.Status}.";
            return Redirect(Url.Action(nameof(Index)) + "#section-referrals");
        }

        // ══════════════════════════════════════════════════════════════════════
        // ── 11. Task / Field Activity Management Actions ──────────────────────
        // ══════════════════════════════════════════════════════════════════════

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTask(HealthWorkerTaskInputModel model)
        {
            var workerId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(model.Title))
            {
                TempData["ErrorMessage"] = "Task title is required.";
                return Redirect(Url.Action(nameof(Index)) + "#section-tasks");
            }

            var task = new HealthWorkerTask
            {
                Title = model.Title.Trim(),
                Description = model.Description?.Trim(),
                Category = model.Category ?? "General",
                Priority = model.Priority,
                DueDate = model.DueDate,
                IsCompleted = false,
                HealthWorkerId = workerId,
                CreatedAt = DateTime.UtcNow
            };

            _context.HealthWorkerTasks.Add(task);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Task '{task.Title}' added to your field checklist.";
            return Redirect(Url.Action(nameof(Index)) + "#section-tasks");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleTask(int taskId)
        {
            var task = await _context.HealthWorkerTasks.FindAsync(taskId);
            if (task == null) return NotFound();

            task.IsCompleted = !task.IsCompleted;
            task.CompletedAt = task.IsCompleted ? DateTime.UtcNow : null;
            await _context.SaveChangesAsync();

            var statusStr = task.IsCompleted ? "completed" : "marked pending";
            TempData["SuccessMessage"] = $"Task '{task.Title}' {statusStr}.";
            return Redirect(Url.Action(nameof(Index)) + "#section-tasks");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTask(int taskId)
        {
            var task = await _context.HealthWorkerTasks.FindAsync(taskId);
            if (task == null) return NotFound();

            _context.HealthWorkerTasks.Remove(task);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Task deleted.";
            return Redirect(Url.Action(nameof(Index)) + "#section-tasks");
        }

        // ══════════════════════════════════════════════════════════════════════
        // ── 5. Real-Time Alert Broadcast Action ───────────────────────────────
        // ══════════════════════════════════════════════════════════════════════

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BroadcastAlert(BroadcastAlertInputModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Message))
            {
                TempData["ErrorMessage"] = "Alert message cannot be empty.";
                return Redirect(Url.Action(nameof(Index)) + "#section-alerts");
            }

            var alert = new Alert
            {
                Message = model.Message.Trim(),
                Language = model.Language ?? "en",
                ZoneId = model.ZoneId,
                RadiusKm = model.RadiusKm ?? 3.0,
                SentAt = DateTime.UtcNow,
                DeliveredCount = 1,
                OpenedCount = 0,
                FailedCount = 0
            };

            _context.Alerts.Add(alert);
            await _context.SaveChangesAsync();

            // Broadcast in real-time via SignalR
            try
            {
                await _hubContext.Clients.All.SendAsync("ReceiveAlert", new
                {
                    message = alert.Message,
                    messageBn = alert.Message,
                    sentAt = alert.SentAt.ToString("o")
                });
            }
            catch (Exception ex)
            {
                // SignalR failure should not crash request
                Console.WriteLine($"SignalR broadcast warning: {ex.Message}");
            }

            TempData["SuccessMessage"] = "Urgent sentinel alert broadcasted successfully across the network.";
            return Redirect(Url.Action(nameof(Index)) + "#section-alerts");
        }

        // ══════════════════════════════════════════════════════════════════════
        // ── 13. Profile & Preferences Actions ─────────────────────────────────
        // ══════════════════════════════════════════════════════════════════════

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(string fullName, string phoneNumber, string preferredLanguage)
        {
            var worker = await _userManager.GetUserAsync(User);
            if (worker == null) return RedirectToAction("Login", "Account");

            worker.FullName = fullName?.Trim();
            worker.PhoneNumber = phoneNumber?.Trim();
            worker.PreferredLanguage = preferredLanguage == "bn" ? "bn" : "en";

            var result = await _userManager.UpdateAsync(worker);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Profile details updated successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = string.Join("; ", result.Errors.Select(e => e.Description));
            }

            return Redirect(Url.Action(nameof(Index)) + "#section-profile");
        }
    }
}
