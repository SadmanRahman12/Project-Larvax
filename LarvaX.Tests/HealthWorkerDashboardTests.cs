using System;
using System.Linq;
using System.Threading.Tasks;
using LarvaX.Core.Entities;
using LarvaX.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LarvaX.Tests
{
    public class HealthWorkerDashboardTests
    {
        private ApplicationDbContext GetInMemoryContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task DengueCase_CreationAndStatusProgression_WorksCorrectly()
        {
            // Arrange
            var context = GetInMemoryContext("HW_CaseProgression");
            var dengueCase = new DengueCase
            {
                PatientName = "Farid Ahmed",
                PatientPhone = "+8801700112233",
                PatientAddress = "Dhanmondi 27, Dhaka",
                Latitude = 23.7501,
                Longitude = 90.3702,
                Age = 29,
                Gender = "Male",
                Status = CaseStatus.Suspected,
                Severity = CaseSeverity.Mild,
                PlateletCount = 140000,
                Hematocrit = 40.0,
                Symptoms = "Sudden fever, headache, back pain"
            };

            // Act: Add case
            context.DengueCases.Add(dengueCase);
            await context.SaveChangesAsync();

            // Assert initial status
            Assert.True(dengueCase.Id > 0);
            Assert.Equal(CaseStatus.Suspected, dengueCase.Status);

            // Act: Progression: Suspected -> UnderObservation -> Confirmed -> Recovering -> Closed
            dengueCase.Status = CaseStatus.UnderObservation;
            dengueCase.PlateletCount = 98000; // Low platelet warning
            await context.SaveChangesAsync();
            Assert.Equal(CaseStatus.UnderObservation, dengueCase.Status);

            dengueCase.Status = CaseStatus.Confirmed;
            dengueCase.Severity = CaseSeverity.Severe;
            dengueCase.IsEscalated = true;
            dengueCase.EscalationReason = "Platelet dropped below 50k";
            await context.SaveChangesAsync();
            Assert.Equal(CaseStatus.Confirmed, dengueCase.Status);
            Assert.True(dengueCase.IsEscalated);

            dengueCase.Status = CaseStatus.Recovering;
            dengueCase.PlateletCount = 160000;
            await context.SaveChangesAsync();
            Assert.Equal(CaseStatus.Recovering, dengueCase.Status);

            dengueCase.Status = CaseStatus.Closed;
            await context.SaveChangesAsync();
            Assert.Equal(CaseStatus.Closed, dengueCase.Status);
        }

        [Fact]
        public async Task HealthWorkerTask_ToggleAndPriority_WorksCorrectly()
        {
            // Arrange
            var context = GetInMemoryContext("HW_Tasks");
            var task = new HealthWorkerTask
            {
                Title = "Investigate Standing Water at Sector 7 Park",
                Category = "ReportInvestigation",
                Priority = TaskPriority.Urgent,
                DueDate = DateTime.UtcNow.AddHours(4),
                IsCompleted = false
            };

            context.HealthWorkerTasks.Add(task);
            await context.SaveChangesAsync();

            // Assert
            Assert.False(task.IsCompleted);
            Assert.Null(task.CompletedAt);

            // Act: Toggle complete
            task.IsCompleted = true;
            task.CompletedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();

            // Assert
            var saved = await context.HealthWorkerTasks.FindAsync(task.Id);
            Assert.NotNull(saved);
            Assert.True(saved.IsCompleted);
            Assert.NotNull(saved.CompletedAt);
            Assert.Equal(TaskPriority.Urgent, saved.Priority);
        }

        [Fact]
        public async Task CaseReferral_EmergencyReferral_CreatesCorrectly()
        {
            // Arrange
            var context = GetInMemoryContext("HW_Referrals");
            var referral = new CaseReferral
            {
                PatientName = "Salma Begum",
                PatientPhone = "+8801811223344",
                Target = ReferralTarget.Hospital,
                Urgency = ReferralUrgency.Emergency,
                Status = ReferralStatus.Pending,
                Reason = "Severe thrombocytopenia with plasma leakage and persistent abdominal pain",
                ClinicalNotes = "Platelet 38k, HCT 48%, BP 90/60",
                CreatedAt = DateTime.UtcNow
            };

            context.CaseReferrals.Add(referral);
            await context.SaveChangesAsync();

            // Assert
            Assert.True(referral.Id > 0);
            Assert.Equal(ReferralTarget.Hospital, referral.Target);
            Assert.Equal(ReferralUrgency.Emergency, referral.Urgency);
            Assert.Equal(ReferralStatus.Pending, referral.Status);

            // Act: Accept referral
            referral.Status = ReferralStatus.Accepted;
            referral.ResolvedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();

            Assert.Equal(ReferralStatus.Accepted, referral.Status);
            Assert.NotNull(referral.ResolvedAt);
        }

        [Fact]
        public async Task CitizenReport_ExtendedStatuses_PersistCorrectly()
        {
            // Arrange
            var context = GetInMemoryContext("HW_CitizenReports");
            var report = new Report
            {
                UserId = "user-123",
                Latitude = 23.75,
                Longitude = 90.38,
                Description = "Aedes larvae found in uncleaned construction drums",
                DiseaseType = DiseaseType.Dengue,
                Status = ReportStatus.Received,
                Verification = ReportVerification.Pending,
                FieldNotes = null
            };

            context.Reports.Add(report);
            await context.SaveChangesAsync();

            // Act: Worker reviews and assigns
            report.Status = ReportStatus.Investigating;
            report.Verification = ReportVerification.Verified;
            report.FieldNotes = "Field visited. Larvicide granules applied. Water discarded.";
            await context.SaveChangesAsync();

            // Assert
            var saved = await context.Reports.FindAsync(report.Id);
            Assert.NotNull(saved);
            Assert.Equal(ReportStatus.Investigating, saved.Status);
            Assert.Equal(ReportVerification.Verified, saved.Verification);
            Assert.Equal("Field visited. Larvicide granules applied. Water discarded.", saved.FieldNotes);
        }
    }
}
