using AutoMapper;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Models
{
    public class Mapper:Profile
    {
        public Mapper()
        {
            CreateMap<FileModel, InsuranceUserProfile>()
                .ForMember(dto => dto.Id, mem => mem.Ignore())

                .ForMember(src => src.Surname, opt => opt.MapFrom(dest => dest.LastName))
                .ForMember(src => src.Othernames, opt => opt.MapFrom(dest => dest.FirstName))
                .ForMember(src => src.Premium, opt => opt.MapFrom(dest => dest.PremiumFee))
                .ForMember(src => src.ContactAddress, opt => opt.MapFrom(dest => dest.Address))
                .ForMember(src => src.Gender, opt => opt.MapFrom(dest => dest.Gender))
                .ForMember(src => src.DateOfBirth, opt => opt.MapFrom(dest => dest.DateOfBirth))
                .ForMember(src => src.PhoneNumber, opt => opt.MapFrom(dest => dest.PhoneNumber)).ReverseMap();

            //.ForMember(dest => dest.Othernames, source => source.MapFrom(source => source.FirstName))
            //.ForMember(dest => dest.Surname, source => source.MapFrom(source => source.LastName))
            //.ForMember(dest => dest.Premium, source => source.MapFrom(source => source.PremiumFee))
            //.ForMember(dest => dest.ContactAddress, source => source.MapFrom(source => source.Address))
            //.ForMember(dest => dest.Gender, source => source.MapFrom(source => source.Gender))
            //.ForMember(dest => dest.DateOfBirth, source => source.MapFrom(source => source.DateOfBirth))
            //.ForMember(dest => dest.PhoneNumber, source => source.MapFrom(source => source.PhoneNumber));


        }
    }
}
