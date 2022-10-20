using DataAccess;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.ViewModels.HealthInsured
{
    public class GetAdminActivityLogsViewModel : PaginationQuery
    {
        public string Channel { get; set; }
    }
}
