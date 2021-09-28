using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Models
{
    public class UserSession
    {
        public long Id { get; set; }
        public DateTime SessionExpireDate { get; set; }
        public string Browser { get; set; }
        public int UserId { get; set; }
        public string DeviceIp { get; set; }
    }
}
