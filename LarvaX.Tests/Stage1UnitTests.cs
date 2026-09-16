using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LarvaX.Application.Services;
using LarvaX.Core.Entities;
using LarvaX.Infrastructure.Data;
using LarvaX.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace LarvaX.Tests
{
    public class Stage1UnitTests
    {
        private ApplicationDbContext CreateInMemoryContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new ApplicationDbContext(options);
        }

        // UT-01: Inventory Inflow Stock Increment (Expected)
        [Fact]
        public async Task UT01_InventoryService_RecordTransaction_IncrementsStockOnInflow()
        {
            // Arrange
            var context = CreateInMemoryContext("UT01_InventoryDb");
            var service = new InventoryService(context);
            var item = new InventoryItem
            {
                Name = "Normal Saline 0.9% 500ml",
                Quantity = 50,
                Threshold = 20,
                Location = "Dhaka Medical College Hospital"
            };
            await service.AddItemAsync(item);

            // Act
            await service.RecordTransactionAsync(item.Id, quantityChange: 100, reason: "Bulk shipment receipt", userId: "admin-1");

            // Assert
            var updatedItem = await service.GetItemByIdAsync(item.Id);
            Assert.NotNull(updatedItem);
            Assert.Equal(150, updatedItem.Quantity);

            var transactions = await service.GetTransactionsAsync(item.Id);
            Assert.Single(transactions);
            Assert.Equal(100, transactions.First().QuantityChange);
            Assert.Equal("Bulk shipment receipt", transactions.First().Reason);
        }

        // UT-02: Negative Inventory Stock Depletion Prevention (Unexpected)
        [Fact]
        public async Task UT02_InventoryService_RecordTransaction_ThrowsOnDepletionBelowZero()
        {
            // Arrange
            var context = CreateInMemoryContext("UT02_InventoryDb");
            var service = new InventoryService(context);
            var item = new InventoryItem
            {
                Name = "Platelet Concentrate",
                Quantity = 5,
                Threshold = 10,
                Location = "Mugda Medical College"
            };
            await service.AddItemAsync(item);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.RecordTransactionAsync(item.Id, quantityChange: -10, reason: "Emergency Ward requisition", userId: "doc-1"));

            Assert.Contains("Insufficient stock. Cannot reduce below zero.", ex.Message);

            var unchangedItem = await service.GetItemByIdAsync(item.Id);
            Assert.NotNull(unchangedItem);
            Assert.Equal(5, unchangedItem.Quantity);
        }

        // UT-03: Inventory Transaction for Non-Existent Item (Exceptional)
        [Fact]
        public async Task UT03_InventoryService_RecordTransaction_ThrowsWhenItemNotFound()
        {
            // Arrange
            var context = CreateInMemoryContext("UT03_InventoryDb");
            var service = new InventoryService(context);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.RecordTransactionAsync(itemId: 9999, quantityChange: 5, reason: "Adjustment", userId: "admin-1"));

            Assert.Contains("Inventory item not found.", ex.Message);
        }

        // UT-04: Education Quiz Scoring with Partial Correct Answers (Expected)
        [Fact]
        public async Task UT04_EducationService_SubmitQuiz_CalculatesPartialScoreAccurately()
        {
            // Arrange
            var context = CreateInMemoryContext("UT04_EducationDb");
            var service = new EducationService(context);

            var quiz = new Quiz
            {
                Title = "Dengue Warning Signs Quiz",
                Language = "en",
                Questions = new List<QuizQuestion>
                {
                    new QuizQuestion
                    {
                        Text = "Which mosquito spreads dengue?",
                        Options = new List<QuizOption>
                        {
                            new QuizOption { Text = "Aedes aegypti", IsCorrect = true },
                            new QuizOption { Text = "Anopheles", IsCorrect = false }
                        }
                    },
                    new QuizQuestion
                    {
                        Text = "Which medicine should be strictly avoided in dengue?",
                        Options = new List<QuizOption>
                        {
                            new QuizOption { Text = "Paracetamol", IsCorrect = false },
                            new QuizOption { Text = "Aspirin & Ibuprofen", IsCorrect = true }
                        }
                    },
                    new QuizQuestion
                    {
                        Text = "What is a red-flag emergency sign?",
                        Options = new List<QuizOption>
                        {
                            new QuizOption { Text = "Persistent vomiting & mucosal bleeding", IsCorrect = true },
                            new QuizOption { Text = "Mild sneezing", IsCorrect = false }
                        }
                    }
                }
            };

            context.Quizzes.Add(quiz);
            await context.SaveChangesAsync();

            var questionList = quiz.Questions.ToList();
            var q1 = questionList[0];
            var q2 = questionList[1];
            var q3 = questionList[2];

            // Submit: Q1 correct, Q2 wrong, Q3 correct (Score should be 2)
            var answers = new Dictionary<int, int>
            {
                { q1.Id, q1.Options.First(o => o.IsCorrect).Id },
                { q2.Id, q2.Options.First(o => !o.IsCorrect).Id },
                { q3.Id, q3.Options.First(o => o.IsCorrect).Id }
            };

            // Act
            int score = await service.SubmitQuizAsync(quiz.Id, answers);

            // Assert
            Assert.Equal(2, score);
        }

        // UT-05: Quiz Submission with Empty or Missing Answer Dictionary (Unexpected)
        [Fact]
        public async Task UT05_EducationService_SubmitQuiz_HandlesEmptyAnswersSafely()
        {
            // Arrange
            var context = CreateInMemoryContext("UT05_EducationDb");
            var service = new EducationService(context);

            var quiz = new Quiz
            {
                Title = "General Mosquito Awareness",
                Language = "en",
                Questions = new List<QuizQuestion>
                {
                    new QuizQuestion
                    {
                        Text = "Where do Aedes mosquitoes breed?",
                        Options = new List<QuizOption>
                        {
                            new QuizOption { Text = "Clean stagnant water", IsCorrect = true },
                            new QuizOption { Text = "Flowing rivers", IsCorrect = false }
                        }
                    }
                }
            };
            context.Quizzes.Add(quiz);
            await context.SaveChangesAsync();

            // Act
            int scoreEmpty = await service.SubmitQuizAsync(quiz.Id, new Dictionary<int, int>());
            int scoreNonExistentQuiz = await service.SubmitQuizAsync(99999, new Dictionary<int, int>());

            // Assert
            Assert.Equal(0, scoreEmpty);
            Assert.Equal(0, scoreNonExistentQuiz);
        }

        // UT-06: Telemedicine Past Date UTC Normalization & Audit Timestamps (Exceptional)
        [Fact]
        public async Task UT06_TelemedicineService_BookAppointment_NormalizesUtcAndAudits()
        {
            // Arrange
            var context = CreateInMemoryContext("UT06_TelemedDb");
            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
            var userManagerMock = new Mock<UserManager<ApplicationUser>>(userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

            var service = new TelemedicineService(context, userManagerMock.Object);
            var scheduledLocal = DateTime.Now.AddDays(1);

            // Act
            var appointment = await service.BookAppointmentAsync("patient-1", "doc-1", scheduledLocal, "Severe joint pain follow-up");

            // Assert
            Assert.NotNull(appointment);
            Assert.Equal(AppointmentStatus.Booked, appointment.Status);
            Assert.Equal(DateTimeKind.Utc, appointment.ScheduledAt.Kind);
            Assert.True((DateTime.UtcNow - appointment.CreatedAt).TotalSeconds < 5);
            Assert.Equal("Severe joint pain follow-up", appointment.Notes);

            // Ensure video room creation helper generates a room ID
            await service.EnsureVideoRoomExistsAsync(appointment);
            Assert.False(string.IsNullOrWhiteSpace(appointment.VideoRoomId));
        }

        // UT-07: Pediatric Fluid Rate Scaling in Holliday-Segar Protocol (Expected)
        [Fact]
        public void UT07_FluidManagementService_CalculateFluidPlan_ScalesPediatricWeightCorrectly()
        {
            // Arrange
            var service = new FluidManagementService();
            var parameters = new FluidManagementParameters
            {
                Weight = 8.0m, // Pediatric < 10 kg
                DehydrationPercent = 5m,
                OngoingLosses = 0m,
                ClinicalMode = "Maintenance"
            };

            // Act
            var plan = service.CalculateFluidPlan(parameters);

            // Assert
            // Holliday-Segar for 8 kg = 8 * 100 ml = 800 ml total maintenance for 24h
            Assert.Equal(800.00m, plan.Maintenance24h);
            Assert.True(plan.RecommendedRateHour > 0);
            // In pediatric cases (<10kg), baseline maintenance exceeds the default 70ml/kg adult safety ceiling, triggering a cautionary alert
            Assert.True(plan.IsOverThreshold);
            Assert.Contains("WARNING", plan.SafetyAlert);
        }

        // UT-08: Inverted Date Range Handling in PDF Report Generation (Unexpected)
        [Fact]
        public async Task UT08_PdfReportService_GenerateGovernmentReport_RendersWithoutCrashingOnInvertedDates()
        {
            // Arrange
            var context = CreateInMemoryContext("UT08_PdfReportDb");
            var service = new PdfReportService(context);

            // Start date chronologically after end date
            var startDate = new DateTime(2026, 9, 30, 0, 0, 0, DateTimeKind.Utc);
            var endDate = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);

            // Act
            var pdfBytes = await service.GenerateGovernmentReportAsync(startDate, endDate);

            // Assert
            Assert.NotNull(pdfBytes);
            Assert.NotEmpty(pdfBytes);
            // PDF binary format starts with magic header %PDF-
            var header = System.Text.Encoding.ASCII.GetString(pdfBytes, 0, 5);
            Assert.Equal("%PDF-", header);
        }
    }
}
