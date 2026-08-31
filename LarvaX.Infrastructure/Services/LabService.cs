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
    }
}
