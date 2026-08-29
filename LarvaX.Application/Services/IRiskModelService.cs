namespace LarvaX.Application.Services;

public interface IRiskModelService
{
    RiskAssessmentResult EvaluateRisk(int reportCount, double recentTrend, string regionName);
}

public sealed class RiskAssessmentResult
{
    public double Score { get; set; }
    public string RiskLabel { get; set; } = "Low";
    public string DataSufficiencyLabel { get; set; } = "Sufficient data";
    public string ConfidenceText { get; set; } = "Low confidence";
    public string Summary { get; set; } = string.Empty;
}
