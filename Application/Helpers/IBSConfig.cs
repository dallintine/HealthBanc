using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Helpers
{
    public class IBSConfig
    {
        public string AppId { get; set; }
        public string TransferRequestType { get; set; }
        public string EnquiryRequestType { get; set; }
        public string SharedKey { get; set; }
        public string SharedVector { get; set; }
    }
}
