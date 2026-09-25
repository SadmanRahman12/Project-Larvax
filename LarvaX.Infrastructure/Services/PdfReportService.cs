using System;
using System.Linq;
using System.Threading.Tasks;
using LarvaX.Core.Entities;
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

        public async Task<byte[]> GenerateGovernmentReportAsync(
            DateTime startDate,
            DateTime endDate,
            string? division = null,
            string? district = null,
            string? riskLevel = null,
            string? caseStatus = null)
        {
            // Normalize dates to full day boundaries
            var startUtc = DateTime.SpecifyKind(startDate.Date, DateTimeKind.Utc);
            var endUtc = DateTime.SpecifyKind(endDate.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);

            // Query Dengue Cases
            var casesQuery = _db.DengueCases
                .Where(c => c.ReportedDate >= startUtc && c.ReportedDate <= endUtc);

            if (!string.IsNullOrWhiteSpace(caseStatus) && Enum.TryParse<CaseStatus>(caseStatus, true, out var parsedStatus))
            {
                casesQuery = casesQuery.Where(c => c.Status == parsedStatus);
            }

            if (!string.IsNullOrWhiteSpace(division) && !division.Equals("ALL", StringComparison.OrdinalIgnoreCase))
            {
                casesQuery = casesQuery.Where(c => c.PatientAddress != null && c.PatientAddress.Contains(division));
            }

            if (!string.IsNullOrWhiteSpace(district) && !district.Equals("ALL", StringComparison.OrdinalIgnoreCase))
            {
                casesQuery = casesQuery.Where(c => c.PatientAddress != null && c.PatientAddress.Contains(district));
            }

            var dengueCases = await casesQuery.OrderByDescending(c => c.ReportedDate).ToListAsync();

            // Query Risk Zones
            var riskZonesQuery = _db.RiskZones.AsQueryable();
            if (!string.IsNullOrWhiteSpace(riskLevel) && Enum.TryParse<RiskLevel>(riskLevel, true, out var parsedRisk))
            {
                riskZonesQuery = riskZonesQuery.Where(z => z.RiskLevel == parsedRisk);
            }
            if (!string.IsNullOrWhiteSpace(division) && !division.Equals("ALL", StringComparison.OrdinalIgnoreCase))
            {
                riskZonesQuery = riskZonesQuery.Where(z => z.Region.Contains(division));
            }
            var riskZones = await riskZonesQuery.OrderByDescending(z => z.RiskLevel).ToListAsync();

            // Query Reports (Citizen hazards)
            var citizenReports = await _db.Reports
                .Where(r => r.CreatedAt >= startUtc && r.CreatedAt <= endUtc)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            // Query Alerts
            var alerts = await _db.Alerts
                .Where(a => a.SentAt >= startUtc && a.SentAt <= endUtc)
                .OrderByDescending(a => a.SentAt)
                .ToListAsync();

            // Query Resources
            var inventoryItems = await _db.InventoryItems.ToListAsync();
            var donors = await _db.Donors.Where(d => d.IsAvailable).ToListAsync();

            // Key Metrics
            var totalCases = dengueCases.Count;
            var confirmedCases = dengueCases.Count(c => c.Status == CaseStatus.Confirmed);
            var suspectedCases = dengueCases.Count(c => c.Status == CaseStatus.Suspected);
            var activeCases = dengueCases.Count(c => c.Status == CaseStatus.Suspected || c.Status == CaseStatus.UnderObservation || c.Status == CaseStatus.Confirmed);
            var deceasedCases = dengueCases.Count(c => c.Status == CaseStatus.Deceased);
            var caseFatalityRate = totalCases > 0 ? ((double)deceasedCases / totalCases * 100).ToString("F2") + "%" : "0.00%";
            var highRiskZonesCount = riskZones.Count(z => z.RiskLevel == RiskLevel.High);
            var sufficientZonesCount = riskZones.Count(z => z.DataSufficiency == DataSufficiency.Sufficient);
            var dataSufficiencyRate = riskZones.Any() ? ((double)sufficientZonesCount / riskZones.Count * 100).ToString("F1") + "%" : "N/A";

            var reportRef = $"LX-DGHS-{DateTime.UtcNow:yyyyMMdd}-{new Random().Next(1000, 9999)}";

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(35);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Arial).FontColor(Color.FromHex("#1e293b")));

                    // HEADER
                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("GOVERNMENT OF THE PEOPLE'S REPUBLIC OF BANGLADESH")
                                    .FontSize(9).Bold().LetterSpacing(0.05f).FontColor(Color.FromHex("#047857"));
                                c.Item().Text("Directorate General of Health Services (DGHS)")
                                    .FontSize(15).Bold().FontColor(Color.FromHex("#0f172a"));
                                c.Item().Text("LarvaX National Dengue Surveillance & Epidemic Intelligence Report")
                                    .FontSize(11).SemiBold().FontColor(Color.FromHex("#334155"));
                            });

                            row.ConstantItem(150).AlignRight().Column(c =>
                            {
                                c.Item().Text($"Ref: {reportRef}").FontSize(8).Bold().FontColor(Color.FromHex("#64748b"));
                                c.Item().Text($"Date: {DateTime.UtcNow:dd MMM yyyy, HH:mm} UTC").FontSize(8);
                                c.Item().Text($"Period: {startDate:dd MMM yyyy} – {endDate:dd MMM yyyy}").FontSize(8).Bold();
                                c.Item().Container().PaddingTop(2)
                                    .Background(Color.FromHex("#fee2e2")).PaddingHorizontal(4).PaddingVertical(1)
                                    .Text("CONFIDENTIAL / OFFICIAL USE").FontSize(7).Bold().FontColor(Color.FromHex("#991b1b")).AlignCenter();
                            });
                        });

                        col.Item().PaddingTop(8).LineHorizontal(1.5f).LineColor(Color.FromHex("#047857"));
                    });

                    // CONTENT
                    page.Content().PaddingTop(12).Column(col =>
                    {
                        // 1. Filter Criteria Badge Bar
                        col.Item().Background(Color.FromHex("#f8fafc")).Border(1).BorderColor(Color.FromHex("#e2e8f0")).Padding(8).Row(r =>
                        {
                            r.RelativeItem().Text(t =>
                            {
                                t.Span("Surveillance Scope: ").Bold().FontSize(8).FontColor(Color.FromHex("#475569"));
                                t.Span($"Division: {division ?? "National (All)"} | District: {district ?? "All"} | Status: {caseStatus ?? "All"} | Risk Level: {riskLevel ?? "All"}")
                                    .FontSize(8).FontColor(Color.FromHex("#0f172a"));
                            });
                            r.ConstantItem(120).AlignRight().Text($"Records: {totalCases} Cases").FontSize(8).Bold().FontColor(Color.FromHex("#047857"));
                        });

                        // 2. Executive Surveillance KPIs
                        col.Item().PaddingTop(12).Text("1. National Surveillance Summary").Bold().FontSize(12).FontColor(Color.FromHex("#047857"));
                        col.Item().PaddingTop(4).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn();
                                c.RelativeColumn();
                                c.RelativeColumn();
                                c.RelativeColumn();
                                c.RelativeColumn();
                                c.RelativeColumn();
                            });

                            table.Header(h =>
                            {
                                h.Cell().Background(Color.FromHex("#047857")).Padding(5).Text("Total Cases").Bold().FontColor(Colors.White).FontSize(8).AlignCenter();
                                h.Cell().Background(Color.FromHex("#b91c1c")).Padding(5).Text("Confirmed").Bold().FontColor(Colors.White).FontSize(8).AlignCenter();
                                h.Cell().Background(Color.FromHex("#d97706")).Padding(5).Text("Suspected").Bold().FontColor(Colors.White).FontSize(8).AlignCenter();
                                h.Cell().Background(Color.FromHex("#2563eb")).Padding(5).Text("Active Cases").Bold().FontColor(Colors.White).FontSize(8).AlignCenter();
                                h.Cell().Background(Color.FromHex("#450a0a")).Padding(5).Text("Deaths (CFR)").Bold().FontColor(Colors.White).FontSize(8).AlignCenter();
                                h.Cell().Background(Color.FromHex("#0f766e")).Padding(5).Text("Sufficiency").Bold().FontColor(Colors.White).FontSize(8).AlignCenter();
                            });

                            table.Cell().Background(Color.FromHex("#f0fdf4")).Padding(6).Text(totalCases.ToString()).Bold().FontSize(12).AlignCenter();
                            table.Cell().Background(Color.FromHex("#fef2f2")).Padding(6).Text(confirmedCases.ToString()).Bold().FontSize(12).FontColor(Color.FromHex("#b91c1c")).AlignCenter();
                            table.Cell().Background(Color.FromHex("#fffbeb")).Padding(6).Text(suspectedCases.ToString()).Bold().FontSize(12).FontColor(Color.FromHex("#d97706")).AlignCenter();
                            table.Cell().Background(Color.FromHex("#eff6ff")).Padding(6).Text(activeCases.ToString()).Bold().FontSize(12).FontColor(Color.FromHex("#2563eb")).AlignCenter();
                            table.Cell().Background(Color.FromHex("#fef2f2")).Padding(6).Text($"{deceasedCases} ({caseFatalityRate})").Bold().FontSize(11).FontColor(Color.FromHex("#991b1b")).AlignCenter();
                            table.Cell().Background(Color.FromHex("#f0fdfa")).Padding(6).Text(dataSufficiencyRate).Bold().FontSize(12).FontColor(Color.FromHex("#0f766e")).AlignCenter();
                        });

                        // 3. Regional Surveillance & Risk Matrix
                        col.Item().PaddingTop(12).Text("2. Regional Surveillance & Risk Matrix").Bold().FontSize(12).FontColor(Color.FromHex("#047857"));
                        col.Item().PaddingTop(4).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(3);
                                c.RelativeColumn(2);
                                c.RelativeColumn(2);
                                c.RelativeColumn(2);
                                c.RelativeColumn(2);
                                c.RelativeColumn(2);
                            });

                            table.Header(h =>
                            {
                                h.Cell().Background(Color.FromHex("#e2e8f0")).Padding(4).Text("Surveillance Zone / Region").Bold().FontSize(8);
                                h.Cell().Background(Color.FromHex("#e2e8f0")).Padding(4).Text("Disease Type").Bold().FontSize(8);
                                h.Cell().Background(Color.FromHex("#e2e8f0")).Padding(4).Text("Risk Level").Bold().FontSize(8);
                                h.Cell().Background(Color.FromHex("#e2e8f0")).Padding(4).Text("Confidence").Bold().FontSize(8);
                                h.Cell().Background(Color.FromHex("#e2e8f0")).Padding(4).Text("Sufficiency").Bold().FontSize(8);
                                h.Cell().Background(Color.FromHex("#e2e8f0")).Padding(4).Text("Last Run").Bold().FontSize(8);
                            });

                            foreach (var zone in riskZones.Take(12))
                            {
                                var riskColor = zone.RiskLevel == RiskLevel.High ? "#dc2626" : (zone.RiskLevel == RiskLevel.Medium ? "#d97706" : "#16a34a");
                                var suffColor = zone.DataSufficiency == DataSufficiency.Sufficient ? "#16a34a" : (zone.DataSufficiency == DataSufficiency.PartiallySufficient ? "#d97706" : "#dc2626");

                                table.Cell().BorderBottom(1).BorderColor(Color.FromHex("#f1f5f9")).Padding(4).Text(zone.Region).FontSize(8).SemiBold();
                                table.Cell().BorderBottom(1).BorderColor(Color.FromHex("#f1f5f9")).Padding(4).Text(zone.DiseaseType.ToString()).FontSize(8);
                                table.Cell().BorderBottom(1).BorderColor(Color.FromHex("#f1f5f9")).Padding(4).Text(zone.RiskLevel.ToString()).FontSize(8).Bold().FontColor(Color.FromHex(riskColor));
                                table.Cell().BorderBottom(1).BorderColor(Color.FromHex("#f1f5f9")).Padding(4).Text($"{(zone.ConfidenceScore * 100):F0}%").FontSize(8);
                                table.Cell().BorderBottom(1).BorderColor(Color.FromHex("#f1f5f9")).Padding(4).Text(zone.DataSufficiency.ToString()).FontSize(8).Bold().FontColor(Color.FromHex(suffColor));
                                table.Cell().BorderBottom(1).BorderColor(Color.FromHex("#f1f5f9")).Padding(4).Text(zone.LastModelRun.ToString("dd MMM HH:mm")).FontSize(7).FontColor(Color.FromHex("#64748b"));
                            }

                            if (!riskZones.Any())
                            {
                                table.Cell().ColumnSpan(6).Padding(8).Text("No risk zones found matching filter criteria.").Italic().FontColor(Color.FromHex("#94a3b8"));
                            }
                        });

                        // 4. Clinical Dengue Cases Breakdown
                        col.Item().PaddingTop(12).Text("3. Clinical Case Breakdown & Severity").Bold().FontSize(12).FontColor(Color.FromHex("#047857"));
                        var casesBySeverity = dengueCases.GroupBy(c => c.Severity).Select(g => new { Severity = g.Key, Count = g.Count() }).ToList();
                        var casesByStatus = dengueCases.GroupBy(c => c.Status).Select(g => new { Status = g.Key, Count = g.Count() }).ToList();

                        col.Item().PaddingTop(4).Row(r =>
                        {
                            // Status breakdown table
                            r.RelativeItem().Table(t =>
                            {
                                t.ColumnsDefinition(cd => { cd.RelativeColumn(3); cd.RelativeColumn(1); });
                                t.Header(th =>
                                {
                                    th.Cell().Background(Color.FromHex("#f1f5f9")).Padding(4).Text("Case Status").Bold().FontSize(8);
                                    th.Cell().Background(Color.FromHex("#f1f5f9")).Padding(4).Text("Cases").Bold().FontSize(8).AlignRight();
                                });
                                foreach (var st in casesByStatus)
                                {
                                    t.Cell().BorderBottom(1).BorderColor(Color.FromHex("#f8fafc")).Padding(3).Text(st.Status.ToString()).FontSize(8);
                                    t.Cell().BorderBottom(1).BorderColor(Color.FromHex("#f8fafc")).Padding(3).Text(st.Count.ToString()).FontSize(8).Bold().AlignRight();
                                }
                                if (!casesByStatus.Any())
                                    t.Cell().ColumnSpan(2).Padding(4).Text("No case records").Italic().FontSize(8);
                            });

                            r.ConstantItem(15);

                            // Severity breakdown table
                            r.RelativeItem().Table(t =>
                            {
                                t.ColumnsDefinition(cd => { cd.RelativeColumn(3); cd.RelativeColumn(1); });
                                t.Header(th =>
                                {
                                    th.Cell().Background(Color.FromHex("#f1f5f9")).Padding(4).Text("Severity Classification").Bold().FontSize(8);
                                    th.Cell().Background(Color.FromHex("#f1f5f9")).Padding(4).Text("Cases").Bold().FontSize(8).AlignRight();
                                });
                                foreach (var sv in casesBySeverity)
                                {
                                    t.Cell().BorderBottom(1).BorderColor(Color.FromHex("#f8fafc")).Padding(3).Text(sv.Severity.ToString()).FontSize(8);
                                    t.Cell().BorderBottom(1).BorderColor(Color.FromHex("#f8fafc")).Padding(3).Text(sv.Count.ToString()).FontSize(8).Bold().AlignRight();
                                }
                                if (!casesBySeverity.Any())
                                    t.Cell().ColumnSpan(2).Padding(4).Text("No severity records").Italic().FontSize(8);
                            });
                        });

                        // 5. Citizen Reports & Mosquito Breeding Hazards
                        col.Item().PaddingTop(12).Text("4. Community Mosquito Breeding Hazard Surveillance").Bold().FontSize(12).FontColor(Color.FromHex("#047857"));
                        var verifiedCitizenReports = citizenReports.Count(r => r.Verification == ReportVerification.Verified);
                        var pendingCitizenReports = citizenReports.Count(r => r.Verification == ReportVerification.Pending);

                        col.Item().PaddingTop(4).Background(Color.FromHex("#f8fafc")).Border(1).BorderColor(Color.FromHex("#e2e8f0")).Padding(8).Row(r =>
                        {
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Total Citizen Reports:").FontSize(8).FontColor(Color.FromHex("#64748b"));
                                c.Item().Text(citizenReports.Count.ToString()).Bold().FontSize(13);
                            });
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Verified Breeding Spots:").FontSize(8).FontColor(Color.FromHex("#64748b"));
                                c.Item().Text(verifiedCitizenReports.ToString()).Bold().FontSize(13).FontColor(Color.FromHex("#16a34a"));
                            });
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Under Field Review:").FontSize(8).FontColor(Color.FromHex("#64748b"));
                                c.Item().Text(pendingCitizenReports.ToString()).Bold().FontSize(13).FontColor(Color.FromHex("#d97706"));
                            });
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Emergency Alerts Issued:").FontSize(8).FontColor(Color.FromHex("#64748b"));
                                c.Item().Text(alerts.Count.ToString()).Bold().FontSize(13).FontColor(Color.FromHex("#dc2626"));
                            });
                        });

                        // 6. Medical Resource & Hospital Readiness
                        col.Item().PaddingTop(12).Text("5. Essential Health Resource Readiness").Bold().FontSize(12).FontColor(Color.FromHex("#047857"));
                        col.Item().PaddingTop(4).Row(r =>
                        {
                            // Inventory table
                            r.RelativeItem(3).Table(t =>
                            {
                                t.ColumnsDefinition(cd => { cd.RelativeColumn(3); cd.RelativeColumn(1); cd.RelativeColumn(1); });
                                t.Header(th =>
                                {
                                    th.Cell().Background(Color.FromHex("#e2e8f0")).Padding(3).Text("Essential Medical Supplies").Bold().FontSize(7);
                                    th.Cell().Background(Color.FromHex("#e2e8f0")).Padding(3).Text("Stock").Bold().FontSize(7).AlignRight();
                                    th.Cell().Background(Color.FromHex("#e2e8f0")).Padding(3).Text("Status").Bold().FontSize(7).AlignCenter();
                                });
                                foreach (var item in inventoryItems.Take(4))
                                {
                                    var isLow = item.Quantity <= item.Threshold;
                                    t.Cell().BorderBottom(1).BorderColor(Color.FromHex("#f1f5f9")).Padding(3).Text(item.Name).FontSize(7);
                                    t.Cell().BorderBottom(1).BorderColor(Color.FromHex("#f1f5f9")).Padding(3).Text(item.Quantity.ToString()).FontSize(7).AlignRight();
                                    t.Cell().BorderBottom(1).BorderColor(Color.FromHex("#f1f5f9")).Padding(3)
                                        .Text(isLow ? "LOW STOCK" : "ADEQUATE").FontSize(7).Bold().FontColor(Color.FromHex(isLow ? "#dc2626" : "#16a34a")).AlignCenter();
                                }
                            });

                            r.ConstantItem(15);

                            // Blood donors & alert summary
                            r.RelativeItem(2).Column(c =>
                            {
                                c.Item().Background(Color.FromHex("#eff6ff")).Border(1).BorderColor(Color.FromHex("#bfdbfe")).Padding(6).Column(bc =>
                                {
                                    bc.Item().Text("Active Blood Donors").Bold().FontSize(8).FontColor(Color.FromHex("#1d4ed8"));
                                    bc.Item().Text($"{donors.Count} Donors Registered & On-Call").FontSize(8).FontColor(Color.FromHex("#1e40af"));
                                    bc.Item().PaddingTop(2).Text($"O+: {donors.Count(d => d.BloodGroup.ToString() == "OPositive")} | A+: {donors.Count(d => d.BloodGroup.ToString() == "APositive")} | B+: {donors.Count(d => d.BloodGroup.ToString() == "BPositive")} | AB+: {donors.Count(d => d.BloodGroup.ToString() == "ABPositive")}")
                                        .FontSize(7).FontColor(Color.FromHex("#3b82f6"));
                                });

                                c.Item().PaddingTop(6).Background(Color.FromHex("#fef2f2")).Border(1).BorderColor(Color.FromHex("#fecaca")).Padding(6).Column(ac =>
                                {
                                    ac.Item().Text("Critical Alert Sentinel").Bold().FontSize(8).FontColor(Color.FromHex("#991b1b"));
                                    ac.Item().Text($"{alerts.Count} Public Health Alerts Dispatched").FontSize(8).FontColor(Color.FromHex("#b91c1c"));
                                });
                            });
                        });

                        // 7. Official Surveillance Recommendations & Digital Sign-off
                        col.Item().PaddingTop(14).Background(Color.FromHex("#f1f5f9")).Padding(8).Column(c =>
                        {
                            c.Item().Text("Epidemiological Action Recommendations (DGHS Dengue Control Cell):")
                                .Bold().FontSize(8).FontColor(Color.FromHex("#0f172a"));
                            c.Item().PaddingTop(2).Text("1. Intensify door-to-door larval destruction and larviciding in designated High-Risk Zones.\n" +
                                                        "2. Expand platelet and IV fluid replenishment buffers in secondary and tertiary referral hospitals.\n" +
                                                        "3. Mobilize community field health workers to address data insufficiency in surveillance blindspots.\n" +
                                                        "4. Transmit automated warning advisories to local upazila administration and clinics.")
                                .FontSize(7.5f).FontColor(Color.FromHex("#334155"));
                        });

                        col.Item().PaddingTop(12).Row(r =>
                        {
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Generated through LarvaX Autonomous Surveillance System").FontSize(7).Italic().FontColor(Color.FromHex("#64748b"));
                                c.Item().Text("DGHS Integrated Disease Surveillance & Response (IDSR) Protocol").FontSize(7).FontColor(Color.FromHex("#94a3b8"));
                            });

                            r.ConstantItem(200).AlignRight().Column(c =>
                            {
                                c.Item().Text("_________________________________").FontSize(8).FontColor(Color.FromHex("#cbd5e1"));
                                c.Item().Text("National Dengue Surveillance Lead").Bold().FontSize(8).FontColor(Color.FromHex("#0f172a"));
                                c.Item().Text("Directorate General of Health Services (DGHS)").FontSize(7.5f).FontColor(Color.FromHex("#64748b"));
                            });
                        });
                    });

                    // FOOTER
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.DefaultTextStyle(s => s.FontSize(8).FontColor(Color.FromHex("#64748b")));
                        x.Span("LarvaX Dengue Surveillance Platform | DGHS Bangladesh | Confidential Official Document | Page ");
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
