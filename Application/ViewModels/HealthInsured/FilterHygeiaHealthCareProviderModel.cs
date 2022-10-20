using DataAccess;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.ViewModels.HealthInsured
{
    public class FilterHygeiaHealthCareProviderModel : PaginationQuery
    {
        public string State { get; set; }
        public string City { get; set; }
    }
}
