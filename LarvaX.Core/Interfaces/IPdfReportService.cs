namespace LarvaX.Core.Interfaces
{
    public interface IPdfReportService
    {
        Task<byte[]> GenerateGovernmentReportAsync(DateTime startDate, DateTime endDate);
    }
}
