using LarvaX.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LarvaX.Web.Controllers
{
    public class EducationController : Controller
    {
        private readonly IEducationService _educationService;

        public EducationController(IEducationService educationService)
        {
            _educationService = educationService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var lang = System.Globalization.CultureInfo.CurrentUICulture.Name.StartsWith("bn") ? "bn" : "en";
            var articles = await _educationService.GetArticlesAsync(lang);
            var quizzes = await _educationService.GetQuizzesAsync(lang);
            ViewBag.Quizzes = quizzes;
            return View(articles);
        }

        [HttpGet]
        public async Task<IActionResult> Article(int id)
        {
            var article = await _educationService.GetArticleByIdAsync(id);
            if (article == null) return NotFound();
            return View(article);
        }

        [HttpGet]
        public async Task<IActionResult> Quiz(int id)
        {
            var quiz = await _educationService.GetQuizByIdAsync(id);
            if (quiz == null) return NotFound();
            return View(quiz);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitQuiz(int quizId, [FromForm] Dictionary<int, int> answers)
        {
            var quiz = await _educationService.GetQuizByIdAsync(quizId);
            if (quiz == null) return NotFound();
            var score = await _educationService.SubmitQuizAsync(quizId, answers);
            ViewBag.Score = score;
            ViewBag.Total = quiz.Questions.Count;
            return View("QuizResult", quiz);
        }
    }
}