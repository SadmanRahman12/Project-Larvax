using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LarvaX.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHealthWorkerEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FieldNotes",
                table: "Reports",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DengueCases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PatientName = table.Column<string>(type: "text", nullable: false),
                    PatientPhone = table.Column<string>(type: "text", nullable: true),
                    PatientAddress = table.Column<string>(type: "text", nullable: true),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    Age = table.Column<int>(type: "integer", nullable: false),
                    Gender = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Severity = table.Column<string>(type: "text", nullable: false),
                    PlateletCount = table.Column<int>(type: "integer", nullable: true),
                    Hematocrit = table.Column<double>(type: "double precision", nullable: true),
                    Symptoms = table.Column<string>(type: "text", nullable: true),
                    FieldNotes = table.Column<string>(type: "text", nullable: true),
                    AssignedWorkerId = table.Column<string>(type: "text", nullable: true),
                    NextFollowUpDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsEscalated = table.Column<bool>(type: "boolean", nullable: false),
                    EscalationReason = table.Column<string>(type: "text", nullable: true),
                    ReportedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DengueCases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DengueCases_AspNetUsers_AssignedWorkerId",
                        column: x => x.AssignedWorkerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "HealthWorkerTasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Category = table.Column<string>(type: "text", nullable: false),
                    Priority = table.Column<string>(type: "text", nullable: false),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    HealthWorkerId = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HealthWorkerTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HealthWorkerTasks_AspNetUsers_HealthWorkerId",
                        column: x => x.HealthWorkerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CaseReferrals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DengueCaseId = table.Column<int>(type: "integer", nullable: true),
                    PatientName = table.Column<string>(type: "text", nullable: false),
                    PatientPhone = table.Column<string>(type: "text", nullable: true),
                    Target = table.Column<string>(type: "text", nullable: false),
                    Urgency = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    ClinicalNotes = table.Column<string>(type: "text", nullable: true),
                    ReferredById = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CaseReferrals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CaseReferrals_AspNetUsers_ReferredById",
                        column: x => x.ReferredById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CaseReferrals_DengueCases_DengueCaseId",
                        column: x => x.DengueCaseId,
                        principalTable: "DengueCases",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_CaseReferrals_DengueCaseId",
                table: "CaseReferrals",
                column: "DengueCaseId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseReferrals_ReferredById",
                table: "CaseReferrals",
                column: "ReferredById");

            migrationBuilder.CreateIndex(
                name: "IX_DengueCases_AssignedWorkerId",
                table: "DengueCases",
                column: "AssignedWorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_DengueCases_Status",
                table: "DengueCases",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_HealthWorkerTasks_HealthWorkerId",
                table: "HealthWorkerTasks",
                column: "HealthWorkerId");

            migrationBuilder.CreateIndex(
                name: "IX_HealthWorkerTasks_IsCompleted",
                table: "HealthWorkerTasks",
                column: "IsCompleted");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CaseReferrals");

            migrationBuilder.DropTable(
                name: "HealthWorkerTasks");

            migrationBuilder.DropTable(
                name: "DengueCases");

            migrationBuilder.DropColumn(
                name: "FieldNotes",
                table: "Reports");
        }
    }
}
