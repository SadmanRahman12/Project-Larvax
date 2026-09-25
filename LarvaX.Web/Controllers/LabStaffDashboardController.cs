using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using LarvaX.Application.Services;
using LarvaX.Core.Entities;
using LarvaX.Infrastructure.Data;
using LarvaX.Web.Hubs;
using LarvaX.Web.Models;

namespace LarvaX.Web.Controllers
{
    [Authorize(Roles = "LabStaff,Administrator")]
    public class LabStaffDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILabService _labService;
        private readonly IPatientRecordService _patientRecordService;
        private readonly IWebHostEnvironment _environment;
        private readonly IHubContext<AlertsHub> _alertsHub;

        public LabStaffDashboardController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILabService labService,
            IPatientRecordService patientRecordService,
            IWebHostEnvironment environment,
            IHubContext<AlertsHub> alertsHub)
        {
            _context = context;
            _userManager = userManager;
            _labService = labService;
            _patientRecordService = patientRecordService;
            _environment = environment;
            _alertsHub = alertsHub;
        }

        public async Task<IActionResult> Index(string? tab, string? q)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var roles = await _userManager.GetRolesAsync(user);
            var todayUtc = DateTime.UtcNow.Date;

            // Fetch all lab bookings with patient and lab test
            var query = _context.LabBookings
                .Include(b => b.Patient)
                .Include(b => b.LabTest)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim().ToLower();
                query = query.Where(b =>
                    (b.Patient != null && ((b.Patient.FullName != null && b.Patient.FullName.ToLower().Contains(term)) || (b.Patient.Email != null && b.Patient.Email.ToLower().Contains(term)) || (b.Patient.PhoneNumber != null && b.Patient.PhoneNumber.Contains(term)))) ||
                    (b.LabTest != null && b.LabTest.Name != null && b.LabTest.Name.ToLower().Contains(term)) ||
                    b.Id.ToString().Contains(term) ||
                    (b.BarcodeNumber != null && b.BarcodeNumber.ToLower().Contains(term)) ||
                    (b.Status != null && b.Status.ToLower().Contains(term)));
            }

            var allBookings = await query
                .OrderByDescending(b => b.ScheduledAt)
                .ToListAsync();

            // Segment bookings for the dashboard
            var todayBookings = allBookings
                .Where(b => b.ScheduledAt.Date == todayUtc)
                .OrderBy(b => b.ScheduledAt)
                .ToList();

            var pendingTests = allBookings
                .Where(b => b.Status == "Pending" || b.Status == "Confirmed" || b.Status == "SampleCollected")
                .ToList();

            var waitingUploadTests = allBookings
                .Where(b => (b.Status == "SampleReceived" || b.Status == "Processing") && string.IsNullOrEmpty(b.ResultLink) && !b.PlateletCount.HasValue)
                .ToList();

            var completedTests = allBookings
                .Where(b => b.Status == "Completed" || !string.IsNullOrEmpty(b.ResultLink) || b.PlateletCount.HasValue)
                .ToList();

            var criticalCount = allBookings.Count(b => b.PlateletCount.HasValue && b.PlateletCount.Value < 50000);

            // Available Tests
            var availableTests = await _context.LabTests
                .OrderBy(t => t.Name)
                .ToListAsync();

            // Distinct Patients with test summaries
            var patientGroups = allBookings
                .Where(b => b.Patient != null)
                .GroupBy(b => b.PatientId)
                .Select(g =>
                {
                    var p = g.First().Patient!;
                    var latestBooking = g.OrderByDescending(b => b.ScheduledAt).FirstOrDefault();
                    var hasCritical = g.Any(b => b.PlateletCount.HasValue && b.PlateletCount.Value < 50000);
                    return new LabPatientSummary
                    {
                        PatientId = p.Id,
                        PatientName = string.IsNullOrWhiteSpace(p.FullName) ? (p.UserName ?? "Patient") : p.FullName,
                        Email = p.Email,
                        PhoneNumber = p.PhoneNumber,
                        Address = p.Address,
                        TotalTests = g.Count(),
                        LatestTestDate = latestBooking?.ScheduledAt,
                        LatestTestName = latestBooking?.LabTest?.Name,
                        LatestStatus = latestBooking?.Status,
                        HasCriticalResult = hasCritical
                    };
                })
                .OrderByDescending(p => p.LatestTestDate)
                .ToList();

            // Recent Patient Records
            var recentRecords = await _context.PatientRecords
                .Include(r => r.Patient)
                .Where(r => r.RecordType == "LabResult")
                .OrderByDescending(r => r.Date)
                .Take(10)
                .ToListAsync();

            // Urgent alerts (recent 5)
            var urgentAlerts = await _context.Alerts
                .OrderByDescending(a => a.SentAt)
                .Take(5)
                .ToListAsync();

            // Activity feed generated from latest booking updates
            var activities = new List<LabActivityItem>();
            foreach (var b in allBookings.Take(8))
            {
                var patientName = b.Patient?.FullName ?? "Patient";
                var testName = b.LabTest?.Name ?? "Laboratory Test";

                if (b.Status == "Completed")
                {
                    activities.Add(new LabActivityItem
                    {
                        Title = $"Result Uploaded: #{b.Id}",
                        Description = $"Official report published for {patientName} ({testName}).",
                        Icon = "bi-file-earmark-check-fill",
                        BadgeClass = "bg-success",
                        Timestamp = b.CompletedAt ?? b.UpdatedAt
                    });
                }
                else if (b.Status == "Processing")
                {
                    activities.Add(new LabActivityItem
                    {
                        Title = $"Processing Started: #{b.Id}",
                        Description = $"Sample for {patientName} ({testName}) loaded into analyzer.",
                        Icon = "bi-gear-wide-connected",
                        BadgeClass = "bg-primary",
                        Timestamp = b.ProcessingStartedAt ?? b.UpdatedAt
                    });
                }
                else if (b.Status == "SampleCollected" || b.Status == "SampleReceived")
                {
                    activities.Add(new LabActivityItem
                    {
                        Title = $"Sample {b.Status.Replace("Sample", "")}: #{b.Id}",
                        Description = $"{patientName} ({testName}) sample verified in laboratory queue.",
                        Icon = "bi-eyedropper",
                        BadgeClass = "bg-info text-dark",
                        Timestamp = b.SampleReceivedAt ?? b.SampleCollectedAt ?? b.UpdatedAt
                    });
                }
                else
                {
                    activities.Add(new LabActivityItem
                    {
                        Title = $"New Booking Received: #{b.Id}",
                        Description = $"{patientName} requested {testName}.",
                        Icon = "bi-calendar-plus-fill",
                        BadgeClass = "bg-warning text-dark",
                        Timestamp = b.CreatedAt
                    });
                }
            }

            var vm = new LabStaffDashboardViewModel
            {
                LabStaff = user,
                Roles = roles,
                TodayTestsCount = todayBookings.Count,
                PendingTestsCount = pendingTests.Count,
                ResultsWaitingUploadCount = waitingUploadTests.Count,
                CompletedTestsCount = completedTests.Count,
                TotalBookingsCount = allBookings.Count,
                CriticalPlateletAlertsCount = criticalCount,
                TodayBookings = todayBookings,
                PendingTests = pendingTests,
                WaitingUploadTests = waitingUploadTests,
                CompletedTests = completedTests,
                AllBookings = allBookings,
                AvailableTests = availableTests,
                Patients = patientGroups,
                RecentLabPatientRecords = recentRecords,
                RecentActivities = activities,
                UrgentAlerts = urgentAlerts,
                SearchTerm = q,
                ActiveTab = string.IsNullOrWhiteSpace(tab) ? "overview" : tab
            };

            return View(vm);
        }

        // ── Workflow Action: Update Sample Management Status ───────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSampleStatus(UpdateSampleStatusInputModel model)
        {
            var booking = await _context.LabBookings
                .Include(b => b.Patient)
                .Include(b => b.LabTest)
                .FirstOrDefaultAsync(b => b.Id == model.BookingId);

            if (booking == null)
            {
                TempData["ErrorMessage"] = "Test booking not found.";
                return RedirectToAction(nameof(Index), new { tab = "samples" });
            }

            if (!string.IsNullOrWhiteSpace(model.BarcodeNumber))
            {
                booking.BarcodeNumber = model.BarcodeNumber.Trim();
            }

            await _labService.UpdateSampleStatusAsync(model.BookingId, model.Status, model.RejectionReason);

            TempData["SuccessMessage"] = $"Booking #{booking.Id} sample status updated to '{model.Status}'.";
            return RedirectToAction(nameof(Index), new { tab = "samples" });
        }

        // ── Workflow Action: Upload Test Result & Enter Test Values ────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadResult(UploadLabResultInputModel model)
        {
            var booking = await _context.LabBookings
                .Include(b => b.Patient)
                .Include(b => b.LabTest)
                .FirstOrDefaultAsync(b => b.Id == model.BookingId);

            if (booking == null)
            {
                TempData["ErrorMessage"] = "Test booking not found.";
                return RedirectToAction(nameof(Index), new { tab = "results" });
            }

            string? uploadedFileUrl = model.ExternalFileUrl;

            // Handle file upload if provided
            if (model.ResultFile != null && model.ResultFile.Length > 0)
            {
                var allowedExts = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
                var ext = Path.GetExtension(model.ResultFile.FileName).ToLowerInvariant();
                if (!allowedExts.Contains(ext))
                {
                    TempData["ErrorMessage"] = "Only PDF, JPG, and PNG files are accepted for lab reports.";
                    return RedirectToAction(nameof(Index), new { tab = "results" });
                }

                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "lab-results");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = $"lab_result_{booking.Id}_{DateTime.UtcNow:yyyyMMddHHmmss}{ext}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ResultFile.CopyToAsync(stream);
                }

                uploadedFileUrl = $"/uploads/lab-results/{uniqueFileName}";
            }

            var staffId = _userManager.GetUserId(User);

            await _labService.SaveTestResultsAsync(
                booking.Id,
                model.PlateletCount,
                model.WbcCount,
                model.Hematocrit,
                model.DengueNs1,
                model.DengueIgm,
                model.DengueIgg,
                model.TestNotes,
                uploadedFileUrl,
                staffId
            );

            if (model.AutoVerify)
            {
                var staffUser = await _userManager.GetUserAsync(User);
                var verifierName = staffUser?.FullName ?? "Authorized Lab Staff";
                await _labService.VerifyBookingResultAsync(booking.Id, verifierName);
            }

            // Real-time critical alert notification if Platelet < 50,000
            if (model.PlateletCount.HasValue && model.PlateletCount.Value < 50000)
            {
                var patientName = booking.Patient?.FullName ?? "Unknown";
                var alert = new Alert
                {
                    Message = $"CRITICAL LAB ALERT: Thrombocytopenia ({model.PlateletCount.Value:N0}/mcL) detected for Patient {patientName} (Booking #{booking.Id}). Immediate clinical monitoring required.",
                    Language = "en",
                    SentAt = DateTime.UtcNow,
                    DeliveredCount = 1,
                    OpenedCount = 0,
                    FailedCount = 0
                };
                _context.Alerts.Add(alert);
                await _context.SaveChangesAsync();

                // Broadcast via SignalR AlertsHub
                try
                {
                    await _alertsHub.Clients.All.SendAsync("ReceiveAlert", new
                    {
                        message = alert.Message,
                        messageBn = $"জরুরী ল্যাব সতর্কতা: রোগীর রক্তে প্লাটিলেট আশঙ্কাজনকভাবে কমেছে ({model.PlateletCount.Value:N0}/mcL) - রোগী: {patientName} (বুকিং #{booking.Id})",
                        sentAt = alert.SentAt.ToString("o")
                    });
                }
                catch
                {
                    // Fallback gracefully if hub transport is offline
                }

                TempData["WarningMessage"] = $"Critical platelet count (<50,000/mcL) logged! A high-priority clinical alert was generated.";
            }
            else
            {
                TempData["SuccessMessage"] = $"Lab results for Booking #{booking.Id} uploaded and synced with Patient Records successfully.";
            }

            return RedirectToAction(nameof(Index), new { tab = "results" });
        }

        // ── Workflow Action: Verify Test Result ─────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyResult(int bookingId)
        {
            var user = await _userManager.GetUserAsync(User);
            var staffName = user?.FullName ?? "Certified Medical Technologist";

            await _labService.VerifyBookingResultAsync(bookingId, staffName);

            TempData["SuccessMessage"] = $"Test Result #{bookingId} verified and patient notification sent.";
            return RedirectToAction(nameof(Index), new { tab = "results" });
        }

        // ── Workflow Action: Manage Available Lab Tests ─────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveTest(LabTestManageInputModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Name))
            {
                TempData["ErrorMessage"] = "Test name is required.";
                return RedirectToAction(nameof(Index), new { tab = "tests" });
            }

            var labTest = new LabTest
            {
                Id = model.Id,
                Name = model.Name.Trim(),
                Description = model.Description?.Trim() ?? string.Empty,
                Cost = model.Cost >= 0 ? model.Cost : 0,
                IsAvailable = model.IsAvailable
            };

            await _labService.SaveLabTestAsync(labTest);

            TempData["SuccessMessage"] = model.Id == 0 
                ? $"New diagnostic test '{model.Name}' added to laboratory catalog." 
                : $"Test '{model.Name}' updated successfully.";

            return RedirectToAction(nameof(Index), new { tab = "tests" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleTest(int id)
        {
            await _labService.ToggleTestAvailabilityAsync(id);
            TempData["SuccessMessage"] = "Test availability status toggled.";
            return RedirectToAction(nameof(Index), new { tab = "tests" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmBooking(int id)
        {
            await _labService.UpdateBookingStatusAsync(id, "Confirmed");
            TempData["SuccessMessage"] = $"Test booking #{id} has been confirmed.";
            return RedirectToAction(nameof(Index), new { tab = "bookings" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelBooking(int id)
        {
            await _labService.UpdateBookingStatusAsync(id, "Cancelled");
            TempData["WarningMessage"] = $"Test booking #{id} has been cancelled.";
            return RedirectToAction(nameof(Index), new { tab = "bookings" });
        }

        // ── Printable Lab Diagnostic Report ─────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> PrintReport(int id)
        {
            var booking = await _context.LabBookings
                .Include(b => b.Patient)
                .Include(b => b.LabTest)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
            {
                return NotFound();
            }

            return View(booking);
        }
    }
}
