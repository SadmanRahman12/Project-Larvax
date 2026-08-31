using LarvaX.Application.Models;
using LarvaX.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace LarvaX.Web.Controllers
{
    public class SymptomCheckerController : Controller
    {
        private readonly ISymptomCheckerService _symptomCheckerService;

        public SymptomCheckerController(ISymptomCheckerService symptomCheckerService)
        {
            _symptomCheckerService = symptomCheckerService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Result(SymptomAnswers answers)
        {
            var input = new SymptomAssessmentInput
            {
                Fever = answers.Fever,
                SeverHeadache = answers.SeverHeadache,
                RashBehindEyes = answers.RashBehindEyes,
                JointPain = answers.JointPain,
                Bleeding = answers.Bleeding,
                VomitingNausea = answers.VomitingNausea,
                AbdominalPain = answers.AbdominalPain
            };

            var assessment = _symptomCheckerService.Assess(input);

            ViewBag.RiskLevel = assessment.RiskLevel;
            ViewBag.Advice = assessment.Advice;
            ViewBag.ConfidenceNote = assessment.ConfidenceNote;
            ViewBag.IsEmergency = assessment.IsEmergency;
            ViewBag.Score = assessment.Score;
            ViewBag.WarningFlags = assessment.WarningFlags;

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
