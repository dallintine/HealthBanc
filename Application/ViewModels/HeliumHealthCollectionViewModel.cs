using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Application.ViewModels
{
    public class HeliumHealthCollectionViewModel
    {
        [Required]
        public string HealthServiceProviderName { get; set; }
        [Required]
        public string HealthServiveProviderType { get; set; }
        [Required]
        public string EmailAddress { get; set; }
        [Required]
        public string PhoneNumber { get; set; }
    }
}
