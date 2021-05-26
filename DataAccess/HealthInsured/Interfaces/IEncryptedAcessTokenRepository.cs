using DataAccess.General.Interfaces;
using Domain.Models.Axa_Hygeia_Insurance;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.HealthInsured.Interfaces
{
    public interface IEncryptedAcessTokenRepository : IBaseRepository<EncryptedAcessToken>
    {
        Task<EncryptedAcessToken> GetEncryptedToken();
    }
}
