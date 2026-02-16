using CcmsBankIsraelProxy.Middleware;
using CcmsBankIsraelProxy.Models;
using CcmsBankIsraelProxy.Services;
using NLog;
using NLog.Web;

var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();

try
{
    logger.Info("Starting CcmsBankIsraelProxy application");

    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    // Configuration
    builder.Services.Configure<BoiCardFields>(builder.Configuration.GetSection("Boi:CardFields"));

    // SRHR HttpClient with Windows Authentication
    builder.Services.AddHttpClient("Srhr")
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { UseDefaultCredentials = true });

    // Services
    builder.Services.AddScoped<IPictureService, PictureService>();
    builder.Services.AddScoped<ISrhrService, SrhrService>();
    builder.Services.AddScoped<IBoiHandler, BoiHandler>();

    builder.Services.AddControllers();

    WebApplication app = builder.Build();

    app.UseMiddleware<RequestLoggingMiddleware>();
    app.UseHttpsRedirection();
    app.UseAuthorization();
    app.MapControllers();

    logger.Info("CcmsBankIsraelProxy application started successfully");
    app.Run();
}
catch (Exception ex)
{
    logger.Error(ex, "Application stopped because of exception");
    throw;
}
finally
{
    LogManager.Shutdown();
}
