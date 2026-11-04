using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UserPanelApp.Data;

namespace UserPanelApp.Tests;

public sealed class UserPanelFactory : WebApplicationFactory<Program>
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "user-panel-tests", Guid.NewGuid().ToString("N"));
    private readonly bool _seedAdmin;

    public string Password { get; } = "Demo-" + Guid.NewGuid().ToString("N");
    public const string AdminEmail = "admin@example.test";

    public UserPanelFactory(bool seedAdmin = false)
    {
        _seedAdmin = seedAdmin;
        Directory.CreateDirectory(_directory);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Testing does not load the developer's user-secrets.
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = $"Data Source={Path.Combine(_directory, "test.db")};Pooling=False",
                ["SeedAdmin:Email"] = _seedAdmin ? AdminEmail : "",
                ["SeedAdmin:Password"] = _seedAdmin ? Password : "",
                ["Logging:LogLevel:Default"] = "Warning",
                ["Logging:LogLevel:Microsoft.EntityFrameworkCore"] = "Warning"
            }));
        builder.ConfigureServices(services =>
            services.AddDataProtection().UseEphemeralDataProtectionProvider());
    }

    public HttpClient CreateBrowser() => CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        HandleCookies = true,
        BaseAddress = new Uri("https://localhost")
    });

    public async Task<T> ReadDatabaseAsync<T>(Func<AppDbContext, Task<T>> query)
    {
        using var scope = Services.CreateScope();
        return await query(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }
}
