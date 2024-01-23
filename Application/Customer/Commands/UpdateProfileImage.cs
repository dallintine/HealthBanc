using Application.Common.DTO;
using Application.Image.DTO;
using MediatR;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Customer.Commands
{
    public class UpdateProfileImage : IRequest<BaseResponse<ImagesURLDTO>>
    {
        public string Base64Image { get; set; }
        public string FileName { get; set; }
    }
}
