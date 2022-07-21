using Application.DTO;
using Application.HealthInsured_AxaMansard_Service.Insurance;
using Application.Interfaces;
using Application.Services.HealthInsured_AxaMansard.Insurance;
using AutoMapper;
using DataAccess;
using Domain.Enums;
using Domain.Models;
using Domain.Models.Axa_Hygeia_Insurance;
using Domain.Models.ReportAndLogs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.Card
{
    public class CardService
    {
        private readonly IRepositoryWrapper _repositoryWrapper;
        private readonly ILogger<CardService> _logger;
        private readonly InsuranceService _insuranceService;
        private readonly IMapper _mapper;
        private readonly IUniqueIdentifier _uniqueIdentifier;
        private readonly TokenizationService _tokenizationService;

        public CardService(IRepositoryWrapper repositoryWrapper,ILogger<CardService> logger,InsuranceService insuranceService,IMapper mapper
            ,IUniqueIdentifier uniqueIdentifier,TokenizationService tokenizationService)
        {
            _repositoryWrapper = repositoryWrapper;
            _logger = logger;
            _insuranceService = insuranceService;
            _mapper = mapper;
            _uniqueIdentifier = uniqueIdentifier;
            _tokenizationService = tokenizationService;
        }

        /// <summary>
        /// Service to delete debit card
        /// </summary>
        /// <param name="cardId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<ResponseMessage> DeleteCard(int cardId, int userId)
        {
            _logger.LogInformation($"Processing Deleting Card [CardId :{cardId} | UserId : {userId}]\n");
            var card = await _repositoryWrapper.Card.GetCardByIdAsync(cardId, userId);
            bool isCardDeletable = false;
            if (card == null)
            {
                _logger.LogInformation($"Delete card was terminated [Reason : Card record was not found] \n");
                return new ResponseMessage { Message = "Card  was not found", ResponseCode = 12 };
            }

            //If card is for an insurance profile
            if (card.InsuranceUserProfileId != null)
            {
                _logger.LogInformation($"Delete card processing for individual insurance profile \n");
                var insuranceProfile = await _repositoryWrapper.InsuranceProfile.GetByUserIdAsync(userId);
                if ((card.Status == (int)DebitCard_StatusValue.primary && insuranceProfile.SubscriptionStatus != true) || card.Status == (int)DebitCard_StatusValue.secondary)
                {
                    _logger.LogInformation($"Delete card processing for individual insurance profile [ Card is deletable] \n");
                    isCardDeletable = true;
                    var activityLog = new ActivityLog(insuranceProfile.Id, null, null, "Debit Card Was Removed", ServiceNames.HealthInsured.ToString());
                    _repositoryWrapper.ActivityLog.Create(activityLog);
                }
            }
            else if (card.CompanyProfileId != null)
            {
                _logger.LogInformation($"Delete card processing for corporate insurance profile \n");
                var companyProfile = await _repositoryWrapper.CompanyProfile.GetCompanyProfileByUserId(userId);
                if (card.Status == (int)DebitCard_StatusValue.secondary)
                {
                    _logger.LogInformation($"Delete card processing for company insurance profile [ Card is deletable] \n");
                    isCardDeletable = true;
                    var activityLog = new ActivityLog(null, companyProfile.Id, null, "Debit Card Was Removed", ServiceNames.HealthInsured.ToString());
                    _repositoryWrapper.ActivityLog.Create(activityLog);
                }
            }
            else
            {
                _logger.LogInformation($"Delete card processing for family insurance profile \n");
                var familyProfile = await _repositoryWrapper.FamilyProfile.GetByUserId(userId);
                if (card.Status == (int)DebitCard_StatusValue.secondary)
                {
                    _logger.LogInformation($"Delete card processing for family insurance profile [ Card is deletable] \n");
                    isCardDeletable = true;
                    var activityLog = new ActivityLog(null, null, familyProfile.Id, "Debit Card Was Removed", ServiceNames.HealthInsured.ToString());
                    _repositoryWrapper.ActivityLog.Create(activityLog);
                }
            }

            if (isCardDeletable)
            {
                _repositoryWrapper.Card.Delete(card);
                await _repositoryWrapper.Save();
                _logger.LogInformation($"Card was deleted successfully \n");
                return new ResponseMessage { Message = "Card was deleted successfully", Status = true };
            }
            else
            {
                _logger.LogInformation($"Card was not deleted \n");
                return new ResponseMessage
                {
                    Message = "Kindly set a new card as " +
                    "primary card to delete present primary card"
                };
            }
        }

        /// <summary>
        /// Fetch debit card
        /// </summary>
        /// <param name="userId"></param>
        ///  /// <param name="email"></param>
        /// <returns></returns>
        public async Task<ResponseMessage> GetCards(int userId, string email)
        {
            _logger.LogInformation($"Processing get Cards [UserId : {userId} | Email : {email}]\n");
            var profileCompletion = await _insuranceService.GetProfileCompletion(userId, email);
            if(profileCompletion is null)
            {
                _logger.LogInformation($"Get card failed [Reason : Could not get profile completion ]\n");
                return new ResponseMessage {Message = "Cannot fetch card now, please try again later", Status = false };
            }
            List<DebitCard> debitCard = new List<DebitCard>();

            if (profileCompletion.Data?.HealthInsuredPlan == HealthInsuredPlan.Individual)
            {
                _logger.LogInformation($"Get card for individual \n");
                var individualInsuranceUser = await _repositoryWrapper.InsuranceProfile.GetByUserIdAsync(userId);
                debitCard = individualInsuranceUser.Cards;
            }
            else if (profileCompletion.Data?.HealthInsuredPlan == HealthInsuredPlan.Corporate)
            {
                _logger.LogInformation($"Get card for individual \n");
                var coporateInsuranceUser = await _repositoryWrapper.CompanyProfile.GetCompanyProfileByUserId(userId);
                debitCard = coporateInsuranceUser.Cards;
            }
            else
            {
                _logger.LogInformation($"Get card for individual \n");
                var familyProfile = await _repositoryWrapper.FamilyProfile.GetByUserId(userId);
                debitCard = familyProfile.Cards;
            }
            if (debitCard.Count > 0)
            {
                _logger.LogInformation($"Get card for individual \n");
                var cardDTO = _mapper.Map<List<DebitCard>, List<CardDTO>>(debitCard);
                return new ResponseMessage { Data = cardDTO, Status = true, Message = "Card was fetched successfully" };
            }
            var emptyCardDTO = new List<CardDTO>();
            _logger.LogInformation($"Get card for individual [User has no cards] \n");
            return new ResponseMessage { Data = emptyCardDTO, Message = "User has no card, Kindly add a card", Status = true };
        }

        /// <summary>
        /// Service to change primary card
        /// </summary>
        /// <param name="newCardId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<ResponseMessage> ChangePrimaryCard(int newCardId, int userId)
        {
            _logger.LogInformation($"Change primary card processing [newCardId : {newCardId} | UserId : {userId}]\n");
            var presentPrimaryCard = await _repositoryWrapper.Card.GetPrimaryCard(userId);
            if (presentPrimaryCard == null)
            {
                _logger.LogInformation($"Change primary card terminated [Reason : present primary debit card not found]\n");
                return new ResponseMessage { Message = "You dont have a debit card, Kindly add one" };
            }

            var newPrimaryCard = await _repositoryWrapper.Card.GetCardByIdAsync(newCardId, userId);
            if (newPrimaryCard == null)
            {
                _logger.LogInformation($"Change primary card terminated [Reason : Secondary primary debit card not found]\n");
                return new ResponseMessage { Message = "Card was not found", ResponseCode = 12 };
            }
            if (newPrimaryCard.Status == (int)DebitCard_StatusValue.primary)
            {
                _logger.LogInformation($"Change primary card terminated [Reason : Preseumed Secondary primary is the primary card]\n");
                return new ResponseMessage { Message = "This card is presently the primary card" };
            }
            presentPrimaryCard.Status = (int)DebitCard_StatusValue.secondary;
            _repositoryWrapper.Card.Update(presentPrimaryCard);
            newPrimaryCard.Status = (int)DebitCard_StatusValue.primary;
            _repositoryWrapper.Card.Update(newPrimaryCard);

            if (newPrimaryCard.InsuranceUserProfileId != null)
            {
                _logger.LogInformation($"Change primary card for individual \n");
                var insuranceProfile = await _repositoryWrapper.InsuranceProfile.GetByUserIdAsync(userId);
                var activityLog = new ActivityLog(insuranceProfile.Id, null, null, "Change Primary Card", ServiceNames.HealthInsured.ToString());
                _repositoryWrapper.ActivityLog.Create(activityLog);
            }
            else if (newPrimaryCard.CompanyProfileId != null)
            {
                _logger.LogInformation($"Change primary card for company \n");
                var companyProfile = await _repositoryWrapper.CompanyProfile.GetCompanyProfileByUserId(userId);
                var activityLog = new ActivityLog(null, companyProfile.Id, null, "Change Primary Card", ServiceNames.HealthInsured.ToString());
                _repositoryWrapper.ActivityLog.Create(activityLog);
            }
            else
            {
                _logger.LogInformation($"Change primary card for family \n");
                var familyProfile = await _repositoryWrapper.FamilyProfile.GetByUserId(userId);
                var activityLog = new ActivityLog(null, null, familyProfile.Id, "Change Primary Card", ServiceNames.HealthInsured.ToString());
                _repositoryWrapper.ActivityLog.Create(activityLog);
            }
            _logger.LogInformation($"Change primary card was successfully \n");
            await _repositoryWrapper.Save();
            return new ResponseMessage { Message = "Primary card was changed successfully", Status = true };
        }

    }
}
