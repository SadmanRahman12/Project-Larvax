using Microsoft.AspNetCore.Mvc;
using LarvaX.Application.Services;
using System.Security.Claims;
using System.Threading.Tasks;

namespace LarvaX.Web.Controllers
{
    public class PatientRecordsController : Controller
    {
        private readonly IPatientRecordService _recordService;

        public PatientRecordsController(IPatientRecordService recordService)
        {
            _recordService = recordService;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return RedirectToAction("Login", "Account");

            var records = await _recordService.GetPatientRecordsAsync(userId);
            return View(records);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new PatientRecordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PatientRecordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return RedirectToAction("Login", "Account");

            // For simplicity in Phase 3, we simulate file upload by just saving the string URL/Path
            await _recordService.AddPatientRecordAsync(userId, model.Title, model.Description, model.RecordType, model.FileUrl);

            TempData["SuccessMessage"] = "Record added successfully.";
            return RedirectToAction(nameof(Index));
        }
    }

    public class PatientRecordViewModel
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string RecordType { get; set; } = "Prescription";
        public string? FileUrl { get; set; }
    }
}
