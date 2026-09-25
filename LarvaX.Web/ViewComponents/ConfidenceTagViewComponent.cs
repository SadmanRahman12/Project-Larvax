using Microsoft.AspNetCore.Mvc;

namespace LarvaX.Web.ViewComponents
{
    /// <summary>
    /// Renders a model-confidence annotation tag (e.g. "High — 1,250+ validated cases").
    /// Usage: @await Component.InvokeAsync("ConfidenceTag", new { note = result.ConfidenceNote, isEmergency = result.IsEmergency })
    /// </summary>
    public class ConfidenceTagViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(string note, bool isEmergency = false)
        {
            return View(new ConfidenceTagViewModel { Note = note, IsEmergency = isEmergency });
        }
    }

    public class ConfidenceTagViewModel
    {
        public string Note        { get; set; } = "";
        public bool   IsEmergency { get; set; }
    }
}
