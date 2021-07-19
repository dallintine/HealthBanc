using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Application.ViewModels
{
    public class HealthFinanceCollectionViewModel
    {
        [Required]
        public string Name { get; set; }
        [Required, DataType(DataType.EmailAddress)]
        public string Email { get; set; }
        [Required]
        public string BusinessName { get; set; }
        [Required]
        public string BusinessType { get; set; }
        [Required]
        public string BusinessAddress { get; set; }
        [Required, DataType(DataType.PhoneNumber)]
        public string Phonenumber { get; set; }
        [Required]
        public string Amount { get; set; }
        public string Comment { get; set; }
    }
}
