using Application.Card.Commands;
using Application.Card.DTO;
using Application.Common.DTO;
using Application.Common.Interfaces;
using AutoMapper;
using Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Persistence.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Card
{
    public class CardCommandHandler : IRequestHandler<DeleteCardCommand, BaseResponse>,
        IRequestHandler<SetDefaultCardCommand, BaseResponse>,
        IRequestHandler<AddCardCommand, BaseResponse<CardDTO>>
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CardCommandHandler> _logger;
        private readonly ITokenService _tokenService;
        private readonly IMapper _mapper;
        private readonly IPaystackService _paystackService;

        public CardCommandHandler(ApplicationDbContext context, ILogger<CardCommandHandler> logger, ITokenService tokenService, IMapper mapper, IPaystackService paystackService)
        {
            _context = context;
            _logger = logger;
            _tokenService = tokenService;
            _mapper = mapper;
            _paystackService = paystackService;
        }

        public async Task<BaseResponse> Handle(DeleteCardCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Delete Card Command Request [ID : {request.Id} ]");

            var userId = _tokenService.GetClaims().FirstOrDefault(x => x.Type == "UserId")?.Value;

            var card = await _context.Cards.SingleOrDefaultAsync(x => x.ApplicationUserId == long.Parse(userId) && x.Id == request.Id && !x.IsDeleted,
                cancellationToken: cancellationToken);

            if (card == null) return BaseResponse.Failure("25", "No record found, Card not found");

            if (card.IsDefaultCard) return BaseResponse.Failure("06", "Cannot delete default card");

            card.IsDeleted = true;
            _context.Cards.Update(card);
            await _context.SaveChangesAsync();

            return BaseResponse.Success();
        }

        public async Task<BaseResponse> Handle(SetDefaultCardCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Set Default Command Request [Id : {request.Id} ]");
            var userId = _tokenService.GetClaims().FirstOrDefault(x => x.Type == "UserId")?.Value;

            var card = await _context.Cards.SingleOrDefaultAsync(x => x.ApplicationUserId == long.Parse(userId) && x.Id == request.Id && !x.IsDeleted,
                cancellationToken: cancellationToken);

            if (card == null) return BaseResponse.Failure("25", "No record found, Card not found");

            card.IsDefaultCard = true;
            _context.Cards.Update(card);
            await _context.SaveChangesAsync();

            return BaseResponse.Success();
        }

        public async Task<BaseResponse<CardDTO>> Handle(AddCardCommand request, CancellationToken cancellationToken)
        {
            var userId = _tokenService.GetClaims().FirstOrDefault(x => x.Type == "UserId")?.Value;

            _logger.LogInformation($"Add Card Command Request [Reference : {request.Reference} ]");
            var validatePayment = await _paystackService.VerifyPayment(request.Reference);
            if (!validatePayment.Status) return BaseResponse<CardDTO>.Failure(null,"06", validatePayment.Message);
            var data = validatePayment.Data;

            var existingCard = _context.Cards.Any(x => x.ApplicationUserId == long.Parse(userId) && !x.IsDeleted && x.Signature == data.Authorization.Signature);
            if (existingCard) return BaseResponse<CardDTO>.Failure(null, "26", "Duplicate card exist");            

            var cardCount = _context.Cards.Count();
            var card = new Domain.Entities.Card
            {
                AuthorizationCode = data.Authorization.AuthorizationCode,
                LastFourDigit = data.Authorization.Last4,
                Bank = data.Authorization.Bank,
                CardType = data.Authorization.Brand,
                CardHolder = $"{data.Customer?.FirstName} {data.Customer?.LastName}",
                ExpMonth = data.Authorization.ExpMonth,
                ExpYear = data.Authorization.ExpYear,
                Email = data.Customer.Email,
                Signature = data.Authorization.Signature,
                ApplicationUserId = long.Parse(userId),
                IsDefaultCard = cardCount == 0,
            };

            _context.Cards.Add(card);
            await _context.SaveChangesAsync();

            var cardDTO = _mapper.Map<CardDTO>(card);
            return BaseResponse<CardDTO>.Success(cardDTO);
        }
    }
}
