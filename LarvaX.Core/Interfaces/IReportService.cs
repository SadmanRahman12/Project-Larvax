using LarvaX.Core.Entities;

namespace LarvaX.Core.Interfaces
{
    /// <summary>
    /// Defines the contract for report status management and transition logic.
    /// Interface lives in Core so all layers can reference it without coupling to Application.
    /// </summary>
    public interface IReportService
    {
        /// <summary>
        /// Determines the next <see cref="ReportStatus"/> after an admin verification decision.
        /// </summary>
        ReportStatus DetermineNextStatus(ReportStatus currentStatus, ReportVerification verification);

        /// <summary>
        /// Returns whether a direct status transition is allowed by the state machine.
        /// </summary>
        bool CanTransition(ReportStatus currentStatus, ReportStatus targetStatus);
    }
}
