using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Domain.Models
{
    public class AxaMansardCompletionProfile
    {
        public AxaMansardCompletionProfile()
        {

        }
        public AxaMansardCompletionProfile(int userId, bool profileCompleted, bool tokenizationCompleted)
        {
            UserId = userId;
            ProfileCompleted = profileCompleted;
            TokenizationCompleted = tokenizationCompleted;
        }

        public int Id { get; set; }
        public int UserId { get; set; }
        public bool ProfileCompleted { get; set; }
        public bool TokenizationCompleted { get; set; }
    }
}
