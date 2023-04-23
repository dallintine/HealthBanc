using DataAccess.Implementations;
using DataAccess.Interfaces;
using Persistence.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess
{
    public class RepositoryWrapper : IRepositoryWrapper
    {
        private ApplicationDbContext _context;
        private IApplicationUserRepository _applicationUser;
        private IProductRepository _product;
        private IPlanRepository _plan;
        private IOneTimePasswordRepository _oneTimePassword;
        private IUserSessionRepository _userSession;

        public RepositoryWrapper(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<int> Save()
        {
            return await _context.SaveChangesAsync();
        }

        public IUserSessionRepository UserSession
        {
            get
            {
                if (_userSession == null)
                {
                    _userSession = new UserSessionRepository(_context);
                }
                return _userSession;
            }
        }

        public IOneTimePasswordRepository OneTimePassword
        {
            get
            {
                if (_oneTimePassword == null)
                {
                    _oneTimePassword = new OneTimePasswordRepository(_context);
                }
                return _oneTimePassword;
            }
        }

        public IApplicationUserRepository ApplicationUser
        {
            get
            {
                if (_applicationUser == null)
                {
                    _applicationUser = new ApplicationUserRepository(_context);
                }
                return _applicationUser;
            }
        }

        public IProductRepository Product
        {
            get
            {
                if (_product == null)
                {
                    _product = new ProductRepository(_context);
                }
                return _product;
            }
        }

        public IPlanRepository Plan
        {
            get
            {
                if (_plan == null)
                {
                    _plan = new PlanRepository(_context);
                }
                return _plan;
            }
        }
    }
}
