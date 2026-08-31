using LarvaX.Application.Services;
using Xunit;

namespace LarvaX.Tests
{
    public class ChatbotServiceTests
    {
        private readonly IChatbotService _service;

        public ChatbotServiceTests()
        {
            _service = new ChatbotService();
        }

        [Theory]
        [InlineData("I have bleeding and rash", "en")]
        [InlineData("patient is unconscious and in shock", "en")]
        [InlineData("রোগীর রক্ত পড়ছে এবং অজ্ঞান", "bn")]
        [InlineData("শ্বাস নিতে পারছি না", "bn")]
        public void GetResponse_WhenEmergencyKeywordsPresent_SetsEmergencyFlag(string message, string lang)
        {
            var response = _service.GetResponse(message, lang);

            Assert.True(response.IsEmergency);
            Assert.NotNull(response.EmergencyMessage);
            if (lang == "bn")
            {
                Assert.Contains("জরুরি", response.EmergencyMessage);
            }
            else
            {
                Assert.Contains("emergency", response.EmergencyMessage, StringComparison.OrdinalIgnoreCase);
            }
        }

        [Fact]
        public void GetResponse_WhenEnglishFeverQuery_ReturnsEnglishAdvice()
        {
            var response = _service.GetResponse("I have a high fever and headache", "en");

            Assert.False(response.IsEmergency);
            Assert.Contains("Paracetamol", response.Reply);
        }

        [Fact]
        public void GetResponse_WhenBanglaFeverQuery_ReturnsBanglaAdvice()
        {
            var response = _service.GetResponse("আমার খুব জ্বর এবং গা ব্যথা", "bn");

            Assert.False(response.IsEmergency);
            Assert.Contains("প্যারাসিটামল", response.Reply);
        }

        [Fact]
        public void GetResponse_WhenEmptyQuery_ReturnsPrompt()
        {
            var responseEn = _service.GetResponse("", "en");
            var responseBn = _service.GetResponse("   ", "bn");

            Assert.Contains("Please enter", responseEn.Reply);
            Assert.Contains("প্রশ্ন লিখুন", responseBn.Reply);
        }
    }
}
