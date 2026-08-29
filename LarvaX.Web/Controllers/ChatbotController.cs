using Microsoft.AspNetCore.Mvc;

namespace LarvaX.Web.Controllers
{
    public class ChatbotController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Reply([FromBody] ChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
                return BadRequest();

            string msg = request.Message.ToLower();
            bool isEmergency = false;

            // Emergency keyword detection (Bangla + English)
            string[] emergencyKeywords = {
                "bleeding", "unconscious", "shock", "cannot breathe", "faint", "collapse",
                "blood in urine", "blood in stool", "rash with fever", "severe vomiting",
                "রক্ত", "জ্ঞান হারিয়ে", "শ্বাস নিতে পারছি না", "অজ্ঞান", "গুরুতর"
            };

            foreach (var keyword in emergencyKeywords)
            {
                if (msg.Contains(keyword))
                {
                    isEmergency = true;
                    break;
                }
            }

            string response = GetBotReply(msg, request.Language ?? "en");

            return Ok(new
            {
                reply = response,
                isEmergency = isEmergency,
                emergencyMessage = isEmergency
                    ? (request.Language == "bn"
                        ? "⚠️ এটি একটি জরুরি অবস্থা হতে পারে। এখনই সাহায্য নিন।"
                        : "⚠️ This may be a medical emergency. Seek help immediately.")
                    : null
            });
        }

        private string GetBotReply(string msg, string lang)
        {
            // Assumption: Mocked rule-based responses. In production, route to an LLM API (OpenAI/Gemini).
            bool bn = lang == "bn";

            if (msg.Contains("fever") || msg.Contains("জ্বর"))
                return bn
                    ? "ডেঙ্গু জ্বরে সাধারণত তীব্র মাথাব্যথা, চোখের পিছনে ব্যথা এবং র‍্যাশ দেখা যায়। ডাক্তারের সাথে পরামর্শ করুন।"
                    : "Dengue fever typically causes high fever, severe headache, pain behind the eyes, and skin rash. Consult a doctor if symptoms persist.";

            if (msg.Contains("prevent") || msg.Contains("prevention") || msg.Contains("প্রতিরোধ"))
                return bn
                    ? "মশা থেকে রক্ষা পেতে: পানি জমতে দেবেন না, মশারি ব্যবহার করুন, ফুল-হাতা পোশাক পরুন।"
                    : "Prevent dengue by: removing stagnant water, using mosquito nets, wearing long-sleeved clothing, and using repellent.";

            if (msg.Contains("symptom") || msg.Contains("উপসর্গ"))
                return bn
                    ? "ডেঙ্গুর উপসর্গ: হঠাৎ উচ্চ জ্বর, তীব্র মাথাব্যথা, বমি বমি ভাব, ত্বকে র‍্যাশ এবং হাড়ে-মাংসে ব্যথা।"
                    : "Dengue symptoms include: sudden high fever, severe headaches, nausea, skin rash, and muscle/joint pain.";

            if (msg.Contains("platelet") || msg.Contains("প্লেটলেট"))
                return bn
                    ? "প্লেটলেট কমে যাওয়া ডেঙ্গুর একটি গুরুতর লক্ষণ। দ্রুত হাসপাতালে যান এবং রক্ত পরীক্ষা করান।"
                    : "Low platelet count is a serious dengue complication. Go to a hospital immediately for a blood test if suspected.";

            if (msg.Contains("mosquito") || msg.Contains("মশা"))
                return bn
                    ? "Aedes aegypti মশা ডেঙ্গু ছড়ায়। এরা দিনের বেলা কামড়ায়। জমা পানি পরিষ্কার করুন।"
                    : "The Aedes aegypti mosquito spreads dengue. It bites during daytime. Eliminate standing water sources.";

            if (msg.Contains("doctor") || msg.Contains("hospital") || msg.Contains("ডাক্তার") || msg.Contains("হাসপাতাল"))
                return bn
                    ? "আমাদের টেলিমেডিসিন সেবায় ডাক্তারের সাথে কথা বলুন অথবা নিকটবর্তী হাসপাতালে যান।"
                    : "Use our Telemedicine service to speak with a doctor, or visit your nearest hospital for examination.";

            if (msg.Contains("blood") || msg.Contains("donor") || msg.Contains("রক্ত") || msg.Contains("দাতা"))
                return bn
                    ? "রক্তদাতা খুঁজতে আমাদের 'Blood Donor Network' ব্যবহার করুন। সেখানে আপনার কাছের দাতার তালিকা পাবেন।"
                    : "Use our Blood Donor Network section to find verified donors near you, sorted by availability and distance.";

            // Default fallback
            return bn
                ? "আমি DenAI — ডেঙ্গু তথ্য সহায়ক। জ্বর, উপসর্গ, প্রতিরোধ বা জরুরি সাহায্য সম্পর্কে জিজ্ঞাসা করুন।"
                : "I'm DenAI, your dengue health assistant. Ask me about symptoms, prevention, fever management, or emergency guidance.";
        }
    }

    public class ChatRequest
    {
        public string? Message { get; set; }
        public string? Language { get; set; }
    }
}
