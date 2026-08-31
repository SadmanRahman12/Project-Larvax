namespace LarvaX.Application.Services
{
    public interface IDonorService
    {
        string GetFreshnessLabel(DateTime lastConfirmedUtc, DateTime? currentUtc = null, string language = "en");
        bool IsConsideredFresh(DateTime lastConfirmedUtc, int maxDays = 30);
    }

    public class DonorService : IDonorService
    {
        public string GetFreshnessLabel(DateTime lastConfirmedUtc, DateTime? currentUtc = null, string language = "en")
        {
            var now = currentUtc ?? DateTime.UtcNow;
            var diff = now - lastConfirmedUtc;
            bool bn = language == "bn";

            if (diff.TotalMinutes < 1)
            {
                return bn ? "এইমাত্র সক্রিয়" : "Just now";
            }
            if (diff.TotalMinutes < 60)
            {
                int mins = (int)diff.TotalMinutes;
                return bn ? $"{mins} মিনিট আগে নিশ্চিত" : $"{mins}m ago";
            }
            if (diff.TotalHours < 24)
            {
                int hours = (int)diff.TotalHours;
                return bn ? $"{hours} ঘণ্টা আগে নিশ্চিত" : $"{hours}h ago";
            }
            if (diff.TotalDays < 30)
            {
                int days = (int)diff.TotalDays;
                return bn ? $"{days} দিন আগে নিশ্চিত" : $"{days}d ago";
            }

            int months = (int)(diff.TotalDays / 30);
            return bn ? $"{months} মাস আগে নিশ্চিত" : $"{months}mo ago";
        }

        public bool IsConsideredFresh(DateTime lastConfirmedUtc, int maxDays = 30)
        {
            var diff = DateTime.UtcNow - lastConfirmedUtc;
            return diff.TotalDays <= maxDays;
        }
    }
}
