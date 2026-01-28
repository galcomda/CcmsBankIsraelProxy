using CcmsBankIsraelProxy.Middleware;
using CcmsBankIsraelProxy.Models;
using CcmsBankIsraelProxy.Services;
using NLog;
using NLog.Web;

// Early init of NLog to allow startup and exception logging
var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();

try
{
    logger.Info("Starting CcmsBankIsraelProxy application");

    var builder = WebApplication.CreateBuilder(args);

    // NLog: Setup NLog for Dependency injection
    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    // Add HttpClient factory
    builder.Services.AddHttpClient();

    // Configure BOI card fields mapping
    builder.Services.Configure<BoiCardFields>(builder.Configuration.GetSection("Boi:CardFields"));

    // Register BOI services
    builder.Services.AddScoped<ISapEmployeeService, SapEmployeeService>();
    builder.Services.AddScoped<IPictureService, PictureService>();
    builder.Services.AddScoped<ISmsService, SmsService>();
    builder.Services.AddScoped<IBoiHandler, BoiHandler>();

    builder.Services.AddControllers();

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    
    // Add request logging middleware
    app.UseMiddleware<RequestLoggingMiddleware>();

    app.UseHttpsRedirection();

    app.UseAuthorization();

    app.MapControllers();

    logger.Info("CcmsBankIsraelProxy application started successfully");
    
    app.Run();
}
catch (Exception ex)
{
    // NLog: catch setup errors
    logger.Error(ex, "Application stopped because of exception");
    throw;
}
finally
{
    // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
    LogManager.Shutdown();
}
