using Domain.Entities.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Persistence.Data
{
    public class LogDbContext : DbContext
    {
        public LogDbContext(DbContextOptions<LogDbContext> options) : base(options)
        {
        }
        public DbSet<LogResponse> LogResponses { get; set; }



        //protected override void OnConfiguring(DbContextOptionsBuilder options)
        //{
        //    // connect to mysql with connection string from app settings
        //    var connectionString = Configuration.GetConnectionString("LogConnection");
        //    options.UseMySQL(connectionString);
        //}


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
