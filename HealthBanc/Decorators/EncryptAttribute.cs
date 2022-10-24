using Application.Interfaces;
using Infrastructure.EncryptionService;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;

namespace HealthBanc.Decorators
{
    public class EncryptAttribute : ResultFilterAttribute
    {
        public async override void OnResultExecuting(ResultExecutingContext context)
        {
            var encryptAndDecrypt = context.HttpContext.RequestServices.GetService<IEncryptAndDecrypt>();

            var jsonString = String.Empty;
            //using (var inputStream = new StreamReader(context.HttpContext.Response.Body))
            //{
            //    jsonString = inputStream.ReadToEnd();
            //}

            var stream = context.HttpContext.Response.Body;// currently holds the original stream                    
            var originalReader = new StreamReader(stream);
            var originalContent = await originalReader.ReadToEndAsync();
            var notModified = true;
            try
            {

                if (originalContent != null)
                {
                    //var str = "This is just a string as deserializing returns string";
                    ////convert string to jsontype
                    //var json = JsonConvert.SerializeObject(str);
                    ////modified stream
                    //var requestData = Encoding.UTF8.GetBytes(json);
                    //stream = new MemoryStream(requestData);
                    //notModified = false;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            var encryptedObject = encryptAndDecrypt.EncryptStream(context.HttpContext.Response.Body);
            base.OnResultExecuting(context);
        }
    }
}
