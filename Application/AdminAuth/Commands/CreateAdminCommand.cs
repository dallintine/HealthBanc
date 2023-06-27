using Application.Common.DTO;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.AdminAuth.Commands
{
    public class CreateAdminCommand : IRequest<BaseResponse>
    {
        public string UniqueUsername { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
}
