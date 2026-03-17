using FluentValidation;
using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.FeatureManagement;
using PLN.Azure.Function.Common.Extensions;
using PLN.Azure.Function.Common.Middlewares;
using PLN.Azure.Function.Common.Services;
using PLN_Broadband.Azure.Function.Options;
using POCoutlookManageIdentity.OutlookManageTrigger.Services;

FunctionsApplicationBuilder builder = FunctionsApplication.CreateBuilder(args);
builder.ConfigureFunctionsWebApplication();
AddSettings(builder);

#pragma warning disable S125 // Sections of code should not be commented out.  It is intended to be used for Azure App Configuration setup.
//AddAzureAppConfiguration(builder);
#pragma warning restore S125 // Sections of code should not be commented out.  It is intended to be used for Azure App Configuration setup.

builder.Services.AddFeatureManagement();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights()
    .AddSingleton<ITelemetryModule, DependencyTrackingTelemetryModule>()
    .ConfigureTelemetryModule<DependencyTrackingTelemetryModule>((module, _) =>
    {
        module.EnableSqlCommandTextInstrumentation = true;
    })
    .AddScoped<FunctionContextService>()
    .AddValidatorsFromAssemblyContaining<Program>()
    .AddCommonServices()
    .AddFunctionServices(ServiceLifetime.Scoped)
    .AddFunctionsSettings()
    .LogFunctionSettings()
    .AddPlenitudeFunctionAppLogging()
    .AddHttpClient();

// Register OutlookManageTrigger service which requires IConfiguration and HttpClient
builder.Services.AddHttpClient<IOutlookManageTriggerService, OutlookManageTriggerService>();

builder.UseMiddleware<FunctionContextMiddleware>();
builder.UseMiddleware<ExceptionHandlerMiddleware>();


await builder.Build().RunAsync();

static FunctionsApplicationBuilder AddSettings(FunctionsApplicationBuilder builder)
{
    builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);

#if DEBUG
    var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "UNKNOWN_ENV";
    builder.Configuration.AddJsonFile($"local.settings.{environment}.json", optional: true, reloadOnChange: false);
#endif

    return builder;
}

#pragma warning disable CS8321 // Local function is declared but never used. It is intended to be used for Azure App Configuration setup.
static void AddAzureAppConfiguration(FunctionsApplicationBuilder builder)
{
    const int RefreshIntervalSeconds = 5;

    builder.Configuration.AddAzureAppConfiguration(options =>
    {
        var connectionString = builder.Configuration.GetValue<string>("AzureAppConfigurationConnectionString") ??
            throw new InvalidOperationException("The environment variable 'AzureAppConfigurationConnectionString' is not set or is empty.");

        options.Connect(connectionString)
                .Select("*")
                .ConfigureRefresh(refreshOptions =>
                {
                    refreshOptions.SetRefreshInterval(TimeSpan.FromSeconds(RefreshIntervalSeconds));
                    refreshOptions.RegisterAll();
                }
                )
                .UseFeatureFlags();
    });

    builder.Services.AddAzureAppConfiguration();
    builder.UseAzureAppConfiguration();
}
#pragma warning restore CS8321 // Local function is declared but never used. It is intended to be used for Azure App Configuration setup.