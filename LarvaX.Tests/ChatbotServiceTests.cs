using System;
using System.Linq;
using LarvaX.Application.ML;
using LarvaX.Application.Services;
using LarvaX.Core.Interfaces;
using LarvaX.Core.ML;
using Xunit;

namespace LarvaX.Tests
{
    /// <summary>
    /// Unit tests for the ML.NET-powered DenAI ChatbotService.
    /// The DenAIIntentClassifier is instantiated once per test class (expensive: trains the model).
    /// </summary>
    public class ChatbotServiceTests : IDisposable
    {
        private readonly DenAIIntentClassifier _classifier;
        private readonly IChatbotService _service;

        public ChatbotServiceTests()
        {
            // Trains the ML.NET model from the embedded corpus — runs once per test class instance.
            _classifier = new DenAIIntentClassifier();
            _service = new ChatbotService(_classifier);
        }

        public void Dispose() { }

        // ─── Emergency Safety Override ───────────────────────────────────────────────

        [Theory]
        [InlineData("I have severe bleeding and rash", "en")]
        [InlineData("patient is unconscious and in shock", "en")]
        [InlineData("শ্বাস নিতে পারছি না গুরুতর অবস্থা", "bn")]
        [InlineData("রক্ত বমি হচ্ছে এবং অজ্ঞান", "bn")]
        public void GetResponse_WhenEmergencyKeywordsPresent_SetsEmergencyFlag(string message, string lang)
        {
            var response = _service.GetResponse(message, lang);

            Assert.True(response.IsEmergency, $"Expected IsEmergency=true for: '{message}'");
            Assert.NotNull(response.EmergencyMessage);
            Assert.Equal(DenAIIntents.Emergency, response.DetectedIntent);
            Assert.Equal("Safety", response.ModelSource);
            Assert.Contains(response.ActionButtons, b => b.Url == "tel:999");

            if (lang == "bn")
                Assert.Contains("জরুরি", response.EmergencyMessage);
            else
                Assert.Contains("emergency", response.EmergencyMessage, StringComparison.OrdinalIgnoreCase);
        }

        // ─── ML Intent: Fever Management ─────────────────────────────────────────────

        [Fact]
        public void GetResponse_FeverQuery_English_ReturnsFeverIntent()
        {
            var response = _service.GetResponse("I have high fever for three days", "en");

            Assert.False(response.IsEmergency);
            Assert.Equal("ML", response.ModelSource);
            // Should either be Fever or trigger the multi-turn context
            Assert.True(
                response.DetectedIntent == DenAIIntents.FeverManagement ||
                response.ContextState == "awaiting_fever_days",
                $"Expected FeverManagement intent or fever context, got: {response.DetectedIntent}");
            // Safety rule verified: must not recommend Aspirin
            Assert.DoesNotContain("Aspirin and", response.Reply, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void GetResponse_FeverQuery_Bangla_ReturnsBanglaContent()
        {
            var response = _service.GetResponse("আমার জ্বর আছে", "bn");

            Assert.False(response.IsEmergency);
            Assert.Equal("ML", response.ModelSource);
            // Response should be in Bangla
            Assert.True(
                response.Reply.Contains("জ্বর") ||
                response.Reply.Contains("প্যারাসিটামল") ||
                response.Reply.Contains("তাপমাত্রা") ||
                response.ContextState == "awaiting_fever_days",
                "Expected Bangla fever guidance");
        }

        [Fact]
        public void GetResponse_FeverWithoutDays_TriggersMultiTurnFlow()
        {
            var response = _service.GetResponse("I have fever", "en");

            // Short fever statement should trigger the fever duration follow-up
            Assert.Equal("awaiting_fever_days", response.ContextState);
            Assert.Contains("days", response.Reply, StringComparison.OrdinalIgnoreCase);
        }

        // ─── ML Intent: Symptoms ──────────────────────────────────────────────────────

        [Fact]
        public void GetResponse_SymptomsQuery_ReturnsSymptomCheckerAction()
        {
            var response = _service.GetResponse("What are the symptoms of dengue fever?", "en");

            Assert.Equal(DenAIIntents.Symptoms, response.DetectedIntent);
            Assert.Contains(response.ActionButtons, b => b.Url == "/SymptomChecker");
        }

        [Fact]
        public void GetResponse_BanglaSymptomsQuery_ReturnsBanglaSymptoms()
        {
            var response = _service.GetResponse("ডেঙ্গুর উপসর্গ কী কী", "bn");

            Assert.Equal(DenAIIntents.Symptoms, response.DetectedIntent);
            Assert.Contains("মাথাব্যথা", response.Reply);
            Assert.Contains(response.ActionButtons, b => b.Url == "/SymptomChecker");
        }

        // ─── ML Intent: Warning Signs ─────────────────────────────────────────────────

        [Fact]
        public void GetResponse_WarningSignsQuery_ReturnsWarningSignsIntent()
        {
            var response = _service.GetResponse("What are the dengue warning signs I should watch for?", "en");

            Assert.Equal(DenAIIntents.WarningSigns, response.DetectedIntent);
            Assert.Contains(response.ActionButtons, b => b.Url.Contains("/FirstAid"));
        }

        // ─── ML Intent: Doctor Routing ────────────────────────────────────────────────

        [Fact]
        public void GetResponse_DoctorRouting_ReturnsTelemedicineAction()
        {
            var response = _service.GetResponse("I want to consult a doctor for dengue", "en");

            Assert.Equal(DenAIIntents.DoctorRouting, response.DetectedIntent);
            Assert.Contains(response.ActionButtons, b => b.Url.Contains("/Telemedicine"));
        }

        // ─── ML Intent: Lab Test ─────────────────────────────────────────────────────

        [Fact]
        public void GetResponse_LabTestQuery_ReturnsLabIntent()
        {
            var response = _service.GetResponse("Where can I get a dengue NS1 blood test?", "en");

            Assert.Equal(DenAIIntents.LabTest, response.DetectedIntent);
            Assert.Contains(response.ActionButtons, b => b.Url.Contains("/Lab"));
            Assert.Contains("NS1", response.Reply);
        }

        // ─── ML Intent: Blood Donor ───────────────────────────────────────────────────

        [Fact]
        public void GetResponse_BloodDonorQuery_ReturnsDonorsAction()
        {
            var response = _service.GetResponse("I need an urgent blood donor for dengue patient", "en");

            Assert.Equal(DenAIIntents.BloodDonor, response.DetectedIntent);
            Assert.Contains(response.ActionButtons, b => b.Url.Contains("/Donors"));
        }

        // ─── ML Intent: ICU Bed ───────────────────────────────────────────────────────

        [Fact]
        public void GetResponse_IcuQuery_ReturnsIcuAction()
        {
            var response = _service.GetResponse("I need to find an ICU bed for a critical dengue patient", "en");

            Assert.Equal(DenAIIntents.IcuBed, response.DetectedIntent);
            Assert.Contains(response.ActionButtons, b => b.Url.Contains("/IcuBeds"));
        }

        // ─── ML Intent: Prevention ───────────────────────────────────────────────────

        [Fact]
        public void GetResponse_PreventionQuery_ReturnsPreventionIntent()
        {
            var response = _service.GetResponse("How can I prevent dengue mosquito bites at home?", "en");

            Assert.Equal(DenAIIntents.Prevention, response.DetectedIntent);
            Assert.Contains(response.ActionButtons, b => b.Url.Contains("/Reports/CitizenReport"));
        }

        // ─── ML Intent: Nutrition / Diet ─────────────────────────────────────────────

        [Fact]
        public void GetResponse_NutritionQuery_ReturnsHydrationGuidance()
        {
            var response = _service.GetResponse("What food and diet should I take during dengue recovery?", "en");

            Assert.Equal(DenAIIntents.NutritionDiet, response.DetectedIntent);
            Assert.Contains("ORS", response.Reply);
            Assert.Contains(response.ActionButtons, b => b.Url.Contains("/FluidManagement"));
        }

        // ─── ML Intent: Risk Area ─────────────────────────────────────────────────────

        [Fact]
        public void GetResponse_RiskAreaQuery_ReturnsDashboardAction()
        {
            var response = _service.GetResponse("Is my area in Dhaka a high dengue risk zone?", "en");

            Assert.Equal(DenAIIntents.RiskArea, response.DetectedIntent);
            Assert.Contains(response.ActionButtons, b => b.Url.Contains("/Dashboard"));
        }

        // ─── ML Intent: First Aid ─────────────────────────────────────────────────────

        [Fact]
        public void GetResponse_FirstAidQuery_ReturnsFirstAidGuideAction()
        {
            var response = _service.GetResponse("What is the first aid procedure for a dengue patient?", "en");

            Assert.Equal(DenAIIntents.FirstAid, response.DetectedIntent);
            Assert.Contains(response.ActionButtons, b => b.Url.Contains("/FirstAid"));
        }

        // ─── ML Intent: Education ─────────────────────────────────────────────────────

        [Fact]
        public void GetResponse_EducationQuery_ReturnsEducationIntent()
        {
            var response = _service.GetResponse("I want to learn about dengue myths and take a quiz", "en");

            Assert.Equal(DenAIIntents.Education, response.DetectedIntent);
            Assert.Contains(response.ActionButtons, b => b.Url.Contains("/Education"));
        }

        // ─── Empty Message ────────────────────────────────────────────────────────────

        [Fact]
        public void GetResponse_EmptyMessage_ReturnsPrompt()
        {
            var responseEn = _service.GetResponse("", "en");
            var responseBn = _service.GetResponse("   ", "bn");

            Assert.Contains("Please enter", responseEn.Reply);
            Assert.Contains("প্রশ্ন লিখুন", responseBn.Reply);
            Assert.Equal("Safety", responseEn.ModelSource);
        }

        // ─── Multi-turn: Fever Flow ───────────────────────────────────────────────────

        [Fact]
        public void GetResponse_MultiTurnFeverFlow_ProgressesCorrectly()
        {
            // Turn 1: short fever statement → multi-turn context
            var turn1 = _service.GetResponse("I have fever", "en");
            Assert.Equal("awaiting_fever_days", turn1.ContextState);

            // Turn 2: days provided → advances to warning signs
            var turn2 = _service.GetResponse("3 days", "en", turn1.ContextState);
            Assert.Equal("awaiting_warning_signs", turn2.ContextState);
            Assert.Contains("warning signs", turn2.Reply, StringComparison.OrdinalIgnoreCase);

            // Turn 3: bleeding/vomiting → emergency escalation
            var turn3 = _service.GetResponse("Yes I have severe vomiting and bleeding", "en", turn2.ContextState);
            Assert.True(turn3.IsEmergency, "Reporting bleeding/vomiting in context should trigger emergency");
        }

        [Fact]
        public void GetResponse_MultiTurnFeverFlow_Bangla_ProgressesCorrectly()
        {
            var turn1 = _service.GetResponse("আমার জ্বর আছে", "bn");
            Assert.Equal("awaiting_fever_days", turn1.ContextState);

            var turn2 = _service.GetResponse("৩ দিন", "bn", turn1.ContextState);
            Assert.Equal("awaiting_warning_signs", turn2.ContextState);

            var turn3 = _service.GetResponse("হ্যাঁ রক্তপাত হচ্ছে ও বমি হচ্ছে", "bn", turn2.ContextState);
            Assert.True(turn3.IsEmergency);
        }

        [Fact]
        public void GetResponse_MultiTurnFeverFlow_NoWarnings_ReturnsHomeCarePlan()
        {
            var turn1 = _service.GetResponse("I have fever", "en");
            var turn2 = _service.GetResponse("2 days", "en", turn1.ContextState);
            var turn3 = _service.GetResponse("No warning signs at all", "en", turn2.ContextState);

            Assert.False(turn3.IsEmergency);
            Assert.Contains(turn3.ActionButtons, b => b.Url == "/SymptomChecker");
        }

        // ─── Model Source Verification ────────────────────────────────────────────────

        [Theory]
        [InlineData("What are dengue symptoms?", "en", "ML")]
        [InlineData("I need blood donors", "en", "ML")]
        [InlineData("How to prevent dengue?", "en", "ML")]
        [InlineData("bleeding gums and shock", "en", "Safety")]   // safety override
        public void GetResponse_ModelSource_IsCorrectlyTagged(string message, string lang, string expectedSource)
        {
            var response = _service.GetResponse(message, lang);
            Assert.Equal(expectedSource, response.ModelSource);
        }

        // ─── ML Response Quality ──────────────────────────────────────────────────────

        [Fact]
        public void GetResponse_AllIntents_ReturnNonEmptyReply()
        {
            var queries = new[]
            {
                ("What are dengue symptoms?", "en"),
                ("I have fever for 5 days", "en"),
                ("What are the warning signs?", "en"),
                ("I want to see a doctor", "en"),
                ("Where to get dengue test?", "en"),
                ("I need blood donor", "en"),
                ("Find ICU bed", "en"),
                ("How to prevent dengue?", "en"),
                ("What food during dengue?", "en"),
                ("Is my area dengue risk zone?", "en"),
                ("First aid for dengue patient", "en"),
                ("Dengue quiz and education", "en"),
            };

            foreach (var (msg, lang) in queries)
            {
                var response = _service.GetResponse(msg, lang);
                Assert.False(string.IsNullOrWhiteSpace(response.Reply), $"Empty reply for: '{msg}'");
                Assert.False(string.IsNullOrWhiteSpace(response.DetectedIntent), $"No intent for: '{msg}'");
            }
        }
    }
}
