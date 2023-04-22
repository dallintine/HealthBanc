using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IHttpConnection
    {
        Task<T> DevAPIRequest<T>(HttpRequestMessage request, HttpContent content, string client) where T : new();
    }
}
