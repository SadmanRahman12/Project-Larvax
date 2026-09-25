using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Hangfire;
using Hangfire.PostgreSql;
using LarvaX.Web.Hubs;
using LarvaX.Web.Filters;
using LarvaX.Web.Jobs;
using LarvaX.Infrastructure.Data;
using LarvaX.Core.Entities;
using LarvaX.Core.Interfaces;
using LarvaX.Application.Services;
using LarvaX.Infrastructure.Services;
using Microsoft.AspNetCore.Localization;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString)
           .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// NOTE (Gap 11): RequireConfirmedAccount = true is intentional but EmailConfirmed is set
// to true at registration time (AccountController) to skip email verification in development.
// For production: set EmailConfirmed = false and configure an email sender (IEmailSender).
//
// IMPORTANT: Use AddIdentity (not AddDefaultIdentity) so that the authentication cookie
// login/logout paths can be overridden to the custom MVC AccountController.
// AddDefaultIdentity hard-codes /Identity/Account/Login (Razor Pages) which causes an
// infinite redirect loop when using custom controller-based auth.
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Override cookie paths to point to the custom MVC AccountController instead of Razor Pages.
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// Application Services — bound to Core.Interfaces contracts (Clean Architecture)
builder.Services.AddScoped<ISymptomCheckerService, SymptomCheckerService>();
builder.Services.AddScoped<IChatbotService, ChatbotService>();
builder.Services.AddScoped<IRiskAssessmentService, RiskAssessmentService>();
builder.Services.AddScoped<IDonorService, DonorService>();
builder.Services.AddScoped<IReportService, ReportService>();

// Phase 3 Services
builder.Services.AddScoped<ITelemedicineService, TelemedicineService>();
builder.Services.AddScoped<ILabService, LabService>();
builder.Services.AddScoped<IIcuBedService, IcuBedService>();
builder.Services.AddScoped<IPatientRecordService, PatientRecordService>();
builder.Services.AddScoped<IFluidManagementService, FluidManagementService>();
builder.Services.AddScoped<RiskCalculationJob>();

// Phase 4 Services
builder.Services.AddScoped<IEducationService, EducationService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddScoped<IPdfReportService, PdfReportService>();
builder.Services.AddSingleton<LarvaX.Web.Services.IAdminSettingsService, LarvaX.Web.Services.AdminSettingsService>();

// Localization Support (English & Bangla)
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

// Add SignalR
builder.Services.AddSignalR();

// Add Hangfire
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(c => c.UseNpgsqlConnection(connectionString)));
builder.Services.AddHangfireServer();

builder.Services.AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var dbContext = services.GetRequiredService<ApplicationDbContext>();
        if (dbContext.Database.IsRelational())
        {
            await dbContext.Database.MigrateAsync();
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
    }

    await RoleSeeder.SeedRolesAsync(services);
}

// Request Localization Middleware
var supportedCultures = new[] { new CultureInfo("en"), new CultureInfo("bn") };
var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("en"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
};
app.UseRequestLocalization(localizationOptions);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() }
});

// Register Recurring Risk Zone Recalculation & Alert Dispatch Job
RecurringJob.AddOrUpdate<RiskCalculationJob>(
    "refresh-risk-zones-and-alerts",
    job => job.ExecuteAsync(),
    Cron.Hourly);

app.MapStaticAssets();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapHub<AlertsHub>("/alertshub");
app.MapHub<VideoConsultHub>("/videoconsulthub");

// Note: MapRazorPages() removed — project uses MVC controllers, not Razor Pages.

app.Run();

