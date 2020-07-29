using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.Messages.Events
{
    public class AdminCreated : IEvent
    {
        public int Id { get; set; }
        public int StockOrderLimit { get; set; }
        public string FirstName { get; protected set; }
        public string LastName { get; protected set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public int SuperAdminId { get; set; }
        public string SuperAdminEmail { get; set; }
        public int ClassOrRoleId { get; set; }
        public AdminCreated()
        {

        }

        [JsonConstructor]
        public AdminCreated(int id, int stockOrderLimit, string firstName, string lastName, string email, string phoneNumber, int superAdminId, string superAdminEmail, int classOrRoleId)
        {
            Id = id;
            StockOrderLimit = stockOrderLimit;
            FirstName = firstName;
            LastName = lastName;
            Email = email;
            PhoneNumber = phoneNumber;
            SuperAdminId = superAdminId;
            SuperAdminEmail = superAdminEmail;
            ClassOrRoleId = classOrRoleId;
        }
    }
    
}
