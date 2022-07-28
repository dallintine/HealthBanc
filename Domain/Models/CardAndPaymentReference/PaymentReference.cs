using Domain.Models.Axa.Hygeia_Insurance;
using Domain.Models.Axa_Hygeia_Insurance;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Domain.Models
{
    public class PaymentReference
    {
        public PaymentReference()
        {

        }
        public PaymentReference(string channel,string refernce, int? insuranceUserProfileId,int? companyProfileId,int? familyprofileId, int userId, decimal amount,string status
            ,string paymentMethod = "Card")
        {
            Date = DateTime.Now;
            Channel = channel;
            Refernce = refernce;
            InsuranceUserProfileId = insuranceUserProfileId;
            CompanyProfileId = companyProfileId;
            FamilyProfileId = familyprofileId;
            UserId = userId;
            Amount = amount;
            Status = status;
            PaymentMethod = paymentMethod;
        }

        public int Id { get; set; }
        public DateTime Date { get; set; }

        public string _channel;
        /// <summary>
        /// The Channel Payment was made from. For Possible Providers <see cref="PaymentReference_ChannelValue"/>
        /// </summary>
        public string Channel
        {
            get { return _channel; }
            set
            {
                if (Enum.IsDefined(typeof(PaymentReference_ChannelValue), value))
                {
                    _channel = value;
                }
                else
                {
                    throw new ArgumentException("Value of PaymentReference.Channel is not valid. Please check defined enumerated values for channel in the PaymentReference_ChannelValue class");
                }
            }
        }
        /// <summary>
        /// The reference string in correlation with paystack
        /// </summary>
        public string Refernce { get; set; }
        public int? InsuranceUserProfileId { get; set; }
        public int? CompanyProfileId { get; set; }
        public int? FamilyProfileId { get; set; }
        public int UserId { get; set; }
        public Decimal Amount { get; set; }
        public string PaymentMethod { get; set; }

        public string _status;
        /// <summary>
        /// Status of Payment transactions .For Possible Providers <see cref="PaymentReference_StatusValue"/>
        /// </summary>
        public string Status
        {
            get { return _status; }
            set
            {
                if (Enum.IsDefined(typeof(PaymentReference_StatusValue), value))
                {
                    _status = value;
                }
                else
                {
                    throw new ArgumentException("Value of PaymentReference.Status is not valid. Please check defined enumerated values for status in the PaymentReference_StatusValue class");
                }
            }
        }
        [JsonIgnore]
        public InsuranceUserProfile InsuranceUserProfile { get; set; }
        public CompanyProfile CompanyProfile { get; set; }
        public FamilyProfile FamilyProfile { get; set; }
    }

    // <summary>
    /// The Possible Values for <see cref="PaymentReference.Status"/>
    /// </summary>
    public enum PaymentReference_StatusValue
    {
        Pending,
        Failed,
        Successful,
        Send_Otp,
        Send_Url
    }

    // <summary>
    /// The Possible Values for <see cref="PaymentReference.Channel"/>
    /// </summary>
    public enum PaymentReference_ChannelValue
    {
        healthinsured_hygeia,
        healthinsured_axamansard,
    }

    public enum PaymentMethod
    {
        Card,
        Wallet,
    }
}
