using Application.API_ResponseModel.IBSResponse;
using Application.Helpers;
using IBSService;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Application.Services
{
    public class IBSIntegrationService  
    {
        //public readonly string serviceUrl = "http://10.0.41.102:818/IBSServices.asmx";
        //public readonly string serviceUrl = "http://10.0.41.189:833/IBSServices.asmx";
        //public readonly EndpointAddress endpointAddress;
        //public readonly BasicHttpBinding basicHttpBinding;
        private readonly ILogger<IBSIntegrationService> _logger;
        private IBSConfig IBSAccessor { get; }

        public IBSIntegrationService(ILogger<IBSIntegrationService> logger,IOptions<IBSConfig> ibsAccessor)
        { 
            _logger = logger;
            IBSAccessor = ibsAccessor.Value;
        }

        private async Task<BSServicesSoapClient> GetInstanceAsync()
        {
            EndpointAddress endpointAddress = new EndpointAddress(IBSAccessor.EndpointAddress);
            BasicHttpBinding basicHttpBinding =
                new BasicHttpBinding(endpointAddress.Uri.Scheme.ToLower() == "http" ?
                            BasicHttpSecurityMode.None : BasicHttpSecurityMode.Transport);

            //Please set the time accordingly, this is only for demo
            basicHttpBinding.OpenTimeout = TimeSpan.MaxValue;
            basicHttpBinding.CloseTimeout = TimeSpan.MaxValue;
            basicHttpBinding.ReceiveTimeout = TimeSpan.MaxValue;
            basicHttpBinding.SendTimeout = TimeSpan.MaxValue;
            basicHttpBinding.UseDefaultWebProxy = true;
            return await Task.Run(() => new BSServicesSoapClient(basicHttpBinding, endpointAddress));
        }

        public async Task<OBJ_IBS_Transfer_Response_Class> SterlingBankIntraBank(decimal amount, string toAccount,string fromAccount,string paymentReference,string branchCode)
        {
            var referenceId = Guid.NewGuid().ToString();
            var requestType = IBSAccessor.TransferRequestType;
            var appId = IBSAccessor.AppId;
            var call = await GetInstanceAsync();

            OBJ_IBS_Transfer_Response_Class finalAcctName = new OBJ_IBS_Transfer_Response_Class();

            try
            {
                // call.IBSBridge()
                StringBuilder sbk = new StringBuilder();
                sbk.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
                sbk.Append("<IBSRequest>");
                sbk.Append("<ReferenceID>" + referenceId + "</ReferenceID>");
                sbk.Append("<RequestType>" + requestType+ "</RequestType>");
                sbk.Append("<FromAccount>" + fromAccount + "</FromAccount>");
                sbk.Append("<ToAccount>" + toAccount + "</ToAccount>");
                sbk.Append("<Amount>" + Math.Round(amount, 2) + "</Amount>");
                sbk.Append("<PaymentReference>" + paymentReference +  "</PaymentReference>");
                sbk.Append("<branchCode>" + branchCode + "</branchCode>");// This is only required for Naps Income and Vat
                sbk.Append("</IBSRequest>");

                var enq = Encrypt(sbk.ToString());
                _logger.LogInformation("Logging Intrabank transfer Request");
                _logger.LogCritical("Logging Intrabank transfer Request");

                _logger.LogInformation(sbk.ToString());
                _logger.LogCritical(sbk.ToString());



                int AppID = Convert.ToInt16(appId);
                var responcode = await call.IBSBridgeAsync(enq, AppID);
                var finalresponse = Decrypt(responcode.Body.IBSBridgeResult);
                XmlDocument xmlDoc = new XmlDocument();
                _logger.LogInformation("Logging Intrabank transfer Response");
                _logger.LogCritical("Logging Intrabank transfer Response");

                xmlDoc.LoadXml(finalresponse);
                _logger.LogInformation(finalresponse.ToString());
                _logger.LogCritical(finalresponse.ToString());

                finalAcctName.ResponseCode = Convert.ToString(xmlDoc.GetElementsByTagName("ResponseCode").Item(0).InnerText);
                finalAcctName.ResponseText = Convert.ToString(xmlDoc.GetElementsByTagName("ResponseText").Item(0).InnerText);
                finalAcctName.Reference = referenceId;
                if (finalAcctName.ResponseCode == "00")
                {
                    finalAcctName.FTReference = Convert.ToString(xmlDoc.GetElementsByTagName("FTReference").Item(0).InnerText);
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message + ":" + ex.StackTrace, ex);
            }
            return finalAcctName;
        }

        //Sterling Name Enquiry
        public async Task<OBJ_IBS_Reponse_Class> SterlingNameEnquiry(string accountNumber)
        {
            var referenceId = Guid.NewGuid().ToString();
            var requestType = IBSAccessor.EnquiryRequestType;

            OBJ_IBS_Reponse_Class finalAcctName = new OBJ_IBS_Reponse_Class();
            try
            {
                var  call = await GetInstanceAsync();
                StringBuilder sbk = new StringBuilder();
                sbk.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
                sbk.Append("<IBSRequest>");
                sbk.Append("<ReferenceID>"+ referenceId + "</ReferenceID>");
                sbk.Append("<RequestType>" + requestType + "</RequestType>");
                sbk.Append("<NUBAN>" + accountNumber + "</NUBAN>");
                sbk.Append("</IBSRequest>");

                var enq = Encrypt(sbk.ToString());
                var responcode = await call.IBSBridgeAsync(enq, Convert.ToInt16(IBSAccessor.AppId));
                var finalresponse = Decrypt(responcode.Body.IBSBridgeResult);
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(finalresponse);

                finalAcctName.ResponseCode = Convert.ToString(xmlDoc.GetElementsByTagName("ResponseCode").Item(0).InnerText);
                finalAcctName.ResponseText = Convert.ToString(xmlDoc.GetElementsByTagName("ResponseText").Item(0).InnerText);

                _logger.LogInformation(sbk.ToString());
                _logger.LogInformation(finalresponse);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message + ":" + ex.StackTrace, ex);
            }
            return finalAcctName;
        }

        private string Encrypt(string val)
        {
            MemoryStream ms = new MemoryStream();
            try
            {

                string sharedkeyval = ""; string sharedvectorval = "[1]";

                sharedkeyval = IBSAccessor.SharedKey;
                sharedkeyval = BinaryToString(IBSAccessor.SharedKey);

                sharedvectorval = IBSAccessor.SharedVector;
                sharedvectorval = BinaryToString(sharedvectorval);

                byte[] sharedkey = System.Text.Encoding.GetEncoding("utf-8").GetBytes(sharedkeyval);

                byte[] sharedvector = System.Text.Encoding.GetEncoding("utf-8").GetBytes(sharedvectorval);

                TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider();

                byte[] toEncrypt = Encoding.UTF8.GetBytes(val);

                CryptoStream cs = new CryptoStream(ms, tdes.CreateEncryptor(sharedkey, sharedvector), CryptoStreamMode.Write);
                cs.Write(toEncrypt, 0, toEncrypt.Length);
                cs.FlushFinalBlock();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message.ToString());
                return "";
            }
            return Convert.ToBase64String(ms.ToArray());
        }

        private string Decrypt(string val)
        {
            MemoryStream ms = new MemoryStream();
            try
            {
                string sharedkeyval = ""; string sharedvectorval = "[1]";

                sharedkeyval = IBSAccessor.SharedKey;
                sharedkeyval = BinaryToString(sharedkeyval);

                sharedvectorval = IBSAccessor.SharedVector;
                sharedvectorval = BinaryToString(sharedvectorval);

                byte[] sharedkey = System.Text.Encoding.GetEncoding("utf-8").GetBytes(sharedkeyval);
                byte[] sharedvector = System.Text.Encoding.GetEncoding("utf-8").GetBytes(sharedvectorval);

                TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider();
                byte[] toDecrypt = Convert.FromBase64String(val);
                CryptoStream cs = new CryptoStream(ms, tdes.CreateDecryptor(sharedkey, sharedvector), CryptoStreamMode.Write);
                cs.Write(toDecrypt, 0, toDecrypt.Length);
                cs.FlushFinalBlock();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message.ToString());
            }
            return Encoding.UTF8.GetString(ms.ToArray());
        }

        public static string BinaryToString(string data)
        {
            List<Byte> byteList = new List<Byte>();

            for (int i = 0; i < data.Length; i += 8)
            {
                byteList.Add(Convert.ToByte(data.Substring(i, 8), 2));
            }
            return Encoding.ASCII.GetString(byteList.ToArray());
        }
    }
}
