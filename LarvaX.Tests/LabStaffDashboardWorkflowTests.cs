using System;
using System.Linq;
using System.Threading.Tasks;
using LarvaX.Application.Services;
using LarvaX.Core.Entities;
using LarvaX.Infrastructure.Data;
using LarvaX.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LarvaX.Tests
{
    public class LabStaffDashboardWorkflowTests
    {
        private async Task<(ApplicationDbContext Context, ApplicationUser Patient, LabTest Test)> CreateSeededContextAsync(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            var context = new ApplicationDbContext(options);

            var patient = new ApplicationUser
            {
                Id = "patient-" + Guid.NewGuid().ToString("N").Substring(0, 8),
                UserName = "patient@example.com",
                FullName = "Rahim Uddin",
                Email = "patient@example.com",
                PhoneNumber = "+8801700000000"
            };
            context.Users.Add(patient);

            var labTest = new LabTest
            {
                Name = "Dengue Duo & CBC",
                Description = "Dengue NS1 Antigen and Platelet Count",
                Cost = 650m,
                IsAvailable = true
            };
            context.LabTests.Add(labTest);

            await context.SaveChangesAsync();

            return (context, patient, labTest);
        }

        [Fact]
        public async Task SampleManagement_StatusProgression_UpdatesTimestamps()
        {
            // Arrange
            var (context, patient, labTest) = await CreateSeededContextAsync("SampleManagementDb");
            var service = new LabService(context);

            var booking = await service.BookLabTestAsync(patient.Id, labTest.Id, DateTime.UtcNow.AddHours(2));
            Assert.Equal("Pending", booking.Status);

            // Act 1: Collect sample
            await service.UpdateSampleStatusAsync(booking.Id, "SampleCollected");
            var collectedBooking = await service.GetLabBookingByIdAsync(booking.Id);
            Assert.NotNull(collectedBooking);
            Assert.Equal("SampleCollected", collectedBooking.SampleStatus);
            Assert.NotNull(collectedBooking.SampleCollectedAt);

            // Act 2: Mark Received in Lab
            await service.UpdateSampleStatusAsync(booking.Id, "SampleReceived");
            var receivedBooking = await service.GetLabBookingByIdAsync(booking.Id);
            Assert.NotNull(receivedBooking);
            Assert.Equal("SampleReceived", receivedBooking.SampleStatus);
            Assert.NotNull(receivedBooking.SampleReceivedAt);

            // Act 3: Mark Processing
            await service.UpdateSampleStatusAsync(booking.Id, "Processing");
            var processingBooking = await service.GetLabBookingByIdAsync(booking.Id);
            Assert.NotNull(processingBooking);
            Assert.Equal("Processing", processingBooking.SampleStatus);
            Assert.NotNull(processingBooking.ProcessingStartedAt);
        }

        [Fact]
        public async Task SaveTestResults_SyncsStructuredValuesAndPatientRecord()
        {
            // Arrange
            var (context, patient, labTest) = await CreateSeededContextAsync("SaveResultsDb");
            var service = new LabService(context);

            var booking = await service.BookLabTestAsync(patient.Id, labTest.Id, DateTime.UtcNow);

            // Act: Enter test values
            var result = await service.SaveTestResultsAsync(
                booking.Id,
                plateletCount: 42000,
                wbcCount: 3200,
                hematocrit: 44.5,
                dengueNs1: "Positive",
                dengueIgm: "Negative",
                dengueIgg: "Positive",
                testNotes: "Severe thrombocytopenia with leukopenia. Early acute infection.",
                resultLink: "/uploads/lab-results/lab_result_202.pdf",
                staffId: "labstaff-user-1"
            );

            // Assert LabBooking state
            Assert.NotNull(result);
            Assert.Equal("Completed", result.Status);
            Assert.Equal(42000, result.PlateletCount);
            Assert.Equal("Positive", result.DengueNs1);
            Assert.Equal("labstaff-user-1", result.LabStaffId);
            Assert.NotNull(result.CompletedAt);

            // Assert automatic sync to PatientRecord table
            var patientRecord = await context.PatientRecords.FirstOrDefaultAsync(r => r.PatientId == patient.Id);
            Assert.NotNull(patientRecord);
            Assert.Equal("LabResult", patientRecord.RecordType);
            Assert.Contains("42,000", patientRecord.Description);
            Assert.Contains("NS1: Positive", patientRecord.Description);
            Assert.Equal("/uploads/lab-results/lab_result_202.pdf", patientRecord.FileUrl);
        }

        [Fact]
        public async Task VerifyBookingResult_SetsVerificationAndNotifiedFlags()
        {
            // Arrange
            var (context, patient, labTest) = await CreateSeededContextAsync("VerifyBookingDb");
            var service = new LabService(context);

            var booking = await service.BookLabTestAsync(patient.Id, labTest.Id, DateTime.UtcNow);
            Assert.False(booking.IsVerified);
            Assert.False(booking.PatientNotified);

            // Act
            await service.VerifyBookingResultAsync(booking.Id, "Dr. Rahman (Pathologist)");

            // Assert
            var verified = await service.GetLabBookingByIdAsync(booking.Id);
            Assert.NotNull(verified);
            Assert.True(verified.IsVerified);
            Assert.Equal("Dr. Rahman (Pathologist)", verified.VerifiedBy);
            Assert.NotNull(verified.VerifiedAt);
            Assert.True(verified.PatientNotified);
        }

        [Fact]
        public async Task CatalogManagement_AddAndToggleAvailability_WorksCorrectly()
        {
            // Arrange
            var (context, _, _) = await CreateSeededContextAsync("CatalogManagementDb");
            var service = new LabService(context);

            var newTest = new LabTest
            {
                Name = "Rapid Dengue NS1 Antigen",
                Description = "Early detection chromatographic strip",
                Cost = 350m,
                IsAvailable = true
            };

            // Act 1: Save test
            var saved = await service.SaveLabTestAsync(newTest);
            Assert.True(saved.Id > 0);

            // Act 2: Toggle
            await service.ToggleTestAvailabilityAsync(saved.Id);
            var toggled = await service.GetLabTestByIdAsync(saved.Id);
            Assert.NotNull(toggled);
            Assert.False(toggled.IsAvailable);

            // Act 3: Toggle back
            await service.ToggleTestAvailabilityAsync(saved.Id);
            var activeAgain = await service.GetLabTestByIdAsync(saved.Id);
            Assert.NotNull(activeAgain);
            Assert.True(activeAgain.IsAvailable);
        }
    }
}
