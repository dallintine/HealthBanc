using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Autofac.Extensions.DependencyInjection;
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
            var connectionString = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=pharmhallstracct;AccountKey=6L23/VXGOIk8QDo87OzGTs0wXbp7Vra2DPPWZ34AUGheDYtpNyCffJDW1oZZNvibJzfaYYLE+3ESaqwRryaM+g==;EndpointSuffix=core.windows.net");

            Log.Logger = new LoggerConfiguration()
                .WriteTo.AzureBlobStorage(connectionString, Serilog.Events.LogEventLevel.Information, "logfolder", "healthbanc.txt", "{Timestamp:G}{Message}{NewLine:1}{Exception:1}")
                .WriteTo.Console()
                .CreateLogger();

            var host = Host.CreateDefaultBuilder(args)
           .UseSerilog()
           .UseServiceProviderFactory(new AutofacServiceProviderFactory())
           .ConfigureWebHostDefaults(webHostBuilder =>
           {
               webHostBuilder
               .UseContentRoot(Directory.GetCurrentDirectory())
               .UseIISIntegration()
               .UseStartup<Startup>();
           })
           .Build();

            try
            {
                Log.Information("Application Starting Up");
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

            host.Run();
        }
    }
}
