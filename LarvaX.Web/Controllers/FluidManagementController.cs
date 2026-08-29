using System.Globalization;
using Microsoft.AspNetCore.Mvc;

namespace LarvaX.Web.Controllers;

public class FluidManagementController : Controller
{
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
            model.Results = CalculateFluidPlan(model);
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
            model.Schedule = GenerateOralPlan(model);
            model.Total24HourVolume = CalculateMaintenanceDaily(model.Weight) + (model.ShockStatus ? 0 : 0) + (model.DeficitPercent * model.Weight * 10m);
        }

        return View(model);
    }

    private static FluidPlanResult CalculateFluidPlan(FluidManagementViewModel model)
    {
        var bolusPerKg = model.ClinicalMode == "DengueShock" || model.ClinicalMode == "GeneralResuscitation" ? 20m : 10m;
        var bolusVolume = bolusPerKg * model.Weight;
        var maintenanceDaily = CalculateMaintenanceDaily(model.Weight);
        var dehydrationDeficit = model.DehydrationPercent * model.Weight * 10m;
        var firstPhaseVolume = dehydrationDeficit * 0.60m;
        var remainderVolume = dehydrationDeficit * 0.40m;
        var safetyThreshold = model.HeartFailure || model.Ckd ? 42m : 70m;
        var dailyTarget = model.Weight * safetyThreshold;
        var total24HourFluid = maintenanceDaily + dehydrationDeficit + (model.OngoingLosses * 24m) + bolusVolume;
        var overThreshold = total24HourFluid > dailyTarget;
        var sodiumAdvice = model.SerumSodium switch
        {
            < 130m => "Hyponatraemia: use isotonic fluids and avoid rapid correction.",
            > 150m => "Hypernatraemia: correct slowly and seek senior review.",
            _ => "Serum sodium is within the expected range. Continue routine monitoring."
        };

        var alert = overThreshold ? "WARNING: 24-hour fluid target has been exceeded." : "Within target range.";

        return new FluidPlanResult
        {
            Maintenance24h = maintenanceDaily,
            BolusVolume = bolusVolume,
            DehydrationDeficit = dehydrationDeficit,
            FirstPhaseVolume = firstPhaseVolume,
            RemainingPhaseVolume = remainderVolume,
            Total24hVolume = total24HourFluid,
            SafetyTarget = dailyTarget,
            SafetyAlert = alert,
            SodiumAdvice = sodiumAdvice,
            IsOverThreshold = overThreshold,
            ModeLabel = ResolveModeLabel(model.ClinicalMode),
            RecommendedRateHour = (maintenanceDaily / 24m) + model.OngoingLosses
        };
    }

    private static decimal CalculateMaintenanceDaily(decimal weight)
    {
        if (weight <= 10m)
        {
            return weight * 100m;
        }

        if (weight <= 20m)
        {
            return 1000m + 50m * (weight - 10m);
        }

        return 1500m + 20m * (weight - 20m);
    }

    private static List<FluidScheduleItem> GenerateOralPlan(OralRehydrationPlannerViewModel model)
    {
        var maintenanceDaily = CalculateMaintenanceDaily(model.Weight);
        var maintenanceHour = maintenanceDaily / 24m;
        var bolusVolume = model.ShockStatus && model.PatientType == "Pediatric" ? 20m * model.Weight : model.ShockStatus ? 10m * model.Weight : 0m;
        var hourlyRate = model.ShockStatus switch
        {
            true when model.PatientType == "Pediatric" => 5m * model.Weight,
            true when model.PatientType == "Adult" => 7m * model.Weight,
            false when model.DenguePhase == "CriticalPhase" => 4m * model.Weight,
            _ => maintenanceHour
        };

        var schedule = new List<FluidScheduleItem>();
        if (model.ShockStatus)
        {
            schedule.Add(new FluidScheduleItem
            {
                Hour = "Bolus Phase",
                Phase = "Bolus",
                Duration = model.PatientType == "Pediatric" ? "15–30 mins" : "1–2 hours",
                VolumeMl = bolusVolume,
                Notes = model.PatientType == "Pediatric"
                    ? "Give 20 mL/kg isotonic crystalloid and reassess perfusion every 15 minutes."
                    : "Give 10 mL/kg isotonic crystalloid and monitor blood pressure and pulse response."
            });
        }

        var hourCount = model.ShockStatus ? 6 : 24;
        for (var i = 1; i <= hourCount; i++)
        {
            schedule.Add(new FluidScheduleItem
            {
                Hour = model.ShockStatus ? $"Hour {i}" : $"Hour {i}",
                Phase = model.ShockStatus ? "Maintenance" : "Maintenance",
                Duration = "1 hour",
                VolumeMl = model.ShockStatus ? hourlyRate : maintenanceHour,
                Notes = model.ShockStatus
                    ? "Review urine output, vital signs, capillary refill, and signs of fluid overload."
                    : "Continue maintenance rate and monitor hemodynamics and hematocrit trend."
            });
        }

        return schedule;
    }

    private static string ResolveModeLabel(string mode)
    {
        return mode switch
        {
            "DengueNoWarning" => "Dengue (no warning signs)",
            "DengueWarning" => "Dengue with warning signs",
            "DengueShock" => "Dengue shock",
            "MildDehydration" => "Dehydration - mild",
            "ModerateDehydration" => "Dehydration - moderate",
            "SevereDehydration" => "Dehydration - severe",
            "GeneralResuscitation" => "General resuscitation",
            "MaintenanceOnly" => "Maintenance only",
            _ => "Standard care"
        };
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

public class FluidPlanResult
{
    public decimal Maintenance24h { get; set; }
    public decimal BolusVolume { get; set; }
    public decimal DehydrationDeficit { get; set; }
    public decimal FirstPhaseVolume { get; set; }
    public decimal RemainingPhaseVolume { get; set; }
    public decimal Total24hVolume { get; set; }
    public decimal SafetyTarget { get; set; }
    public decimal RecommendedRateHour { get; set; }
    public string SafetyAlert { get; set; } = string.Empty;
    public string SodiumAdvice { get; set; } = string.Empty;
    public bool IsOverThreshold { get; set; }
    public string ModeLabel { get; set; } = string.Empty;
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

public class FluidScheduleItem
{
    public string Hour { get; set; } = string.Empty;
    public string Phase { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
    public decimal VolumeMl { get; set; }
    public string Notes { get; set; } = string.Empty;
}
