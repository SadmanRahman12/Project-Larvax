using LarvaX.Web.Models;

namespace LarvaX.Web.Services
{
    public interface IAdminSettingsService
    {
        AdminSettingsViewModel GetSettings();
        void UpdateSettings(AdminSettingsViewModel newSettings);
    }

    public class AdminSettingsService : IAdminSettingsService
    {
        private readonly object _lock = new();
        private AdminSettingsViewModel _settings = new()
        {
            RequireProfessionalApproval = true,
            DefaultAlertRadiusKm = 5.0,
            EnableSignalRBroadcast = true,
            CriticalPlateletThreshold = 50000,
            MaintenanceMode = false,
            DefaultLanguage = "en",
            AutoEscalateSevereReports = true
        };

        public AdminSettingsViewModel GetSettings()
        {
            lock (_lock)
            {
                return new AdminSettingsViewModel
                {
                    RequireProfessionalApproval = _settings.RequireProfessionalApproval,
                    DefaultAlertRadiusKm = _settings.DefaultAlertRadiusKm,
                    EnableSignalRBroadcast = _settings.EnableSignalRBroadcast,
                    CriticalPlateletThreshold = _settings.CriticalPlateletThreshold,
                    MaintenanceMode = _settings.MaintenanceMode,
                    DefaultLanguage = _settings.DefaultLanguage,
                    AutoEscalateSevereReports = _settings.AutoEscalateSevereReports
                };
            }
        }

        public void UpdateSettings(AdminSettingsViewModel newSettings)
        {
            lock (_lock)
            {
                _settings.RequireProfessionalApproval = newSettings.RequireProfessionalApproval;
                _settings.DefaultAlertRadiusKm = newSettings.DefaultAlertRadiusKm;
                _settings.EnableSignalRBroadcast = newSettings.EnableSignalRBroadcast;
                _settings.CriticalPlateletThreshold = newSettings.CriticalPlateletThreshold;
                _settings.MaintenanceMode = newSettings.MaintenanceMode;
                _settings.DefaultLanguage = newSettings.DefaultLanguage;
                _settings.AutoEscalateSevereReports = newSettings.AutoEscalateSevereReports;
            }
        }
    }
}
