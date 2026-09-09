global using AnywhereEdureach;
global using AnywhereEdureach.Models;
global using Online_Tuition_Systems.Data;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using AnywhereEdureach.Services;
using Online_Tuition_Systems.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("The DefaultConnection connection string was not found.");

// Every module uses the same EF Core context and the same OnlineTuitionDb database.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddScoped<Helper>();
builder.Services.AddScoped<AnywhereEdureach.NotificationService>();
builder.Services.AddScoped<INotificationService, Online_Tuition_Systems.Services.NotificationService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();
builder.Services.AddHttpClient<GoogleRecaptchaService>();
builder.Services.Configure<RecaptchaOptions>(builder.Configuration.GetSection("GoogleRecaptcha"));
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Smtp"));
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapStaticAssets();
app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Welcome}/{id?}")
    .WithStaticAssets();

if (app.Environment.IsDevelopment())
{
    await DevelopmentAccountSeeder.SeedAsync(app.Services, app.Configuration);
}

app.Run();
