using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Models.Axa_Hygeia_Insurance
{
    public class HMOPayment
    {
        public int Id { get; set; }
        public DateTime PaymentDate { get; set; }

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
        public decimal Amount { get; set; }
        public string Reference { get; set; }
    }
}
