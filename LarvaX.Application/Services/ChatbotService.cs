using System;
using System.Collections.Generic;
using System.Linq;
using LarvaX.Application.ML;
using LarvaX.Core.Interfaces;
using LarvaX.Core.ML;

namespace LarvaX.Application.Services
{
    /// <summary>
    /// DenAI — ML-Powered Bilingual Conversational Health Assistant for the LarvaX Dengue Platform.
    ///
    /// Intent detection is driven by an ML.NET TF-IDF + SDCA Multiclass classifier trained
    /// entirely in-process from a 400+ sample bilingual corpus (English + Bangla).
    /// No API key, no external service. The model is injected as a singleton and shared
    /// via a thread-safe PredictionEngine.
    ///
    /// Safety overrides always take precedence over the ML classifier:
    ///   Emergency keyword matching → immediately escalates, bypassing ML routing.
    /// </summary>
    public class ChatbotService : IChatbotService
    {
        private readonly DenAIIntentClassifier _classifier;

        // Hard safety keywords that bypass ML classification — clinical safety boundary.
        private static readonly string[] EmergencyKeywords = {
            "bleeding", "unconscious", "shock", "cannot breathe", "faint", "collapse",
            "blood in urine", "blood in stool", "severe vomiting", "difficulty breathing",
            "persistent vomiting", "mucosal bleed", "plasma leakage", "dss", "dhf", "cold skin",
            "vomiting blood", "coughing blood", "unresponsive",
            // Bangla safety terms
            "রক্ত বমি", "জ্ঞান হারিয়ে", "শ্বাস নিতে পারছি না", "অজ্ঞান", "রক্তপাত",
            "শ্বাসকষ্ট", "তীব্র পেটে ব্যথা", "ক্রমাগত বমি", "নিস্তেজ", "শক", "রক্তবমি"
        };

        public ChatbotService(DenAIIntentClassifier classifier)
        {
            _classifier = classifier;
        }

        public ChatbotResponse GetResponse(string message, string? language)
            => GetResponse(message, language, null);

        public ChatbotResponse GetResponse(string message, string? language, string? contextState)
        {
            bool isBangla = string.Equals(language, "bn", StringComparison.OrdinalIgnoreCase);

            // ── Empty message ────────────────────────────────────────────────────────
            if (string.IsNullOrWhiteSpace(message))
            {
                return new ChatbotResponse
                {
                    Reply = isBangla
                        ? "অনুগ্রহ করে একটি প্রশ্ন লিখুন। যেমন: 'ডেঙ্গুর প্রধান লক্ষণগুলো কী?' বা 'রক্তদাতার সন্ধান চাই'।"
                        : "Please enter a message or question. For example: 'What are dengue symptoms?' or 'Find blood donors'.",
                    DetectedIntent = DenAIIntents.Greeting,
                    ModelSource = "Safety",
                    SuggestedQuestions = GetDefaultSuggestions(isBangla)
                };
            }

            string cleanMsg = message.Trim();
            string lowerMsg = cleanMsg.ToLowerInvariant();

            // ── PRIORITY 1: Hard safety override — Emergency keywords ─────────────────
            bool isEmergency = EmergencyKeywords.Any(k => lowerMsg.Contains(k));
            if (isEmergency)
            {
                return BuildEmergencyResponse(isBangla, "Safety");
            }

            // ── PRIORITY 2: Multi-turn context continuation ───────────────────────────
            if (!string.IsNullOrEmpty(contextState))
            {
                var contextResponse = HandleContextualFlow(cleanMsg, lowerMsg, isBangla, contextState);
                if (contextResponse != null) return contextResponse;
            }

            // ── PRIORITY 3: ML.NET Intent Classification ──────────────────────────────
            string predictedIntent = _classifier.Predict(lowerMsg);

            // Multi-turn fever flow triggered by ML FeverManagement intent
            // when the user hasn't given enough context (short message without days/medicine info).
            if (predictedIntent == DenAIIntents.FeverManagement &&
                !lowerMsg.Contains("day") && !lowerMsg.Contains("দিন") &&
                !lowerMsg.Contains("medicine") && !lowerMsg.Contains("ওষুধ") &&
                lowerMsg.Split(' ').Length < 8)
            {
                return new ChatbotResponse
                {
                    Reply = isBangla
                        ? "ডেঙ্গু জ্বরে হঠাৎ তীব্র তাপমাত্রা (১০৪°F পর্যন্ত), চোখের পেছনে তীব্র ব্যথা এবং শরীরে প্রচণ্ড ব্যথা হতে পারে। চিকিৎসকের পরামর্শ ব্যতীত এসপিরিন বা আইবুপ্রোফেন খাবেন না — শুধুমাত্র প্যারাসিটামল গ্রহণ করুন।\n\nআপনার জ্বর কত দিন ধরে চলছে?"
                        : "Dengue fever typically presents with sudden high fever (up to 104°F), retro-orbital eye pain, and severe muscle/joint aches. Avoid Aspirin or Ibuprofen — take only Paracetamol and stay well-hydrated.\n\nHow many days have you had the fever?",
                    DetectedIntent = DenAIIntents.FeverManagement,
                    ContextState = "awaiting_fever_days",
                    ModelSource = "ML",
                    ActionButtons = new List<ChatActionButton>
                    {
                        new() { Label = isBangla ? "উপসর্গ মূল্যায়ন" : "Check Symptoms", Url = "/SymptomChecker", Icon = "bi-clipboard2-pulse", ButtonClass = "btn-primary" },
                        new() { Label = isBangla ? "ডাক্তার পরামর্শ" : "Consult Doctor", Url = "/Telemedicine", Icon = "bi-camera-video", ButtonClass = "btn-outline-primary" }
                    },
                    SuggestedQuestions = isBangla
                        ? new List<string> { "১ দিন", "৩ দিন", "৫ দিনের বেশি", "আমার বমি ভাব হচ্ছে" }
                        : new List<string> { "1 day", "3 days", "More than 5 days", "I also have nausea" }
                };
            }

            // ── Route by ML-predicted intent ─────────────────────────────────────────
            return BuildResponseForIntent(predictedIntent, lowerMsg, isBangla);
        }

        // ─────────────────────────────────────────────────────────────────────────────
        // MULTI-TURN CONTEXT HANDLER
        // ─────────────────────────────────────────────────────────────────────────────
        private ChatbotResponse? HandleContextualFlow(string cleanMsg, string lowerMsg, bool isBangla, string contextState)
        {
            if (contextState == "awaiting_fever_days")
            {
                bool hasDays = lowerMsg.Contains("day") || lowerMsg.Contains("দিন") ||
                               System.Text.RegularExpressions.Regex.IsMatch(lowerMsg, @"\b[1-9]\b") ||
                               lowerMsg.Contains("১") || lowerMsg.Contains("২") || lowerMsg.Contains("৩") ||
                               lowerMsg.Contains("৪") || lowerMsg.Contains("৫") || lowerMsg.Contains("৬") || lowerMsg.Contains("৭");

                if (hasDays)
                {
                    return new ChatbotResponse
                    {
                        Reply = isBangla
                            ? "ধন্যবাদ। ডেঙ্গুর ৩য় থেকে ৭ম দিনকে 'ক্রিটিকাল ফেজ' বলা হয়, যখন জ্বর কমলেও প্লাজমা লিকেজ ও জটিলতা দেখা দিতে পারে।\n\nআপনার কি কোনো বিপদের লক্ষণ আছে — যেমন: তীব্র পেটে ব্যথা, বারবার বমি, দাঁতের মাড়ি বা নাক দিয়ে রক্তপাত, অতিরিক্ত দুর্বলতা বা শ্বাসকষ্ট?"
                            : "Thank you. Days 3–7 of dengue are the 'Critical Phase' — fever may subside but risks of plasma leakage and complications increase significantly.\n\nDo you currently have any warning signs such as severe abdominal pain, persistent vomiting, bleeding gums/nose, extreme lethargy, or difficulty breathing?",
                        DetectedIntent = "WarningSignsCheck",
                        ContextState = "awaiting_warning_signs",
                        ModelSource = "ML",
                        ActionButtons = new List<ChatActionButton>
                        {
                            new() { Label = isBangla ? "উপসর্গ মূল্যায়ন" : "Symptom Assessment", Url = "/SymptomChecker", Icon = "bi-clipboard2-pulse", ButtonClass = "btn-primary" },
                            new() { Label = isBangla ? "জরুরি ফার্স্ট এইড" : "First Aid Guide", Url = "/FirstAid", Icon = "bi-heart-pulse", ButtonClass = "btn-outline-danger" }
                        },
                        SuggestedQuestions = isBangla
                            ? new List<string> { "হ্যাঁ, রক্তপাত হচ্ছে", "না, কোনো বিপদের লক্ষণ নেই", "প্লেটলেট পরীক্ষা কোথায়?", "কী খাবার খাওয়া উচিত?" }
                            : new List<string> { "Yes, I have bleeding", "No warning signs", "Where to get platelet test?", "What food should I take?" }
                    };
                }
            }
            else if (contextState == "awaiting_warning_signs")
            {
                bool hasWarning = lowerMsg.Contains("yes") || lowerMsg.Contains("হ্যাঁ") ||
                                  lowerMsg.Contains("আছে") || lowerMsg.Contains("vomit") ||
                                  lowerMsg.Contains("bleed") || lowerMsg.Contains("pain") ||
                                  lowerMsg.Contains("বমি") || lowerMsg.Contains("রক্ত") || lowerMsg.Contains("ব্যথা");

                if (hasWarning)
                    return BuildEmergencyResponse(isBangla, "ML");

                return new ChatbotResponse
                {
                    Reply = isBangla
                        ? "স্বস্তির বিষয় যে কোনো জরুরি বিপদের লক্ষণ নেই। পর্যাপ্ত বিশ্রাম নিন, প্রতি কেজি ওজনের জন্য পর্যাপ্ত তরল (ওআরএস, ডাবের পানি, সুপ) গ্রহণ করুন এবং তাপমাত্রা প্রতি ৪-৬ ঘণ্টা পরপর পর্যবেক্ষণ করুন। শুধু প্যারাসিটামল সেবন করুন। আমাদের সিম্পটম চেকার সম্পূর্ণ করুন।"
                        : "It's encouraging that there are no acute warning signs. Rest, drink plenty of fluids (ORS, coconut water, soups), and monitor temperature every 4–6 hours. Take only Paracetamol — never Aspirin or NSAIDs. Please complete our formal Symptom Checker for a structured risk score.",
                    DetectedIntent = "FeverManagement",
                    ModelSource = "ML",
                    ActionButtons = new List<ChatActionButton>
                    {
                        new() { Label = isBangla ? "সিম্পটম চেকার" : "Run Symptom Checker", Url = "/SymptomChecker", Icon = "bi-clipboard2-pulse", ButtonClass = "btn-success" },
                        new() { Label = isBangla ? "ডাক্তার পরামর্শ" : "Doctor Consultation", Url = "/Telemedicine", Icon = "bi-person-badge", ButtonClass = "btn-outline-primary" },
                        new() { Label = isBangla ? "ল্যাব টেস্ট বুক" : "Book Lab Test", Url = "/Lab", Icon = "bi-droplet-half", ButtonClass = "btn-outline-secondary" }
                    },
                    SuggestedQuestions = isBangla
                        ? new List<string> { "ডেঙ্গুতে কী খাবার উপকারী?", "প্লেটলেট কমলে করণীয়?", "এডিস মশা প্রতিরোধ?" }
                        : new List<string> { "What foods aid recovery?", "What if platelets drop?", "How to prevent dengue at home?" }
                };
            }

            return null;
        }

        // ─────────────────────────────────────────────────────────────────────────────
        // ML INTENT → RESPONSE ROUTER
        // ─────────────────────────────────────────────────────────────────────────────
        private ChatbotResponse BuildResponseForIntent(string intent, string lowerMsg, bool isBangla)
        {
            return intent switch
            {
                DenAIIntents.Emergency => BuildEmergencyResponse(isBangla, "ML"),

                DenAIIntents.FeverManagement => new ChatbotResponse
                {
                    Reply = isBangla
                        ? "ডেঙ্গু জ্বরে সাধারণত তীব্র হঠাৎ জ্বর (১০৪°F পর্যন্ত), চোখের পেছনে ব্যথা এবং শরীরে প্রচণ্ড ব্যথা হয়। চিকিৎসকের পরামর্শ ব্যতীত এসপিরিন বা আইবুপ্রোফেন খাবেন না — শুধুমাত্র প্যারাসিটামল গ্রহণ করুন এবং প্রচুর তরল পান করুন।"
                        : "Dengue fever typically presents with sudden high fever (up to 104°F), retro-orbital eye pain, and severe muscle/joint aches. Avoid Aspirin or Ibuprofen — take only Paracetamol and stay well-hydrated.",
                    DetectedIntent = DenAIIntents.FeverManagement,
                    ModelSource = "ML",
                    ActionButtons = new List<ChatActionButton>
                    {
                        new() { Label = isBangla ? "সিম্পটম চেকার" : "Check Symptoms", Url = "/SymptomChecker", Icon = "bi-clipboard2-pulse", ButtonClass = "btn-primary" },
                        new() { Label = isBangla ? "ডাক্তার পরামর্শ" : "Consult Doctor", Url = "/Telemedicine", Icon = "bi-camera-video", ButtonClass = "btn-outline-primary" }
                    },
                    SuggestedQuestions = isBangla
                        ? new List<string> { "কত দিন জ্বর থাকলে টেস্ট করাব?", "বিপদের লক্ষণ কী?", "খাবার তালিকা" }
                        : new List<string> { "When should I get lab test?", "What are warning signs?", "Recommended diet" }
                },

                DenAIIntents.Symptoms => new ChatbotResponse
                {
                    Reply = isBangla
                        ? "ডেঙ্গুর প্রধান উপসর্গসমূহ: তীব্র জ্বর, তীব্র মাথাব্যথা, চোখের পেছনে ব্যথা, জয়েন্টে ব্যথা, বমি বমি ভাব এবং ত্বকে লালচে র‍্যাশ। রক্তক্ষরণ দেখা দিলে তাৎক্ষণিক হাসপাতালে যান। আপনার উপসর্গগুলোর সঠিক ঝুঁকি পর্যালোচনার জন্য আমাদের সিম্পটম চেকার ব্যবহার করুন।"
                        : "Key symptoms include: acute high fever, severe headache, retro-orbital eye pain, intense joint/muscle aches, nausea, and skin rash. Watch for warning signs like persistent vomiting or mucosal bleeding. Please complete our Symptom Checker for an automated risk classification.",
                    DetectedIntent = DenAIIntents.Symptoms,
                    ModelSource = "ML",
                    ActionButtons = new List<ChatActionButton>
                    {
                        new() { Label = isBangla ? "সিম্পটম চেকার শুরু" : "Start Symptom Checker", Url = "/SymptomChecker", Icon = "bi-clipboard2-pulse", ButtonClass = "btn-primary" },
                        new() { Label = isBangla ? "বিপদের লক্ষণ" : "Warning Signs", Url = "/FirstAid/Guide?type=dengue", Icon = "bi-exclamation-triangle", ButtonClass = "btn-outline-warning" }
                    },
                    SuggestedQuestions = isBangla
                        ? new List<string> { "ডেঙ্গুর বিপদের লক্ষণ কী?", "জ্বর হলে কী ওষুধ?", "ল্যাব টেস্ট কখন?" }
                        : new List<string> { "What are the warning signs?", "What medicine for fever?", "When to get lab test?" }
                },

                DenAIIntents.WarningSigns => new ChatbotResponse
                {
                    Reply = isBangla
                        ? "ডেঙ্গুর ৭টি প্রধান বিপদের লক্ষণ:\n১. তীব্র ও ক্রমাগত পেটে ব্যথা\n২. ২৪ ঘণ্টায় ৩ বারের বেশি বমি\n৩. নাক, মাড়ি বা ত্বকের নিচে রক্তক্ষরণ\n৪. রক্তবমি বা কালো পায়খানা\n৫. অতিরিক্ত দুর্বলতা, অস্থিরতা বা নিস্তেজতা\n৬. পেটে বা ফুসফুসে পানি জমা\n৭. হঠাৎ তাপমাত্রা স্বাভাবিক বা নিচে — হাত-পা ঠান্ডা।\nএদের যেকোনো একটি দেখা দিলে অবিলম্বে হাসপাতালে ভর্তি হতে হবে।"
                        : "Dengue Warning Signs requiring immediate hospital care:\n1. Severe, persistent abdominal pain\n2. Persistent vomiting (≥3 times in 24 hours)\n3. Bleeding from gums, nose, or petechiae on skin\n4. Vomiting blood or black stools\n5. Extreme lethargy, restlessness, or altered mental status\n6. Fluid accumulation causing breathing difficulty\n7. Sudden temperature drop with cold, clammy extremities.\nAny one of these requires immediate emergency care.",
                    DetectedIntent = DenAIIntents.WarningSigns,
                    ModelSource = "ML",
                    ActionButtons = new List<ChatActionButton>
                    {
                        new() { Label = isBangla ? "জরুরি ফার্স্ট এইড" : "First Aid Guide", Url = "/FirstAid", Icon = "bi-heart-pulse", ButtonClass = "btn-danger" },
                        new() { Label = isBangla ? "আইসিইউ বেড দেখুন" : "Find ICU Beds", Url = "/IcuBeds", Icon = "bi-hospital", ButtonClass = "btn-outline-danger" },
                        new() { Label = isBangla ? "কল ৯৯৯" : "Call 999", Url = "tel:999", Icon = "bi-telephone-fill", ButtonClass = "btn-outline-dark" }
                    },
                    SuggestedQuestions = isBangla
                        ? new List<string> { "ডেঙ্গু শকে কী করণীয়?", "রক্ত বা প্লেটলেট দরকার", "ডাক্তার যোগাযোগ" }
                        : new List<string> { "What to do during dengue shock?", "I need blood or platelets", "Contact a doctor" }
                },

                DenAIIntents.DoctorRouting => new ChatbotResponse
                {
                    Reply = isBangla
                        ? "আমাদের টেলিমেডিসিন পোর্টালে নিবন্ধিত ডাক্তারের সাথে ভিডিও কনসাল্টেশন বুক করতে পারেন অথবা নিকটস্থ হাসপাতালে যেতে পারেন। ডেঙ্গু জ্বরে নিজে নিজে অ্যান্টিবায়োটিক বা ব্যথানাশক গ্রহণ না করে চিকিৎসকের পরামর্শ নেওয়া জরুরি।"
                        : "You can book a telemedicine video consultation with verified doctors through our platform, or visit the nearest healthcare facility. In dengue, avoiding self-medication and getting professional guidance is essential.",
                    DetectedIntent = DenAIIntents.DoctorRouting,
                    ModelSource = "ML",
                    ActionButtons = new List<ChatActionButton>
                    {
                        new() { Label = isBangla ? "ডাক্তার বুক করুন" : "Consult a Doctor", Url = "/Telemedicine", Icon = "bi-camera-video", ButtonClass = "btn-primary" },
                        new() { Label = isBangla ? "ডাক্তার তালিকা" : "Browse Doctors", Url = "/Telemedicine/Doctors", Icon = "bi-person-lines-fill", ButtonClass = "btn-outline-primary" }
                    },
                    SuggestedQuestions = isBangla
                        ? new List<string> { "ল্যাব টেস্ট কীভাবে বুক করব?", "ডেঙ্গুর বিপদের লক্ষণ কী?", "কখন হাসপাতালে ভর্তি হতে হবে?" }
                        : new List<string> { "How to book a dengue lab test?", "What are dengue warning signs?", "When is hospital admission required?" }
                },

                DenAIIntents.LabTest => new ChatbotResponse
                {
                    Reply = isBangla
                        ? "ডেঙ্গু শনাক্তকরণে জ্বরের প্রথম ১–৫ দিনের মধ্যে 'NS1 Antigen' টেস্ট এবং ৫ম দিনের পর 'Dengue IgM/IgG Antibody' টেস্ট অত্যন্ত কার্যকর। সাথে রক্তের CBC (সিবিসি) ও প্লেটলেট কাউন্ট নিয়মিত মনিটর করা উচিত। আমাদের ল্যাব বুকিং সুবিধায় অনুমোদিত ল্যাবে টেস্ট বুক করতে পারেন।"
                        : "For dengue diagnosis: NS1 Antigen test is recommended within days 1–5 of fever onset; after day 5, Dengue IgM/IgG antibody tests are standard. A Complete Blood Count (CBC) with Hematocrit and Platelet count should be monitored regularly.",
                    DetectedIntent = DenAIIntents.LabTest,
                    ModelSource = "ML",
                    ActionButtons = new List<ChatActionButton>
                    {
                        new() { Label = isBangla ? "ডেঙ্গু টেস্ট বুক" : "Book Dengue Lab Test", Url = "/Lab/Book", Icon = "bi-calendar-check", ButtonClass = "btn-primary" },
                        new() { Label = isBangla ? "অনুমোদিত ল্যাব" : "Browse Verified Labs", Url = "/Lab", Icon = "bi-building-check", ButtonClass = "btn-outline-primary" }
                    },
                    SuggestedQuestions = isBangla
                        ? new List<string> { "প্লেটলেট কত হলে বিপজ্জনক?", "ডাক্তারের সাথে কথা বলতে চাই", "রক্তদাতার খোঁজ" }
                        : new List<string> { "What is a dangerous platelet level?", "I want to talk to a doctor", "I need blood donors" }
                },

                DenAIIntents.BloodDonor => new ChatbotResponse
                {
                    Reply = isBangla
                        ? "প্লেটলেট ২০,০০০ এর নিচে নেমে গেলে বা রক্তক্ষরণ শুরু হলে দ্রুত হাসপাতালে রক্ত/প্লেটলেট ট্রান্সফিউশন প্রয়োজন হতে পারে। আমাদের রক্তদাতা নেটওয়ার্ক ব্যবহার করুন। সেখানে সক্রিয় রক্তদাতাদের ফোন নম্বর ও শেষ সক্রিয়তার তথ্য রয়েছে।"
                        : "A rapid drop in platelets or active bleeding requires urgent medical evaluation and possibly transfusion. Use our Blood Donor Network to locate verified active donors nearby — it includes real-time freshness timestamps.",
                    DetectedIntent = DenAIIntents.BloodDonor,
                    ModelSource = "ML",
                    ActionButtons = new List<ChatActionButton>
                    {
                        new() { Label = isBangla ? "রক্তদাতা খুঁজুন" : "Find Blood Donors", Url = "/Donors", Icon = "bi-droplet-fill", ButtonClass = "btn-danger" },
                        new() { Label = isBangla ? "আইসিইউ বেড দেখুন" : "Check ICU Beds", Url = "/IcuBeds", Icon = "bi-hospital", ButtonClass = "btn-outline-danger" }
                    },
                    SuggestedQuestions = isBangla
                        ? new List<string> { "আইসিইউ বেড কোথায়?", "ডাক্তার দেখাতে চাই", "ফার্স্ট এইড নির্দেশিকা" }
                        : new List<string> { "Where to find ICU beds?", "Book doctor consultation", "First aid emergency guide" }
                },

                DenAIIntents.IcuBed => new ChatbotResponse
                {
                    Reply = isBangla
                        ? "মারাত্মক ডেঙ্গু (ডেঙ্গু শক সিন্ড্রোম / হেমোরেজিক ফিভার) হলে রোগীর রক্তচাপ অস্বাভাবিক কমে যেতে পারে এবং জরুরি আইসিইউ বেড প্রয়োজন হতে পারে। আমাদের প্ল্যাটফর্মে বিভিন্ন হাসপাতালের রিয়েল-টাইম আইসিইউ বেড প্রাপ্যতা যাচাই করতে পারেন।"
                        : "Severe dengue (Dengue Shock Syndrome / DHF) can trigger circulatory failure and plasma leakage requiring intensive care. Browse real-time ICU bed availability across regional hospitals using our live ICU locator.",
                    DetectedIntent = DenAIIntents.IcuBed,
                    ModelSource = "ML",
                    ActionButtons = new List<ChatActionButton>
                    {
                        new() { Label = isBangla ? "আইসিইউ বেড সন্ধান" : "Search ICU Beds", Url = "/IcuBeds", Icon = "bi-hospital-fill", ButtonClass = "btn-danger" },
                        new() { Label = isBangla ? "জরুরি ৯৯৯ কল" : "Call 999", Url = "tel:999", Icon = "bi-telephone-fill", ButtonClass = "btn-outline-danger" }
                    },
                    SuggestedQuestions = isBangla
                        ? new List<string> { "জরুরি রক্তদাতা", "ডেঙ্গু শকের লক্ষণ কী?", "টেলিমেডিসিন ডাক্তার" }
                        : new List<string> { "Find blood donors", "Symptoms of dengue shock", "Consult telemedicine doctor" }
                },

                DenAIIntents.Prevention => new ChatbotResponse
                {
                    Reply = isBangla
                        ? "মশা থেকে সুরক্ষিত থাকতে: প্রতি ৩ দিনে একবার জমা পানি ফেলে দিন (টব, টায়ার, বালতি, এসি ট্রে), মশারি টাঙিয়ে ঘুমান এবং সকালে ও বিকেলে ফুল-হাতা জামা পরিধান করুন। এডিস মশা মূলত পরিষ্কার জমা পানিতে ডিম পাড়ে এবং দিনের বেলায় বেশি কামড়ায়। আপনার এলাকায় লার্ভার প্রজননস্থল দেখলে সিটিজেন রিপোর্টে জানান।"
                        : "To prevent dengue: eliminate standing water every 3 days (flower pots, tires, AC drain trays), sleep under mosquito bed nets, and apply mosquito repellent. Aedes mosquitoes breed in clean stagnant water and bite mainly during early morning and late afternoon. You can report breeding spots via our Citizen Hazard Report.",
                    DetectedIntent = DenAIIntents.Prevention,
                    ModelSource = "ML",
                    ActionButtons = new List<ChatActionButton>
                    {
                        new() { Label = isBangla ? "লার্ভার স্থান রিপোর্ট" : "Report Breeding Site", Url = "/Reports/CitizenReport", Icon = "bi-geo-alt", ButtonClass = "btn-warning" },
                        new() { Label = isBangla ? "সচেতনতা কুইজ" : "Dengue Quiz", Url = "/Education/Quiz", Icon = "bi-question-circle", ButtonClass = "btn-outline-success" }
                    },
                    SuggestedQuestions = isBangla
                        ? new List<string> { "ডেঙ্গু ঝুঁকিপূর্ণ এলাকা", "ডেঙ্গুর উপসর্গ কী?", "জ্বর হলে কী করব?" }
                        : new List<string> { "Which areas are high risk?", "What are dengue symptoms?", "What to do with fever?" }
                },

                DenAIIntents.NutritionDiet => new ChatbotResponse
                {
                    Reply = isBangla
                        ? "ডেঙ্গু চলাকালীন ও সুস্থতার জন্য:\n১. হাইড্রেশন সবচেয়ে জরুরি — ওআরএস স্যালাইন, ডাবের পানি, তাজা ফলের রস ও সুপ প্রচুর পরিমাণে পান করুন।\n২. সহজপাচ্য পুষ্টিকর খাবার — নরম খিচুড়ি, জাউ ভাত, সবজি সুপ, ডিম।\n৩. ভিটামিন সি সমৃদ্ধ ফল — কমলা, মাল্টা, আমলকী।\n৪. চকলেট বা গাঢ় রঙের পানীয় এড়িয়ে চলুন (রক্তবমির সাথে বিভ্রান্তি এড়াতে)।\n৫. চিকিৎসকের পরামর্শ ছাড়া ভেষজ মিশ্রণ অতিরিক্ত মাত্রায় খাবেন না।"
                        : "Dengue Nutritional & Hydration Guidance:\n1. Hydration is vital — drink plenty of ORS salts, green coconut water, clear soups, and citrus juices.\n2. Light digestible foods — khichuri, porridge, boiled vegetables, soft-boiled eggs.\n3. Vitamin C rich fruits — oranges, papayas, amla support immune recovery.\n4. Avoid dark-colored foods/drinks to avoid masking gastrointestinal bleeding signs.\n5. Maintain continuous fluid intake to offset plasma leakage.",
                    DetectedIntent = DenAIIntents.NutritionDiet,
                    ModelSource = "ML",
                    ActionButtons = new List<ChatActionButton>
                    {
                        new() { Label = isBangla ? "ফ্লুইড ক্যালকুলেটর" : "IV Fluid Calculator", Url = "/FluidManagement", Icon = "bi-calculator", ButtonClass = "btn-info text-white" },
                        new() { Label = isBangla ? "সিম্পটম চেকার" : "Symptom Checker", Url = "/SymptomChecker", Icon = "bi-clipboard2-pulse", ButtonClass = "btn-outline-primary" }
                    },
                    SuggestedQuestions = isBangla
                        ? new List<string> { "জ্বরের সময় কোন ওষুধ নিষিদ্ধ?", "প্লেটলেট বাড়ানোর উপায়?", "কখন ডাক্তার দেখাব?" }
                        : new List<string> { "Which medicine is prohibited?", "How to support platelet count?", "When should I see a doctor?" }
                },

                DenAIIntents.RiskArea => new ChatbotResponse
                {
                    Reply = isBangla
                        ? "আমাদের রিয়েল-টাইম ডেঙ্গু নজরদারি ড্যাশবোর্ডে ঢাকাসহ দেশের অন্যান্য অঞ্চলের জোনভিত্তিক ঝুঁকি বিশ্লেষণ ও হটস্পট ম্যাপ দেখতে পারেন। আবহাওয়ার পূর্বাভাস ও লার্ভার ঘনত্বের ভিত্তিতে ঝুঁকি স্তর আপডেট করা হয়।"
                        : "You can view district and ward-level dengue vulnerability heatmaps, rainfall correlations, and community reports on our live Dengue Surveillance Risk Dashboard.",
                    DetectedIntent = DenAIIntents.RiskArea,
                    ModelSource = "ML",
                    ActionButtons = new List<ChatActionButton>
                    {
                        new() { Label = isBangla ? "রিস্ক ড্যাশবোর্ড" : "View Risk Dashboard", Url = "/Dashboard", Icon = "bi-map-fill", ButtonClass = "btn-primary" },
                        new() { Label = isBangla ? "নাগরিক রিপোর্ট" : "Citizen Hazard Report", Url = "/Reports/CitizenReport", Icon = "bi-megaphone", ButtonClass = "btn-outline-primary" }
                    },
                    SuggestedQuestions = isBangla
                        ? new List<string> { "এডিস মশা প্রতিরোধ কীভাবে?", "ডেঙ্গুর প্রধান লক্ষণ?", "টেস্ট কোথায় করাব?" }
                        : new List<string> { "How to prevent dengue at home?", "What are key dengue symptoms?", "Where to get tested?" }
                },

                DenAIIntents.FirstAid => new ChatbotResponse
                {
                    Reply = isBangla
                        ? "ডেঙ্গুর জরুরি প্রাথমিক চিকিৎসা:\n• রোগীকে সম্পূর্ণ বিশ্রামে রাখুন।\n• স্বাভাবিক তাপমাত্রার পানি দিয়ে স্পঞ্জ করুন (বরফ পানি নয়)।\n• ওআরএস বা তরল খাবার অল্প অল্প করে ঘন ঘন দিন।\n• রোগী নিস্তেজ বা বমি করলে একপাশে কাত করে শুইয়ে দিন।\n• রক্তপাত বা অজ্ঞান হলে অবিলম্বে ৯৯৯ কল করুন।"
                        : "Dengue First-Aid Steps:\n• Keep the patient at complete rest in a cool, ventilated area.\n• Sponge with lukewarm water — never ice water.\n• Administer ORS fluids in frequent small sips.\n• If vomiting or fainting, place in the recovery position (on their side).\n• For bleeding or unconsciousness, call 999 immediately.",
                    DetectedIntent = DenAIIntents.FirstAid,
                    ModelSource = "ML",
                    ActionButtons = new List<ChatActionButton>
                    {
                        new() { Label = isBangla ? "ফার্স্ট এইড গাইড" : "Open First Aid Guide", Url = "/FirstAid/Guide?type=dengue", Icon = "bi-heart-pulse-fill", ButtonClass = "btn-danger" },
                        new() { Label = isBangla ? "কল ৯৯৯" : "Call 999", Url = "tel:999", Icon = "bi-telephone-fill", ButtonClass = "btn-outline-danger" }
                    },
                    SuggestedQuestions = isBangla
                        ? new List<string> { "বিপদের লক্ষণ কী?", "আইসিইউ বেড কোথায়?", "রক্তদাতার খোঁজ" }
                        : new List<string> { "What are warning signs?", "Where to find ICU beds?", "I need blood donors" }
                },

                DenAIIntents.Education => new ChatbotResponse
                {
                    Reply = isBangla
                        ? "ডেঙ্গু সম্পর্কে কুসংস্কার দূর করতে এবং সঠিক বৈজ্ঞানিক তথ্য জানতে আমাদের শিক্ষামূলক পোর্টাল ও ইন্টারঅ্যাক্টিভ কুইজ ব্যবহার করুন। নিজের ও পরিবারের সুরক্ষা নিশ্চিত করুন।"
                        : "Explore our Dengue Awareness Portal and test your knowledge with interactive bilingual quizzes to understand vector habits, myths, and clinical precautions.",
                    DetectedIntent = DenAIIntents.Education,
                    ModelSource = "ML",
                    ActionButtons = new List<ChatActionButton>
                    {
                        new() { Label = isBangla ? "কুইজ খেলুন" : "Play Dengue Quiz", Url = "/Education/Quiz", Icon = "bi-patch-question", ButtonClass = "btn-success" },
                        new() { Label = isBangla ? "শিক্ষামূলক নিবন্ধ" : "Read Articles", Url = "/Education", Icon = "bi-book", ButtonClass = "btn-outline-success" }
                    },
                    SuggestedQuestions = isBangla
                        ? new List<string> { "ডেঙ্গু প্রতিরোধ কীভাবে?", "লক্ষণগুলো কী কী?", "কখন ডাক্তার দেখাব?" }
                        : new List<string> { "How to prevent dengue?", "What are the symptoms?", "When to see a doctor?" }
                },

                DenAIIntents.Greeting => new ChatbotResponse
                {
                    Reply = isBangla
                        ? "আমি DenAI — আপনার ডেঙ্গু স্বাস্থ্য সহকারী। আমি একটি ML মডেল দ্বারা চালিত যা ডেঙ্গু সম্পর্কিত প্রশ্ন বুঝতে পারে। জ্বর, উপসর্গ, বিপদের লক্ষণ, প্রতিরোধ, রক্তদাতা বা ডাক্তার পরামর্শ সম্পর্কে জিজ্ঞাসা করুন।"
                        : "Hello! I'm DenAI, your bilingual Dengue Health Assistant powered by an in-process ML.NET classifier. Ask me about dengue symptoms, fever management, warning signs, blood donors, ICU beds, lab tests, or prevention tips.",
                    DetectedIntent = DenAIIntents.Greeting,
                    ModelSource = "ML",
                    SuggestedQuestions = GetDefaultSuggestions(isBangla)
                },

                // GeneralInfo and any unknown intents
                _ => new ChatbotResponse
                {
                    Reply = isBangla
                        ? "আমি DenAI — আপনার ডেঙ্গু সচেতনতা সহকারী। আমি একটি বিশেষ ML মডেল দ্বারা চালিত। জ্বর, উপসর্গ, প্রতিরোধ, রক্তদাতা খোঁজা বা জরুরি নির্দেশনার বিষয়ে প্রশ্ন করুন। আমি রোগ নির্ণয়ের বিকল্প নই, তবে সঠিক স্বাস্থ্যসেবায় পথ দেখাতে পারি।"
                        : "I am DenAI, your bilingual Dengue Health Assistant, powered by an ML.NET classifier trained on dengue triage data. Ask me about symptoms, home care, prevention, or finding blood donors, doctors, labs, or ICU beds. I provide guidance and routing — not clinical diagnosis.",
                    DetectedIntent = DenAIIntents.GeneralInfo,
                    ModelSource = "ML",
                    ActionButtons = new List<ChatActionButton>
                    {
                        new() { Label = isBangla ? "উপসর্গ পরীক্ষা" : "Check Symptoms", Url = "/SymptomChecker", Icon = "bi-clipboard2-pulse", ButtonClass = "btn-primary" },
                        new() { Label = isBangla ? "ফার্স্ট এইড গাইড" : "First Aid Guide", Url = "/FirstAid", Icon = "bi-heart-pulse", ButtonClass = "btn-outline-danger" },
                        new() { Label = isBangla ? "রক্তদাতা নেটওয়ার্ক" : "Blood Donors", Url = "/Donors", Icon = "bi-droplet-half", ButtonClass = "btn-outline-danger" }
                    },
                    SuggestedQuestions = GetDefaultSuggestions(isBangla)
                }
            };
        }

        // ─────────────────────────────────────────────────────────────────────────────
        // HELPERS
        // ─────────────────────────────────────────────────────────────────────────────
        private static ChatbotResponse BuildEmergencyResponse(bool isBangla, string source)
        {
            return new ChatbotResponse
            {
                Reply = isBangla
                    ? "⚠️ এটি একটি গুরুতর জরুরি অবস্থা হতে পারে। তীব্র পেটে ব্যথা, ক্রমাগত বমি, রক্তপাত, চরম দুর্বলতা বা অজ্ঞান হওয়া ডেঙ্গু শক সিন্ড্রোম বা হেমোরেজিক ফিভারের লক্ষণ। অবিলম্বে নিকটস্থ হাসপাতালের ইমার্জেন্সিতে যান অথবা ৯৯৯ কল করুন। কোনো অবস্থাতেই এসপিরিন বা এনএসএআইডি খাবেন না।"
                    : "⚠️ This may be a medical emergency. Bleeding, severe abdominal pain, persistent vomiting, or extreme lethargy are critical indicators of Dengue Shock Syndrome (DSS) or Severe Dengue. Seek immediate hospital emergency care or call 999. Do NOT take Aspirin or NSAIDs.",
                IsEmergency = true,
                EmergencyMessage = isBangla
                    ? "⚠️ এটি একটি জরুরি অবস্থা হতে পারে। অবিলম্বে নিকটস্থ হাসপাতালে যোগাযোগ করুন অথবা ৯৯৯ কল করুন।"
                    : "⚠️ This may be a medical emergency. Seek immediate emergency care or call 999.",
                DetectedIntent = DenAIIntents.Emergency,
                ModelSource = source,
                ActionButtons = new List<ChatActionButton>
                {
                    new() { Label = isBangla ? "জরুরি ৯৯৯ কল" : "Call 999 Ambulance", Url = "tel:999", Icon = "bi-telephone-fill", ButtonClass = "btn-danger" },
                    new() { Label = isBangla ? "আইসিইউ বেড সন্ধান" : "Search ICU Beds", Url = "/IcuBeds", Icon = "bi-hospital", ButtonClass = "btn-outline-danger" },
                    new() { Label = isBangla ? "ফার্স্ট এইড গাইড" : "First Aid Guide", Url = "/FirstAid", Icon = "bi-heart-pulse", ButtonClass = "btn-outline-dark" }
                },
                SuggestedQuestions = isBangla
                    ? new List<string> { "আইসিইউ বেড কোথায়?", "জরুরি রক্তদাতা", "ফার্স্ট এইডে কী করব?" }
                    : new List<string> { "Find ICU bed availability", "Find emergency blood donor", "First aid steps" }
            };
        }

        private static List<string> GetDefaultSuggestions(bool isBangla) => isBangla
            ? new List<string>
            {
                "ডেঙ্গুর প্রধান উপসর্গ কী?",
                "জ্বর হলে কী করব?",
                "বিপদের লক্ষণগুলো কী?",
                "রক্তদাতার খোঁজ চাই",
                "এডিস মশা প্রতিরোধ কীভাবে?"
            }
            : new List<string>
            {
                "What are dengue symptoms?",
                "What to do when I have fever?",
                "What are the warning signs?",
                "Find blood donors nearby",
                "How to prevent dengue at home?"
            };
    }
}
