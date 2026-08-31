using LarvaX.Core.Entities;

namespace LarvaX.Core.Interfaces
{
    public interface IAnalyticsService
    {
        Task RecordFlowStepAsync(string flowName, string step, string? sessionId = null);
        Task<Dictionary<string, int>> GetFlowStatsAsync(string flowName);
        Task<double> GetAbandonmentRateAsync(string flowName);
    }
}
