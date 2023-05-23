using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Payment
{
    public class InitializePaymentDTO
    {
        [Required]
        public long PlanId { get; set; }
        [Required]
        public int Quantity { get; set; }
    }
}
