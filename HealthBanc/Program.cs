using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HealthBanc.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace HealthBanc
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                 .AddEnvironmentVariables()
                 .AddJsonFile("appsettings.json")
                 .Build();

            var connectionString = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=pharmhallstracct;AccountKey=6L23/VXGOIk8QDo87OzGTs0wXbp7Vra2DPPWZ34AUGheDYtpNyCffJDW1oZZNvibJzfaYYLE+3ESaqwRryaM+g==;EndpointSuffix=core.windows.net");

            Log.Logger = new LoggerConfiguration()
                .WriteTo.AzureBlobStorage(connectionString)
                .ReadFrom.Configuration(configuration)
                .CreateLogger();

            try
            {
                Log.Information("Application Starting Up");
                var host = CreateHostBuilder(args).Build();
                using (var scope = host.Services.CreateScope())
                {
                    var services = scope.ServiceProvider;
                    try
                    {
                        var context = services.GetRequiredService<ApplicationDbContext>();
                        context.Database.Migrate();
                    }
                    catch (Exception ex)
                    {
                        Log.Fatal(ex, "Än error occurred during migration");
                    }
                }
                host.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "The application failed to start correctly.");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
