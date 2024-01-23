using Application.Common.DTO;
using Application.Common.Interfaces;
using Application.Customer.DTO;
using Application.Customer.Queries;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Customer
{
    public class CustomerQueryHandler : IRequestHandler<GetCustomerListQuery, PageBaseResponse<List<CustomerDTO>>> , 
        IRequestHandler<ExportCustomerListQuery , BaseResponse<byte[]>>,
        IRequestHandler<GetCustomerProfileQuery, BaseResponse<CustomerProfileDTO>>,
        IRequestHandler<GetProfileIdQuery, BaseResponse<CustomerIdDTO>>
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileService _fileService;
        private readonly ITokenService _tokenService;
        private readonly IMapper _mapper;

        public CustomerQueryHandler(ApplicationDbContext context, IFileService fileService,ITokenService tokenService,IMapper mapper)
        {
            _context = context;
            _fileService = fileService;
            _tokenService = tokenService;
            _mapper = mapper;
        }

        public  async Task<PageBaseResponse<List<CustomerDTO>>> Handle(GetCustomerListQuery request, CancellationToken cancellationToken)
        {
            var paginatedResponse = new PageBaseResponse<List<CustomerDTO>>();
            if (request.StartDate is null)
            {
                request.StartDate = new DateTime();
            }
            var customers = _context.Users.Where(x => !x.Email.Contains(".admin")).OrderByDescending(x => x.CreatedAt).AsQueryable();


            customers = customers.Where(x => x.CreatedAt.Date >= request.StartDate.Value.Date);
            if (request.EndDate != null)
            {
                customers = customers.Where(x => x.CreatedAt.Date <= request.EndDate.Value.Date);
            }
            if (!String.IsNullOrEmpty(request.SearchText))
            {
                customers = customers.Where(x => x.Email.ToLower().Contains(request.SearchText.ToLower()) || x.FirstName.ToLower().Contains(request.SearchText.ToLower())
                || x.LastName.ToLower().Contains(request.SearchText.ToLower()) || x.PhoneNumber.Contains(request.SearchText));
            }
            var skip = (request.PageNumber - 1) * request.PageSize;

            var filteredQueryable = customers.Skip(skip).Take(request.PageSize);
            var recordCount = await customers.CountAsync();
            paginatedResponse.RecordCount = recordCount;
            paginatedResponse.PageCount = Convert.ToInt32(Math.Ceiling((double)recordCount / (double)request.PageSize));
            paginatedResponse.PageNumber = request.PageNumber >= 1 ? request.PageNumber : (int?)null;
            paginatedResponse.PageSize = request.PageSize >= 1 ? request.PageSize : (int?)null;
            var customersData = await filteredQueryable.Select(x => new CustomerDTO
            {
                FirstName = x.FirstName,
                LastName = x.LastName,
                Email = x.Email,
                PhoneNumber = x.PhoneNumber,
                LastLoginDate = x.LastLoginDate,
                Address = x.Address,
                CreatedAt = x.CreatedAt,
                CustomerID = x.Id.ToString().PadLeft(6, '0')
            }).ToListAsync(cancellationToken: cancellationToken);
            paginatedResponse.Data = customersData;
            paginatedResponse.Code = "00";
            paginatedResponse.Description = "Approved or Completed Successfully";
            return paginatedResponse;
        }

        public async Task<BaseResponse<byte[]>> Handle(ExportCustomerListQuery request, CancellationToken cancellationToken)
        {
            var customers = _context.Users.Where(x => !x.Email.Contains(".admin")).OrderByDescending(x => x.CreatedAt).AsQueryable();

            if (request.StartDate is null)
            {
                request.StartDate = new DateTime();
            }

            customers = customers.Where(x => x.CreatedAt.Date >= request.StartDate.Value.Date);

            if (request.EndDate != null)
            {
                customers = customers.Where(x => x.CreatedAt.Date <= request.EndDate.Value.Date);
            }
            var count = await customers.CountAsync();

            var customersData = customers.Select(x => new CustomerExportDTO
            {
                FirstName = x.FirstName,
                LastName = x.LastName,
                Email = x.Email,
                PhoneNumber = x.PhoneNumber,
                LastLoginDate = x.LastLoginDate.ToShortDateString(),
                Address = x.Address,
                CreatedAt = x.CreatedAt.ToShortDateString(),
                CustomerId = x.Id != 0 ? x.Id.ToString().PadLeft(6, '0') : null
            }).ToList();
            var fileData = _fileService.GenerateGenericExcelFile(customersData, $"customerdata-{Guid.NewGuid().ToString()}");
            return BaseResponse<byte[]>.Success(fileData,"00" ,"Customer data would be processed and sent to your email address");
        }

        public async  Task<BaseResponse<CustomerProfileDTO>> Handle(GetCustomerProfileQuery request, CancellationToken cancellationToken)
        {
            var userId = _tokenService.GetClaims().FirstOrDefault(x => x.Type == "UserId")?.Value;
            var user = await _context.Users.Include(x => x.HealthDetail).SingleOrDefaultAsync(x => x.Id == long.Parse(userId), cancellationToken: cancellationToken);

            var profileDTO = _mapper.Map<CustomerProfileDTO>(user);

            return BaseResponse<CustomerProfileDTO>.Success(profileDTO);
        }

        public async Task<BaseResponse<CustomerIdDTO>> Handle(GetProfileIdQuery request, CancellationToken cancellationToken)
        {
            var userId = _tokenService.GetClaims().FirstOrDefault(x => x.Type == "UserId")?.Value;
            var user = await _context.Users.FindAsync(new object[] { long.Parse(userId) }, cancellationToken: cancellationToken);

            return BaseResponse<CustomerIdDTO>.Success(new CustomerIdDTO
            {
                ProfileImageURL = user.ProfileImageURL,
                CustomerID = user.Id.ToString().PadLeft(6, '0')
            });
        }
    }
}
