using LarvaX.Application.Services;
using LarvaX.Core.Entities;
using Xunit;

namespace LarvaX.Tests
{
    public class ReportServiceTests
    {
        private readonly IReportService _service;

        public ReportServiceTests()
        {
            _service = new ReportService();
        }

        [Fact]
        public void DetermineNextStatus_WhenVerifiedAndReceived_MovesToUnderReview()
        {
            var next = _service.DetermineNextStatus(ReportStatus.Received, ReportVerification.Verified);
            Assert.Equal(ReportStatus.UnderReview, next);
        }

        [Fact]
        public void DetermineNextStatus_WhenInvalid_MovesToResolved()
        {
            var next = _service.DetermineNextStatus(ReportStatus.Received, ReportVerification.Invalid);
            Assert.Equal(ReportStatus.Resolved, next);
        }

        [Fact]
        public void CanTransition_ValidatesAllowedLifecycle()
        {
            Assert.True(_service.CanTransition(ReportStatus.Received, ReportStatus.UnderReview));
            Assert.True(_service.CanTransition(ReportStatus.UnderReview, ReportStatus.Resolved));
            Assert.False(_service.CanTransition(ReportStatus.Resolved, ReportStatus.Received));
        }
    }
}
