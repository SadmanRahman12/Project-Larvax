using LarvaX.Application.Services;
using Xunit;

namespace LarvaX.Tests
{
    public class DonorServiceTests
    {
        private readonly IDonorService _service;

        public DonorServiceTests()
        {
            _service = new DonorService();
        }

        [Fact]
        public void GetFreshnessLabel_WhenJustNow_ReturnsJustNowLabel()
        {
            var now = DateTime.UtcNow;
            var labelEn = _service.GetFreshnessLabel(now.AddSeconds(-15), now, "en");
            var labelBn = _service.GetFreshnessLabel(now.AddSeconds(-15), now, "bn");

            Assert.Equal("Just now", labelEn);
            Assert.Equal("এইমাত্র সক্রিয়", labelBn);
        }

        [Fact]
        public void GetFreshnessLabel_WhenHoursAgo_ReturnsHoursLabel()
        {
            var now = DateTime.UtcNow;
            var labelEn = _service.GetFreshnessLabel(now.AddHours(-3), now, "en");
            var labelBn = _service.GetFreshnessLabel(now.AddHours(-3), now, "bn");

            Assert.Equal("3h ago", labelEn);
            Assert.Equal("3 ঘণ্টা আগে নিশ্চিত", labelBn);
        }

        [Fact]
        public void IsConsideredFresh_WhenConfirmedRecently_ReturnsTrue()
        {
            var isFresh = _service.IsConsideredFresh(DateTime.UtcNow.AddDays(-10), maxDays: 30);
            Assert.True(isFresh);
        }

        [Fact]
        public void IsConsideredFresh_WhenOld_ReturnsFalse()
        {
            var isFresh = _service.IsConsideredFresh(DateTime.UtcNow.AddDays(-60), maxDays: 30);
            Assert.False(isFresh);
        }
    }
}
