using System;
using System.Linq;
using System.Threading.Tasks;
using LarvaX.Core.Entities;
using LarvaX.Infrastructure.Data;
using LarvaX.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LarvaX.Tests
{
    public class DoctorScheduleTests
    {
        private ApplicationDbContext GetInMemoryContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task DoctorSchedule_AddAndRetrieve_WorksCorrectly()
        {
            // Arrange
            var context = GetInMemoryContext("DocSched_AddAndRetrieve");
            var service = new TelemedicineService(context, null!);

            var doctorId = "doc-100";
            var schedule = new DoctorSchedule
            {
                DoctorId = doctorId,
                DayOfWeek = DayOfWeek.Monday,
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(13, 0, 0),
                SlotDurationMinutes = 30,
                ShiftName = "Morning Clinic",
                IsActive = true
            };

            // Act
            await service.AddDoctorScheduleAsync(schedule);
            var schedules = await service.GetDoctorSchedulesAsync(doctorId);

            // Assert
            Assert.Single(schedules);
            Assert.Equal(DayOfWeek.Monday, schedules[0].DayOfWeek);
            Assert.Equal(new TimeSpan(9, 0, 0), schedules[0].StartTime);
            Assert.Equal(new TimeSpan(13, 0, 0), schedules[0].EndTime);
            Assert.True(schedules[0].IsActive);
        }

        [Fact]
        public async Task DoctorSchedule_QuickPresets_AppliesWeekdayShifts()
        {
            // Arrange
            var context = GetInMemoryContext("DocSched_Presets");
            var service = new TelemedicineService(context, null!);

            var doctorId = "doc-preset-1";

            // Act
            await service.SetQuickPresetScheduleAsync(doctorId, "weekday_morning");
            var schedules = await service.GetDoctorSchedulesAsync(doctorId);

            // Assert: Sunday through Thursday (5 days)
            Assert.Equal(5, schedules.Count);
            Assert.Contains(schedules, s => s.DayOfWeek == DayOfWeek.Sunday);
            Assert.Contains(schedules, s => s.DayOfWeek == DayOfWeek.Thursday);
            Assert.DoesNotContain(schedules, s => s.DayOfWeek == DayOfWeek.Friday);
        }

        [Fact]
        public async Task AvailableSlots_ReturnsSlotsForScheduledDay_AndEmptyForNonScheduledDay()
        {
            // Arrange
            var context = GetInMemoryContext("DocSched_Slots");
            var service = new TelemedicineService(context, null!);

            var doctorId = "doc-slots-1";
            // Set Monday 09:00 - 11:00 with 30-min slots -> 4 slots: 09:00, 09:30, 10:00, 10:30
            await service.AddDoctorScheduleAsync(new DoctorSchedule
            {
                DoctorId = doctorId,
                DayOfWeek = DayOfWeek.Monday,
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(11, 0, 0),
                SlotDurationMinutes = 30,
                ShiftName = "Monday Shift",
                IsActive = true
            });

            // Find next Monday
            var nextMonday = DateTime.Today;
            while (nextMonday.DayOfWeek != DayOfWeek.Monday || nextMonday <= DateTime.Today)
            {
                nextMonday = nextMonday.AddDays(1);
            }

            // Find next Tuesday (doctor has no schedule on Tuesday)
            var nextTuesday = nextMonday.AddDays(1);

            // Act
            var mondaySlots = await service.GetAvailableSlotsForDateAsync(doctorId, nextMonday);
            var tuesdaySlots = await service.GetAvailableSlotsForDateAsync(doctorId, nextTuesday);

            // Assert
            Assert.Equal(4, mondaySlots.Count);
            Assert.True(mondaySlots.All(s => s.IsAvailable));
            Assert.Empty(tuesdaySlots);
        }

        [Fact]
        public async Task ValidateAppointmentSlot_RejectsInvalidOrBookedSlot()
        {
            // Arrange
            var context = GetInMemoryContext("DocSched_Validation");
            var service = new TelemedicineService(context, null!);

            var doctorId = "doc-val-1";
            await service.AddDoctorScheduleAsync(new DoctorSchedule
            {
                DoctorId = doctorId,
                DayOfWeek = DayOfWeek.Wednesday,
                StartTime = new TimeSpan(10, 0, 0),
                EndTime = new TimeSpan(12, 0, 0),
                SlotDurationMinutes = 30,
                IsActive = true
            });

            // Find next Wednesday in future
            var nextWed = DateTime.Today;
            while (nextWed.DayOfWeek != DayOfWeek.Wednesday || nextWed <= DateTime.Today)
            {
                nextWed = nextWed.AddDays(1);
            }

            var validSlotTime = nextWed.AddHours(10); // 10:00 AM on Wednesday
            var invalidSlotTime = nextWed.AddHours(15); // 03:00 PM on Wednesday (outside schedule)
            var nonScheduledDay = nextWed.AddDays(1).AddHours(10); // Thursday (no schedule)

            // Act 1: Valid slot
            var validCheck = await service.ValidateAppointmentSlotAsync(doctorId, validSlotTime);
            Assert.True(validCheck.IsValid);
            Assert.Null(validCheck.ErrorMessage);

            // Act 2: Out of schedule hour
            var invalidCheck = await service.ValidateAppointmentSlotAsync(doctorId, invalidSlotTime);
            Assert.False(invalidCheck.IsValid);

            // Act 3: Day without schedule
            var noDayCheck = await service.ValidateAppointmentSlotAsync(doctorId, nonScheduledDay);
            Assert.False(noDayCheck.IsValid);

            // Act 4: Book the slot, then verify another booking at that time is rejected
            await service.BookAppointmentAsync("patient-1", doctorId, validSlotTime, "First booking");
            var doubleBookingCheck = await service.ValidateAppointmentSlotAsync(doctorId, validSlotTime);
            Assert.False(doubleBookingCheck.IsValid);
            Assert.Contains("already booked", doubleBookingCheck.ErrorMessage ?? "");
        }
    }
}
