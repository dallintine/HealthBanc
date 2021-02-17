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
        public HealthInsuredProfileStateDTO(bool? corporateUser,bool? emailConfirmed, bool profileCompleted, bool tokenizationCompleted,string serviceUsed)
        {
            CorporateUser = corporateUser;
            EmailConfirmed = emailConfirmed;
            ProfileCompleted = profileCompleted;
            TokenizationCompleted = tokenizationCompleted;
            ServiceUsed = serviceUsed;
        }

        public bool? CorporateUser { get; set; }
        public bool? EmailConfirmed { get; set; }
        public bool ProfileCompleted { get; set; }
        public bool TokenizationCompleted { get; set; }
        public string ServiceUsed { get; set; }
    }
}
