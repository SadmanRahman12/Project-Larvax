namespace LarvaX.Application.Services;

public class RiskModelService : IRiskModelService
{
    // Assumption: a simple heuristic model is enough for this project prototype;
    // production replacement can use a trained epidemiology model later.
    public RiskAssessmentResult EvaluateRisk(int reportCount, double recentTrend, string regionName)
    {
        var normalizedReportCount = Math.Max(0, reportCount);
        var riskScore = Math.Clamp((normalizedReportCount * 0.12) + Math.Max(0, recentTrend) * 0.4, 0, 1);

        var result = new RiskAssessmentResult
        {
            Score = Math.Round(riskScore, 3),
            RiskLabel = riskScore switch
            {
                >= 0.7 => "High",
                >= 0.4 => "Medium",
                _ => "Low"
            },
            ConfidenceText = normalizedReportCount >= 15 ? "High confidence" : normalizedReportCount >= 8 ? "Medium confidence" : "Low confidence",
            Summary = $"{regionName} currently shows a {GetRiskLabel(riskScore)} risk profile based on the latest surveillance signal."
        };

        if (normalizedReportCount < 5)
        {
            result.DataSufficiencyLabel = "Insufficient data";
            result.Score = Math.Min(result.Score, 0.4);
            result.RiskLabel = "Low";
            result.Summary = $"{regionName} has insufficient data to calculate a stable risk estimate; the model is intentionally conservative.";
        }
        else
        {
            result.DataSufficiencyLabel = "Sufficient data";
        }

        return result;
    }

    private static string GetRiskLabel(double score)
    {
        return score switch
        {
            >= 0.7 => "high",
            >= 0.4 => "moderate",
            _ => "low"
        };
    }
}
