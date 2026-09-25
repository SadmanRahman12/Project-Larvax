namespace LarvaX.Core.Interfaces
{
    /// <summary>
    /// Freshness / availability utilities for the Blood Donor network.
    /// Interface lives in Core so controllers can depend on the abstraction,
    /// not the Application.Services concrete assembly.
    /// </summary>
    public interface IDonorService
    {
        /// <summary>Returns a human-readable "X ago" label in English or Bangla.</summary>
        string GetFreshnessLabel(DateTime lastConfirmedUtc, DateTime? currentUtc = null, string language = "en");

        /// <summary>Returns true when the donor confirmed availability within <paramref name="maxDays"/> days.</summary>
        bool IsConsideredFresh(DateTime lastConfirmedUtc, int maxDays = 30);
    }
}
