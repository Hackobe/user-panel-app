using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using UserPanelApp.Data;
using UserPanelApp.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")
                      ?? "Data Source=userpanel.db"));

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
    });

builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await SeedAdminAsync(db, app.Configuration);
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

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

static async Task SeedAdminAsync(AppDbContext db, IConfiguration configuration)
{
    var email = configuration["SeedAdmin:Email"];
    var password = configuration["SeedAdmin:Password"];

    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
    {
        return;
    }

    var normalizedEmail = email.Trim().ToLowerInvariant();

    if (await db.AppUsers.AnyAsync(user => user.Email == normalizedEmail))
    {
        return;
    }

    db.AppUsers.Add(new AppUser
    {
        Email = normalizedEmail,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
        Role = "Admin",
        CreatedAt = DateTime.UtcNow
    });

    await db.SaveChangesAsync();
}