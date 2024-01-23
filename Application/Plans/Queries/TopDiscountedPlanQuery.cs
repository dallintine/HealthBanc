using Application.Common.DTO;
using Application.Plans.DTO;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Plans.Queries
{
    public class TopDiscountedPlanQuery : IRequest<BaseResponse<List<TopDiscountedPlanDTO>>>
    {
    }
}
