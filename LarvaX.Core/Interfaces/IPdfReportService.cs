namespace LarvaX.Core.Interfaces
{
    public interface IPdfReportService
    {
        Task<byte[]> GenerateGovernmentReportAsync(
            DateTime startDate,
            DateTime endDate,
            string? division = null,
            string? district = null,
            string? riskLevel = null,
            string? caseStatus = null);
    }
}
