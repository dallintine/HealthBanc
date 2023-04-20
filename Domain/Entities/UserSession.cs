using Domain.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class UserSession : BaseEntity
    {
        public DateTime ExpiryDate{ get; set; }
        public string Browser { get; set; }
        public long UserId { get; set; }
        public string DeviceIp { get; set; }
    }
}
