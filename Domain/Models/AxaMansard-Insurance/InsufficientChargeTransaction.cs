using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Models.AxaMansard_Insurance
{
    public class InsufficientChargeTransaction
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int AxaMansardUserProfileId { get; set; }
        public DateTime DateScheduled { get; set; }
        public DateTime MatureDate { get; set; }
        public string JobId { get; set; }
        public AxaMansardUserProfile AxaMansardUserProfile { get; set; }
    }
}
