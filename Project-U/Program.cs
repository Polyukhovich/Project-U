using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProjectU.Core.Interfaces;
using ProjectU.Core.Models;
using ProjectU.Data;
using ProjectU.Data.Repositories;

var builder = WebApplication.CreateBuilder(args);

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
// MVC + Razor Pages (для Identity Scaffolding)
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Автентифікація та авторизація
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Маршрут для Razor Pages (Identity)
app.MapRazorPages();



// Запуск сідера ролей при старті
using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    await RoleSeeder.SeedAsync(userManager, roleManager);
}

app.Run();