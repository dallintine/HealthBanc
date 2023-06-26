using Application.Common.DTO;
using Application.Service.DTO;
using Application.Service.Queries;
using AutoMapper;
using DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Persistence.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Service;

public class ServiceQueryHandler : IRequestHandler<GetServiceListQuery, PageBaseResponse<List<ServiceDTO>>>
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public ServiceQueryHandler(ApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PageBaseResponse<List<ServiceDTO>>> Handle(GetServiceListQuery request, CancellationToken cancellationToken)
    {
        var services = await _context.Services.Where(x => !x.IsDeleted).ToListAsync(cancellationToken);
        var serviceDTOs = _mapper.Map<List<Domain.Entities.Service>, List<ServiceDTO>>(services);
        return PageBaseResponse<List<ServiceDTO>>.Success(serviceDTOs, 1, request.PageSize, 1, serviceDTOs.Count);
    }
}
