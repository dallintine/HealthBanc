using Application.Vendors.DTO;
using AutoMapper;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Vendors
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Vendor,VendorDTO>();
        }
    }
}
