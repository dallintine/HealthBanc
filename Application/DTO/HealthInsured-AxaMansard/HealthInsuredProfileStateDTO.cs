using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.DTO.HealthInsured_AxaMansard
{
    public class HealthInsuredProfileStateDTO
    {
        public HealthInsuredProfileStateDTO()
        {

        }
        public HealthInsuredProfileStateDTO(HealthInsuredPlan? healthInsuredPlan, bool? emailConfirmed, bool profileCompleted, bool tokenizationCompleted,string serviceUsed,
            bool referee = false)
        {
            HealthInsuredPlan = healthInsuredPlan;
            EmailConfirmed = emailConfirmed;
            ProfileCompleted = profileCompleted;
            TokenizationCompleted = tokenizationCompleted;
            ServiceUsed = serviceUsed;
            Referee = referee;
        }

        public HealthInsuredPlan? HealthInsuredPlan { get; set; }
        public bool? EmailConfirmed { get; set; }
        public bool ProfileCompleted { get; set; }
        public bool TokenizationCompleted { get; set; }
        public string ServiceUsed { get; set; }
        public bool Referee { get; set; }
    }
}
