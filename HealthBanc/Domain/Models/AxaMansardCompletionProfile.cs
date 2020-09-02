using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.Domain.Models
{
    public class AxaMansardCompletionProfile
    {
        public AxaMansardCompletionProfile()
        {

        }
        public AxaMansardCompletionProfile(int superAdminId, bool profileCompleted, bool tokenizationCompleted)
        {
            SuperAdminId = superAdminId;
            ProfileCompleted = profileCompleted;
            TokenizationCompleted = tokenizationCompleted;
        }

        public int Id { get; set; }
        public int SuperAdminId { get; set; }
        public bool ProfileCompleted { get; set; }
        public bool TokenizationCompleted { get; set; }
    }
}
