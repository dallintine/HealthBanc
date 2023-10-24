using Application.Common.DTO;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Service.Commands
{
    public class CreateServiceCommand : IRequest<BaseResponse>
    {
        public string Name { get; set; }
        public string Tag { get; set; }
        public string ServiceDetailURL { get; set; }
        public string ImageUrl { get; set; }
        public string BackgroundColor { get; set; }
        public string ActiveColor { get; set; }
    }
}
