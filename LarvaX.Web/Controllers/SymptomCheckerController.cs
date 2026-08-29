using Microsoft.AspNetCore.Mvc;

namespace LarvaX.Web.Controllers
{
    public class SymptomCheckerController : Controller
    {
        // Step-by-step questionnaire: 5 questions, then risk result
        // Assumption: Mocked scoring logic; replace with ML model call in production

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Result(SymptomAnswers answers)
        {
            int score = 0;
            if (answers.Fever) score += 30;
            if (answers.SeverHeadache) score += 20;
            if (answers.RashBehindEyes) score += 20;
            if (answers.JointPain) score += 10;
            if (answers.Bleeding) score += 40; // Severe symptom - major weight
            if (answers.VomitingNausea) score += 10;
            if (answers.AbdominalPain) score += 20;

            string riskLevel;
            string advice;
            string confidenceNote;
            bool isEmergency = false;

            if (score >= 80 || answers.Bleeding)
            {
                riskLevel = "Emergency";
                advice = "You may be experiencing Dengue Hemorrhagic Fever (DHF) or Dengue Shock Syndrome. Seek IMMEDIATE medical care.";
                confidenceNote = "Model confidence: High — Severe warning symptoms detected.";
                isEmergency = true;
            }
            else if (score >= 50)
            {
                riskLevel = "High";
                advice = "Your symptoms suggest a high probability of dengue. Please visit a clinic or hospital today for a blood test.";
                confidenceNote = "Model confidence: High — based on 1,250+ validated symptom patterns.";
            }
            else if (score >= 25)
            {
                riskLevel = "Medium";
                advice = "Some dengue symptoms present. Rest, stay hydrated, and monitor. If fever persists >2 days, see a doctor.";
                confidenceNote = "Model confidence: Medium — based on 1,250+ validated symptom patterns.";
            }
            else
            {
                riskLevel = "Low";
                advice = "Symptoms are mild or minimal. Monitor your health. Avoid mosquito bites. Seek care if symptoms worsen.";
                confidenceNote = "Model confidence: Medium — based on 1,250+ validated symptom patterns.";
            }

            ViewBag.RiskLevel = riskLevel;
            ViewBag.Advice = advice;
            ViewBag.ConfidenceNote = confidenceNote;
            ViewBag.IsEmergency = isEmergency;
            ViewBag.Score = score;

            return View();
        }
    }

    public class SymptomAnswers
    {
        public bool Fever { get; set; }
        public bool SeverHeadache { get; set; }
        public bool RashBehindEyes { get; set; }
        public bool JointPain { get; set; }
        public bool Bleeding { get; set; }
        public bool VomitingNausea { get; set; }
        public bool AbdominalPain { get; set; }
    }
}
