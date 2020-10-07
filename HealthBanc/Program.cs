using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Autofac.Extensions.DependencyInjection;
using HealthBanc.Data;
using HealthBanc.Logging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Twilio;

namespace HealthBanc
{
    public class Program
    {   
        public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
        {
            public ApplicationDbContext CreateDbContext(string[] args)
            {
                var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
                optionsBuilder.UseSqlServer("Server=10.0.41.101; Database=HealthBanc; User ID=sa; Password=tylent; Trusted_Connection=False; MultipleActiveResultSets=true");                                  

                return new ApplicationDbContext(optionsBuilder.Options);
            }
        }

        public static void Main(string[] args)
        {
            const string accountSid = "AC873adab76c91476279740be6e94ea5b1";
            const string authToken = "6153db4ce7db433412dca8837f4ba5fa";

            TwilioClient.Init(accountSid, authToken);

            var host = Host.CreateDefaultBuilder(args)
           .UseServiceProviderFactory(new AutofacServiceProviderFactory())
           .ConfigureWebHostDefaults(webHostBuilder =>
           {
               webHostBuilder
               .UseContentRoot(Directory.GetCurrentDirectory())
               .UseIISIntegration()
               .UseStartup<Startup>()
               .UseLogging();
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
                        Log.Fatal(ex, "�n error occurred during migration");
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
