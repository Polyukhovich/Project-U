using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Project_U.Middlewares;
using ProjectU.Core.Interfaces;
using ProjectU.Core.Models;
using ProjectU.Data;
using ProjectU.Data.Repositories;

var builder = WebApplication.CreateBuilder(args);

// SignalR
builder.Services.AddSignalR();
// Реєстрація сервісу антиплагіату
builder.Services.AddScoped<ProjectU.Core.Services.PlagiarismService>();
// Реєстрація сервісу витягування тексту з файлів
builder.Services.AddScoped<ProjectU.Core.Services.FileTextExtractorService>();
// Фоновий сервіс для дедлайнів
builder.Services.AddHostedService<Project_U.Services.DeadlineNotificationService>();

builder.Services.AddScoped<Project_U.Helpers.NotificationHelper>();
// збільшуємо ліміт ASP.NET Core на розмір запиту
builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = 50 * 1024 * 1024; // 50MB
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 50 * 1024 * 1024; // 50MB
});

// Підключення бази даних
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Налаштування Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
})

.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
});
// Реєстрація репозиторіїв в DI
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ICourseRepository, CourseRepository>();
builder.Services.AddScoped<ILabWorkRepository, LabWorkRepository>();
builder.Services.AddScoped<IGradeRepository, GradeRepository>();
// Заглушка для EmailSender
builder.Services.AddTransient<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender,
    Project_U.Services.EmailSender>();

builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
// Підтримувані культури
var supportedCultures = new[] { "uk-UA", "en-US" };
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.SetDefaultCulture("uk-UA")
           .AddSupportedCultures(supportedCultures)
           .AddSupportedUICultures(supportedCultures);
});
// MVC + Razor Pages (для Identity Scaffolding)
builder.Services.AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization();
builder.Services.AddRazorPages();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStatusCodePagesWithReExecute("/Home/Error/{0}");

app.UseStaticFiles();
// Rate Limiting
app.UseRequestLimiter(authLimit: 250, anonLimit: 150, window: TimeSpan.FromMinutes(1));
// Middleware локалізаці
app.UseRequestLocalization();

app.UseRouting();

// Автентифікація та авторизація
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Маршрут для Razor Pages (Identity)
app.MapRazorPages();
app.MapHub<Project_U.Hubs.NotificationHub>("/hubs/notifications");


// Запуск сідера ролей при старті
using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    await RoleSeeder.SeedAsync(userManager, roleManager);
}

app.Run();