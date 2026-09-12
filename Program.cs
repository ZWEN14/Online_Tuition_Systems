global using AnywhereEdureach;
global using AnywhereEdureach.Models;
global using Online_Tuition_Systems.Data;

using AnywhereEdureach.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Online_Tuition_Systems.Services;
using Online_Tuition_Systems.Services.Billing;
using Online_Tuition_Systems.Services.Courses;
using Online_Tuition_Systems.Services.Enrollments;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Keep the SQL Server Express database file inside the project for the
// assignment's file-based database requirement. |DataDirectory| makes the
// connection string portable across different team members' computers.
var databaseDirectory = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
Directory.CreateDirectory(databaseDirectory);
var databaseFile = Path.Combine(databaseDirectory, "OnlineTuitionSystems.mdf");

var baseConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("The DefaultConnection connection string was not found.");
var databaseConnectionBuilder = new SqlConnectionStringBuilder(baseConnectionString);
var masterConnectionBuilder = new SqlConnectionStringBuilder(baseConnectionString)
{
    InitialCatalog = "master"
};
masterConnectionBuilder.Remove("AttachDbFilename");

string? attachedDatabaseName = null;
using (var connection = new SqlConnection(masterConnectionBuilder.ConnectionString))
{
    connection.Open();

    using var command = connection.CreateCommand();
    command.CommandText = """
        SELECT TOP (1) DB_NAME(database_id)
        FROM sys.master_files
        WHERE physical_name = @databaseFile
        """;
    command.Parameters.AddWithValue("@databaseFile", databaseFile);
    attachedDatabaseName = command.ExecuteScalar() as string;
}

if (attachedDatabaseName is not null)
{
    databaseConnectionBuilder.Remove("AttachDbFilename");
    databaseConnectionBuilder.InitialCatalog = attachedDatabaseName;
}
else
{
    databaseConnectionBuilder.AttachDBFilename = databaseFile;
}

var connectionString = databaseConnectionBuilder.ConnectionString;

// Every module uses the same EF Core context and file-based database.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        connectionString,
        sqlServerOptions => sqlServerOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorNumbersToAdd: null)));

builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<SurveyBuilderService>();
builder.Services.AddScoped<SubmissionUploadService>();
// Integrated teammate services.
builder.Services.AddScoped<Helper>();
builder.Services.AddScoped<AnywhereEdureach.NotificationService>();
builder.Services.AddScoped<INotificationService, Online_Tuition_Systems.Services.NotificationService>();
builder.Services.AddScoped<Online_Tuition_Systems.Services.NotificationFeedService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();
builder.Services.AddHttpClient<GoogleRecaptchaService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(5);
});
builder.Services.Configure<RecaptchaOptions>(builder.Configuration.GetSection("GoogleRecaptcha"));
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Smtp"));
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();

// Course Management services.
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<ICourseAdministrationService, CourseAdministrationService>();
builder.Services.AddScoped<ILocalCourseImageStorage, LocalCourseImageStorage>();
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();
builder.Services.AddScoped<IBillingService, BillingService>();
builder.Services.AddScoped<IStripeCheckoutService, StripeCheckoutService>();
builder.Services.AddScoped<IPromotionService, PromotionService>();
builder.Services.AddScoped<IPromotionPricingService, PromotionPricingService>();

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

// Keep local development databases reproducible for the team by applying the
// committed EF Core migration chain when the application starts.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await database.Database.MigrateAsync();
}

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

app.Run();
