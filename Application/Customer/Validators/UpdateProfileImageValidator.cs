using Application.Customer.Commands;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Customer.Validators
{
    public class UpdateProfileImageValidator : AbstractValidator<UpdateProfileImage>
    {
        public UpdateProfileImageValidator()
        {
            RuleFor(x => x.Base64Image).NotEmpty().NotNull();
            RuleFor(x => x.FileName).NotEmpty().NotNull();
        }
    }
}
