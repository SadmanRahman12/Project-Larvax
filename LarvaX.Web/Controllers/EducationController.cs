using Microsoft.AspNetCore.Mvc;

namespace LarvaX.Web.Controllers;

public class EducationController : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View(new DengueQuizViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Index(DengueQuizViewModel model)
    {
        model.Answered = true;
        model.Score = 0;
        if (model.QuestionOne == "A") model.Score++;
        if (model.QuestionTwo == "B") model.Score++;
        if (model.QuestionThree == "C") model.Score++;
        if (model.QuestionFour == "A") model.Score++;

        return View(model);
    }
}

public class DengueQuizViewModel
{
    public string? QuestionOne { get; set; }
    public string? QuestionTwo { get; set; }
    public string? QuestionThree { get; set; }
    public string? QuestionFour { get; set; }
    public bool Answered { get; set; }
    public int Score { get; set; }
}