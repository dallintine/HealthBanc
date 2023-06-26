using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Common.Interfaces
{
    public interface IHttpConnection
    {
        Task<T> DevAPIRequest<T>(HttpRequestMessage request, HttpContent content, string client, Dictionary<string, string> headers = null) where T : new();
    }
}
