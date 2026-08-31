namespace LarvaX.Application.Services
{
    public class ChatbotResponse
    {
        public string Reply { get; set; } = string.Empty;
        public bool IsEmergency { get; set; }
        public string? EmergencyMessage { get; set; }
    }

    public interface IChatbotService
    {
        ChatbotResponse GetResponse(string message, string? language);
    }

    public class ChatbotService : IChatbotService
    {
        private static readonly string[] EmergencyKeywords = {
            "bleeding", "unconscious", "shock", "cannot breathe", "faint", "collapse",
            "blood in urine", "blood in stool", "rash with fever", "severe vomiting", "difficulty breathing",
            "রক্ত", "জ্ঞান হারিয়ে", "শ্বাস নিতে পারছি না", "অজ্ঞান", "গুরুতর", "রক্তপাত"
        };

        public ChatbotResponse GetResponse(string message, string? language)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return new ChatbotResponse
                {
                    Reply = language == "bn"
                        ? "অনুগ্রহ করে একটি প্রশ্ন লিখুন।"
                        : "Please enter a message or question."
                };
            }

            string msg = message.ToLowerInvariant();
            bool isEmergency = false;

            foreach (var keyword in EmergencyKeywords)
            {
                if (msg.Contains(keyword))
                {
                    isEmergency = true;
                    break;
                }
            }

            string reply = GenerateReply(msg, language ?? "en");

            return new ChatbotResponse
            {
                Reply = reply,
                IsEmergency = isEmergency,
                EmergencyMessage = isEmergency
                    ? (language == "bn"
                        ? "⚠️ এটি একটি জরুরি অবস্থা হতে পারে। অবিলম্বে নিকটস্থ হাসপাতালে যোগাযোগ করুন অথবা ৯৯৯ কল করুন।"
                        : "⚠️ This may be a medical emergency. Seek immediate emergency care or call 999.")
                    : null
            };
        }

        private string GenerateReply(string msg, string lang)
        {
            bool bn = lang == "bn";

            if (msg.Contains("fever") || msg.Contains("জ্বর"))
            {
                return bn
                    ? "ডেঙ্গু জ্বরে সাধারণত তীব্র হঠাৎ জ্বর (১০৪°F পর্যন্ত), চোখের পেছনে ব্যথা, এবং শরীরে প্রচণ্ড ব্যথা হয়। চিকিৎসকের পরামর্শ ব্যতীত এসপিরিন বা আইবুপ্রোফেন খাবেন না, শুধুমাত্র প্যারাসিটামল গ্রহণ করুন।"
                    : "Dengue fever typically presents with sudden high fever (up to 104°F), retro-orbital eye pain, and severe muscle/joint aches. Avoid Aspirin or Ibuprofen; take only Paracetamol and stay well-hydrated.";
            }

            if (msg.Contains("prevent") || msg.Contains("prevention") || msg.Contains("প্রতিরোধ"))
            {
                return bn
                    ? "মশা থেকে সুরক্ষিত থাকতে: প্রতি ৩ দিনে একবার জমা পানি ফেলে দিন (টব, টায়ার, বালতি), মশারি টাঙিয়ে ঘুমান এবং সকালে ও বিকেলে ফুল-হাতা জামা পরিধান করুন।"
                    : "To prevent dengue: eliminate standing water every 3 days (flower pots, discarded containers, tires), sleep inside mosquito bed nets, and apply mosquito repellent.";
            }

            if (msg.Contains("symptom") || msg.Contains("উপসর্গ") || msg.Contains("লক্ষণ"))
            {
                return bn
                    ? "প্রধান উপসর্গসমূহ: তীব্র জ্বর, তীব্র মাথাব্যথা, চোখের পেছনে ব্যথা, জয়েন্টে ব্যথা, বমি বমি ভাব এবং ত্বকে লালচে র‍্যাশ। রক্তক্ষরণ দেখা দিলে তাৎক্ষণিক হাসপাতালে যান।"
                    : "Key symptoms include: acute high fever, severe headache, retro-orbital eye pain, intense joint aches, nausea, and rash. Watch out for warning signs like persistent vomiting or mucosal bleeding.";
            }

            if (msg.Contains("platelet") || msg.Contains("প্লেটলেট") || msg.Contains("রক্তকণিকা"))
            {
                return bn
                    ? "প্লেটলেট ২০,০০০ এর নিচে নেমে গেলে অথবা রক্তক্ষরণ শুরু হলে দ্রুত হাসপাতালে রক্ত/প্লেটলেট ট্রান্সফিউশন প্রয়োজন হতে পারে। আমাদের রক্তদাতা সন্ধান মেনু ব্যবহার করুন।"
                    : "A rapid drop in platelets or bleeding requires urgent medical evaluation and possibly transfusion. Use our Blood Donor Network to locate verified donors nearby.";
            }

            if (msg.Contains("mosquito") || msg.Contains("মশা") || msg.Contains("লার্ভা") || msg.Contains("aedes"))
            {
                return bn
                    ? "এডিস মশা (Aedes aegypti) মূলত পরিষ্কার জমা পানিতে ডিম পাড়ে এবং দিনের বেলায় (ভোরে ও সন্ধ্যায়) বেশি কামড়ায়। পরিষ্কার পরিচ্ছন্নতা সবচেয়ে কার্যকর প্রতিরোধ।"
                    : "The Aedes mosquito breeds in clean, stagnant water containers and primarily bites during early morning and late afternoon hours. Emptying water collectors is critical.";
            }

            if (msg.Contains("doctor") || msg.Contains("hospital") || msg.Contains("ডাক্তার") || msg.Contains("হাসপাতাল"))
            {
                return bn
                    ? "আমাদের টেলিমেডিসিন পোর্টালে নিবন্ধিত ডাক্তারের সাথে ভিডিও কনসাল্টেশন বুক করতে পারেন অথবা নিকটস্থ সরকারি/বেসরকারি হাসপাতালে যেতে পারেন।"
                    : "You can book a telemedicine consultation with verified doctors through our platform or visit the nearest healthcare facility.";
            }

            if (msg.Contains("blood") || msg.Contains("donor") || msg.Contains("রক্ত") || msg.Contains("দাতা"))
            {
                return bn
                    ? "রক্ত বা প্লেটলেট প্রয়োজন হলে LarvaX রক্তদাতা নেটওয়ার্ক ব্রাউজ করুন। সেখানে সক্রিয় রক্তদাতাদের ফোন নম্বর ও শেষ সক্রিয়তার তথ্য রয়েছে।"
                    : "If you need blood or platelets, check our Blood Donor Network. It displays verified active donors with real-time freshness timestamps.";
            }

            return bn
                ? "আমি DenAI — আপনার ডেঙ্গু সচেতনতা সহকারী। জ্বর, প্রতিরোধ ব্যবস্থা, রক্তদাতা খোঁজা বা জরুরি নির্দেশনার বিষয়ে প্রশ্ন করুন।"
                : "I am DenAI, your bilingual Dengue Health Assistant. Ask me about dengue symptoms, home care hydration, prevention measures, or finding blood donors.";
        }
    }
}
