using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HealthBanc.DTO.ApplicationUserDTOs
{
    public class HealthInsuredProfileStateDTO
    {
        public HealthInsuredProfileStateDTO()
        {

        }
        public HealthInsuredProfileStateDTO(bool profileCompleted, bool tokenizationCompleted)
        {
            ProfileCompleted = profileCompleted;
            TokenizationCompleted = tokenizationCompleted;
        }

        public bool ProfileCompleted { get; set; }
        public bool TokenizationCompleted { get; set; }
    }
}
