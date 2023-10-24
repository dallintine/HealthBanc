using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Common.Interfaces
{
    public interface IResponseCacheService
    {
        Task CacheResponseAsync(string cacheKey, object response, int absoluteExpireTime, TimeSpan unsuedExpiraryTime);
        Task<string> GetCachedResponseAsync(string cacheKey);
    }
}
