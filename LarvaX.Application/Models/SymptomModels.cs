namespace LarvaX.Application.Models
{
    public class SymptomAssessmentInput
    {
        public bool Fever { get; set; }
        public bool SeverHeadache { get; set; }
        public bool RashBehindEyes { get; set; }
        public bool JointPain { get; set; }
        public bool Bleeding { get; set; }
        public bool VomitingNausea { get; set; }
        public bool AbdominalPain { get; set; }
    }

    public class SymptomAssessmentResult
    {
        public int Score { get; set; }
        public string RiskLevel { get; set; } = "Low"; // Emergency, High, Medium, Low
        public string Advice { get; set; } = string.Empty;
        public string ConfidenceNote { get; set; } = string.Empty;
        public bool IsEmergency { get; set; }
        public List<string> WarningFlags { get; set; } = new();
    }
}
