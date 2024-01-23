using Application.Common.DTO;
using MediatR;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Customer.Commands
{
    public class UpdateCustomerProfile : IRequest<BaseResponse>
    {
        [Required]
        public string FirstName { get; set; }
        [Required]
        public string LastName { get; set; }
        public string Phonenumber { get; set; }
        public string Address { get; set; }
        public string State { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public UpdateHealthDetail UpdateHealthDetail { get; set; }
    }

    public class UpdateHealthDetail
    {
        public string Weight { get; set; }
        public string Height { get; set; }
        public string Allergies { get; set; }
    }
}
