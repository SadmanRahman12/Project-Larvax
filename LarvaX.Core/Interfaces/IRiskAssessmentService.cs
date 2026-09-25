using LarvaX.Core.Entities;

namespace LarvaX.Core.Interfaces
{
    /// <summary>
    /// Risk assessment and bilingual alert generation for risk zones.
    /// Interface lives in Core so Hangfire jobs in Web and services in Application share one contract.
    /// </summary>
    public interface IRiskAssessmentService
    {
        RiskLevel CalculateRiskLevel(int verifiedCaseCount, int activeHazardCount);
        double CalculateConfidenceScore(int sampleCount);
        DataSufficiency DetermineDataSufficiency(int sampleCount);
        string GeneratePublicAdvice(RiskLevel level, string language = "en");
        string GenerateAlertMessage(string region, RiskLevel level, string disease, string language = "en");
    }
}
