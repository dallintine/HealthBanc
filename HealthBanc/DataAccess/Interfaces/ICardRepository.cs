using HealthBanc.DataAccess.Implementation;
using HealthBanc.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.DataAccess.Interfaces
{
    public interface ICardRepository : IBaseRepository<DebitCard>
    {
        Task<DebitCard> CheckIfCardWasPreviouslyTokenized(int id, string signature);
        Task<DebitCard> GetCardByIdAsync(int id, int userId);
        Task<DebitCard> GetPrimaryCard(int userId);
        Task<TokenizationReference> GetPrimaryCardReference(int id);
        Task<int> GetUserCardCount(int userId);
    }
}
