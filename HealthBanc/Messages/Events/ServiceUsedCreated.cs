using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.Messages.Events
{
    [MessageNamespace("identity")]
    public class ServiceUsedCreated : IEvent
    {
        public int Id { get; set; }
        public string Email { get; set; }

        [JsonConstructor]
        public ServiceUsedCreated(int id,string email)
        {
            Id = id;
            Email = email;
        }
        public ServiceUsedCreated()
        {

        }
    }
}
