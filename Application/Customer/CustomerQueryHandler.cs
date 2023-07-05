using Application.Common.DTO;
using Application.Customer.DTO;
using Application.Customer.Queries;
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
        IRequestHandler<ExportCustomerListQuery , BaseResponse>
    {
        private readonly ApplicationDbContext _context;

        public CustomerQueryHandler(ApplicationDbContext context)
        {
            _context = context;
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
                CustomerID = "3"
            }).ToListAsync(cancellationToken: cancellationToken);
            paginatedResponse.Data = customersData;
            paginatedResponse.Code = "00";
            paginatedResponse.Description = "Approved or Completed Successfully";
            return paginatedResponse;
        }

        public async Task<BaseResponse> Handle(ExportCustomerListQuery request, CancellationToken cancellationToken)
        {
            var customers = _context.Users.Where(x => !x.Email.Contains(".admin")).OrderByDescending(x => x.CreatedAt).AsQueryable();

            if (request.StartDate is null)
            {
                request.StartDate = new DateTime();
            }

            customers = customers.Where(x => x.CreatedAt.Date >= request.EndDate.Value.Date);
            if (request.EndDate != null)
            {
                customers = customers.Where(x => x.CreatedAt.Date <= request.EndDate.Value.Date);
            }
            return BaseResponse.Success("Customer data would be processed and sent to your email address");
        }
    }
}
