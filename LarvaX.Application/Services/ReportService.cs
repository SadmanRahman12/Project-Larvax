using LarvaX.Core.Entities;
using LarvaX.Core.Interfaces;

namespace LarvaX.Application.Services
{
    // IReportService is defined in LarvaX.Core.Interfaces — the correct Clean Architecture location.
    // This class is the Application-layer implementation of that contract.
    public class ReportService : IReportService
    {
        public ReportStatus DetermineNextStatus(ReportStatus currentStatus, ReportVerification verification)
        {
            if (verification == ReportVerification.Invalid)
            {
                return ReportStatus.Resolved;
            }

            if (verification == ReportVerification.Verified && currentStatus == ReportStatus.Received)
            {
                return ReportStatus.UnderReview;
            }

            return currentStatus;
        }

        public bool CanTransition(ReportStatus currentStatus, ReportStatus targetStatus)
        {
            if (currentStatus == targetStatus) return true;

            return currentStatus switch
            {
                ReportStatus.Received => targetStatus == ReportStatus.UnderReview || targetStatus == ReportStatus.Resolved,
                ReportStatus.UnderReview => targetStatus == ReportStatus.Resolved,
                ReportStatus.Resolved => false,
                _ => false
            };
        }
    }
}
