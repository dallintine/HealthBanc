using Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Infrastructure.Middlewares
{
    public class ResponseCacheService : IResponseCacheService
    {
        private readonly IMemoryCache _memoryCache;

        public ResponseCacheService(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        public async Task CacheResponseAsync(string cacheKey, Object response, int absoluteExpireTime, TimeSpan unsuedExpiraryTime)
        {
            if (response == null)
            {
                return;
            }
            var serializeOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
            //var serialisedResponse = JsonConvert.SerializeObject(response, serializeOptions);
            var serialisedResponse = System.Text.Json.JsonSerializer.Serialize(response, serializeOptions);

            _memoryCache.Set(cacheKey, serialisedResponse, new MemoryCacheEntryOptions
            {
                Priority = CacheItemPriority.High,
                AbsoluteExpiration = DateTime.Now.AddSeconds(absoluteExpireTime),
                SlidingExpiration = unsuedExpiraryTime,
                Size = 1024,
            });
        }

        public async Task<string> GetCachedResponseAsync(string cacheKey)
        {
            string value = string.Empty;
            var cacheResponse = _memoryCache.TryGetValue(cacheKey, out value);
            if (cacheResponse is false)
            {
                return null;
            }
            return value;
        }
    }
}
