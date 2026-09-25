using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LarvaX.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLabStaffFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BarcodeNumber",
                table: "LabBookings",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "LabBookings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DengueIgg",
                table: "LabBookings",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DengueIgm",
                table: "LabBookings",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DengueNs1",
                table: "LabBookings",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Hematocrit",
                table: "LabBookings",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsVerified",
                table: "LabBookings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LabStaffId",
                table: "LabBookings",
                type: "character varying(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PatientNotified",
                table: "LabBookings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PlateletCount",
                table: "LabBookings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProcessingStartedAt",
                table: "LabBookings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "LabBookings",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SampleCollectedAt",
                table: "LabBookings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SampleReceivedAt",
                table: "LabBookings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SampleStatus",
                table: "LabBookings",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SampleType",
                table: "LabBookings",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TestNotes",
                table: "LabBookings",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VerifiedAt",
                table: "LabBookings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VerifiedBy",
                table: "LabBookings",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WbcCount",
                table: "LabBookings",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BarcodeNumber",
                table: "LabBookings");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "LabBookings");

            migrationBuilder.DropColumn(
                name: "DengueIgg",
                table: "LabBookings");

            migrationBuilder.DropColumn(
                name: "DengueIgm",
                table: "LabBookings");

            migrationBuilder.DropColumn(
                name: "DengueNs1",
                table: "LabBookings");

            migrationBuilder.DropColumn(
                name: "Hematocrit",
                table: "LabBookings");

            migrationBuilder.DropColumn(
                name: "IsVerified",
                table: "LabBookings");

            migrationBuilder.DropColumn(
                name: "LabStaffId",
                table: "LabBookings");

            migrationBuilder.DropColumn(
                name: "PatientNotified",
                table: "LabBookings");

            migrationBuilder.DropColumn(
                name: "PlateletCount",
                table: "LabBookings");

            migrationBuilder.DropColumn(
                name: "ProcessingStartedAt",
                table: "LabBookings");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "LabBookings");

            migrationBuilder.DropColumn(
                name: "SampleCollectedAt",
                table: "LabBookings");

            migrationBuilder.DropColumn(
                name: "SampleReceivedAt",
                table: "LabBookings");

            migrationBuilder.DropColumn(
                name: "SampleStatus",
                table: "LabBookings");

            migrationBuilder.DropColumn(
                name: "SampleType",
                table: "LabBookings");

            migrationBuilder.DropColumn(
                name: "TestNotes",
                table: "LabBookings");

            migrationBuilder.DropColumn(
                name: "VerifiedAt",
                table: "LabBookings");

            migrationBuilder.DropColumn(
                name: "VerifiedBy",
                table: "LabBookings");

            migrationBuilder.DropColumn(
                name: "WbcCount",
                table: "LabBookings");
        }
    }
}
