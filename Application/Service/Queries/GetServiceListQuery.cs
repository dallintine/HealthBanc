using Application.Common.DTO;
using Application.Common.Query;
using Application.Service.DTO;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Service.Queries
{
    public class GetServiceListQuery : PaginationQuery, IRequest<PageBaseResponse<List<ServiceDTO>>>
    {
    }
}
