using Application.Card.DTO;
using Application.Card.Queries;
using Application.Common.DTO;
using Application.Common.Interfaces;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Persistence.Data;

namespace Application.Card
{
    public class CardQueryHandler : IRequestHandler<GetCardsQuery, BaseResponse<List<CardDTO>>>
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CardQueryHandler> _logger;
        private readonly ITokenService _tokenService;
        private readonly IMapper _mapper;

        public CardQueryHandler(ApplicationDbContext context , ILogger<CardQueryHandler> logger,ITokenService tokenService,IMapper mapper)
        {
            _context = context;
            _logger = logger;
            _tokenService = tokenService;
            _mapper = mapper;
        }

        public async Task<BaseResponse<List<CardDTO>>> Handle(GetCardsQuery request, CancellationToken cancellationToken)
        {
            var userId = _tokenService.GetClaims().FirstOrDefault(x => x.Type == "UserId")?.Value;

            var cards = await _context.Cards.Where(x => x.ApplicationUserId == long.Parse(userId) && !x.IsDeleted).ToListAsync(cancellationToken: cancellationToken);

            var cardDTOs = _mapper.Map<List<CardDTO>>(cards);

            return BaseResponse<List<CardDTO>>.Success(cardDTOs);
        }
    }
}
