using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using LarvaX.Core.Entities;
using LarvaX.Infrastructure.Data;
using LarvaX.Infrastructure.Services;
using LarvaX.Web.Controllers;
using LarvaX.Web.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace LarvaX.Tests
{
    public class Stage2IntegrationTests
    {
        private ApplicationDbContext CreateInMemoryContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new ApplicationDbContext(options);
        }

        // IT-01: SMS Webhook Ingestion & TwiML Response Pipeline (Expected)
        [Fact]
        public async Task IT01_SmsWebhookController_Receive_ParsesReportCommandAndReturnsXml()
        {
            // Arrange
            var context = CreateInMemoryContext("IT01_SmsDb");
            var controller = new SmsWebhookController(context);

            // Act
            var result = await controller.Receive(From: "+8801711000000", Body: "REPORT Dhanmondi 32 standing water");

            // Assert
            var contentResult = Assert.IsType<ContentResult>(result);
            Assert.Equal("application/xml", contentResult.ContentType);
            Assert.Contains("Your dengue hazard report has been received", contentResult.Content);

            var savedCommand = await context.SmsCommands.FirstOrDefaultAsync(s => s.SenderNumber == "+8801711000000");
            Assert.NotNull(savedCommand);
            Assert.True(savedCommand.Processed);
            Assert.Contains("REPORT", savedCommand.CommandText);
        }

        // IT-02: SMS Webhook Rejection of Empty Payloads (Unexpected)
        [Fact]
        public async Task IT02_SmsWebhookController_Receive_RejectsEmptySenderOrBody()
        {
            // Arrange
            var context = CreateInMemoryContext("IT02_SmsDb");
            var controller = new SmsWebhookController(context);

            // Act
            var resultEmptyFrom = await controller.Receive(From: "", Body: "HELP");
            var resultEmptyBody = await controller.Receive(From: "+8801711000000", Body: "  ");

            // Assert
            var badRequest1 = Assert.IsType<BadRequestObjectResult>(resultEmptyFrom);
            var badRequest2 = Assert.IsType<BadRequestObjectResult>(resultEmptyBody);
            Assert.Equal("Missing sender number or message body.", badRequest1.Value);
            Assert.Equal("Missing sender number or message body.", badRequest2.Value);
        }

        // IT-03: SMS Webhook Malicious Script & SQL Injection Payload (Exceptional)
        [Fact]
        public async Task IT03_SmsWebhookController_Receive_HandlesInjectionPayloadSafely()
        {
            // Arrange
            var context = CreateInMemoryContext("IT03_SmsDb");
            var controller = new SmsWebhookController(context);
            var maliciousBody = "<script>alert('pwned')</script> '; DROP TABLE Reports; --";

            // Act
            var result = await controller.Receive(From: "+8801999999999", Body: maliciousBody);

            // Assert
            var contentResult = Assert.IsType<ContentResult>(result);
            Assert.Equal("application/xml", contentResult.ContentType);

            var savedCommand = await context.SmsCommands.FirstOrDefaultAsync(s => s.SenderNumber == "+8801999999999");
            Assert.NotNull(savedCommand);
            // Verify payload was stored safely without executing or corrupting the DB
            Assert.Equal(maliciousBody, savedCommand.CommandText);
        }

        // IT-04: Video Consultation SignalR Group Join Handshake (Expected)
        [Fact]
        public async Task IT04_VideoConsultHub_JoinRoom_AddsCallerToRoomGroup()
        {
            // Arrange
            var hub = new VideoConsultHub();
            var mockGroups = new Mock<IGroupManager>();
            var mockContext = new Mock<HubCallerContext>();

            mockContext.Setup(c => c.ConnectionId).Returns("conn-doctor-42");
            hub.Context = mockContext.Object;
            hub.Groups = mockGroups.Object;

            // Act
            await hub.JoinRoom("telemed-room-99");

            // Assert
            mockGroups.Verify(g => g.AddToGroupAsync("conn-doctor-42", "VideoRoom_telemed-room-99", default), Times.Once);
        }

        // IT-05: WebRTC SignalR Hub Authorization Inspection (Exceptional)
        [Fact]
        public void IT05_VideoConsultHub_AuthorizationPolicy_ExposesSecurityDefectWhenUnattributed()
        {
            // Arrange
            var hubType = typeof(VideoConsultHub);

            // Act
            var authorizeAttribute = hubType.GetCustomAttribute<AuthorizeAttribute>();

            // Assert / Defect Verification:
            // DEF-01 documents that VideoConsultHub is missing [Authorize].
            // This test verifies whether the hub is gated or open to unauthenticated peers.
            // If authorizeAttribute is null, it confirms the security observation in DEF-01.
            Assert.Null(authorizeAttribute);
        }

        // IT-06: Laboratory Test Booking DB Persistence & Foreign Keys (Expected)
        [Fact]
        public async Task IT06_LabController_Book_PersistsRecordAndLinksForeignKeys()
        {
            // Arrange
            var context = CreateInMemoryContext("IT06_LabDb");
            var labService = new LabService(context);
            var controller = new LabController(labService);

            // Seed a lab test and patient user
            var testItem = new LabTest
            {
                Name = "Dengue NS1 Rapid Antigen Test",
                Description = "Early diagnostic marker (Days 1-5)",
                Cost = 800m,
                IsAvailable = true
            };
            context.LabTests.Add(testItem);

            var patient = new ApplicationUser
            {
                Id = "patient-uuid-101",
                UserName = "patient101@test.com",
                Email = "patient101@test.com",
                ModePreference = "Citizen"
            };
            context.Users.Add(patient);
            await context.SaveChangesAsync();

            // Set up Controller HttpContext with patient identity
            var userPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "patient-uuid-101"),
                new Claim(ClaimTypes.Name, "patient101@test.com")
            }, "TestAuth"));

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = userPrincipal }
            };
            controller.TempData = new TempDataDictionary(controller.HttpContext, Mock.Of<ITempDataProvider>());

            var model = new LabBookingViewModel
            {
                LabTestId = testItem.Id,
                ScheduledAt = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-ddTHH:mm")
            };

            // Act
            var actionResult = await controller.Book(model);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(actionResult);
            Assert.Equal("Bookings", redirect.ActionName);

            var savedBooking = await context.LabBookings
                .Include(b => b.LabTest)
                .FirstOrDefaultAsync(b => b.PatientId == "patient-uuid-101");

            Assert.NotNull(savedBooking);
            Assert.Equal("Pending", savedBooking.Status);
            Assert.Equal(testItem.Id, savedBooking.LabTestId);
            Assert.NotNull(savedBooking.LabTest);
            Assert.Equal("Dengue NS1 Rapid Antigen Test", savedBooking.LabTest.Name);
        }

        // IT-07: Laboratory Booking State Lifecycle Transition Guard (Unexpected)
        [Fact]
        public async Task IT07_LabService_UpdateBookingStatus_DocumentsDirectStateMutationDefect()
        {
            // Arrange
            var context = CreateInMemoryContext("IT07_LabDb");
            var service = new LabService(context);

            var testItem = new LabTest { Name = "Dengue NS1 Rapid Antigen Test", Description = "Test", Cost = 800m, IsAvailable = true };
            context.LabTests.Add(testItem);

            var patient = new ApplicationUser { Id = "patient-1", UserName = "patient1@test.com", Email = "patient1@test.com" };
            context.Users.Add(patient);
            await context.SaveChangesAsync();

            var booking = await service.BookLabTestAsync("patient-1", testItem.Id, DateTime.UtcNow.AddDays(1));
            Assert.Equal("Pending", booking.Status);

            // Move to Completed
            await service.UpdateBookingStatusAsync(booking.Id, "Completed", "https://reports.larvax.gov.bd/res/101.pdf");
            var completedBooking = await service.GetLabBookingByIdAsync(booking.Id);
            Assert.NotNull(completedBooking);
            Assert.Equal("Completed", completedBooking.Status);

            // Act: Attempt illegal regression from Completed back to Pending (DEF-06)
            await service.UpdateBookingStatusAsync(booking.Id, "Pending");

            // Assert: Currently allows regression because lifecycle state machine is not yet enforced (DEF-06)
            var regressedBooking = await service.GetLabBookingByIdAsync(booking.Id);
            Assert.NotNull(regressedBooking);
            Assert.Equal("Pending", regressedBooking.Status);
        }

        // IT-08: SignalR Real-Time Outbreak Alert Zone Grouping (Expected)
        [Fact]
        public async Task IT08_AlertsHub_JoinAndLeaveZoneGroup_ManagesSignalRGroups()
        {
            // Arrange
            var hub = new AlertsHub();
            var mockGroups = new Mock<IGroupManager>();
            var mockContext = new Mock<HubCallerContext>();

            mockContext.Setup(c => c.ConnectionId).Returns("conn-citizen-77");
            hub.Context = mockContext.Object;
            hub.Groups = mockGroups.Object;

            // Act: Join Dhaka North zone
            await hub.JoinZoneGroup("DhakaNorth_Ward12");

            // Act: Leave zone
            await hub.LeaveZoneGroup("DhakaNorth_Ward12");

            // Assert
            mockGroups.Verify(g => g.AddToGroupAsync("conn-citizen-77", "Zone_DhakaNorth_Ward12", default), Times.Once);
            mockGroups.Verify(g => g.RemoveFromGroupAsync("conn-citizen-77", "Zone_DhakaNorth_Ward12", default), Times.Once);
        }
    }
}
