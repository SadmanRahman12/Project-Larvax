namespace LarvaX.Core.Entities
{
    public enum RiskLevel
    {
        Low,
        Medium,
        High
    }

    public enum DataSufficiency
    {
        Sufficient,
        Insufficient
    }

    public class RiskZone
    {
        public int Id { get; set; }
        public string Region { get; set; } = null!;
        public DiseaseType DiseaseType { get; set; } = DiseaseType.Dengue;
        public RiskLevel RiskLevel { get; set; } = RiskLevel.Low;
        public double ConfidenceScore { get; set; } // e.g. 0.0 to 1.0
        public DateTime LastModelRun { get; set; } = DateTime.UtcNow;
        public DataSufficiency DataSufficiency { get; set; } = DataSufficiency.Insufficient;
    }
}
