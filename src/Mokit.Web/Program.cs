using Microsoft.EntityFrameworkCore;
using Mokit.Application.Interfaces;
using Mokit.Infrastructure;
using Mokit.Infrastructure.Data;
using Mokit.MockEngine.Middleware;
using Mokit.Web.Components;
using Mokit.Web.Services;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.AspNetCore.Components.Server.Circuits", Serilog.Events.LogEventLevel.Debug)
    .MinimumLevel.Override("Microsoft.AspNetCore.SignalR", Serilog.Events.LogEventLevel.Debug)
    .WriteTo.Console()
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Add Serilog
    builder.Host.UseSerilog();

    // Add services to the container
    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents()
        .AddCircuitOptions(options => 
        {
            options.DetailedErrors = true;
        });

    // Add Cascading Authentication State for Blazor
    builder.Services.AddCascadingAuthenticationState();

    // Add Infrastructure services (EF Core, Identity, etc.)
    builder.Services.AddInfrastructure(builder.Configuration);

    // Add SignalR for real-time updates
    builder.Services.AddSignalR();
    
    // Add Request Log Notifier for real-time log updates
    // Add Request Log Notifier for real-time log updates
    builder.Services.AddScoped<IRequestLogNotifier, SignalRRequestLogNotifier>();

    // Add Webhook Services
    builder.Services.AddHttpClient();
    builder.Services.AddSingleton<IWebhookJobQueue, Mokit.Infrastructure.Services.WebhookJobQueue>();
    builder.Services.AddHostedService<Mokit.HostManager.Services.WebhookProcessingService>();

    // Add UI Services
    builder.Services.AddScoped<IToastService, ToastService>();

    // Add Authentication pages
    builder.Services.AddRazorPages();

    // Add Controllers for API endpoints
    builder.Services.AddControllers();

    var app = builder.Build();

    // Apply migrations
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<MokitDbContext>();
        db.Database.Migrate();
    }

    // Configure the HTTP request pipeline
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error", createScopeForErrors: true);
    }

    app.UseStaticFiles();
    
    // Mock Routing Middleware - handles mock API requests
    // Must be before UseAntiforgery to allow mock requests without tokens
    app.UseMockRouting();
    
    app.UseAntiforgery();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapRazorPages();
    app.MapControllers();
    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();

    // Health check endpoint
    app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

    // SignalR hub
    app.MapHub<Mokit.Web.Hubs.MokitSignalR>("/Mokit");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
