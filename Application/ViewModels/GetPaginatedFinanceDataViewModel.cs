using DataAccess;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.ViewModels
{
    public class GetPaginatedFinanceDataViewModel : PaginationQuery
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
