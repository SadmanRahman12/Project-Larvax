using Hangfire.Dashboard;

namespace LarvaX.Web.Filters
{
    public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            var httpContext = context.GetHttpContext();
            // Allow all authenticated users to see the Dashboard (for now) or limit to Admin
            return httpContext.User.Identity?.IsAuthenticated == true && httpContext.User.IsInRole("Administrator");
        }
    }
}
