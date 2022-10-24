using System;
using System.Collections.Generic;
using System.Text;

namespace Application.API_ResponseModel.Wallet
{
    public class ApiResponse<T>
    {
        public string Message { get; set; }
        public string Response { get; set; }
        public object Responsedata { get; set; }
        public T Data { get; set; }
    }
}
