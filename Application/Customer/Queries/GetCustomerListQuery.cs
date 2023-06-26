using Application.Common.DTO;
using Application.Common.Query;
using Application.Customer.DTO;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Customer.Queries
{
    public class GetCustomerListQuery : PaginationQuery , IRequest<PageBaseResponse<List<CustomerDTO>>>
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
