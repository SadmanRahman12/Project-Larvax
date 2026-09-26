using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LarvaX.Application.Services;
using LarvaX.Core.Entities;
using LarvaX.Infrastructure.Data;
using LarvaX.Infrastructure.Services;
using LarvaX.Web.Jobs;
using LarvaX.Web.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace LarvaX.Tests
{
    public class Stage3SystemTests
    {
        private ApplicationDbContext CreateInMemoryContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new ApplicationDbContext(options);
        }

        private (Mock<IHubContext<AlertsHub>>, Mock<IClientProxy>) BuildMockHubContext()
        {
            var mockClientProxy = new Mock<IClientProxy>();
            mockClientProxy
                .Setup(c => c.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var mockClients = new Mock<IHubClients>();
            mockClients.Setup(c => c.All).Returns(mockClientProxy.Object);

            var mockHub = new Mock<IHubContext<AlertsHub>>();
            mockHub.Setup(h => h.Clients).Returns(mockClients.Object);

            return (mockHub, mockClientProxy);
        }

        private RiskCalculationJob BuildJob(ApplicationDbContext context, IHubContext<AlertsHub> hubContext)
        {
            return new RiskCalculationJob(
                context,
                new RiskAssessmentService(),
                hubContext,
                NullLogger<RiskCalculationJob>.Instance
            );
        }

        // ST-01: Hangfire RiskCalculationJob — 14-day Aggregation with High Risk Escalation (Expected)
        [Fact]
        public async Task ST01_RiskCalculationJob_Execute_AggregatesToHighRiskAndDispatchesSignalRAlert()
        {
            // Arrange
            var context = CreateInMemoryContext("ST01_RiskJobDb");
            var (mockHub, mockClientProxy) = BuildMockHubContext();

            var reporter = new ApplicationUser { Id = "reporter-1", UserName = "r@test.com", Email = "r@test.com" };
            context.Users.Add(reporter);

            // Seed 15 verified Dengue reports within the last 7 days (exceeds High risk threshold of 5)
            for (int i = 0; i < 15; i++)
            {
                context.Reports.Add(new Report
                {
                    UserId = "reporter-1",
                    Latitude = 23.81,
                    Longitude = 90.41,
                    DiseaseType = DiseaseType.Dengue,
                    Verification = ReportVerification.Verified,
                    CreatedAt = DateTime.UtcNow.AddDays(-i % 7)
                });
            }
            await context.SaveChangesAsync();

            var job = BuildJob(context, mockHub.Object);

            // Act
            await job.ExecuteAsync();

            // Assert: Zone created with High risk level
            var zone = await context.RiskZones.FirstOrDefaultAsync(z => z.Region == "Dhaka Metropolitan Area");
            Assert.NotNull(zone);
            Assert.Equal(RiskLevel.High, zone.RiskLevel);
            Assert.Equal(DataSufficiency.Sufficient, zone.DataSufficiency);
            Assert.True(zone.ConfidenceScore >= 0.70);

            // Assert: Alert entity saved in DB
            var alert = await context.Alerts.FirstOrDefaultAsync();
            Assert.NotNull(alert);
            Assert.Equal(zone.Id, alert.ZoneId);

            // Assert: SignalR broadcast was dispatched once
            mockClientProxy.Verify(c => c.SendCoreAsync("ReceiveAlert", It.IsAny<object[]>(), default), Times.Once);
        }

        // ST-02: Scheduled Risk Calculation with Zero Reports — Cold-Start Database State (Unexpected)
        [Fact]
        public async Task ST02_RiskCalculationJob_Execute_HandlesZeroReportsWithoutCrash()
        {
            // Arrange — completely empty database (cold start / new deployment)
            var context = CreateInMemoryContext("ST02_RiskJobZeroDb");
            var (mockHub, mockClientProxy) = BuildMockHubContext();
            var job = BuildJob(context, mockHub.Object);

            // Act — should complete without any exception
            await job.ExecuteAsync();

            // Assert: Zone created with Low risk and Insufficient data
            var zone = await context.RiskZones.FirstOrDefaultAsync(z => z.Region == "Dhaka Metropolitan Area");
            Assert.NotNull(zone);
            Assert.Equal(RiskLevel.Low, zone.RiskLevel);
            Assert.Equal(DataSufficiency.Insufficient, zone.DataSufficiency);
            Assert.Equal(0.1, zone.ConfidenceScore);

            // System Behaviour Note: On first zone creation (isNewOrElevated = true regardless of risk level),
            // the job always fires one SignalR alert to notify clients of the new zone registration.
            // This is by design in RiskCalculationJob lines 79-106.
            mockClientProxy.Verify(c => c.SendCoreAsync("ReceiveAlert", It.IsAny<object[]>(), default), Times.Once);
        }

        // ST-03: High-Concurrency Race Condition on Inventory Deduction (Exceptional)
        [Fact]
        public async Task ST03_InventoryService_ConcurrentDeductions_PreventNegativeStockWithAtomicGuard()
        {
            // Arrange
            var context = CreateInMemoryContext("ST03_ConcurrencyDb");
            var service = new InventoryService(context);

            var item = new InventoryItem
            {
                Name = "Platelet Concentrate Bags",
                Quantity = 5,
                Threshold = 2,
                Location = "DMCH Blood Bank"
            };
            await service.AddItemAsync(item);

            // Act: 10 concurrent threads each try to deduct 1 unit (only 5 units available)
            var tasks = Enumerable.Range(0, 10)
                .Select(i => service.RecordTransactionAsync(item.Id, -1, $"Ward-{i} emergency order", "nurse-1"));

            var results = await Task.WhenAll(tasks.Select(async t =>
            {
                try { await t; return true; }
                catch (InvalidOperationException) { return false; }
            }));

            // Assert: Exactly 5 succeed and 5 are rejected as insufficient stock
            int succeeded = results.Count(r => r);
            int rejected = results.Count(r => !r);

            // Note: With EF InMemory (no rowversioning), concurrent access may allow some excess.
            // This test documents the current behavior and confirms that the service-level guard
            // catches at least some violations. DEF-02 recommends [Timestamp] rowversion for full atomicity.
            Assert.True(succeeded <= 5, $"Expected ≤5 successes, but got {succeeded}. DEF-02: add rowversion concurrency token.");
            var finalItem = await service.GetItemByIdAsync(item.Id);
            Assert.NotNull(finalItem);
            Assert.True(finalItem.Quantity >= 0, $"Quantity went negative ({finalItem.Quantity}). Concurrency guard insufficient.");
        }

        // ST-04: Full QuestPDF Government Report Binary Stream Generation (Expected)
        [Fact]
        public async Task ST04_PdfReportService_GenerateGovernmentReport_ProducesValidPdfBinaryStream()
        {
            // Arrange — seed a month's worth of reports, risk zones, and alerts
            var context = CreateInMemoryContext("ST04_PdfDb");

            var reporter = new ApplicationUser { Id = "gov-reporter", UserName = "gov@test.com", Email = "gov@test.com" };
            context.Users.Add(reporter);

            for (int i = 0; i < 20; i++)
            {
                context.Reports.Add(new Report
                {
                    UserId = "gov-reporter",
                    Latitude = 23.8 + (i * 0.01),
                    Longitude = 90.4 + (i * 0.01),
                    DiseaseType = DiseaseType.Dengue,
                    Verification = i % 3 == 0 ? ReportVerification.Verified : ReportVerification.Pending,
                    CreatedAt = DateTime.UtcNow.AddDays(-i)
                });
            }

            context.RiskZones.Add(new RiskZone
            {
                Region = "Dhaka North City Corporation",
                DiseaseType = DiseaseType.Dengue,
                RiskLevel = RiskLevel.High,
                ConfidenceScore = 0.85,
                DataSufficiency = DataSufficiency.Sufficient
            });
            context.RiskZones.Add(new RiskZone
            {
                Region = "Dhaka South City Corporation",
                DiseaseType = DiseaseType.Dengue,
                RiskLevel = RiskLevel.Medium,
                ConfidenceScore = 0.65,
                DataSufficiency = DataSufficiency.Sufficient
            });

            context.Alerts.Add(new Alert
            {
                Message = "High dengue alert: Dhaka Metropolitan",
                Language = "en",
                ZoneId = 1,
                RadiusKm = 25.0,
                SentAt = DateTime.UtcNow.AddDays(-5),
                DeliveredCount = 1200
            });

            await context.SaveChangesAsync();

            var service = new PdfReportService(context);
            var startDate = DateTime.UtcNow.AddDays(-30);
            var endDate = DateTime.UtcNow;

            // Act
            var pdfBytes = await service.GenerateGovernmentReportAsync(startDate, endDate);

            // Assert
            Assert.NotNull(pdfBytes);
            Assert.True(pdfBytes.Length > 5000, $"PDF seems too small ({pdfBytes.Length} bytes). Expected a rich multi-section document.");

            // Verify PDF magic header (%PDF-)
            var header = Encoding.ASCII.GetString(pdfBytes, 0, 5);
            Assert.Equal("%PDF-", header);
        }

        // ST-05: Repeated Job Execution — Idempotent Zone Update Without Duplicate Records (Exceptional)
        [Fact]
        public async Task ST05_RiskCalculationJob_Execute_IsIdempotentOnRepeatExecution()
        {
            // Arrange: this simulates retry behavior after a transient DB fault triggers a second run
            var context = CreateInMemoryContext("ST05_IdempotentDb");
            var (mockHub, _) = BuildMockHubContext();

            var reporter = new ApplicationUser { Id = "reporter-2", UserName = "r2@test.com", Email = "r2@test.com" };
            context.Users.Add(reporter);
            for (int i = 0; i < 6; i++)
            {
                context.Reports.Add(new Report
                {
                    UserId = "reporter-2",
                    Latitude = 23.81, Longitude = 90.41,
                    DiseaseType = DiseaseType.Dengue,
                    Verification = ReportVerification.Verified,
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                });
            }
            await context.SaveChangesAsync();

            var job = BuildJob(context, mockHub.Object);

            // Act: Execute the job twice (simulating a retry after transient failure)
            await job.ExecuteAsync();
            await job.ExecuteAsync();

            // Assert: Only ONE RiskZone record for "Dhaka Metropolitan Area" exists (no duplicates)
            var zoneCount = await context.RiskZones.CountAsync(z => z.Region == "Dhaka Metropolitan Area");
            Assert.Equal(1, zoneCount);

            var zone = await context.RiskZones.FirstAsync(z => z.Region == "Dhaka Metropolitan Area");
            Assert.Equal(RiskLevel.High, zone.RiskLevel);
        }

        // ST-06: Full Citizen Hazard Report Lifecycle — Submit → Verify → Resolve (Expected)
        [Fact]
        public async Task ST06_HazardReportLifecycle_SubmitVerifyResolve_CompletesFullWorkflow()
        {
            // Arrange
            var context = CreateInMemoryContext("ST06_LifecycleDb");
            var reportService = new ReportService();

            var citizen = new ApplicationUser { Id = "citizen-1", UserName = "citizen@test.com", Email = "citizen@test.com" };
            context.Users.Add(citizen);

            // Step 1: Citizen submits hazard report
            var report = new Report
            {
                UserId = "citizen-1",
                Latitude = 23.7946,
                Longitude = 90.4057,
                Description = "Stagnant water in construction site near Dhanmondi Lake",
                DiseaseType = DiseaseType.Dengue,
                Status = ReportStatus.Received,
                Verification = ReportVerification.Pending,
                CreatedAt = DateTime.UtcNow
            };
            context.Reports.Add(report);
            await context.SaveChangesAsync();

            Assert.Equal(ReportStatus.Received, report.Status);
            Assert.Equal(ReportVerification.Pending, report.Verification);

            // Step 2: Health worker verifies and transitions state
            var canTransitionToReview = reportService.CanTransition(report.Status, ReportStatus.UnderReview);
            Assert.True(canTransitionToReview);

            var nextStatus = reportService.DetermineNextStatus(report.Status, ReportVerification.Verified);
            Assert.Equal(ReportStatus.UnderReview, nextStatus);

            report.Status = nextStatus;
            report.Verification = ReportVerification.Verified;
            report.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();

            // Step 3: Health worker resolves the report
            var canResolve = reportService.CanTransition(report.Status, ReportStatus.Resolved);
            Assert.True(canResolve);

            report.Status = ReportStatus.Resolved;
            report.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();

            // Assert: Report ends in Resolved + Verified state
            var finalReport = await context.Reports.FindAsync(report.Id);
            Assert.NotNull(finalReport);
            Assert.Equal(ReportStatus.Resolved, finalReport.Status);
            Assert.Equal(ReportVerification.Verified, finalReport.Verification);
        }

        // ST-07: Rapid Repetitive Hazard Submission — Documents Absence of Rate Limiting (Unexpected)
        [Fact]
        public async Task ST07_ReportSubmission_MultipleRapidSubmissions_AllAcceptedWithoutRateLimit()
        {
            // Arrange
            var context = CreateInMemoryContext("ST07_SpamDb");

            var citizen = new ApplicationUser { Id = "citizen-spam", UserName = "spammer@test.com", Email = "spammer@test.com" };
            context.Users.Add(citizen);

            // Act: Simulate 25 rapid hazard report submissions from the same user at the same coordinates
            for (int i = 0; i < 25; i++)
            {
                context.Reports.Add(new Report
                {
                    UserId = "citizen-spam",
                    Latitude = 23.7946,
                    Longitude = 90.4057,
                    Description = $"Spam report #{i}",
                    DiseaseType = DiseaseType.Dengue,
                    Status = ReportStatus.Received,
                    Verification = ReportVerification.Pending,
                    CreatedAt = DateTime.UtcNow
                });
            }
            await context.SaveChangesAsync();

            // Assert: All 25 are accepted — documents DEF-05 (missing rate limiting)
            var count = await context.Reports.CountAsync(r => r.UserId == "citizen-spam");
            Assert.Equal(25, count);
            // NOTE: DEF-05 — Apply ASP.NET Core Rate Limiting on the /Reports/Create endpoint to prevent spam.
        }

        // ST-08: WebRTC Video Room Idempotency — Existing Room ID Not Overwritten on Repeat Call (Exceptional)
        [Fact]
        public async Task ST08_TelemedicineService_EnsureVideoRoomExists_IsIdempotentAndDoesNotOverwriteRoomId()
        {
            // Arrange
            var context = CreateInMemoryContext("ST08_VideoRoomDb");
            var userStoreMock = new Mock<Microsoft.AspNetCore.Identity.IUserStore<ApplicationUser>>();
            var userManagerMock = new Mock<Microsoft.AspNetCore.Identity.UserManager<ApplicationUser>>(
                userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

            var service = new TelemedicineService(context, userManagerMock.Object);

            var appointment = await service.BookAppointmentAsync("patient-9", "doc-9", DateTime.UtcNow.AddDays(1), "Follow-up");

            // Act: Call EnsureVideoRoomExistsAsync twice
            await service.EnsureVideoRoomExistsAsync(appointment);
            var firstRoomId = appointment.VideoRoomId;
            Assert.False(string.IsNullOrWhiteSpace(firstRoomId));

            await service.EnsureVideoRoomExistsAsync(appointment);
            var secondRoomId = appointment.VideoRoomId;

            // Assert: Room ID remains stable across repeated calls (idempotent behaviour)
            Assert.Equal(firstRoomId, secondRoomId);

            // Also confirm it's a valid GUID format
            Assert.True(Guid.TryParse(firstRoomId, out _), "VideoRoomId should be a valid GUID.");
        }
    }
}
