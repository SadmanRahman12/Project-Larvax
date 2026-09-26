using System;
using System.Collections.Generic;

namespace LarvaX.Core.ML
{
    /// <summary>
    /// A single labeled training example for DenAI intent classification.
    /// TF-IDF featurization is applied to the Text field by ML.NET at training time.
    /// </summary>
    public class IntentSample
    {
        public string Text { get; set; } = string.Empty;
        public string Intent { get; set; } = string.Empty;
    }

    /// <summary>
    /// Output of the ML.NET prediction pipeline. The predicted label maps to a DenAI intent.
    /// </summary>
    public class IntentPrediction
    {
        public string PredictedLabel { get; set; } = string.Empty;
        public float[]? Score { get; set; }
    }

    /// <summary>
    /// Well-known DenAI intent labels matched by the ML classifier.
    /// </summary>
    public static class DenAIIntents
    {
        public const string Emergency        = "Emergency";
        public const string FeverManagement  = "FeverManagement";
        public const string Symptoms        = "Symptoms";
        public const string WarningSigns    = "WarningSigns";
        public const string DoctorRouting   = "DoctorRouting";
        public const string LabTest         = "LabTest";
        public const string BloodDonor      = "BloodDonor";
        public const string IcuBed          = "IcuBed";
        public const string Prevention      = "Prevention";
        public const string NutritionDiet   = "NutritionDiet";
        public const string RiskArea        = "RiskArea";
        public const string FirstAid        = "FirstAid";
        public const string Education       = "Education";
        public const string Greeting        = "Greeting";
        public const string GeneralInfo     = "GeneralInfo";
    }
}
