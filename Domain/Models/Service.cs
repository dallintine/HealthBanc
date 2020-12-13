using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Domain.Models
{
    public class Service
    {
        public int Id { get; set; }
        public string Name { get; set; }
        [JsonIgnore]
        public List<Notification> Notifications { get; set; }
    }
}
