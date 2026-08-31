using LarvaX.Core.Interfaces;
using LarvaX.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LarvaX.Infrastructure.Services
{
    public class AnalyticsService : IAnalyticsService
    {
        private readonly ApplicationDbContext _db;

        public AnalyticsService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task RecordFlowStepAsync(string flowName, string step, string? sessionId = null)
        {
            _db.FlowAnalytics.Add(new Core.Entities.FlowAnalytics
            {
                FlowName = flowName,
                Step = step,
                SessionId = sessionId,
                Timestamp = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }

        public async Task<Dictionary<string, int>> GetFlowStatsAsync(string flowName)
        {
            var stats = await _db.FlowAnalytics
                .Where(f => f.FlowName == flowName)
                .GroupBy(f => f.Step)
                .Select(g => new { Step = g.Key, Count = g.Count() })
                .ToListAsync();

            return stats.ToDictionary(s => s.Step, s => s.Count);
        }

        public async Task<double> GetAbandonmentRateAsync(string flowName)
        {
            var stats = await GetFlowStatsAsync(flowName);
            if (!stats.TryGetValue("Started", out var started) || started == 0) return 0;
            stats.TryGetValue("Completed", out var completed);
            return Math.Round((1.0 - (double)completed / started) * 100, 1);
        }
    }
}
