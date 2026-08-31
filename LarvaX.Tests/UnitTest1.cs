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
}
