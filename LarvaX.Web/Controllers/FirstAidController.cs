using Microsoft.AspNetCore.Mvc;

namespace LarvaX.Web.Controllers
{
    public class FirstAidController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Guide(string type)
        {
            var validTypes = new[] { "dengue", "choking", "snakebite", "heartattack", "fainting" };
            if (!validTypes.Contains(type?.ToLower()))
                return RedirectToAction(nameof(Index));

            return View(type?.ToLower());
        }
    }
}
