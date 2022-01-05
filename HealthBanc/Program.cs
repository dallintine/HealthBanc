using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Autofac.Extensions.DependencyInjection;
using Domain.Models;
using HealthBanc.Logging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Azure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Persistence;
using Serilog;

namespace HealthBanc
{
    public class Program
    {
        public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
        {
            public ApplicationDbContext CreateDbContext(string[] args)
            {
                var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
                optionsBuilder.UseSqlServer("Server=10.0.41.101; Database=HealthBancHygeiaImplementation; User ID=sa; Password=tylent; Trusted_Connection=False; MultipleActiveResultSets=true");
                //optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=HealthBancV3.5;Trusted_Connection=True;MultipleActiveResultSets=true");

                return new ApplicationDbContext(optionsBuilder.Options);
            }
        }

        public static void Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args)
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
                        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
                        var roleManager = services.GetRequiredService<RoleManager<AppRole>>();

                        //context.Database.EnsureDeleted();
                        //context.Database.Migrate();
                        //Seed.SeedData(context, userManager, roleManager).Wait();
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
