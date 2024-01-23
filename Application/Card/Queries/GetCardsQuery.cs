using Application.Card.DTO;
using Application.Common.DTO;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Card.Queries
{
    public class GetCardsQuery : IRequest<BaseResponse<List<CardDTO>>>
    {
    }
}
