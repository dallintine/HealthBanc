using Application.CommonDTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Subscriptions
{
    public class AllSubscriptionQuery : PaginationQuery
    {
        public string Status { get; set; }
    }
}
