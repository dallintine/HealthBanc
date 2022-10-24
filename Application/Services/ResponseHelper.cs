using Application.DTO;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace Application.Services
{
    public class ResponseHelper
    {
        private readonly IEncryptAndDecrypt encryptAndDecrypt;

        public ResponseHelper(IEncryptAndDecrypt encryptAndDecrypt)
        {
            this.encryptAndDecrypt = encryptAndDecrypt;
        }
        public  string BuildResponse(int responseCode, ModelStateDictionary errs)
        {
            string firstError = "";
            var errors = new List<string>();
            if (errs != null)
            {
                var errorList = errs.Values.SelectMany(m => m.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                foreach (var error in errorList)
                {
                    errors.Add(error);
                }
                firstError = errors.FirstOrDefault();
            }
            var response = new ResponseMessage
            {
                ResponseCode = responseCode,
                Message = $"Format Error : {firstError}"
            };
            var data = encryptAndDecrypt.EncryptString(JsonConvert.SerializeObject(response));
            return data;
        }
    }
}
