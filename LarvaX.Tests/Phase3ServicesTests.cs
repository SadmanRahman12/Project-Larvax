using LarvaX.Application.Services;
using LarvaX.Core.Entities;
using LarvaX.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Xunit;
using LarvaX.Infrastructure.Services;

namespace LarvaX.Tests
{
    public class Phase3ServicesTests
    {
        private ApplicationDbContext GetInMemoryContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task LabService_BookLabTest_SavesBooking()
        {
            // Arrange
            var context = GetInMemoryContext("LabServiceTestDb");
            var service = new LabService(context);

            var testId = 1;
            var patientId = "patient-1";
            var scheduledAt = DateTime.UtcNow.AddDays(1);

            // Act
            var booking = await service.BookLabTestAsync(patientId, testId, scheduledAt);

            // Assert
            Assert.NotNull(booking);
            Assert.Equal("Pending", booking.Status);
            Assert.Equal(testId, booking.LabTestId);
            Assert.Equal(patientId, booking.PatientId);
            
            var saved = await context.LabBookings.FindAsync(booking.Id);
            Assert.NotNull(saved);
        }

        [Fact]
        public async Task PatientRecordService_AddRecord_SavesRecord()
        {
            // Arrange
            var context = GetInMemoryContext("PatientRecordTestDb");
            var service = new PatientRecordService(context);

            // Act
            var record = await service.AddPatientRecordAsync("user-1", "Test Record", "Test Description", "LabResult", "http://test.com/file");

            // Assert
            Assert.NotNull(record);
            Assert.Equal("Test Record", record.Title);
            Assert.Equal("user-1", record.PatientId);
            
            var saved = await context.PatientRecords.FindAsync(record.Id);
            Assert.NotNull(saved);
        }

        [Fact]
        public async Task IcuBedService_GetIcuHospitals_FiltersByCity()
        {
            // Arrange
            var service = new IcuBedService();

            // Act
            var dhakaBeds = await service.GetIcuHospitalsAsync("Dhaka");
            var allBeds = await service.GetIcuHospitalsAsync();

            // Assert
            Assert.NotEmpty(dhakaBeds);
            Assert.All(dhakaBeds, b => Assert.Equal("Dhaka", b.City));
            Assert.True(allBeds.Count() > dhakaBeds.Count());
        }

        [Fact]
        public void FluidManagementService_CalculateFluidPlan_ReturnsCorrectPlan()
        {
            // Arrange
            var service = new FluidManagementService();
            var parameters = new FluidManagementParameters
            {
                Weight = 50,
                DehydrationPercent = 5,
                OngoingLosses = 1,
                ClinicalMode = "DengueShock"
            };

            // Act
            var result = service.CalculateFluidPlan(parameters);

            // Assert
            Assert.Equal(1000m, result.BolusVolume); // 20ml/kg for Shock
            Assert.Equal(2100m, result.Maintenance24h); // 1500 + 20*(50-20)
            Assert.True(result.IsOverThreshold); // 50kg * 70 = 3500 threshold
        }
    }
}
