using LarvaX.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace LarvaX.Web.ViewComponents
{
    /// <summary>
    /// Renders a human-readable "freshness" timestamp (e.g. "3h ago", "২ দিন আগে নিশ্চিত").
    /// Usage: @await Component.InvokeAsync("FreshnessTimestamp", new { lastConfirmedUtc = donor.LastConfirmedAvailable })
    /// </summary>
    public class FreshnessTimestampViewComponent : ViewComponent
    {
        private readonly IDonorService _donorService;

        public FreshnessTimestampViewComponent(IDonorService donorService)
        {
            _donorService = donorService;
        }

        public IViewComponentResult Invoke(DateTime lastConfirmedUtc)
        {
            string lang  = CultureInfo.CurrentUICulture.Name == "bn" ? "bn" : "en";
            string label = _donorService.GetFreshnessLabel(lastConfirmedUtc, language: lang);
            bool   fresh = _donorService.IsConsideredFresh(lastConfirmedUtc);
            return View(new FreshnessTimestampViewModel { Label = label, IsFresh = fresh });
        }
    }

    public class FreshnessTimestampViewModel
    {
        public string Label   { get; set; } = "";
        public bool   IsFresh { get; set; }
    }
}
