using Application.Payment.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Common.Interfaces
{
    public interface IPaystackService
    {
        Task<InitializePaymentResponse> InitlilizePayment(InitializePaymentRequest initializePayment);
        Task<VerifyPaymentResponse> VerifyPayment(string reference);
    }
}
