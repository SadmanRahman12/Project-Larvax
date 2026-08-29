using Microsoft.AspNetCore.Mvc;

namespace LarvaX.Web.Controllers;

public class EducationController : Controller
{
    private static readonly List<ArticleItem> Articles = new()
    {
        new()
        {
            Title = "What causes dengue outbreaks in Bangladesh?",
            Summary = "Dengue spreads mainly through Aedes mosquitoes that breed in clean, stagnant water found in containers, rooftops, and blocked drains.",
            Url = "https://www.who.int/news-room/fact-sheets/detail/dengue-and-severe-dengue",
            Category = "Prevention"
        },
        new()
        {
            Title = "Recognising early warning signs",
            Summary = "High fever, severe headache, eye pain, nausea, rash, and bleeding can be warning signs that require professional assessment.",
            Url = "https://www.cdc.gov/dengue/signs-symptoms/index.html",
            Category = "Symptoms"
        },
        new()
        {
            Title = "Myth vs fact: common dengue misconceptions",
            Summary = "Dengue is not spread by casual contact or from eating certain foods; it is transmitted by infected mosquito bites.",
            Url = "https://www.mayoclinic.org/diseases-conditions/dengue-fever/symptoms-causes/syc-20353078",
            Category = "Myth-busting"
        },
        new()
        {
            Title = "How to protect your home and neighborhood",
            Summary = "Empty water-holding containers weekly, cover storage, clean drains, and use mosquito nets or repellents to reduce breeding.",
            Url = "https://www.who.int/health-topics/dengue",
            Category = "Community action"
        }
    };

    public IActionResult Index()
    {
        var model = new EducationOverviewViewModel()
        {
            Articles = Articles,
            PreventionTips = new[]
            {
                "Empty and scrub water containers at least once a week.",
                "Cover water tanks and rooftop storage drums.",
                "Use mosquito repellent and window screens when possible.",
                "Seek medical care early if fever lasts more than 24–48 hours."
            },
            WarningSigns = new[]
            {
                "Persistent high fever",
                "Severe abdominal pain",
                "Vomiting or bleeding",
                "Weakness, dizziness, or difficulty breathing"
            }
        };

        return View(model);
    }

    public IActionResult Quiz()
    {
        var questions = new[]
        {
            new QuizQuestion
            {
                Prompt = "Where do dengue mosquitoes most commonly breed?",
                Options = new[] { "In clean, stagnant water containers", "In dry cement walls", "In sunlight-only rooftops", "On untouched hills" },
                CorrectAnswer = "In clean, stagnant water containers"
            },
            new QuizQuestion
            {
                Prompt = "Which symptom is a warning sign that needs urgent medical attention?",
                Options = new[] { "Mild tiredness", "Bleeding, severe weakness, or breathing difficulty", "Mild cough", "Small skin itching" },
                CorrectAnswer = "Bleeding, severe weakness, or breathing difficulty"
            },
            new QuizQuestion
            {
                Prompt = "What is the best prevention habit for a household?",
                Options = new[] { "Leave all containers uncovered", "Empty and scrub water containers weekly", "Keep water in open drums", "Ignore blocked drains" },
                CorrectAnswer = "Empty and scrub water containers weekly"
            },
            new QuizQuestion
            {
                Prompt = "Which statement is true about dengue?",
                Options = new[] { "It spreads through mosquito bites from infected mosquitoes", "It spreads through direct contact with an infected person", "It is caused by dirty food alone", "It is cured by resting only without medical checkup" },
                CorrectAnswer = "It spreads through mosquito bites from infected mosquitoes"
            },
            new QuizQuestion
            {
                Prompt = "When should a person seek medical care after fever begins?",
                Options = new[] { "Only after 2 weeks", "If the fever is severe or warning signs appear", "Only after fever ends naturally", "Never if the person can still eat" },
                CorrectAnswer = "If the fever is severe or warning signs appear"
            }
        };

        return View(questions);
    }

    [HttpPost]
    public IActionResult QuizSubmit(List<int> answers)
    {
        var questions = new[]
        {
            "In clean, stagnant water containers",
            "Bleeding, severe weakness, or breathing difficulty",
            "Empty and scrub water containers weekly",
            "It spreads through mosquito bites from infected mosquitoes",
            "If the fever is severe or warning signs appear"
        };

        var score = 0;
        for (var i = 0; i < questions.Length; i++)
        {
            if (answers.Count > i && answers[i] == 0)
            {
                score++;
            }
        }

        ViewBag.Score = score;
        ViewBag.Total = questions.Length;
        ViewBag.Passed = score >= 3;
        return View("QuizResult");
    }

    public IActionResult Certificate()
    {
        return View();
    }
}

public class ArticleItem
{
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}

public class EducationOverviewViewModel
{
    public List<ArticleItem> Articles { get; set; } = new();
    public string[] PreventionTips { get; set; } = Array.Empty<string>();
    public string[] WarningSigns { get; set; } = Array.Empty<string>();
}

public class QuizQuestion
{
    public string Prompt { get; set; } = string.Empty;
    public string[] Options { get; set; } = Array.Empty<string>();
    public string CorrectAnswer { get; set; } = string.Empty;
}
