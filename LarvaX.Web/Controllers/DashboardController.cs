using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LarvaX.Core.Entities;
using LarvaX.Infrastructure.Data;
using LarvaX.Web.Models;

namespace LarvaX.Web.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = user.Id;
            var roles = await _userManager.GetRolesAsync(user);

            if (roles.Contains("HealthWorker") && !roles.Contains("Doctor") && !roles.Contains("Administrator"))
            {
                return RedirectToAction("Index", "HealthWorkerDashboard");
            }
            if (roles.Contains("Doctor") && !roles.Contains("Administrator"))
            {
                return RedirectToAction("Index", "DoctorDashboard");
            }
            if (roles.Contains("LabStaff") && !roles.Contains("Administrator"))
            {
                return RedirectToAction("Index", "LabStaffDashboard");
            }

            var recentReports = await _context.Reports
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .Take(5)
                .ToListAsync();
            var totalReports = await _context.Reports.CountAsync(r => r.UserId == userId);

            var upcomingAppointments = await _context.Appointments
                .Include(a => a.Doctor)
                .Include(a => a.Patient)
                .Where(a => a.PatientId == userId || a.DoctorId == userId)
                .OrderByDescending(a => a.ScheduledAt)
                .Take(5)
                .ToListAsync();
            var totalAppointments = await _context.Appointments
                .CountAsync(a => a.PatientId == userId || a.DoctorId == userId);

            var recentLabBookings = await _context.LabBookings
                .Include(b => b.LabTest)
                .Where(b => b.PatientId == userId)
                .OrderByDescending(b => b.ScheduledAt)
                .Take(5)
                .ToListAsync();
            var totalLabBookings = await _context.LabBookings
                .CountAsync(b => b.PatientId == userId);

            var recentRecords = await _context.PatientRecords
                .Where(p => p.PatientId == userId)
                .OrderByDescending(p => p.Date)
                .Take(5)
                .ToListAsync();
            var totalRecords = await _context.PatientRecords
                .CountAsync(p => p.PatientId == userId);

            var donorProfile = await _context.Donors
                .FirstOrDefaultAsync(d => d.UserId == userId);

            var activeHighRiskCount = await _context.RiskZones
                .CountAsync(z => z.RiskLevel == RiskLevel.High);

            var totalAlertsCount = await _context.Alerts.CountAsync();

            var viewModel = new UserDashboardViewModel
            {
                User = user,
                Roles = roles,
                TotalReportsCount = totalReports,
                TotalAppointmentsCount = totalAppointments,
                TotalLabBookingsCount = totalLabBookings,
                TotalRecordsCount = totalRecords,
                RecentReports = recentReports,
                UpcomingAppointments = upcomingAppointments,
                RecentLabBookings = recentLabBookings,
                RecentRecords = recentRecords,
                DonorProfile = donorProfile,
                ActiveHighRiskZonesCount = activeHighRiskCount,
                TotalActiveAlertsCount = totalAlertsCount
            };

            return View(viewModel);
        }
    }
}
