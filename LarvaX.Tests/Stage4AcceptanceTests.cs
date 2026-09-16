using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using LarvaX.Application.Models;
using LarvaX.Application.Services;
using LarvaX.Core.Entities;
using LarvaX.Infrastructure.Data;
using LarvaX.Infrastructure.Services;
using LarvaX.Web.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace LarvaX.Tests
{
    /// <summary>
    /// Stage 4 — Acceptance Testing (UAT)
    /// Validates end-to-end user journeys for each of the six LarvaX roles:
    /// Citizen, Doctor, HealthWorker, LabStaff, GovernmentAuthority, Administrator.
    /// </summary>
    public class Stage4AcceptanceTests
    {
        private ApplicationDbContext CreateInMemoryContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new ApplicationDbContext(options);
        }

        // AT-01: Citizen — Emergency Triage & Telemedicine Escalation (Expected)
        [Fact]
        public async Task AT01_Citizen_EmergencyTriageToTelemedicineBooking_CompletesJourney()
        {
            // Arrange
            var context = CreateInMemoryContext("AT01_CitizenDb");
            var symptomService = new SymptomCheckerService();
            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
            var userManagerMock = new Mock<UserManager<ApplicationUser>>(
                userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
            var telemedService = new TelemedicineService(context, userManagerMock.Object);

            var patient = new ApplicationUser
            {
                Id = "citizen-at01",
                UserName = "citizen01@test.com",
                Email = "citizen01@test.com",
                IsApproved = true,
                ModePreference = "Citizen"
            };
            context.Users.Add(patient);
            await context.SaveChangesAsync();

            // Step 1: Citizen submits symptoms — bleeding present triggers Emergency
            var input = new SymptomAssessmentInput
            {
                Fever = true,
                Bleeding = true,
                SeverHeadache = true
            };
            var triage = symptomService.Assess(input);

            // Assert triage
            Assert.True(triage.IsEmergency);
            Assert.Equal("Emergency", triage.RiskLevel);
            Assert.True(triage.Score > 0);

            // Step 2: Citizen proceeds to book telemedicine consultation
            var appointment = await telemedService.BookAppointmentAsync(
                "citizen-at01", "doctor-uuid-01",
                DateTime.UtcNow.AddHours(2),
                $"Emergency dengue triage: {triage.RiskLevel} risk, Score={triage.Score}");

            // Assert booking
            Assert.NotNull(appointment);
            Assert.Equal(AppointmentStatus.Booked, appointment.Status);
            Assert.Equal("citizen-at01", appointment.PatientId);
            Assert.Contains("Emergency", appointment.Notes);

            // Step 3: Ensure video room is provisioned for the session
            await telemedService.EnsureVideoRoomExistsAsync(appointment);
            Assert.False(string.IsNullOrWhiteSpace(appointment.VideoRoomId));
        }

        // AT-02: Doctor — Patient Record Review & Fluid Management Prescription (Expected)
        [Fact]
        public async Task AT02_Doctor_ReviewsPatientRecordAndPrescribesFluidPlan()
        {
            // Arrange
            var context = CreateInMemoryContext("AT02_DoctorDb");
            var patientRecordService = new PatientRecordService(context);
            var fluidService = new FluidManagementService();

            var patient = new ApplicationUser { Id = "patient-at02", UserName = "p02@test.com", Email = "p02@test.com" };
            context.Users.Add(patient);
            await context.SaveChangesAsync();

            // Step 1: Doctor reviews patient's CBC lab result
            var cbcRecord = await patientRecordService.AddPatientRecordAsync(
                patientId: "patient-at02",
                title: "CBC Result Day 4",
                description: "Platelet count: 65,000/uL (dropping). Haematocrit: 48%. NS1 Positive.",
                recordType: "LabResult",
                fileUrl: "https://lab.larvax.gov.bd/results/cbc-day4.pdf");

            Assert.NotNull(cbcRecord);
            Assert.Equal("CBC Result Day 4", cbcRecord.Title);
            Assert.Equal("patient-at02", cbcRecord.PatientId);

            // Step 2: Doctor retrieves full record history
            var records = await patientRecordService.GetPatientRecordsAsync("patient-at02");
            Assert.Single(records);
            Assert.Contains("65,000", records.First().Description);

            // Step 3: Doctor calculates fluid resuscitation plan (60kg, Dengue Warning Signs)
            var fluidParams = new FluidManagementParameters
            {
                Weight = 60m,
                DehydrationPercent = 5m,
                OngoingLosses = 0m,
                ClinicalMode = "DengueWarning"
            };
            var plan = fluidService.CalculateFluidPlan(fluidParams);

            // Assert fluid plan is clinically sensible
            Assert.True(plan.Maintenance24h > 0);
            Assert.True(plan.RecommendedRateHour > 0);
            Assert.Equal("DengueWarning", fluidParams.ClinicalMode);

            // Step 4: Doctor saves the prescription as a patient record
            var prescription = await patientRecordService.AddPatientRecordAsync(
                patientId: "patient-at02",
                title: "Fluid Management Order — Day 4",
                description: $"Maintenance 24h: {plan.Maintenance24h}ml | Rate: {plan.RecommendedRateHour}ml/hr | Mode: {plan.ModeLabel}",
                recordType: "Prescription");

            Assert.NotNull(prescription);
            Assert.Equal("Prescription", prescription.RecordType);
            Assert.Contains("ml/hr", prescription.Description);

            // Total records should now be 2
            var allRecords = await patientRecordService.GetPatientRecordsAsync("patient-at02");
            Assert.Equal(2, allRecords.Count());
        }

        // AT-03: Doctor — Double-Booking at Same Timeslot Succeeds Without Guard (Unexpected / DEF-03)
        [Fact]
        public async Task AT03_Doctor_SimultaneousBookingSameTimeslot_BothSucceedDocumentingDEF03()
        {
            // Arrange
            var context = CreateInMemoryContext("AT03_DoubleBookDb");
            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
            var userManagerMock = new Mock<UserManager<ApplicationUser>>(
                userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
            var telemedService = new TelemedicineService(context, userManagerMock.Object);

            var conflictTime = DateTime.UtcNow.AddDays(1).Date.AddHours(10); // 10:00 AM tomorrow

            // Act: Citizen A and Citizen B both book the same doctor at the same time
            var bookingA = await telemedService.BookAppointmentAsync("citizen-A", "doctor-01", conflictTime, "First visit");
            var bookingB = await telemedService.BookAppointmentAsync("citizen-B", "doctor-01", conflictTime, "Second visit");

            // Assert: DEF-03 — both bookings succeed (no overlap guard exists yet)
            Assert.Equal(AppointmentStatus.Booked, bookingA.Status);
            Assert.Equal(AppointmentStatus.Booked, bookingB.Status);
            Assert.Equal(bookingA.ScheduledAt, bookingB.ScheduledAt);
            Assert.Equal("doctor-01", bookingA.DoctorId);
            Assert.Equal("doctor-01", bookingB.DoctorId);

            var docAppointments = await context.Appointments
                .Where(a => a.DoctorId == "doctor-01" && a.ScheduledAt == conflictTime.ToUniversalTime())
                .ToListAsync();
            Assert.Equal(2, docAppointments.Count);
        }

        // AT-04: HealthWorker — Field Hazard Inspection & Map Pin Verification (Expected)
        [Fact]
        public async Task AT04_HealthWorker_InspectsAndVerifiesHazardReport_UpdatesStatusAndVerification()
        {
            // Arrange
            var context = CreateInMemoryContext("AT04_HealthWorkerDb");
            var reportService = new ReportService();

            var citizen = new ApplicationUser { Id = "citizen-at04", UserName = "c04@test.com", Email = "c04@test.com" };
            context.Users.Add(citizen);

            // Step 1: Citizen submits hazard report (standing water in drain)
            var report = new Report
            {
                UserId = "citizen-at04",
                Latitude = 23.7946,
                Longitude = 90.4057,
                Description = "Blocked drain with stagnant water near Dhanmondi Lake, Ward 4",
                DiseaseType = DiseaseType.Dengue,
                Status = ReportStatus.Received,
                Verification = ReportVerification.Pending,
                CreatedAt = DateTime.UtcNow
            };
            context.Reports.Add(report);
            await context.SaveChangesAsync();

            // Step 2: Health worker views report and confirms field inspection
            var savedReport = await context.Reports.FindAsync(report.Id);
            Assert.NotNull(savedReport);
            Assert.Equal(ReportStatus.Received, savedReport.Status);
            Assert.Equal(ReportVerification.Pending, savedReport.Verification);

            // Step 3: Health worker verifies — transitions state via domain service
            Assert.True(reportService.CanTransition(savedReport.Status, ReportStatus.UnderReview));
            var nextStatus = reportService.DetermineNextStatus(savedReport.Status, ReportVerification.Verified);
            Assert.Equal(ReportStatus.UnderReview, nextStatus);

            savedReport.Status = nextStatus;
            savedReport.Verification = ReportVerification.Verified;
            savedReport.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();

            // Step 4: Verify final state reflects field confirmation
            var verifiedReport = await context.Reports.FindAsync(report.Id);
            Assert.NotNull(verifiedReport);
            Assert.Equal(ReportStatus.UnderReview, verifiedReport.Status);
            Assert.Equal(ReportVerification.Verified, verifiedReport.Verification);
            Assert.True(verifiedReport.UpdatedAt >= verifiedReport.CreatedAt);
        }

        // AT-05: LabStaff — NS1 Antigen Test Result Entry & Booking Completion (Expected)
        [Fact]
        public async Task AT05_LabStaff_EntersTestResultAndCompletesBooking()
        {
            // Arrange
            var context = CreateInMemoryContext("AT05_LabStaffDb");
            var labService = new LabService(context);

            var test = new LabTest
            {
                Name = "Dengue NS1 Antigen Test (ELISA)",
                Description = "Detects NS1 protein, Days 1-9",
                Cost = 1200m,
                IsAvailable = true
            };
            context.LabTests.Add(test);

            var patient = new ApplicationUser { Id = "patient-at05", UserName = "p05@test.com", Email = "p05@test.com" };
            context.Users.Add(patient);
            await context.SaveChangesAsync();

            // Step 1: Patient books the NS1 test
            var booking = await labService.BookLabTestAsync("patient-at05", test.Id, DateTime.UtcNow.AddDays(1));
            Assert.Equal("Pending", booking.Status);
            Assert.Null(booking.ResultLink);

            // Step 2: Lab staff receives the blood sample — confirm booking
            await labService.UpdateBookingStatusAsync(booking.Id, "SampleCollected");
            var collected = await labService.GetLabBookingByIdAsync(booking.Id);
            Assert.NotNull(collected);
            Assert.Equal("SampleCollected", collected.Status);

            // Step 3: Lab completes analysis and uploads result
            const string resultUrl = "https://lab.larvax.gov.bd/results/ns1-positive-88.pdf";
            await labService.UpdateBookingStatusAsync(booking.Id, "Completed", resultUrl);

            var completed = await labService.GetLabBookingByIdAsync(booking.Id);
            Assert.NotNull(completed);
            Assert.Equal("Completed", completed.Status);
            Assert.Equal(resultUrl, completed.ResultLink);

            // Step 4: Verify patient can retrieve their bookings
            var patientBookings = await labService.GetUserLabBookingsAsync("patient-at05");
            Assert.Single(patientBookings);
            Assert.Equal("Completed", patientBookings.First().Status);
        }

        // AT-06: GovernmentAuthority — PDF Report Export Over Date Range (Expected)
        [Fact]
        public async Task AT06_GovernmentAuthority_DownloadsPdfReportForDateRange()
        {
            // Arrange
            var context = CreateInMemoryContext("AT06_GovDb");

            var reporter = new ApplicationUser { Id = "gov-reporter-at06", UserName = "gov06@test.com", Email = "gov06@test.com" };
            context.Users.Add(reporter);

            // Seed surveillance data for the last 30 days
            for (int i = 0; i < 10; i++)
            {
                context.Reports.Add(new Report
                {
                    UserId = "gov-reporter-at06",
                    Latitude = 23.8 + (i * 0.01),
                    Longitude = 90.4 + (i * 0.01),
                    DiseaseType = DiseaseType.Dengue,
                    Verification = i < 6 ? ReportVerification.Verified : ReportVerification.Pending,
                    CreatedAt = DateTime.UtcNow.AddDays(-i * 3)
                });
            }
            context.RiskZones.Add(new RiskZone
            {
                Region = "Dhaka Metropolitan Area",
                DiseaseType = DiseaseType.Dengue,
                RiskLevel = RiskLevel.High,
                ConfidenceScore = 0.85,
                DataSufficiency = DataSufficiency.Sufficient
            });
            await context.SaveChangesAsync();

            var pdfService = new PdfReportService(context);

            // Act: Government official requests last 30-day report
            var startDate = DateTime.UtcNow.AddDays(-30);
            var endDate = DateTime.UtcNow;
            var pdfBytes = await pdfService.GenerateGovernmentReportAsync(startDate, endDate);

            // Assert
            Assert.NotNull(pdfBytes);
            Assert.True(pdfBytes.Length > 1000, "PDF should contain meaningful report content");
            var header = System.Text.Encoding.ASCII.GetString(pdfBytes, 0, 5);
            Assert.Equal("%PDF-", header);
        }

        // AT-07: Security — Citizen Privilege Escalation to Admin/Gov Areas (Exceptional)
        [Theory]
        [InlineData(typeof(AdminController), "Administrator")]
        [InlineData(typeof(GovernmentController), "GovernmentAuthority")]
        public void AT07_RoleBasedAuthorization_AdminAndGovControllers_RestrictedToCorrectRoles(
            Type controllerType, string expectedRole)
        {
            // Act: Inspect controller-level [Authorize] attribute
            var authorizeAttribute = controllerType.GetCustomAttribute<AuthorizeAttribute>();

            // Assert: Controller is protected by [Authorize] with a specific role
            Assert.NotNull(authorizeAttribute);
            Assert.NotNull(authorizeAttribute.Roles);
            Assert.Contains(expectedRole, authorizeAttribute.Roles);
        }

        // AT-08: Administrator — Doctor Account Approval Gate (Expected)
        [Fact]
        public async Task AT08_Administrator_ApprovesDoctorAccount_UnlocksClinicialPrivileges()
        {
            // Arrange
            var context = CreateInMemoryContext("AT08_AdminDb");

            // Step 1: Doctor registers — IsApproved defaults to false (requires admin verification)
            var doctor = new ApplicationUser
            {
                Id = "doctor-at08",
                UserName = "dr.karim@test.com",
                Email = "dr.karim@test.com",
                FullName = "Dr. A.K.M. Karim",
                ModePreference = "Professional",
                Specialty = "Internal Medicine",
                IsApproved = false  // Pending BMDC license verification
            };
            context.Users.Add(doctor);
            await context.SaveChangesAsync();

            // Assert: Doctor starts unapproved
            var pendingDoctor = await context.Users.FindAsync(doctor.Id);
            Assert.NotNull(pendingDoctor);
            Assert.False(pendingDoctor.IsApproved);
            Assert.Equal("Professional", pendingDoctor.ModePreference);
            Assert.Equal("Internal Medicine", pendingDoctor.Specialty);

            // Step 2: Doctor attempts to access clinical features — blocked by IsApproved guard
            // (Simulated by checking the flag; in the real app this redirects via a filter)
            var canAccessClinicalFeatures = pendingDoctor.IsApproved;
            Assert.False(canAccessClinicalFeatures);

            // Step 3: Administrator reviews BMDC registration and approves
            pendingDoctor.IsApproved = true;
            context.Users.Update(pendingDoctor);
            await context.SaveChangesAsync();

            // Step 4: Doctor refreshes — full clinical privileges unlocked
            var approvedDoctor = await context.Users.FindAsync(doctor.Id);
            Assert.NotNull(approvedDoctor);
            Assert.True(approvedDoctor.IsApproved);

            var canNowAccess = approvedDoctor.IsApproved;
            Assert.True(canNowAccess);
        }
    }
}
