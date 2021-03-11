using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Domain.Models
{
    public class DebitCard
    {
        public DebitCard()
        {

        }
        public DebitCard(int userId, int? insuranceUserProfileId,int? companyProfileId, int status, string lastFourDigit,string type,string cardReference,string authorization_Code)
        {
            UserId = userId;
            InsuranceUserProfileId = insuranceUserProfileId;
            CompanyProfileId = companyProfileId;
            Status = status;
            LastFourDigit = lastFourDigit;
            Type = type;
            CardReference = cardReference;
            Authorization_Code = authorization_Code;
        }

        public int Id { get; set; }
        public int UserId { get; set; }
        public int? InsuranceUserProfileId { get; set; }
        public int? CompanyProfileId { get; set; }
        /// <summary>
        /// Status to determine if a card is primary or secondary. Check DebitCard_StatusValue for possibe values
        /// </summary>
        public int _status;

        public int Status
        {
            get { return _status; }
            set
            {
                if (Enum.IsDefined(typeof(DebitCard_StatusValue), value))
                {
                    _status = value;
                }
                else
                {
                    throw new ArgumentException("Value of DebitCard.Status is not valid. Please check defined enumerated values for status in the DebitCard_StatusValue class");
                }
            }
        }
        /// <summary>
        /// Debit card last four digit
        /// </summary>
        public string LastFourDigit { get; set; }

        // Debit card type
        public string Type { get; set; }
        public string CardReference { get; set; }
        /// <summary>
        /// Debit Card authorization code
        /// </summary>
        public string Authorization_Code { get; set; }
    }
    //(int) Months.April
    // <summary>
    /// The Possible Values for Channel
    /// </summary>
    public enum DebitCard_StatusValue
    {
        primary = 1,
        secondary = 0,
    }
}
