using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using LarvaX.Application.Services;

namespace LarvaX.Web.Controllers;

public class FluidManagementController : Controller
{
    private readonly IFluidManagementService _fluidService;

    public FluidManagementController(IFluidManagementService fluidService)
    {
        _fluidService = fluidService;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View(new FluidManagementViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Index(FluidManagementViewModel model)
    {
        if (model.Weight <= 0)
        {
            ModelState.AddModelError(nameof(model.Weight), "Weight is required for fluid calculations.");
        }

        if (ModelState.IsValid)
        {
            var parameters = new FluidManagementParameters
            {
                Weight = model.Weight,
                DehydrationPercent = model.DehydrationPercent,
                OngoingLosses = model.OngoingLosses,
                ClinicalMode = model.ClinicalMode,
                HeartFailure = model.HeartFailure,
                Ckd = model.Ckd,
                SerumSodium = model.SerumSodium
            };

            model.Results = _fluidService.CalculateFluidPlan(parameters);
        }

        return View(model);
    }

    [HttpGet]
    public IActionResult Planner()
    {
        return View(new OralRehydrationPlannerViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Planner(OralRehydrationPlannerViewModel model)
    {
        if (model.Weight <= 0)
        {
            ModelState.AddModelError(nameof(model.Weight), "Weight is required.");
        }

        if (ModelState.IsValid)
        {
            var parameters = new OralRehydrationParameters
            {
                Weight = model.Weight,
                ShockStatus = model.ShockStatus,
                PatientType = model.PatientType,
                DenguePhase = model.DenguePhase
            };

            model.Schedule = _fluidService.GenerateOralPlan(parameters);
            
            // Re-calculate maintenance for the view
            decimal maintenanceDaily = 0;
            if (model.Weight <= 10m)
                maintenanceDaily = model.Weight * 100m;
            else if (model.Weight <= 20m)
                maintenanceDaily = 1000m + 50m * (model.Weight - 10m);
            else
                maintenanceDaily = 1500m + 20m * (model.Weight - 20m);
                
            model.Total24HourVolume = maintenanceDaily + (model.ShockStatus ? 0 : 0) + (model.DeficitPercent * model.Weight * 10m);
        }

        return View(model);
    }
}

public class FluidManagementViewModel
{
    public decimal Weight { get; set; }
    public int Age { get; set; }
    public int HeartRate { get; set; }
    public int SystolicBloodPressure { get; set; }
    public int RespiratoryRate { get; set; }
    public int SpO2 { get; set; }
    public int CapillaryRefillTime { get; set; }
    public decimal Hematocrit { get; set; }
    public decimal UrineOutput { get; set; }
    public decimal OngoingLosses { get; set; }
    public decimal SerumSodium { get; set; }
    public decimal SerumGlucose { get; set; }
    public bool Pregnancy { get; set; }
    public bool HeartFailure { get; set; }
    public bool Ckd { get; set; }
    public string ClinicalMode { get; set; } = "DengueNoWarning";
    public decimal DehydrationPercent { get; set; }
    public FluidPlanResult? Results { get; set; }
}

public class OralRehydrationPlannerViewModel
{
    public string PatientType { get; set; } = "Adult";
    public string DenguePhase { get; set; } = "CriticalPhase";
    public bool ShockStatus { get; set; }
    public decimal Weight { get; set; }
    public int Age { get; set; }
    public decimal DeficitPercent { get; set; } = 5m;
    public decimal Total24HourVolume { get; set; }
    public List<FluidScheduleItem> Schedule { get; set; } = new();
}
