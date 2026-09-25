using LarvaX.Core.Entities;
using Microsoft.AspNetCore.Mvc;

namespace LarvaX.Web.ViewComponents
{
    /// <summary>
    /// Renders a colour-coded Bootstrap badge for a risk level.
    /// Usage: @await Component.InvokeAsync("RiskBadge", new { level = RiskLevel.High })
    /// </summary>
    public class RiskBadgeViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(RiskLevel level)
        {
            var (cssClass, icon, label) = level switch
            {
                RiskLevel.High   => ("bg-danger",  "bi-exclamation-triangle-fill", "High"),
                RiskLevel.Medium => ("bg-warning text-dark", "bi-exclamation-circle-fill", "Medium"),
                _                => ("bg-success",  "bi-check-circle-fill", "Low")
            };

            return View(new RiskBadgeViewModel { CssClass = cssClass, Icon = icon, Label = label });
        }
    }

    public class RiskBadgeViewModel
    {
        public string CssClass { get; set; } = "";
        public string Icon     { get; set; } = "";
        public string Label    { get; set; } = "";
    }
}
