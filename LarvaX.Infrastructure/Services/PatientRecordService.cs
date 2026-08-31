using System;
using System.Collections.Generic;
using LarvaX.Core.Entities;
using LarvaX.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using LarvaX.Application.Services;

namespace LarvaX.Infrastructure.Services
{
    public class PatientRecordService : IPatientRecordService
    {
        private readonly ApplicationDbContext _context;

        public PatientRecordService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<PatientRecord>> GetPatientRecordsAsync(string patientId)
        {
            return await _context.PatientRecords
                .Where(pr => pr.PatientId == patientId)
                .OrderByDescending(pr => pr.Date)
                .ToListAsync();
        }

        public async Task<PatientRecord?> GetPatientRecordByIdAsync(int id)
        {
            return await _context.PatientRecords.FindAsync(id);
        }

        public async Task<PatientRecord> AddPatientRecordAsync(string patientId, string title, string? description, string recordType, string? fileUrl = null)
        {
            var record = new PatientRecord
            {
                PatientId = patientId,
                Title = title,
                Description = description,
                RecordType = recordType,
                FileUrl = fileUrl,
                Date = DateTime.UtcNow
            };

            _context.PatientRecords.Add(record);
            await _context.SaveChangesAsync();

            return record;
        }

        public async Task DeletePatientRecordAsync(int id)
        {
            var record = await _context.PatientRecords.FindAsync(id);
            if (record != null)
            {
                _context.PatientRecords.Remove(record);
                await _context.SaveChangesAsync();
            }
        }
    }
}
