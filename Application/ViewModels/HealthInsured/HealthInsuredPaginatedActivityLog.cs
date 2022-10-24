using DataAccess;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.ViewModels.HealthInsured
{
    public class HealthInsuredPaginatedActivityLog : PaginationQuery
    {
        public string Email { get; set; }
    }
}
