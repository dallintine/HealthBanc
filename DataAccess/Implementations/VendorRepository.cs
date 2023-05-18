using DataAccess.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Persistence.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Implementations
{
    public class VendorRepository : BaseRepository<Vendor>, IProductRepository
    {
        public VendorRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<List<Vendor>> FetchProducts()
        {
            return await _context.Vendors.Where(x => !x.IsDeleted).ToListAsync();
        }

        public async Task<Vendor> GetProductPlans( long productId)
        {
            return await _context.Vendors.Include(x => x.Plans).Where(x => x.Id == productId).SingleOrDefaultAsync();
        }
    }
}
