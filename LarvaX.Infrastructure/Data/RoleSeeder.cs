using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace LarvaX.Infrastructure.Data
{
    public static class RoleSeeder
    {
        public static async Task SeedRolesAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            
            string[] roleNames = { "Citizen", "Doctor", "HealthWorker", "LabStaff", "Administrator", "GovernmentAuthority" };
            
            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // Seed default Administrator user if not present
            var userManager = serviceProvider.GetRequiredService<UserManager<Core.Entities.ApplicationUser>>();
            var adminEmail = "admin@larvax.gov.bd";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new Core.Entities.ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "System Administrator",
                    PreferredLanguage = "en",
                    ModePreference = "Professional",
                    IsApproved = true,
                    EmailConfirmed = true
                };

                var createResult = await userManager.CreateAsync(adminUser, "Admin@123456");
                if (createResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Administrator");
                }
            }

            // Seed default HealthWorker user if not present
            var workerEmail = "healthworker@larvax.gov.bd";
            var workerUser = await userManager.FindByEmailAsync(workerEmail);
            if (workerUser == null)
            {
                workerUser = new Core.Entities.ApplicationUser
                {
                    UserName = workerEmail,
                    Email = workerEmail,
                    FullName = "Rahim Chowdhury (Field Supervisor)",
                    PreferredLanguage = "en",
                    ModePreference = "Professional",
                    IsApproved = true,
                    EmailConfirmed = true,
                    Specialty = "Community Dengue Surveillance"
                };

                var createWorkerResult = await userManager.CreateAsync(workerUser, "Worker@123456");
                if (createWorkerResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(workerUser, "HealthWorker");
                }
            }

            // Seed default Doctor user if not present
            var doctorEmail = "doctor@larvax.gov.bd";
            var doctorUser = await userManager.FindByEmailAsync(doctorEmail);
            if (doctorUser == null)
            {
                doctorUser = new Core.Entities.ApplicationUser
                {
                    UserName = doctorEmail,
                    Email = doctorEmail,
                    FullName = "Dr. Farhana Yasmin, MBBS, FCPS",
                    PreferredLanguage = "en",
                    ModePreference = "Professional",
                    IsApproved = true,
                    EmailConfirmed = true,
                    Specialty = "Infectious Diseases & Dengue Critical Care"
                };

                var createDocResult = await userManager.CreateAsync(doctorUser, "Doctor@123456");
                if (createDocResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(doctorUser, "Doctor");
                }
            }

            // Seed default LabStaff user if not present
            var labStaffEmail = "labstaff@larvax.gov.bd";
            var labStaffUser = await userManager.FindByEmailAsync(labStaffEmail);
            if (labStaffUser == null)
            {
                labStaffUser = new Core.Entities.ApplicationUser
                {
                    UserName = labStaffEmail,
                    Email = labStaffEmail,
                    FullName = "Shahana Begum (Senior Medical Technologist)",
                    PreferredLanguage = "en",
                    ModePreference = "Professional",
                    IsApproved = true,
                    EmailConfirmed = true,
                    Specialty = "Clinical Pathology & Dengue Serology"
                };

                var createLabResult = await userManager.CreateAsync(labStaffUser, "Lab@123456");
                if (createLabResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(labStaffUser, "LabStaff");
                }
            }

            // Seed default Citizen Patient user if not present
            var patientEmail = "patient@larvax.gov.bd";
            var patientUser = await userManager.FindByEmailAsync(patientEmail);
            if (patientUser == null)
            {
                patientUser = new Core.Entities.ApplicationUser
                {
                    UserName = patientEmail,
                    Email = patientEmail,
                    FullName = "Rahim Uddin",
                    PreferredLanguage = "en",
                    ModePreference = "Citizen",
                    IsApproved = true,
                    EmailConfirmed = true,
                    Address = "Dhanmondi 8/A, Dhaka"
                };

                var createPatientResult = await userManager.CreateAsync(patientUser, "Patient@123456");
                if (createPatientResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(patientUser, "Citizen");
                }
            }

            // Seed initial Inventory Items if empty
            var db = serviceProvider.GetRequiredService<ApplicationDbContext>();

            // Seed starter education content for both supported languages
            if (!db.Articles.Any(a => a.Language == "en"))
            {
                db.Articles.AddRange(
                    new Core.Entities.Article
                    {
                        Title = "Dengue Prevention Starts at Home",
                        Content = "Aedes mosquitoes can breed in small amounts of standing water. Empty buckets, flowerpots, discarded containers, and roof gutters every week. Keep water containers covered, use screens or mosquito nets, and wear clothing that covers your arms and legs. These simple steps help protect your family and your community.",
                        Language = "en",
                        PublishedDate = new DateTime(2026, 9, 1)
                    },
                    new Core.Entities.Article
                    {
                        Title = "Dengue Warning Signs to Watch For",
                        Content = "Most people recover from dengue with rest and careful hydration, but warning signs can appear when the fever begins to fall. Seek urgent medical care for severe abdominal pain, repeated vomiting, bleeding, extreme weakness, difficulty breathing, pale or cold skin, or very little urine. Do not take aspirin or ibuprofen unless a clinician tells you to.",
                        Language = "en",
                        PublishedDate = new DateTime(2026, 9, 2)
                    }
                );
            }

            if (!db.Articles.Any(a => a.Language == "bn"))
            {
                db.Articles.AddRange(
                    new Core.Entities.Article
                    {
                        Title = "বাড়ি থেকেই ডেঙ্গু প্রতিরোধ শুরু করুন",
                        Content = "এডিস মশা অল্প জমা পানিতেও বংশবিস্তার করতে পারে। প্রতি সপ্তাহে বালতি, ফুলের টব, ফেলে রাখা পাত্র এবং ছাদের নালা পরিষ্কার করুন। পানির পাত্র ঢেকে রাখুন, মশারি ব্যবহার করুন এবং শরীর ঢেকে রাখা পোশাক পরুন। এই সহজ পদক্ষেপগুলো পরিবার ও প্রতিবেশীদের সুরক্ষিত রাখতে সাহায্য করে।",
                        Language = "bn",
                        PublishedDate = new DateTime(2026, 9, 1)
                    },
                    new Core.Entities.Article
                    {
                        Title = "ডেঙ্গুর সতর্কতা চিহ্ন চিনুন",
                        Content = "জ্বর কমে যাওয়ার সময়ও ডেঙ্গুতে সতর্কতা চিহ্ন দেখা দিতে পারে। তীব্র পেটব্যথা, বারবার বমি, রক্তপাত, চরম দুর্বলতা, শ্বাসকষ্ট, ফ্যাকাশে বা ঠান্ডা ত্বক এবং প্রস্রাব কমে গেলে দ্রুত চিকিৎসা নিন। চিকিৎসকের পরামর্শ ছাড়া অ্যাসপিরিন বা আইবুপ্রোফেন খাবেন না।",
                        Language = "bn",
                        PublishedDate = new DateTime(2026, 9, 2)
                    }
                );
            }

            if (!db.Quizzes.Any(q => q.Language == "en"))
            {
                db.Quizzes.Add(new Core.Entities.Quiz
                {
                    Title = "Dengue Safety Check",
                    Language = "en",
                    Questions = new List<Core.Entities.QuizQuestion>
                    {
                        new Core.Entities.QuizQuestion
                        {
                            Text = "How often should standing water containers be emptied?",
                            Options = new List<Core.Entities.QuizOption>
                            {
                                new Core.Entities.QuizOption { Text = "Every week", IsCorrect = true },
                                new Core.Entities.QuizOption { Text = "Once a year" },
                                new Core.Entities.QuizOption { Text = "Only after rain" }
                            }
                        },
                        new Core.Entities.QuizQuestion
                        {
                            Text = "Which medicine should generally be avoided in dengue unless advised by a clinician?",
                            Options = new List<Core.Entities.QuizOption>
                            {
                                new Core.Entities.QuizOption { Text = "Aspirin", IsCorrect = true },
                                new Core.Entities.QuizOption { Text = "Oral rehydration salts" },
                                new Core.Entities.QuizOption { Text = "Water" }
                            }
                        },
                        new Core.Entities.QuizQuestion
                        {
                            Text = "What should you do if a dengue warning sign appears?",
                            Options = new List<Core.Entities.QuizOption>
                            {
                                new Core.Entities.QuizOption { Text = "Seek urgent medical care", IsCorrect = true },
                                new Core.Entities.QuizOption { Text = "Wait several more days" },
                                new Core.Entities.QuizOption { Text = "Stop drinking fluids" }
                            }
                        }
                    }
                });
            }

            if (!db.Quizzes.Any(q => q.Language == "bn"))
            {
                db.Quizzes.Add(new Core.Entities.Quiz
                {
                    Title = "ডেঙ্গু নিরাপত্তা যাচাই",
                    Language = "bn",
                    Questions = new List<Core.Entities.QuizQuestion>
                    {
                        new Core.Entities.QuizQuestion
                        {
                            Text = "জমে থাকা পানির পাত্র কত ঘন ঘন খালি করা উচিত?",
                            Options = new List<Core.Entities.QuizOption>
                            {
                                new Core.Entities.QuizOption { Text = "প্রতি সপ্তাহে", IsCorrect = true },
                                new Core.Entities.QuizOption { Text = "বছরে একবার" },
                                new Core.Entities.QuizOption { Text = "শুধু বৃষ্টির পরে" }
                            }
                        },
                        new Core.Entities.QuizQuestion
                        {
                            Text = "চিকিৎসকের পরামর্শ ছাড়া ডেঙ্গুতে কোন ওষুধ এড়ানো উচিত?",
                            Options = new List<Core.Entities.QuizOption>
                            {
                                new Core.Entities.QuizOption { Text = "অ্যাসপিরিন", IsCorrect = true },
                                new Core.Entities.QuizOption { Text = "খাবার স্যালাইন" },
                                new Core.Entities.QuizOption { Text = "পানি" }
                            }
                        },
                        new Core.Entities.QuizQuestion
                        {
                            Text = "ডেঙ্গুর সতর্কতা চিহ্ন দেখা দিলে কী করা উচিত?",
                            Options = new List<Core.Entities.QuizOption>
                            {
                                new Core.Entities.QuizOption { Text = "দ্রুত চিকিৎসা নিন", IsCorrect = true },
                                new Core.Entities.QuizOption { Text = "আরও কয়েক দিন অপেক্ষা করুন" },
                                new Core.Entities.QuizOption { Text = "তরল পান বন্ধ করুন" }
                            }
                        }
                    }
                });
            }

            if (db.ChangeTracker.HasChanges())
            {
                await db.SaveChangesAsync();
            }

            if (!db.InventoryItems.Any())
            {
                db.InventoryItems.AddRange(
                    new Core.Entities.InventoryItem { Name = "IV Fluid (Normal Saline 500ml)", Quantity = 25, Threshold = 30, Location = "Community Health Post A" },
                    new Core.Entities.InventoryItem { Name = "Dengue NS1 Antigen Rapid Test Kits", Quantity = 8, Threshold = 20, Location = "Field Lab & Storage" },
                    new Core.Entities.InventoryItem { Name = "PPE Kits (Personal Protective Equipment)", Quantity = 4, Threshold = 15, Location = "Main Medical Depot" },
                    new Core.Entities.InventoryItem { Name = "Nitrile Medical Gloves (Box 100s)", Quantity = 60, Threshold = 25, Location = "Community Health Post A" },
                    new Core.Entities.InventoryItem { Name = "Surgical Face Masks (Box 50s)", Quantity = 45, Threshold = 20, Location = "Community Health Post A" },
                    new Core.Entities.InventoryItem { Name = "Paracetamol Tablets 500mg (Box 100s)", Quantity = 120, Threshold = 35, Location = "Pharmacy Ward" },
                    new Core.Entities.InventoryItem { Name = "Oral Rehydration Salts (ORS Sachets)", Quantity = 180, Threshold = 50, Location = "Field Supply Kit" },
                    new Core.Entities.InventoryItem { Name = "Mosquito Larvicide Granules (kg)", Quantity = 35, Threshold = 15, Location = "Vector Control Unit" }
                );
                await db.SaveChangesAsync();
            }

            // Seed initial Dengue Cases if empty
            if (!db.DengueCases.Any())
            {
                db.DengueCases.AddRange(
                    new Core.Entities.DengueCase
                    {
                        PatientName = "Tanvir Hasan",
                        PatientPhone = "+8801711223344",
                        PatientAddress = "House 14, Road 5, Dhanmondi, Dhaka",
                        Latitude = 23.7465,
                        Longitude = 90.3760,
                        Age = 28,
                        Gender = "Male",
                        Status = Core.Entities.CaseStatus.Suspected,
                        Severity = Core.Entities.CaseSeverity.Moderate,
                        PlateletCount = 115000,
                        Hematocrit = 41.2,
                        Symptoms = "High fever (103°F), retro-orbital pain, mild rash, body aches",
                        FieldNotes = "Field visited on 24 Sep. Advised bed rest, hydration, and paracetamol only.",
                        NextFollowUpDate = DateTime.UtcNow.AddDays(1),
                        ReportedDate = DateTime.UtcNow.AddDays(-2),
                        UpdatedAt = DateTime.UtcNow.AddDays(-1),
                        AssignedWorkerId = workerUser?.Id
                    },
                    new Core.Entities.DengueCase
                    {
                        PatientName = "Nusrat Jahan",
                        PatientPhone = "+8801812334455",
                        PatientAddress = "Sector 4, Uttara, Dhaka",
                        Latitude = 23.8685,
                        Longitude = 90.3980,
                        Age = 34,
                        Gender = "Female",
                        Status = Core.Entities.CaseStatus.Confirmed,
                        Severity = Core.Entities.CaseSeverity.Severe,
                        PlateletCount = 48000,
                        Hematocrit = 47.8,
                        Symptoms = "Persistent vomiting, severe abdominal pain, gum bleeding, NS1 positive",
                        FieldNotes = "NS1 test positive. Severe thrombocytopenia detected. Urgent clinical escalation recommended.",
                        IsEscalated = true,
                        EscalationReason = "Critically low platelet count (<50k) and warning signs of dengue hemorrhagic fever.",
                        NextFollowUpDate = DateTime.UtcNow.AddHours(12),
                        ReportedDate = DateTime.UtcNow.AddDays(-3),
                        UpdatedAt = DateTime.UtcNow,
                        AssignedWorkerId = workerUser?.Id
                    },
                    new Core.Entities.DengueCase
                    {
                        PatientName = "Rafiqul Islam",
                        PatientPhone = "+8801919887766",
                        PatientAddress = "Mirpur-10, Block C, Dhaka",
                        Latitude = 23.8070,
                        Longitude = 90.3686,
                        Age = 45,
                        Gender = "Male",
                        Status = Core.Entities.CaseStatus.UnderObservation,
                        Severity = Core.Entities.CaseSeverity.Moderate,
                        PlateletCount = 92000,
                        Hematocrit = 43.0,
                        Symptoms = "Joint pains, intermittent fever, nausea, fatigue",
                        FieldNotes = "Fluid intake monitored via oral rehydration salts. Vital signs stable.",
                        NextFollowUpDate = DateTime.UtcNow.AddDays(2),
                        ReportedDate = DateTime.UtcNow.AddDays(-4),
                        UpdatedAt = DateTime.UtcNow.AddDays(-1),
                        AssignedWorkerId = workerUser?.Id
                    },
                    new Core.Entities.DengueCase
                    {
                        PatientName = "Farzana Akhter",
                        PatientPhone = "+8801615554433",
                        PatientAddress = "Lalbagh, Old Dhaka",
                        Latitude = 23.7188,
                        Longitude = 90.3882,
                        Age = 19,
                        Gender = "Female",
                        Status = Core.Entities.CaseStatus.Recovering,
                        Severity = Core.Entities.CaseSeverity.Mild,
                        PlateletCount = 165000,
                        Hematocrit = 38.5,
                        Symptoms = "Fever subsided 48h ago, recovering appetite, mild pruritus",
                        FieldNotes = "Convalescent phase. Platelet count rebounding well. Hydration continued.",
                        NextFollowUpDate = DateTime.UtcNow.AddDays(3),
                        ReportedDate = DateTime.UtcNow.AddDays(-7),
                        UpdatedAt = DateTime.UtcNow.AddDays(-1),
                        AssignedWorkerId = workerUser?.Id
                    },
                    new Core.Entities.DengueCase
                    {
                        PatientName = "Kamrul Hassan",
                        PatientPhone = "+8801512223311",
                        PatientAddress = "Mohakhali Wireless, Dhaka",
                        Latitude = 23.7780,
                        Longitude = 90.4050,
                        Age = 52,
                        Gender = "Male",
                        Status = Core.Entities.CaseStatus.Closed,
                        Severity = Core.Entities.CaseSeverity.Mild,
                        PlateletCount = 210000,
                        Hematocrit = 39.0,
                        Symptoms = "Fully recovered, back to normal daily activities",
                        FieldNotes = "Final follow-up complete. Case closed successfully.",
                        ReportedDate = DateTime.UtcNow.AddDays(-14),
                        UpdatedAt = DateTime.UtcNow.AddDays(-2),
                        AssignedWorkerId = workerUser?.Id
                    }
                );
                await db.SaveChangesAsync();
            }

            // Seed initial HealthWorkerTasks if empty
            if (!db.HealthWorkerTasks.Any())
            {
                db.HealthWorkerTasks.AddRange(
                    new Core.Entities.HealthWorkerTask
                    {
                        Title = "Investigate Citizen Report #102 (Dhanmondi Lake stagnation)",
                        Description = "Inspect standing water accumulation and larva breeding signs reported near lake walkway.",
                        Category = "ReportInvestigation",
                        Priority = Core.Entities.TaskPriority.Urgent,
                        DueDate = DateTime.UtcNow.AddHours(6),
                        IsCompleted = false,
                        HealthWorkerId = workerUser?.Id
                    },
                    new Core.Entities.HealthWorkerTask
                    {
                        Title = "Follow up Patient Nusrat Jahan (Uttara Sector 4)",
                        Description = "Check platelet rebound, fluid intake, and ensure referral admission at Kurmitola General Hospital.",
                        Category = "PatientFollowUp",
                        Priority = Core.Entities.TaskPriority.Urgent,
                        DueDate = DateTime.UtcNow.AddHours(12),
                        IsCompleted = false,
                        HealthWorkerId = workerUser?.Id
                    },
                    new Core.Entities.HealthWorkerTask
                    {
                        Title = "Visit High Risk Area — Zone B (Mirpur-10 Cluster)",
                        Description = "Conduct door-to-door larval surveillance and distribute larvicide granules in water tanks.",
                        Category = "AreaVisit",
                        Priority = Core.Entities.TaskPriority.High,
                        DueDate = DateTime.UtcNow.AddDays(1),
                        IsCompleted = false,
                        HealthWorkerId = workerUser?.Id
                    },
                    new Core.Entities.HealthWorkerTask
                    {
                        Title = "Check Test Kit & IV Fluid Inventory at Health Post",
                        Description = "Re-stock NS1 antigen test kits and Normal Saline bottles currently below safety threshold.",
                        Category = "InventoryCheck",
                        Priority = Core.Entities.TaskPriority.Medium,
                        DueDate = DateTime.UtcNow.AddDays(1),
                        IsCompleted = false,
                        HealthWorkerId = workerUser?.Id
                    },
                    new Core.Entities.HealthWorkerTask
                    {
                        Title = "Community Awareness Session at Mohammadpur Town Hall",
                        Description = "Distribute anti-dengue leaflets and demonstrate mosquito breeding prevention in domestic storage.",
                        Category = "CommunityAwareness",
                        Priority = Core.Entities.TaskPriority.Medium,
                        DueDate = DateTime.UtcNow.AddDays(2),
                        IsCompleted = true,
                        CompletedAt = DateTime.UtcNow.AddHours(-5),
                        HealthWorkerId = workerUser?.Id
                    }
                );
                await db.SaveChangesAsync();
            }

            // Seed initial CaseReferrals if empty
            if (!db.CaseReferrals.Any())
            {
                var severeCase = db.DengueCases.FirstOrDefault(c => c.Severity == Core.Entities.CaseSeverity.Severe);
                db.CaseReferrals.Add(new Core.Entities.CaseReferral
                {
                    DengueCaseId = severeCase?.Id,
                    PatientName = "Nusrat Jahan",
                    PatientPhone = "+8801812334455",
                    Target = Core.Entities.ReferralTarget.Hospital,
                    Urgency = Core.Entities.ReferralUrgency.Emergency,
                    Status = Core.Entities.ReferralStatus.Accepted,
                    Reason = "Dengue Shock Syndrome risk with platelet <50k and severe plasma leakage",
                    ClinicalNotes = "Patient transferred to Kurmitola General Hospital Emergency Ward. Telemedicine consult initiated.",
                    ReferredById = workerUser?.Id,
                    CreatedAt = DateTime.UtcNow.AddHours(-18)
                });
                await db.SaveChangesAsync();
            }

            // Seed initial Lab Tests if empty
            if (!db.LabTests.Any())
            {
                db.LabTests.AddRange(
                    new Core.Entities.LabTest
                    {
                        Name = "Dengue NS1 Antigen Rapid Test",
                        Description = "Early detection of Dengue viral NS1 protein during acute phase (Days 1–5).",
                        Cost = 500.00m,
                        IsAvailable = true
                    },
                    new Core.Entities.LabTest
                    {
                        Name = "Complete Blood Count (CBC with Platelets)",
                        Description = "Automated hematology analyzer count for Platelet count, WBC, RBC, and Hematocrit (HCT).",
                        Cost = 400.00m,
                        IsAvailable = true
                    },
                    new Core.Entities.LabTest
                    {
                        Name = "Dengue IgM & IgG Antibodies (ELISA)",
                        Description = "Serological evaluation for primary vs. secondary acute and convalescent dengue infection.",
                        Cost = 950.00m,
                        IsAvailable = true
                    },
                    new Core.Entities.LabTest
                    {
                        Name = "Serum Electrolytes Panel (Na+, K+, Cl-)",
                        Description = "Evaluation of electrolyte balance crucial in plasma leakage and critical fluid loss.",
                        Cost = 650.00m,
                        IsAvailable = true
                    },
                    new Core.Entities.LabTest
                    {
                        Name = "Liver Function Tests (ALT/SGPT & AST/SGOT)",
                        Description = "Hepatic enzyme profiling for secondary acute dengue hepatitis monitoring.",
                        Cost = 800.00m,
                        IsAvailable = true
                    },
                    new Core.Entities.LabTest
                    {
                        Name = "Dengue Duo Combo (NS1 + IgM/IgG)",
                        Description = "Comprehensive dual-marker rapid diagnostic assay covering acute and convalescent stages.",
                        Cost = 1200.00m,
                        IsAvailable = true
                    }
                );
                await db.SaveChangesAsync();
            }

            // Seed initial Lab Bookings if empty
            if (!db.LabBookings.Any())
            {
                var ns1Test = db.LabTests.FirstOrDefault(t => t.Name.Contains("NS1"));
                var cbcTest = db.LabTests.FirstOrDefault(t => t.Name.Contains("CBC"));
                var igmTest = db.LabTests.FirstOrDefault(t => t.Name.Contains("IgM"));
                var electrolytesTest = db.LabTests.FirstOrDefault(t => t.Name.Contains("Electrolytes"));

                var patient = await userManager.FindByEmailAsync("patient@larvax.gov.bd") 
                              ?? await userManager.FindByEmailAsync("admin@larvax.gov.bd");
                var labStaff = await userManager.FindByEmailAsync("labstaff@larvax.gov.bd");

                if (patient != null && ns1Test != null && cbcTest != null)
                {
                    var today = DateTime.UtcNow.Date;

                    db.LabBookings.AddRange(
                        // 1. Rahim - Dengue NS1 - Sample Taken / SampleCollected
                        new Core.Entities.LabBooking
                        {
                            PatientId = patient.Id,
                            LabTestId = ns1Test.Id,
                            ScheduledAt = today.AddHours(10),
                            Status = "SampleCollected",
                            SampleStatus = "SampleCollected",
                            SampleType = "Venous Blood (EDTA)",
                            BarcodeNumber = "LX-LAB-1021",
                            SampleCollectedAt = today.AddHours(10).AddMinutes(15),
                            CreatedAt = today.AddDays(-1),
                            UpdatedAt = today.AddHours(10).AddMinutes(15),
                            LabStaffId = labStaff?.Id
                        },
                        // 2. Karim - CBC - Processing
                        new Core.Entities.LabBooking
                        {
                            PatientId = patient.Id,
                            LabTestId = cbcTest.Id,
                            ScheduledAt = today.AddHours(10).AddMinutes(30),
                            Status = "Processing",
                            SampleStatus = "Processing",
                            SampleType = "Whole Blood (EDTA)",
                            BarcodeNumber = "LX-LAB-1022",
                            SampleCollectedAt = today.AddHours(10).AddMinutes(35),
                            SampleReceivedAt = today.AddHours(10).AddMinutes(45),
                            ProcessingStartedAt = today.AddHours(11),
                            CreatedAt = today.AddDays(-1),
                            UpdatedAt = today.AddHours(11),
                            LabStaffId = labStaff?.Id
                        },
                        // 3. Sadia - Dengue IgM - Pending
                        new Core.Entities.LabBooking
                        {
                            PatientId = patient.Id,
                            LabTestId = igmTest != null ? igmTest.Id : ns1Test.Id,
                            ScheduledAt = today.AddHours(11),
                            Status = "Pending",
                            SampleStatus = "Pending",
                            SampleType = "Serum (Clot Activator)",
                            BarcodeNumber = "LX-LAB-1023",
                            CreatedAt = today.AddHours(8),
                            UpdatedAt = today.AddHours(8)
                        },
                        // 4. Completed test with result uploaded & values entered
                        new Core.Entities.LabBooking
                        {
                            PatientId = patient.Id,
                            LabTestId = ns1Test.Id,
                            ScheduledAt = today.AddHours(9),
                            Status = "Completed",
                            SampleStatus = "Completed",
                            SampleType = "Venous Blood (EDTA)",
                            BarcodeNumber = "LX-LAB-1024",
                            SampleCollectedAt = today.AddHours(9).AddMinutes(10),
                            SampleReceivedAt = today.AddHours(9).AddMinutes(20),
                            ProcessingStartedAt = today.AddHours(9).AddMinutes(30),
                            CompletedAt = today.AddHours(10),
                            PlateletCount = 88000,
                            WbcCount = 3900,
                            Hematocrit = 44.5,
                            DengueNs1 = "Positive",
                            DengueIgm = "Negative",
                            DengueIgg = "Positive",
                            TestNotes = "Strong NS1 antigen positivity detected. Marked thrombocytopenia (<100k). Hemoconcentration noted. Immediate medical hydration advised.",
                            ResultLink = "/uploads/sample-lab-result.pdf",
                            IsVerified = true,
                            VerifiedBy = "Dr. S. Rahman, Pathologist",
                            VerifiedAt = today.AddHours(10).AddMinutes(15),
                            PatientNotified = true,
                            CreatedAt = today.AddDays(-1),
                            UpdatedAt = today.AddHours(10).AddMinutes(15),
                            LabStaffId = labStaff?.Id
                        },
                        // 5. Booking waiting for upload (Sample Received)
                        new Core.Entities.LabBooking
                        {
                            PatientId = patient.Id,
                            LabTestId = electrolytesTest != null ? electrolytesTest.Id : cbcTest.Id,
                            ScheduledAt = today.AddHours(11).AddMinutes(45),
                            Status = "SampleReceived",
                            SampleStatus = "SampleReceived",
                            SampleType = "Serum (Gel Separator)",
                            BarcodeNumber = "LX-LAB-1025",
                            SampleCollectedAt = today.AddHours(11).AddMinutes(50),
                            SampleReceivedAt = today.AddHours(12),
                            CreatedAt = today.AddHours(9),
                            UpdatedAt = today.AddHours(12)
                        }
                    );
                    await db.SaveChangesAsync();
                }
            }

            // Seed initial FlowAnalytics if empty
            if (!db.FlowAnalytics.Any())
            {
                var now = DateTime.UtcNow;
                var flowEntries = new List<Core.Entities.FlowAnalytics>();

                // CitizenReport flow
                for (int i = 0; i < 140; i++)
                {
                    var sid = $"sess-cr-{i}";
                    var t = now.AddDays(-14).AddMinutes(i * 120);
                    flowEntries.Add(new Core.Entities.FlowAnalytics { FlowName = "CitizenReport", Step = "Started", SessionId = sid, Timestamp = t });
                    if (i < 115) flowEntries.Add(new Core.Entities.FlowAnalytics { FlowName = "CitizenReport", Step = "LocationSelected", SessionId = sid, Timestamp = t.AddSeconds(45) });
                    if (i < 95) flowEntries.Add(new Core.Entities.FlowAnalytics { FlowName = "CitizenReport", Step = "PhotoUploaded", SessionId = sid, Timestamp = t.AddMinutes(2) });
                    if (i < 82) flowEntries.Add(new Core.Entities.FlowAnalytics { FlowName = "CitizenReport", Step = "Completed", SessionId = sid, Timestamp = t.AddMinutes(3) });
                }

                // SymptomChecker flow
                for (int i = 0; i < 210; i++)
                {
                    var sid = $"sess-sc-{i}";
                    var t = now.AddDays(-14).AddMinutes(i * 80);
                    flowEntries.Add(new Core.Entities.FlowAnalytics { FlowName = "SymptomChecker", Step = "Started", SessionId = sid, Timestamp = t });
                    if (i < 185) flowEntries.Add(new Core.Entities.FlowAnalytics { FlowName = "SymptomChecker", Step = "SymptomsSelected", SessionId = sid, Timestamp = t.AddMinutes(1) });
                    if (i < 168) flowEntries.Add(new Core.Entities.FlowAnalytics { FlowName = "SymptomChecker", Step = "ResultsViewed", SessionId = sid, Timestamp = t.AddMinutes(2) });
                    if (i < 155) flowEntries.Add(new Core.Entities.FlowAnalytics { FlowName = "SymptomChecker", Step = "Completed", SessionId = sid, Timestamp = t.AddMinutes(3) });
                }

                // TelemedicineBooking flow
                for (int i = 0; i < 90; i++)
                {
                    var sid = $"sess-tb-{i}";
                    var t = now.AddDays(-14).AddMinutes(i * 180);
                    flowEntries.Add(new Core.Entities.FlowAnalytics { FlowName = "TelemedicineBooking", Step = "Started", SessionId = sid, Timestamp = t });
                    if (i < 65) flowEntries.Add(new Core.Entities.FlowAnalytics { FlowName = "TelemedicineBooking", Step = "DoctorSelected", SessionId = sid, Timestamp = t.AddMinutes(1) });
                    if (i < 52) flowEntries.Add(new Core.Entities.FlowAnalytics { FlowName = "TelemedicineBooking", Step = "SlotChosen", SessionId = sid, Timestamp = t.AddMinutes(2) });
                    if (i < 44) flowEntries.Add(new Core.Entities.FlowAnalytics { FlowName = "TelemedicineBooking", Step = "Completed", SessionId = sid, Timestamp = t.AddMinutes(4) });
                }

                // LabBooking flow
                for (int i = 0; i < 110; i++)
                {
                    var sid = $"sess-lb-{i}";
                    var t = now.AddDays(-14).AddMinutes(i * 150);
                    flowEntries.Add(new Core.Entities.FlowAnalytics { FlowName = "LabBooking", Step = "Started", SessionId = sid, Timestamp = t });
                    if (i < 92) flowEntries.Add(new Core.Entities.FlowAnalytics { FlowName = "LabBooking", Step = "TestSelected", SessionId = sid, Timestamp = t.AddMinutes(1) });
                    if (i < 78) flowEntries.Add(new Core.Entities.FlowAnalytics { FlowName = "LabBooking", Step = "ScheduleSelected", SessionId = sid, Timestamp = t.AddMinutes(2) });
                    if (i < 70) flowEntries.Add(new Core.Entities.FlowAnalytics { FlowName = "LabBooking", Step = "Completed", SessionId = sid, Timestamp = t.AddMinutes(3) });
                }

                db.FlowAnalytics.AddRange(flowEntries);
                await db.SaveChangesAsync();
            }

            // Seed Subscription Plans and Features if empty
            if (!db.SubscriptionPlans.Any())
            {
                var freePlan = new Core.Entities.SubscriptionPlan
                {
                    Name = "Citizen Free",
                    NameBn = "নাগরিক ফ্রি",
                    Description = "Essential community dengue safety, basic triage, and emergency access for all citizens.",
                    DescriptionBn = "সকল নাগরিকের জন্য প্রয়োজনীয় ডেঙ্গু সুরক্ষা, প্রাথমিক ট্রায়াজ এবং জরুরি সেবা।",
                    Tier = Core.Entities.PlanTier.CitizenFree,
                    PriceMonthly = 0m,
                    PriceYearly = 0m,
                    Currency = "BDT",
                    IsActive = true,
                    MaxFamilyMembers = 1,
                    DailyDenAiQuota = 5,
                    AllowSymptomTrends = false,
                    AllowPriorityConsultation = false,
                    AllowOrganizationDashboard = false,
                    AllowAdvancedAnalytics = false,
                    Features = new List<Core.Entities.SubscriptionFeature>
                    {
                        new() { FeatureKey = "DenAiBasic", Name = "5 AI queries/day", NameBn = "দৈনিক ৫টি এআই প্রশ্ন", IsIncluded = true },
                        new() { FeatureKey = "HazardReport", Name = "Citizen hazard reporting", NameBn = "নাগরিক ঝুঁকি রিপোর্টিং", IsIncluded = true },
                        new() { FeatureKey = "FirstAid", Name = "Emergency first aid guidance", NameBn = "জরুরি প্রাথমিক চিকিৎসা নির্দেশিকা", IsIncluded = true },
                        new() { FeatureKey = "RiskAlerts", Name = "Public risk map & zone alerts", NameBn = "পাবলিক রিস্ক ম্যাপ ও জোন সতর্কতা", IsIncluded = true },
                        new() { FeatureKey = "BloodDonorSearch", Name = "Blood donor directory search", NameBn = "রক্তদাতা ডিরেক্টরি অনুসন্ধান", IsIncluded = true },
                        new() { FeatureKey = "IcuBedFinder", Name = "Hospital & ICU bed directory", NameBn = "হাসপাতাল ও আইসিইউ বেড ডিরেক্টরি", IsIncluded = true }
                    }
                };

                var premiumPlan = new Core.Entities.SubscriptionPlan
                {
                    Name = "Citizen Premium",
                    NameBn = "নাগরিক প্রিমিয়াম",
                    Description = "Unlimited AI consultations, multi-member family health tracking, recovery trends, and priority donor alerts.",
                    DescriptionBn = "সীমাহীন এআই পরামর্শ, পরিবারের স্বাস্থ্য ট্র্যাকিং, রিকভারি ট্রেন্ড এবং অগ্রাধিকার রক্তদাতা সতর্কতা।",
                    Tier = Core.Entities.PlanTier.CitizenPremium,
                    PriceMonthly = 199m,
                    PriceYearly = 1999m,
                    Currency = "BDT",
                    IsActive = true,
                    MaxFamilyMembers = 6,
                    DailyDenAiQuota = -1, // Unlimited
                    AllowSymptomTrends = true,
                    AllowPriorityConsultation = true,
                    AllowOrganizationDashboard = false,
                    AllowAdvancedAnalytics = false,
                    Features = new List<Core.Entities.SubscriptionFeature>
                    {
                        new() { FeatureKey = "DenAiUnlimited", Name = "Unlimited DenAI consultations", NameBn = "সীমাহীন ডেন-এআই স্বাস্থ্য পরামর্শ", IsIncluded = true },
                        new() { FeatureKey = "FamilyProfiles", Name = "Up to 6 family member profiles", NameBn = "পরিবারের ৬ জন সদস্যের প্রোফাইল", IsIncluded = true },
                        new() { FeatureKey = "SymptomTrends", Name = "Longitudinal symptom & recovery trends", NameBn = "লক্ষণ ও সুস্থতার ধারা ট্র্যাকিং", IsIncluded = true },
                        new() { FeatureKey = "ExportHealthSummary", Name = "Downloadable clinical health summaries", NameBn = "ডাউনলোডযোগ্য ক্লিনিকাল রিপোর্ট", IsIncluded = true },
                        new() { FeatureKey = "HydrationReminders", Name = "Personalized hydration & medicine schedule", NameBn = "ব্যক্তিগত হাইড্রেশন ও ওষুধ রিমাইন্ডার", IsIncluded = true },
                        new() { FeatureKey = "PriorityAlerts", Name = "Immediate SMS & push outbreak alerts", NameBn = "তাৎক্ষণিক এসএমএস ও পুশ সতর্কতা", IsIncluded = true }
                    }
                };

                var proPlan = new Core.Entities.SubscriptionPlan
                {
                    Name = "Doctor & Clinician Suite",
                    NameBn = "ডাক্তার ও ক্লিনিকাল স্যুট",
                    Description = "Advanced digital triage, telemedicine consult hub, e-referral workflows, and clinical practice insights.",
                    DescriptionBn = "উন্নত ডিজিটাল ট্রায়াজ, টেলিমেডিসিন কনসাল্ট হাব, ই-রেফারাল ও প্র্যাকটিস ইনসাইটস।",
                    Tier = Core.Entities.PlanTier.Professional,
                    PriceMonthly = 799m,
                    PriceYearly = 7999m,
                    Currency = "BDT",
                    IsActive = true,
                    MaxFamilyMembers = 10,
                    DailyDenAiQuota = -1,
                    AllowSymptomTrends = true,
                    AllowPriorityConsultation = true,
                    AllowOrganizationDashboard = false,
                    AllowAdvancedAnalytics = true,
                    Features = new List<Core.Entities.SubscriptionFeature>
                    {
                        new() { FeatureKey = "DoctorTelemedSuite", Name = "High-definition Telemedicine video hub", NameBn = "টেলিমেডিসিন ভিডিও কনসালটেশন হাব", IsIncluded = true },
                        new() { FeatureKey = "DoctorAnalytics", Name = "Clinical caseload & outcome analytics", NameBn = "রোগীর ফলাফল ও কেসলোড অ্যানালিটিক্স", IsIncluded = true },
                        new() { FeatureKey = "PatientRecordsAccess", Name = "Integrated digital health records & lab history", NameBn = "ডিজিটাল স্বাস্থ্য রেকর্ড ও ল্যাব হিস্ট্রি", IsIncluded = true },
                        new() { FeatureKey = "CaseReferrals", Name = "Emergency hospital bed & ICU referral suite", NameBn = "জরুরি রেফারেল ও আইসিইউ কেস সমন্বয়", IsIncluded = true },
                        new() { FeatureKey = "FluidCalculator", Name = "WHO / National protocol IV fluid calculator", NameBn = "জাতীয় গাইডলাইন আইভি ফ্লুইড ক্যালকুলেটর", IsIncluded = true }
                    }
                };

                var orgPlan = new Core.Entities.SubscriptionPlan
                {
                    Name = "Healthcare Organization / Lab",
                    NameBn = "স্বাস্থ্য সংস্থা ও ল্যাবরেটরি",
                    Description = "Enterprise multi-seat laboratory management, sample barcodes, outbreak surveillance integration, and priority support.",
                    DescriptionBn = "মাল্টি-ইউজার ল্যাব ব্যবস্থাপনা, বারকোড ট্র্যাকিং, মহামারি নজরদারি ইন্টিগ্রেশন।",
                    Tier = Core.Entities.PlanTier.Institutional,
                    PriceMonthly = 2499m,
                    PriceYearly = 24999m,
                    Currency = "BDT",
                    IsActive = true,
                    MaxFamilyMembers = 50,
                    DailyDenAiQuota = -1,
                    AllowSymptomTrends = true,
                    AllowPriorityConsultation = true,
                    AllowOrganizationDashboard = true,
                    AllowAdvancedAnalytics = true,
                    Features = new List<Core.Entities.SubscriptionFeature>
                    {
                        new() { FeatureKey = "OrgDashboard", Name = "Enterprise multi-seat lab portal", NameBn = "এন্টারপ্রাইজ মাল্টি-সিট ল্যাব পোর্টাল", IsIncluded = true },
                        new() { FeatureKey = "BarcodeTracking", Name = "Digital sample accession & barcode tracking", NameBn = "ডিজিটাল স্যাম্পল বারকোড ট্র্যাকিং", IsIncluded = true },
                        new() { FeatureKey = "EpiSurveillance", Name = "Regional outbreak surveillance feed integration", NameBn = "আঞ্চলিক প্রাদুর্ভাব নজরদারি ফিড", IsIncluded = true },
                        new() { FeatureKey = "PrioritySupport", Name = "24/7 dedicated technical support & SLA", NameBn = "২৪/৭ বিশেষায়িত টেকনিক্যাল সাপোর্ট", IsIncluded = true }
                    }
                };

                db.SubscriptionPlans.AddRange(freePlan, premiumPlan, proPlan, orgPlan);
                await db.SaveChangesAsync();
            }

            // Seed Promo Coupons if empty
            if (!db.Coupons.Any())
            {
                db.Coupons.AddRange(
                    new Core.Entities.Coupon
                    {
                        Code = "LARVAXFREE",
                        DiscountPercent = 100m,
                        ValidUntil = DateTime.UtcNow.AddYears(1),
                        IsActive = true,
                        MaxUses = 500,
                        TimesUsed = 0
                    },
                    new Core.Entities.Coupon
                    {
                        Code = "HEALTH20",
                        DiscountPercent = 20m,
                        ValidUntil = DateTime.UtcNow.AddYears(1),
                        IsActive = true,
                        MaxUses = 1000,
                        TimesUsed = 0
                    },
                    new Core.Entities.Coupon
                    {
                        Code = "DENGUE50",
                        DiscountPercent = 50m,
                        ValidUntil = DateTime.UtcNow.AddYears(1),
                        IsActive = true,
                        MaxUses = 500,
                        TimesUsed = 0
                    }
                );
                await db.SaveChangesAsync();
            }
        }
    }
}
