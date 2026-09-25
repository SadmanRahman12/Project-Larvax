using System;
using System.Collections.Generic;
using LarvaX.Core.Entities;
using LarvaX.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using LarvaX.Application.Services;

namespace LarvaX.Infrastructure.Services
{
    public class LabService : ILabService
    {
        private readonly ApplicationDbContext _context;

        public LabService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<LabTest>> GetAvailableLabTestsAsync()
        {
            return await _context.LabTests
                .Where(lt => lt.IsAvailable)
                .OrderBy(lt => lt.Name)
                .ToListAsync();
        }

        public async Task<LabTest?> GetLabTestByIdAsync(int id)
        {
            return await _context.LabTests.FindAsync(id);
        }

        public async Task<LabBooking> BookLabTestAsync(string patientId, int labTestId, DateTime scheduledAt)
        {
            var booking = new LabBooking
            {
                PatientId = patientId,
                LabTestId = labTestId,
                ScheduledAt = scheduledAt.ToUniversalTime(),
                Status = "Pending",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.LabBookings.Add(booking);
            await _context.SaveChangesAsync();

            return booking;
        }

        public async Task<IEnumerable<LabBooking>> GetUserLabBookingsAsync(string userId)
        {
            return await _context.LabBookings
                .Include(lb => lb.LabTest)
                .Include(lb => lb.Patient)
                .Where(lb => lb.PatientId == userId)
                .OrderByDescending(lb => lb.ScheduledAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<LabBooking>> GetAllBookingsAsync()
        {
            return await _context.LabBookings
                .Include(lb => lb.LabTest)
                .Include(lb => lb.Patient)
                .OrderByDescending(lb => lb.ScheduledAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<LabTest>> GetAllLabTestsAsync()
        {
            return await _context.LabTests
                .OrderBy(lt => lt.Name)
                .ToListAsync();
        }

        public async Task<LabBooking?> GetLabBookingByIdAsync(int bookingId)
        {
            return await _context.LabBookings
                .Include(lb => lb.LabTest)
                .Include(lb => lb.Patient)
                .FirstOrDefaultAsync(lb => lb.Id == bookingId);
        }

        public async Task UpdateBookingStatusAsync(int bookingId, string status, string? resultLink = null)
        {
            var booking = await _context.LabBookings.FindAsync(bookingId);
            if (booking != null)
            {
                booking.Status = status;
                if (resultLink != null)
                {
                    booking.ResultLink = resultLink;
                }
                booking.UpdatedAt = DateTime.UtcNow;
                
                _context.LabBookings.Update(booking);
                await _context.SaveChangesAsync();
            }
        }

        public async Task UpdateSampleStatusAsync(int bookingId, string sampleStatus, string? rejectionReason = null)
        {
            var booking = await _context.LabBookings.FindAsync(bookingId);
            if (booking != null)
            {
                booking.SampleStatus = sampleStatus;
                booking.Status = sampleStatus;
                booking.UpdatedAt = DateTime.UtcNow;

                var now = DateTime.UtcNow;
                if (sampleStatus == "SampleCollected" || sampleStatus == "Collected")
                {
                    booking.SampleCollectedAt ??= now;
                    booking.Status = "SampleCollected";
                }
                else if (sampleStatus == "SampleReceived" || sampleStatus == "Received")
                {
                    booking.SampleReceivedAt ??= now;
                    booking.Status = "SampleReceived";
                }
                else if (sampleStatus == "Processing")
                {
                    booking.ProcessingStartedAt ??= now;
                    booking.Status = "Processing";
                }
                else if (sampleStatus == "Rejected")
                {
                    booking.Status = "Rejected";
                    booking.RejectionReason = rejectionReason;
                }

                _context.LabBookings.Update(booking);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<LabBooking> SaveTestResultsAsync(
            int bookingId,
            int? plateletCount,
            int? wbcCount,
            double? hematocrit,
            string? dengueNs1,
            string? dengueIgm,
            string? dengueIgg,
            string? testNotes,
            string? resultLink,
            string? staffId)
        {
            var booking = await _context.LabBookings
                .Include(b => b.LabTest)
                .Include(b => b.Patient)
                .FirstOrDefaultAsync(b => b.Id == bookingId);

            if (booking == null)
            {
                throw new InvalidOperationException($"Lab booking #{bookingId} was not found.");
            }

            booking.PlateletCount = plateletCount;
            booking.WbcCount = wbcCount;
            booking.Hematocrit = hematocrit;
            booking.DengueNs1 = dengueNs1;
            booking.DengueIgm = dengueIgm;
            booking.DengueIgg = dengueIgg;
            booking.TestNotes = testNotes;
            if (!string.IsNullOrEmpty(resultLink))
            {
                booking.ResultLink = resultLink;
            }
            booking.LabStaffId = staffId;
            booking.Status = "Completed";
            booking.SampleStatus = "Completed";
            booking.CompletedAt = DateTime.UtcNow;
            booking.UpdatedAt = DateTime.UtcNow;

            // Automatically create / integrate with PatientRecord for the patient
            var summaryParts = new List<string>();
            if (plateletCount.HasValue) summaryParts.Add($"Platelets: {plateletCount.Value:N0}/mcL");
            if (wbcCount.HasValue) summaryParts.Add($"WBC: {wbcCount.Value:N0}/mcL");
            if (hematocrit.HasValue) summaryParts.Add($"Hematocrit: {hematocrit.Value}%");
            if (!string.IsNullOrEmpty(dengueNs1)) summaryParts.Add($"NS1: {dengueNs1}");
            if (!string.IsNullOrEmpty(dengueIgm)) summaryParts.Add($"IgM: {dengueIgm}");
            if (!string.IsNullOrEmpty(dengueIgg)) summaryParts.Add($"IgG: {dengueIgg}");

            var valuesSummary = summaryParts.Count > 0 ? string.Join(" | ", summaryParts) : "Laboratory report available.";
            var testTitle = booking.LabTest?.Name ?? "Laboratory Test Result";

            var patientRecord = new PatientRecord
            {
                PatientId = booking.PatientId,
                Title = $"{testTitle} Report",
                Description = $"{valuesSummary}{(string.IsNullOrWhiteSpace(testNotes) ? "" : " — " + testNotes)}",
                FileUrl = booking.ResultLink,
                RecordType = "LabResult",
                Date = DateTime.UtcNow
            };

            _context.PatientRecords.Add(patientRecord);
            _context.LabBookings.Update(booking);
            await _context.SaveChangesAsync();

            return booking;
        }

        public async Task VerifyBookingResultAsync(int bookingId, string verifiedBy)
        {
            var booking = await _context.LabBookings.FindAsync(bookingId);
            if (booking != null)
            {
                booking.IsVerified = true;
                booking.VerifiedBy = verifiedBy;
                booking.VerifiedAt = DateTime.UtcNow;
                booking.PatientNotified = true;
                booking.UpdatedAt = DateTime.UtcNow;

                _context.LabBookings.Update(booking);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<LabTest> SaveLabTestAsync(LabTest labTest)
        {
            if (labTest.Id == 0)
            {
                _context.LabTests.Add(labTest);
            }
            else
            {
                var existing = await _context.LabTests.FindAsync(labTest.Id);
                if (existing != null)
                {
                    existing.Name = labTest.Name;
                    existing.Description = labTest.Description;
                    existing.Cost = labTest.Cost;
                    existing.IsAvailable = labTest.IsAvailable;
                    _context.LabTests.Update(existing);
                }
            }
            await _context.SaveChangesAsync();
            return labTest;
        }

        public async Task ToggleTestAvailabilityAsync(int id)
        {
            var test = await _context.LabTests.FindAsync(id);
            if (test != null)
            {
                test.IsAvailable = !test.IsAvailable;
                _context.LabTests.Update(test);
                await _context.SaveChangesAsync();
            }
        }
    }
}
