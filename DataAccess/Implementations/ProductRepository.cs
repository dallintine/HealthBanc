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
    public class ProductRepository : BaseRepository<Product>, IProductRepository
    {
        public ProductRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<List<Product>> FetchProducts()
        {
            return await _context.Products.Where(x => !x.IsDeleted).ToListAsync();
        }

        public async Task<Product> GetProductPlans( long productId)
        {
            return await _context.Products.Include(x => x.Plans).Where(x => x.Id == productId).SingleOrDefaultAsync();
        }
    }
}
