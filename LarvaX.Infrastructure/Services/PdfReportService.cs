using LarvaX.Core.Interfaces;
using LarvaX.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LarvaX.Infrastructure.Services
{
    public class PdfReportService : IPdfReportService
    {
        private readonly ApplicationDbContext _db;

        public PdfReportService(ApplicationDbContext db)
        {
            _db = db;
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public async Task<byte[]> GenerateGovernmentReportAsync(DateTime startDate, DateTime endDate)
        {
            var reports = await _db.Reports
                .Where(r => r.CreatedAt >= startDate && r.CreatedAt <= endDate)
                .ToListAsync();

            var riskZones = await _db.RiskZones.ToListAsync();
            var alerts = await _db.Alerts
                .Where(a => a.SentAt >= startDate && a.SentAt <= endDate)
                .ToListAsync();

            var totalReports = reports.Count;
            var verifiedReports = reports.Count(r => r.Verification.ToString() == "Verified");
            var highRiskZones = riskZones.Count(z => z.RiskLevel.ToString() == "High");
            var alertsSent = alerts.Count;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily(Fonts.Arial));

                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("LarvaX Dengue Surveillance Platform")
                                    .Bold().FontSize(18).FontColor(Color.FromHex("#1a6b3c"));
                                c.Item().Text("Official Government Report")
                                    .FontSize(12).FontColor(Color.FromHex("#555555"));
                            });
                            row.ConstantItem(120).AlignRight().Column(c =>
                            {
                                c.Item().Text($"Generated: {DateTime.UtcNow:dd MMM yyyy}").FontSize(9);
                                c.Item().Text($"Period: {startDate:dd MMM} – {endDate:dd MMM yyyy}").FontSize(9);
                            });
                        });
                        col.Item().PaddingTop(8).LineHorizontal(1).LineColor(Color.FromHex("#1a6b3c"));
                    });

                    page.Content().PaddingTop(20).Column(col =>
                    {
                        // Summary section
                        col.Item().Text("Executive Summary").Bold().FontSize(14).FontColor(Color.FromHex("#1a6b3c"));
                        col.Item().PaddingTop(6).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn();
                                c.RelativeColumn();
                                c.RelativeColumn();
                                c.RelativeColumn();
                            });
                            table.Header(h =>
                            {
                                h.Cell().Background(Color.FromHex("#1a6b3c")).Padding(6)
                                    .Text("Total Reports").Bold().FontColor(Colors.White);
                                h.Cell().Background(Color.FromHex("#1a6b3c")).Padding(6)
                                    .Text("Verified Reports").Bold().FontColor(Colors.White);
                                h.Cell().Background(Color.FromHex("#1a6b3c")).Padding(6)
                                    .Text("High-Risk Zones").Bold().FontColor(Colors.White);
                                h.Cell().Background(Color.FromHex("#1a6b3c")).Padding(6)
                                    .Text("Alerts Issued").Bold().FontColor(Colors.White);
                            });
                            table.Cell().Background(Color.FromHex("#f0f9f0")).Padding(8)
                                .Text(totalReports.ToString()).Bold().FontSize(16).AlignCenter();
                            table.Cell().Background(Color.FromHex("#f0f9f0")).Padding(8)
                                .Text(verifiedReports.ToString()).Bold().FontSize(16).AlignCenter();
                            table.Cell().Background(Color.FromHex("#fff3cd")).Padding(8)
                                .Text(highRiskZones.ToString()).Bold().FontSize(16).AlignCenter();
                            table.Cell().Background(Color.FromHex("#f8d7da")).Padding(8)
                                .Text(alertsSent.ToString()).Bold().FontSize(16).AlignCenter();
                        });

                        col.Item().PaddingTop(20).Text("Reports by Disease Type").Bold().FontSize(14).FontColor(Color.FromHex("#1a6b3c"));
                        var byDisease = reports
                            .GroupBy(r => r.DiseaseType.ToString())
                            .Select(g => new { Type = g.Key, Count = g.Count() })
                            .OrderByDescending(g => g.Count)
                            .ToList();

                        col.Item().PaddingTop(8).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(2);
                                c.RelativeColumn(1);
                            });
                            table.Header(h =>
                            {
                                h.Cell().Background(Color.FromHex("#e8f5e9")).Padding(5).Text("Disease Type").Bold();
                                h.Cell().Background(Color.FromHex("#e8f5e9")).Padding(5).Text("Count").Bold();
                            });
                            foreach (var item in byDisease)
                            {
                                table.Cell().BorderBottom(1).BorderColor(Color.FromHex("#dddddd")).Padding(5).Text(item.Type);
                                table.Cell().BorderBottom(1).BorderColor(Color.FromHex("#dddddd")).Padding(5).Text(item.Count.ToString()).AlignRight();
                            }
                            if (!byDisease.Any())
                                table.Cell().ColumnSpan(2).Padding(5).Text("No data in selected period.").Italic().FontColor(Color.FromHex("#888888"));
                        });

                        col.Item().PaddingTop(20).Text("Risk Zone Overview").Bold().FontSize(14).FontColor(Color.FromHex("#1a6b3c"));
                        col.Item().PaddingTop(8).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(2);
                                c.RelativeColumn();
                                c.RelativeColumn();
                            });
                            table.Header(h =>
                            {
                                h.Cell().Background(Color.FromHex("#e8f5e9")).Padding(5).Text("Region").Bold();
                                h.Cell().Background(Color.FromHex("#e8f5e9")).Padding(5).Text("Risk Level").Bold();
                                h.Cell().Background(Color.FromHex("#e8f5e9")).Padding(5).Text("Data Sufficiency").Bold();
                            });
                            foreach (var zone in riskZones.Take(20))
                            {
                                table.Cell().BorderBottom(1).BorderColor(Color.FromHex("#dddddd")).Padding(5).Text(zone.Region);
                                table.Cell().BorderBottom(1).BorderColor(Color.FromHex("#dddddd")).Padding(5).Text(zone.RiskLevel.ToString());
                                table.Cell().BorderBottom(1).BorderColor(Color.FromHex("#dddddd")).Padding(5).Text(zone.DataSufficiency.ToString());
                            }
                            if (!riskZones.Any())
                                table.Cell().ColumnSpan(3).Padding(5).Text("No risk zones defined.").Italic().FontColor(Color.FromHex("#888888"));
                        });
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("LarvaX Dengue Surveillance Platform | Confidential Government Report | Page ");
                        x.CurrentPageNumber();
                        x.Span(" of ");
                        x.TotalPages();
                    });
                });
            });

            return document.GeneratePdf();
        }
    }
}
