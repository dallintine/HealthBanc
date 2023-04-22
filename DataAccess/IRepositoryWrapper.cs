using DataAccess.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess
{
    public interface IRepositoryWrapper
    {
        IProductRepository Product { get; }
        IPlanRepository Plan { get; }
        IApplicationUserRepository ApplicationUser { get; }
        IOneTimePasswordRepository OneTimePassword { get; }

        Task<int> Save();
    }
}
