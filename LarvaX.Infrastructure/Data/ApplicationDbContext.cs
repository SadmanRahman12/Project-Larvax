using LarvaX.Core.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LarvaX.Infrastructure.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public DbSet<Report> Reports { get; set; } = null!;
        public DbSet<RiskZone> RiskZones { get; set; } = null!;
        public DbSet<Alert> Alerts { get; set; } = null!;
        public DbSet<Appointment> Appointments { get; set; } = null!;
        public DbSet<Donor> Donors { get; set; } = null!;
        public DbSet<LabTest> LabTests { get; set; } = null!;
        public DbSet<LabBooking> LabBookings { get; set; } = null!;
        public DbSet<PatientRecord> PatientRecords { get; set; } = null!;
        public DbSet<Article> Articles { get; set; } = null!;
        public DbSet<Quiz> Quizzes { get; set; } = null!;
        public DbSet<QuizQuestion> QuizQuestions { get; set; } = null!;
        public DbSet<QuizOption> QuizOptions { get; set; } = null!;
        public DbSet<InventoryItem> InventoryItems { get; set; } = null!;
        public DbSet<InventoryTransaction> InventoryTransactions { get; set; } = null!;
        public DbSet<FlowAnalytics> FlowAnalytics { get; set; } = null!;
        public DbSet<SmsCommand> SmsCommands { get; set; } = null!;
        public DbSet<DoctorSchedule> DoctorSchedules { get; set; } = null!;
        public DbSet<DengueCase> DengueCases { get; set; } = null!;
        public DbSet<HealthWorkerTask> HealthWorkerTasks { get; set; } = null!;
        public DbSet<CaseReferral> CaseReferrals { get; set; } = null!;
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            
            // Enum conversions
            builder.Entity<Report>()
                .Property(r => r.DiseaseType)
                .HasConversion<string>();
            builder.Entity<Report>()
                .Property(r => r.Status)
                .HasConversion<string>();
            builder.Entity<Report>()
                .Property(r => r.Verification)
                .HasConversion<string>();
                
            builder.Entity<RiskZone>()
                .Property(r => r.DiseaseType)
                .HasConversion<string>();
            builder.Entity<RiskZone>()
                .Property(r => r.RiskLevel)
                .HasConversion<string>();
            builder.Entity<RiskZone>()
                .Property(r => r.DataSufficiency)
                .HasConversion<string>();

            // Indexes as requested
            builder.Entity<Report>()
                .HasIndex(r => r.Status);
            builder.Entity<RiskZone>()
                .HasIndex(r => r.Region);
            builder.Entity<Alert>()
                .HasIndex(a => a.SentAt);

            // Donor enum
            builder.Entity<Donor>()
                .Property(d => d.BloodGroup)
                .HasConversion<string>();

            // Appointment status conversion
            builder.Entity<Appointment>()
                .Property(a => a.Status)
                .HasConversion<string>();
            builder.Entity<Appointment>()
                .HasIndex(a => a.ScheduledAt);

            // DoctorSchedule configuration
            builder.Entity<DoctorSchedule>()
                .Property(s => s.DayOfWeek)
                .HasConversion<string>();
            builder.Entity<DoctorSchedule>()
                .HasIndex(s => s.DoctorId);
            builder.Entity<DoctorSchedule>()
                .HasIndex(s => new { s.DoctorId, s.DayOfWeek });

            // DengueCase configuration
            builder.Entity<DengueCase>()
                .Property(c => c.Status)
                .HasConversion<string>();
            builder.Entity<DengueCase>()
                .Property(c => c.Severity)
                .HasConversion<string>();
            builder.Entity<DengueCase>()
                .HasIndex(c => c.Status);

            // HealthWorkerTask configuration
            builder.Entity<HealthWorkerTask>()
                .Property(t => t.Priority)
                .HasConversion<string>();
            builder.Entity<HealthWorkerTask>()
                .HasIndex(t => t.IsCompleted);

            // CaseReferral configuration
            builder.Entity<CaseReferral>()
                .Property(r => r.Target)
                .HasConversion<string>();
            builder.Entity<CaseReferral>()
                .Property(r => r.Urgency)
                .HasConversion<string>();
            builder.Entity<CaseReferral>()
                .Property(r => r.Status)
                .HasConversion<string>();
        }
    }
}
