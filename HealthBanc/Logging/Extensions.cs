using System;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Filters;

namespace HealthBanc.Logging
{
    public static class Extensions
    {
        public static IWebHostBuilder UseLogging(this IWebHostBuilder webHostBuilder, string applicationName = null)
            => webHostBuilder.UseSerilog((context, loggerConfiguration) =>
            {
                var azureBlobOptions = context.Configuration.GetOptions<AzureBlobOptions>("azurebloboption");
                var appOptions = context.Configuration.GetOptions<AppOptions>("app");
                var seqOptions = context.Configuration.GetOptions<SeqOptions>("seq");
                var serilogOptions = context.Configuration.GetOptions<SerilogOptions>("serilog");
                var applicationInsightsOptions = context.Configuration.GetOptions<ApplicationInsightsOptions>("applicationinsightsoptions");
                if (!Enum.TryParse<LogEventLevel>(serilogOptions.Level, true, out var level))
                {
                    level = LogEventLevel.Warning;
                }

                applicationName = string.IsNullOrWhiteSpace(applicationName) ? appOptions.Name : applicationName;
                loggerConfiguration.Enrich.FromLogContext()
                    .MinimumLevel.Is(level)
                    .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
                    .Enrich.WithProperty("ApplicationName", applicationName);
                Configure(loggerConfiguration, Serilog.Events.LogEventLevel.Information, seqOptions, serilogOptions,azureBlobOptions, applicationInsightsOptions);
            });

        private static void Configure(LoggerConfiguration loggerConfiguration, LogEventLevel level,SeqOptions seqOptions, SerilogOptions serilogOptions,AzureBlobOptions azureBlobOptions,
            ApplicationInsightsOptions applicationInsightsOptions)
        {            

            if (seqOptions.Enabled)
            {
                loggerConfiguration.WriteTo.Seq(seqOptions.Url, apiKey: seqOptions.ApiKey);
            }

            if (serilogOptions.ConsoleEnabled)
            {
                loggerConfiguration.WriteTo.Console();
            }

            if (azureBlobOptions.Enabled)
            {
                loggerConfiguration.WriteTo.AzureBlobStorage(azureBlobOptions.ConnectionString, level, azureBlobOptions.StorageContainerName, azureBlobOptions.StorageFileName
                   , azureBlobOptions.OutputTemplate, false, null, null, false, null, null, 3000000, 4);
                loggerConfiguration.MinimumLevel.Verbose().MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", Serilog.Events.LogEventLevel.Warning);
                loggerConfiguration.MinimumLevel.Verbose().MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Query", Serilog.Events.LogEventLevel.Warning);
            }

            if (applicationInsightsOptions.Enabled)
            {
                loggerConfiguration.WriteTo.ApplicationInsights(new TelemetryConfiguration { InstrumentationKey = applicationInsightsOptions.InstrumentalKey}, TelemetryConverter.Traces);
            }            
        }
    }
}