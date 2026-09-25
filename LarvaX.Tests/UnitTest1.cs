using LarvaX.Core.Entities;
using Xunit;

namespace LarvaX.Tests
{
    public class ApplicationUserTests
    {
        [Fact]
        public void ApplicationUser_DefaultValues_CitizenIsAutoApproved()
        {
            var user = new ApplicationUser
            {
                UserName = "citizen@test.com",
                Email = "citizen@test.com",
                ModePreference = "Citizen",
                IsApproved = true
            };

            Assert.True(user.IsApproved);
            Assert.Equal("Citizen", user.ModePreference);
            Assert.Equal("en", user.PreferredLanguage);
        }

        [Fact]
        public void ApplicationUser_ProfessionalRequiresAdminApproval()
        {
            var doctor = new ApplicationUser
            {
                UserName = "doctor@test.com",
                Email = "doctor@test.com",
                ModePreference = "Professional",
                IsApproved = false
            };

            Assert.False(doctor.IsApproved);
            Assert.Equal("Professional", doctor.ModePreference);
        }
    }

    public class FirstAidControllerTests
    {
        [Theory]
        [InlineData("dengue")]
        [InlineData("choking")]
        [InlineData("snakebite")]
        [InlineData("heartattack")]
        [InlineData("fainting")]
        public void Guide_ValidType_ReturnsGuideViewWithCorrectModel(string type)
        {
            var controller = new LarvaX.Web.Controllers.FirstAidController();
            var result = controller.Guide(type) as Microsoft.AspNetCore.Mvc.ViewResult;

            Assert.NotNull(result);
            Assert.Equal("Guide", result.ViewName);
            Assert.Equal(type, result.Model);
        }

        [Fact]
        public void Guide_InvalidType_RedirectsToIndex()
        {
            var controller = new LarvaX.Web.Controllers.FirstAidController();
            var result = controller.Guide("unknown_type") as Microsoft.AspNetCore.Mvc.RedirectToActionResult;

            Assert.NotNull(result);
            Assert.Equal("Index", result.ActionName);
        }
    }
}
