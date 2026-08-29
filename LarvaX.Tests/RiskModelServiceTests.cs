using LarvaX.Application.Services;

namespace LarvaX.Tests;

public class RiskModelServiceTests
{
    [Fact]
    public void EvaluateRisk_ShouldMarkInsufficientData_WhenReportCountIsLow()
    {
        var service = new RiskModelService();

        var result = service.EvaluateRisk(2, 0.22, "Dhaka");

        Assert.Equal("Insufficient data", result.DataSufficiencyLabel);
        Assert.True(result.Score <= 0.4);
    }
}
