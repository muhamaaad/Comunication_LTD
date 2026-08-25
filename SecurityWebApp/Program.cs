using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using SecurityWebApp.Data;
using SecurityWebApp.Helpers;
using SecurityWebApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Version 1 read this file by hand, on every request, inside PasswordManager.
// Here it joins the normal configuration pipeline once, at start-up.
builder.Configuration.AddJsonFile("Properties/passwordOptions.json", optional: false, reloadOnChange: true);

var loginPolicy = builder.Configuration.GetSection("Authentication").Get<LoginPolicy>() ?? new LoginPolicy();

// Options
builder.Services.Configure<PasswordRules>(builder.Configuration.GetSection("PasswordRules"));
builder.Services.Configure<UsernameRules>(builder.Configuration.GetSection("UsernameRules"));
builder.Services.Configure<LoginPolicy>(builder.Configuration.GetSection("Authentication"));
builder.Services.Configure<PasswordResetPolicy>(builder.Configuration.GetSection("PasswordReset"));
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Email"));

// Data. SQLite resolves a relative file name against the current working
// directory, which changes with the folder you start the app from. Pin it to the
// project folder so there is only ever one app.db.
var connection = new SqliteConnectionStringBuilder(
    builder.Configuration.GetConnectionString("DefaultConnection"));

if (!Path.IsPathRooted(connection.DataSource))
{
    connection.DataSource = Path.Combine(builder.Environment.ContentRootPath, connection.DataSource);
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connection.ConnectionString));

// Application services
// Singleton: the word list is read from disk once instead of on every request.
builder.Services.AddSingleton<PasswordManager>();
builder.Services.AddScoped<LoginAttemptManager>();
builder.Services.AddScoped<PasswordHistoryService>();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();

// MVC. The anti-forgery filter is global so a new POST action cannot forget it,
// which is what happened to Login, Register and ForgotPassword in version 1.
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(loginPolicy.SessionTimeoutMinutes);
        options.SlidingExpiration = true;
        options.Cookie.Name = "SecurityWebApp.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    });

builder.Services.AddAuthorization();

// The reset flow carries its state in the session, exactly as in version 1.
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(loginPolicy.SessionTimeoutMinutes);
    options.Cookie.Name = "SecurityWebApp.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

var app = builder.Build();

// The tables are created by Database/schema.sql, not by the application. Check
// once at start-up so a missing database says what to do, instead of failing
// later with "no such table: Users".
using (var scope = app.Services.CreateScope())
{
    var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database;

    if (!TableExists(database, "Users"))
    {
        throw new InvalidOperationException(
            $"The database has no tables yet ({connection.DataSource}). " +
            "From the SecurityWebApp folder run:  sqlite3 app.db \".read Database/schema.sql\"");
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Every script and stylesheet is served from this origin, so the policy can stay
// strict: no inline script, no inline style, no third-party CDN.
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["Content-Security-Policy"] =
        "default-src 'self'; " +
        "script-src 'self'; " +
        "style-src 'self'; " +
        "img-src 'self' data:; " +
        "font-src 'self'; " +
        "form-action 'self'; " +
        "frame-ancestors 'none'; " +
        "base-uri 'self'";
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "DENY";
    headers["Referrer-Policy"] = "no-referrer";
    headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

    await next();
});

app.UseHttpsRedirection();
app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

static bool TableExists(DatabaseFacade database, string tableName)
{
    var dbConnection = database.GetDbConnection();
    dbConnection.Open();

    using var command = dbConnection.CreateCommand();
    command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = $name;";

    var parameter = command.CreateParameter();
    parameter.ParameterName = "$name";
    parameter.Value = tableName;
    command.Parameters.Add(parameter);

    return Convert.ToInt32(command.ExecuteScalar()) > 0;
}
