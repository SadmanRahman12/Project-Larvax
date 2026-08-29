namespace LarvaX.Core.Entities
{
    public enum BloodGroup
    {
        A_Positive,
        A_Negative,
        B_Positive,
        B_Negative,
        AB_Positive,
        AB_Negative,
        O_Positive,
        O_Negative
    }

    public class Donor
    {
        public int Id { get; set; }
        public string UserId { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;
        public BloodGroup BloodGroup { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime LastConfirmedAvailable { get; set; } = DateTime.UtcNow;
        public string? ContactNumber { get; set; }
        public bool IsAvailable { get; set; } = true;
        public string? Location { get; set; } // Human-readable district/area
    }
}
