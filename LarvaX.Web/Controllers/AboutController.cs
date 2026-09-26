using Microsoft.AspNetCore.Mvc;

namespace LarvaX.Web.Controllers
{
    public class AboutController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
