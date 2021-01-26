using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Domain.Models
{
    public class InsuranceCompletionProfile
    {
        public InsuranceCompletionProfile()
        {

        }
        public InsuranceCompletionProfile(int userId, bool profileCompleted, bool tokenizationCompleted,string serviceUsed)
        {
            UserId = userId;
            ProfileCompleted = profileCompleted;
            TokenizationCompleted = tokenizationCompleted;
            ServiceUsed = serviceUsed;
        }

        public int Id { get; set; }
        public int UserId { get; set; }
        public bool ProfileCompleted { get; set; }
        public bool TokenizationCompleted { get; set; }
        public string ServiceUsed { get; set; }
    }
}
