using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Helpers
{
    public class AxaMansardConfiguration
    {
        public string Apikey {get;set;}
        public string ApiSecret { get; set; }
        public string ClientKey { get; set; }
        public string EntityCode { get; set; }
        public string AxaMansardBaseAddress { get; set; }
        public string AxaMansardEnrollement { get; set; }
        public string AxaMansardToken { get; set; }
        public string AxaMansardDeactivation { get; set; }
    }
}
