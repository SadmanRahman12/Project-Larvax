using LarvaX.Application.Models;
using LarvaX.Application.Services;
using Xunit;

namespace LarvaX.Tests
{
    public class SymptomCheckerServiceTests
    {
        private readonly ISymptomCheckerService _service;

        public SymptomCheckerServiceTests()
        {
            _service = new SymptomCheckerService();
        }

        [Fact]
        public void Assess_WhenBleedingIsPresent_ReturnsEmergencyRisk()
        {
            var input = new SymptomAssessmentInput
            {
                Fever = false,
                Bleeding = true
            };

            var result = _service.Assess(input);

            Assert.True(result.IsEmergency);
            Assert.Equal("Emergency", result.RiskLevel);
            Assert.Contains(result.WarningFlags, f => f.Contains("bleeding", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void Assess_WhenMultipleHighRiskSymptomsPresent_ReturnsHighOrEmergency()
        {
            var input = new SymptomAssessmentInput
            {
                Fever = true,
                SeverHeadache = true,
                RashBehindEyes = true
            };

            var result = _service.Assess(input);

            Assert.True(result.Score >= 50);
            Assert.Contains(result.RiskLevel, new[] { "High", "Emergency" });
            Assert.False(string.IsNullOrWhiteSpace(result.ConfidenceNote));
        }

        [Fact]
        public void Assess_WhenMildSymptoms_ReturnsMedium()
        {
            var input = new SymptomAssessmentInput
            {
                Fever = true,
                JointPain = false
            };

            var result = _service.Assess(input);

            Assert.Equal("Medium", result.RiskLevel);
            Assert.False(result.IsEmergency);
        }

        [Fact]
        public void Assess_WhenNoSymptoms_ReturnsLowRisk()
        {
            var input = new SymptomAssessmentInput();

            var result = _service.Assess(input);

            Assert.Equal("Low", result.RiskLevel);
            Assert.Equal(0, result.Score);
            Assert.False(result.IsEmergency);
        }
    }
}
