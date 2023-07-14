using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities.Common
{
    public class LogResponse
    {
        public int Id { get; set; }
        public string RequestId { get; set; }
        public string ServiceName { get; set; }
        public string ActionName { get; set; }//
        public string Route { get; set; }
        public string StatusCode { get; set; }
        public string RequestMethod { get; set; }
        public string UserId { get; set; }
        public string ContentType { get; set; }
        public string RequestHeader { get; set; }
        public string ResponseHeader { get; set; }
        public string QueryString { get; set; }
        public string HostName { get; set; }
        public string Port { get; set; }
        public DateTime DateLogged { get; set; }
        public string RequestDetails { get; set; }
        public string ResponseDetails { get; set; }
    }
}
