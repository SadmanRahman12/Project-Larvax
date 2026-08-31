using LarvaX.Application.Services;
using LarvaX.Core.Entities;
using Xunit;

namespace LarvaX.Tests
{
    public class RiskAssessmentServiceTests
    {
        private readonly IRiskAssessmentService _service;

        public RiskAssessmentServiceTests()
        {
            _service = new RiskAssessmentService();
        }

        [Fact]
        public void CalculateRiskLevel_WhenHighVerifiedCases_ReturnsHighRisk()
        {
            var level = _service.CalculateRiskLevel(verifiedCaseCount: 6, activeHazardCount: 10);
            Assert.Equal(RiskLevel.High, level);
        }

        [Fact]
        public void CalculateRiskLevel_WhenModerateCases_ReturnsMediumRisk()
        {
            var level = _service.CalculateRiskLevel(verifiedCaseCount: 2, activeHazardCount: 2);
            Assert.Equal(RiskLevel.Medium, level);
        }

        [Fact]
        public void CalculateRiskLevel_WhenFewCases_ReturnsLowRisk()
        {
            var level = _service.CalculateRiskLevel(verifiedCaseCount: 0, activeHazardCount: 1);
            Assert.Equal(RiskLevel.Low, level);
        }

        [Fact]
        public void DetermineDataSufficiency_WhenSampleCountBelowThreshold_ReturnsInsufficient()
        {
            var sufficiency = _service.DetermineDataSufficiency(5);
            Assert.Equal(DataSufficiency.Insufficient, sufficiency);
        }

        [Fact]
        public void DetermineDataSufficiency_WhenSampleCountAboveThreshold_ReturnsSufficient()
        {
            var sufficiency = _service.DetermineDataSufficiency(15);
            Assert.Equal(DataSufficiency.Sufficient, sufficiency);
        }

        [Fact]
        public void GeneratePublicAdvice_ReturnsBilingualGuidance()
        {
            var adviceEn = _service.GeneratePublicAdvice(RiskLevel.High, "en");
            var adviceBn = _service.GeneratePublicAdvice(RiskLevel.High, "bn");

            Assert.Contains("HIGH ALERT", adviceEn);
            Assert.Contains("সতর্কতা", adviceBn);
        }
    }
}
