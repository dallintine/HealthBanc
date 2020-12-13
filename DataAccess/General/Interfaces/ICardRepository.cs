using Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccess.General.Interfaces
{
    public interface ICardRepository : IBaseRepository<DebitCard>
    {
        Task<DebitCard> CheckIfCardWasPreviouslyTokenized(int id, string lastFourDigit);
        Task<DebitCard> GetCardByIdAsync(int id, int userId);
        Task<DebitCard> GetPrimaryCard(int userId);
        Task<int> GetUserCardCount(int userId);
    }
}
