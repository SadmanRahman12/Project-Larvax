using System.Collections.Generic;

namespace LarvaX.Application.Services
{
    public class FluidManagementService : IFluidManagementService
    {
        public FluidPlanResult CalculateFluidPlan(FluidManagementParameters parameters)
        {
            var bolusPerKg = parameters.ClinicalMode == "DengueShock" || parameters.ClinicalMode == "GeneralResuscitation" ? 20m : 10m;
            var bolusVolume = bolusPerKg * parameters.Weight;
            var maintenanceDaily = CalculateMaintenanceDaily(parameters.Weight);
            var dehydrationDeficit = parameters.DehydrationPercent * parameters.Weight * 10m;
            var firstPhaseVolume = dehydrationDeficit * 0.60m;
            var remainderVolume = dehydrationDeficit * 0.40m;
            var safetyThreshold = parameters.HeartFailure || parameters.Ckd ? 42m : 70m;
            var dailyTarget = parameters.Weight * safetyThreshold;
            var total24HourFluid = maintenanceDaily + dehydrationDeficit + (parameters.OngoingLosses * 24m) + bolusVolume;
            var overThreshold = total24HourFluid > dailyTarget;
            var sodiumAdvice = parameters.SerumSodium switch
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
                ModeLabel = ResolveModeLabel(parameters.ClinicalMode),
                RecommendedRateHour = (maintenanceDaily / 24m) + parameters.OngoingLosses
            };
        }

        public List<FluidScheduleItem> GenerateOralPlan(OralRehydrationParameters parameters)
        {
            var maintenanceDaily = CalculateMaintenanceDaily(parameters.Weight);
            var maintenanceHour = maintenanceDaily / 24m;
            var bolusVolume = parameters.ShockStatus && parameters.PatientType == "Pediatric" ? 20m * parameters.Weight : parameters.ShockStatus ? 10m * parameters.Weight : 0m;
            var hourlyRate = parameters.ShockStatus switch
            {
                true when parameters.PatientType == "Pediatric" => 5m * parameters.Weight,
                true when parameters.PatientType == "Adult" => 7m * parameters.Weight,
                false when parameters.DenguePhase == "CriticalPhase" => 4m * parameters.Weight,
                _ => maintenanceHour
            };

            var schedule = new List<FluidScheduleItem>();
            if (parameters.ShockStatus)
            {
                schedule.Add(new FluidScheduleItem
                {
                    Hour = "Bolus Phase",
                    Phase = "Bolus",
                    Duration = parameters.PatientType == "Pediatric" ? "15–30 mins" : "1–2 hours",
                    VolumeMl = bolusVolume,
                    Notes = parameters.PatientType == "Pediatric"
                        ? "Give 20 mL/kg isotonic crystalloid and reassess perfusion every 15 minutes."
                        : "Give 10 mL/kg isotonic crystalloid and monitor blood pressure and pulse response."
                });
            }

            var hourCount = parameters.ShockStatus ? 6 : 24;
            for (var i = 1; i <= hourCount; i++)
            {
                schedule.Add(new FluidScheduleItem
                {
                    Hour = parameters.ShockStatus ? $"Hour {i}" : $"Hour {i}",
                    Phase = parameters.ShockStatus ? "Maintenance" : "Maintenance",
                    Duration = "1 hour",
                    VolumeMl = parameters.ShockStatus ? hourlyRate : maintenanceHour,
                    Notes = parameters.ShockStatus
                        ? "Review urine output, vital signs, capillary refill, and signs of fluid overload."
                        : "Continue maintenance rate and monitor hemodynamics and hematocrit trend."
                });
            }

            return schedule;
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
}
