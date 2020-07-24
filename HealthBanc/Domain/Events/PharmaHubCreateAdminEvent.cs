using HealthBanc.Domain.Commands;
using HealthBanc.Messaging.Core.Events;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.Domain.Events
{
    public class PharmaHubCreateAdminEvent : Event
    {
        public int Id { get; set; }
        [Required]
        public int StockOrderLimit { get; set; }
        [Required]
        public string FirstName { get; protected set; }
        [Required]
        public string LastName { get; protected set; }
        [Required]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }
        [Required]
        [DataType(DataType.PhoneNumber)]
        public string PhoneNumber { get; set; }
        [Required]
        public int SuperAdminId { get; set; }

        [Required]
        public string SuperAdminEmail { get; set; }
        [Required]
        public int ClassOrRoleId { get; set; }

        public PharmaHubCreateAdminEvent()
        {

        }
        public PharmaHubCreateAdminEvent(PharmaHubCreateAdminCommand @event)
        {
            Id = @event.Id;
            StockOrderLimit = @event.StockOrderLimit;
            FirstName = @event.FirstName;
            LastName = @event.LastName;
            Email = @event.Email;
            PhoneNumber = @event.PhoneNumber;
            SuperAdminId = @event.SuperAdminId;
            SuperAdminEmail = @event.SuperAdminEmail;
            ClassOrRoleId = @event.ClassOrRoleId;
        }
    }
}
