using System;
using System.Collections.Generic;
using System.Text;

namespace Domain
{
    public class UserSession
    {
        public long Id { get; set; }
        public DateTime ExpireDate { get; set; }
        public string Browser { get; set; }
        public int UserId { get; set; }
        public string Device { get; set; }
    }
}
