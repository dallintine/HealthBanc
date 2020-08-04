using HealthBanc.Data;
using HealthBanc.DataAccess.Interfaces;
using HealthBanc.Domain.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.DataAccess.Implementation
{
    public class ServiceRepository : Interfaces.BaseRepository<Service>, IServiceRepository
    {
        public ServiceRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<List<Service>> GetServicesAsync()
        {
            var services = await _context.Services.ToListAsync();
            return services;
        }

        public async Task<Service> GetServiceById(int id)
        {
            var service = await _context.Services.FirstOrDefaultAsync(x => x.Id == id);
            return service;
        }
    }
}
