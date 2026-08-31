using System.Collections.Generic;

namespace LarvaX.Application.Services
{
    public interface IFluidManagementService
    {
        FluidPlanResult CalculateFluidPlan(FluidManagementParameters parameters);
        List<FluidScheduleItem> GenerateOralPlan(OralRehydrationParameters parameters);
    }

    public class FluidManagementParameters
    {
        public decimal Weight { get; set; }
        public decimal DehydrationPercent { get; set; }
        public decimal OngoingLosses { get; set; }
        public string ClinicalMode { get; set; } = "DengueNoWarning";
        public bool HeartFailure { get; set; }
        public bool Ckd { get; set; }
        public decimal SerumSodium { get; set; }
    }

    public class OralRehydrationParameters
    {
        public decimal Weight { get; set; }
        public bool ShockStatus { get; set; }
        public string PatientType { get; set; } = "Adult";
        public string DenguePhase { get; set; } = "CriticalPhase";
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

    public class FluidScheduleItem
    {
        public string Hour { get; set; } = string.Empty;
        public string Phase { get; set; } = string.Empty;
        public string Duration { get; set; } = string.Empty;
        public decimal VolumeMl { get; set; }
        public string Notes { get; set; } = string.Empty;
    }
}
