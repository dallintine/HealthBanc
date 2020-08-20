using HealthBanc.Request.AxaMansard;
using HealthBanc.Response;
using HealthBanc.ViewModels.AxaMansard;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace HealthBanc.Services.Insurance
{
    public class InsuranceService
    {
        private readonly IWebHostEnvironment _environment;

        public InsuranceService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public async Task<string> GetBase64(IFormFile file,string surnName)
        {
            try
            {
                if (file.Length > 0)
                {
                    var fileName = file.FileName;
                    var myUniqueFileName = Convert.ToString(Guid.NewGuid()).Substring(0, 5);
                    var fileExtension = Path.GetExtension(fileName);
                    var name = string.Concat( surnName, myUniqueFileName);
                    var newFileName = string.Concat(myUniqueFileName, fileExtension);

                    var fileDirectoryPath = Path.Combine(_environment.WebRootPath, "insurance");
                    var filePath = Path.Combine(_environment.WebRootPath, "insurance") + $@"\{newFileName}";
                    if (!System.IO.Directory.Exists(fileDirectoryPath))
                    {
                        Directory.CreateDirectory(fileDirectoryPath);
                    }
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }
                    var base64String = Convert.ToBase64String(File.ReadAllBytes(filePath));
                    File.Delete(filePath);
                    return base64String;
                }
                return "false";
            }
            catch (Exception ex)
            {
                return "false";
            }
        }

        public ResponseInsure AxaMansardCreateUserProfile(UserProfile userProfile,string token)
        {
            var responseMessage = new ResponseInsure();
            try
            {
                const string url = "https://careers.axamansard.com/esales/webservice/axamdemo.asmx";
                const string action = "http://tempuri.org/SaveHealth";

                XmlDocument soapEnvelopXml = CreateSoapEnvelope(userProfile, token);
                HttpWebRequest webRequest = CreateWebRequest(url, action);

                using (Stream stream = webRequest.GetRequestStream())
                {
                    soapEnvelopXml.Save(stream);
                }

                //InsertSoap envelope into web request(soapEnvelopeXMl, webRequest);

                string result;
                using (WebResponse response = webRequest.GetResponse())
                {
                    using (StreamReader rd = new StreamReader(response.GetResponseStream()))
                    {
                        result = rd.ReadToEnd();
                    }
                }
                responseMessage.Data = result; responseMessage.Status = true;
                return responseMessage;
            }
            catch(WebException ex)
            {
                string pageContent = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd().ToString();
                responseMessage.Status = false;
                return responseMessage;
            }           
        }

        public ResponseInsure AxaMansardGetStates(string token)
        {
            var responseMessage = new ResponseInsure();
            try
            {
                const string url = "https://careers.axamansard.com/esales/webservice/axamdemo.asmx";
                const string action = "http://tempuri.org/GetStates";

                XmlDocument soapEnvelopXml = CreateStateEnvelope(token);
                HttpWebRequest webRequest = CreateWebRequest(url, action);

                using (Stream stream = webRequest.GetRequestStream())
                {
                    soapEnvelopXml.Save(stream);
                }

                //InsertSoap envelope into web request(soapEnvelopeXMl, webRequest);

                string result;
                using (WebResponse response = webRequest.GetResponse())
                {
                    using (StreamReader rd = new StreamReader(response.GetResponseStream()))
                    {
                        result = rd.ReadToEnd();
                    }
                }
                responseMessage.Data = result; responseMessage.Status = true;
                return responseMessage;
            }
            catch(Exception ex)
            {
                responseMessage.Status = false;
                return responseMessage;
            }            
        }

        public ResponseInsure AxaMansardGetTowns(string state,string token)
        {
            var responseMessage = new ResponseInsure();
            try
            {
                const string url = "https://careers.axamansard.com/esales/webservice/axamdemo.asmx";
                const string action = "http://tempuri.org/GetTowns";

                XmlDocument soapEnvelopXml = CreateTownEnvelope(state, token);
                HttpWebRequest webRequest = CreateWebRequest(url, action);

                using (Stream stream = webRequest.GetRequestStream())
                {
                    soapEnvelopXml.Save(stream);
                }

                //InsertSoap envelope into web request(soapEnvelopeXMl, webRequest);

                string result;
                using (WebResponse response = webRequest.GetResponse())
                {
                    using (StreamReader rd = new StreamReader(response.GetResponseStream()))
                    {
                        result = rd.ReadToEnd();
                    }
                }
                responseMessage.Data = result; responseMessage.Status = true;
                return responseMessage;
            }
            catch(Exception ex)
            {
                responseMessage.Status = false;
                return responseMessage;
            }            
        }

        public ResponseInsure AxaMansardGetMedicalCondition(string token)
        {
            var responseMessage = new ResponseInsure();
            try
            {
                const string url = "https://careers.axamansard.com/esales/webservice/axamdemo.asmx";
                const string action = "http://tempuri.org/GetMedicalConditions";

                XmlDocument soapEnvelopXml = CreateMedicalConditionEnvelope(token);
                HttpWebRequest webRequest = CreateWebRequest(url, action);

                using (Stream stream = webRequest.GetRequestStream())
                {
                    soapEnvelopXml.Save(stream);
                }

                //InsertSoap envelope into web request(soapEnvelopeXMl, webRequest);

                string result;
                using (WebResponse response = webRequest.GetResponse())
                {
                    using (StreamReader rd = new StreamReader(response.GetResponseStream()))
                    {
                        result = rd.ReadToEnd();
                    }
                }
                responseMessage.Data = result; responseMessage.Status = true;
                return responseMessage;
            }
            catch(Exception ex)
            {
                responseMessage.Status = false;
                return responseMessage;
            }
        }

        public ResponseInsure AxaMansardGetToken()
        {
            var responseMessage = new ResponseInsure();
            try
            {
                const string url = "https://careers.axamansard.com/esales/webservice/axamdemo.asmx";
                const string action = "http://tempuri.org/getToken";

                XmlDocument soapEnvelopXml = CreateTokenEnvelope();
                HttpWebRequest webRequest = CreateWebRequest(url, action);

                using (Stream stream = webRequest.GetRequestStream())
                {
                    soapEnvelopXml.Save(stream);
                }

                //InsertSoap envelope into web request(soapEnvelopeXMl, webRequest);

                string result;
                using (WebResponse response = webRequest.GetResponse())
                {
                    using (StreamReader rd = new StreamReader(response.GetResponseStream()))
                    {
                        result = rd.ReadToEnd();
                    }
                }
                responseMessage.Data = result;
                responseMessage.Status = true;
                return responseMessage;
            }
            catch(Exception ex)
            {
                responseMessage.Data = ex.ToString();
                responseMessage.Status = false;
                return responseMessage;
            }            
        }

        private static HttpWebRequest CreateWebRequest(string url, string action)
        {
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
            webRequest.Headers.Add("SOAPAction:"+action);
            webRequest.ContentType = "text/xml;charset=\"utf-8\"";
            webRequest.Accept = "text/xml";
            webRequest.Method = "POST";
            return webRequest;
        }

        private static XmlDocument CreateSoapEnvelope(UserProfile userProfile,string token)
        {
            //string s = XmlConvert.ToString(userProfile.DateOfBirth);
            //DateTime time = XmlConvert.ToDateTime(s);
            XmlDocument soapEnvelopeXml = new XmlDocument();
            soapEnvelopeXml.LoadXml($@"<?xml version=""1.0"" encoding=""utf-8""?>
            <soap:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
          <soap:Body>
            <SaveHealth xmlns =""http://tempuri.org/"">
              <HealthObject>
                <TransId>123456789123</TransId >
                <Gender>{userProfile.Gender}</Gender>
                <CustomerNo>{userProfile.CustomerNo}</CustomerNo>
                <Surname>{userProfile.Surname}</Surname>
                <Othernames>{userProfile.Othernames}</Othernames>
                <MaidenName>{userProfile.MaidenName}</MaidenName>
                <DateOfBirth>2020-08-20T06:51:28.084Z</DateOfBirth>
                <PhoneNumber>{userProfile.PhoneNumber}</PhoneNumber>
                <Email>{userProfile.Email}</Email>
                <ContactAddress>{userProfile.ContactAddress}</ContactAddress>
                <Occupation>{userProfile.Occupation}</Occupation>
                <MaritalStatus>{userProfile.MaritalStatus}</MaritalStatus>
                <BankName>{userProfile.BankName}</BankName>
                <AccountNo>{userProfile.AccountNo}</AccountNo>
                <BVN>{userProfile.BVN}</BVN>
                <Weight>{userProfile.Weight}</Weight>
                <Height>{userProfile.Height}</Height>
                <BloodGroup>{userProfile.BloodGroup}</BloodGroup>
                <Genotype>{userProfile.Genotype}</Genotype>
                <Identification>{userProfile.Identification}</Identification>
                <Religion>{userProfile.Religion}</Religion>
                <Hobbies>{userProfile.Hobbies}</Hobbies>
                <CareProviderName>{userProfile.CareProviderName}</CareProviderName>
                <CPPhone>{userProfile.CPPhone}</CPPhone>
                <CPAddress>{userProfile.CPAddress}</CPAddress>
                <CPCity>{userProfile.CPCity}</CPCity>
                <CPEmail>{userProfile.CPEmail}</CPEmail>
                <AlternateHospital>{userProfile.AlternateHospital}</AlternateHospital>
                <MedicalCondition>{userProfile.MedicalCondition}</MedicalCondition>
                <PlanCode>{userProfile.PlanCode}</PlanCode>
                <Premium>{userProfile.Premium}</Premium>
                <CustomerPhoto>{userProfile.CustomerPhoto}</CustomerPhoto>
                <IdentityPhoto>{userProfile.IdentityPhoto}</IdentityPhoto>
                <StateOfResidence>{userProfile.StateOfResidence}</StateOfResidence>
                <TownOfResidence>{userProfile.TownOfResidence}</TownOfResidence>
              </HealthObject>
              <token>pKY5hdZE2Eh+1+PZrKIbTKhKwbNKoBhlUwBUAEIAQQBOAEsAMAAxAA==</token>
            </SaveHealth>
          </soap:Body>
        </soap:Envelope>");
            return soapEnvelopeXml;
        }

        private static XmlDocument CreateStateEnvelope(string token)
        {
            XmlDocument soapEnvelopeXml = new XmlDocument();
            soapEnvelopeXml.LoadXml($@"<?xml version=""1.0"" encoding=""utf-8""?>
            <soap:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
                <soap:Body>
                    <GetStates xmlns=""http://tempuri.org/"">
                    <token>{token}</token>
                    </GetStates>
                </soap:Body>
            </soap:Envelope>");
            return soapEnvelopeXml;
        }

        private static XmlDocument CreateTownEnvelope(string state,string token)
        {
            XmlDocument soapEnvelopeXml = new XmlDocument();
            soapEnvelopeXml.LoadXml($@"<?xml version=""1.0"" encoding=""utf-8""?>
            <soap:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
                <soap:Body>
                   <GetTowns xmlns=""http://tempuri.org/"">
                    <State>{state}</State>
                    <token>{token}</token>
                    </GetTowns>
                </soap:Body>
            </soap:Envelope>");
            return soapEnvelopeXml;
        }

        private static XmlDocument CreateMedicalConditionEnvelope(string token)
        {
            XmlDocument soapEnvelopeXml = new XmlDocument();
            soapEnvelopeXml.LoadXml($@"<?xml version=""1.0"" encoding=""utf-8""?>
            <soap:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
                <soap:Body>
                    <GetMedicalConditions xmlns=""http://tempuri.org/"">
                    <token>{token}</token>
                    </GetMedicalConditions>
                </soap:Body>
            </soap:Envelope>");
            return soapEnvelopeXml;
        }

        private static XmlDocument CreateTokenEnvelope()
        {
            XmlDocument soapEnvelopeXml = new XmlDocument();
            //StringBuilder sb = new StringBuilder();
            //sb.Append("<? xml version = \"1.0\" encoding = \"utf - 8\" ?>");
            //sb.Append("<soap:Envelope xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\">");
            //sb.Append("<soap:Body>");
            //sb.Append("<getToken xmlns=\"http://tempuri.org/\">");
            //sb.Append("<Username>userName</Username>");
            soapEnvelopeXml.LoadXml($@"<?xml version=""1.0"" encoding=""utf-8""?>
            <soap:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
                <soap:Body>
                   <getToken xmlns=""http://tempuri.org/"">
                   <Username>STBANK01</Username>
                   <Password>STBank@01-234</Password>
                   </getToken>
                </soap:Body>
            </soap:Envelope>");
            return soapEnvelopeXml;
        }

        //public async Task<string> CreateSoapEnvelope()
        //{
        //    string soapString = @"<?xml version=""1.0"" encoding=""utf-8""?>
        //  <soap:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
        //      <soap:Body>
        //          <HelloWorld xmlns=""http://tempuri.org/"" />
        //      </soap:Body>
        //  </soap:Envelope>";

        //    HttpResponseMessage response = await PostXmlRequest("your_url_here", soapString);
        //    string content = await response.Content.ReadAsStringAsync();

        //    return content;
        //}

        //public static async Task<HttpResponseMessage> PostXmlRequest(string baseUrl, string xmlString)
        //{
        //    using (var httpClient = new HttpClient())
        //    {
        //        var httpContent = new StringContent(xmlString, Encoding.UTF8, "text/xml");
        //        httpContent.Headers.Add("SOAPAction", "http://tempuri.org/HelloWorld");

        //        return await httpClient.PostAsync(baseUrl, httpContent);
        //    }
        //}

    }
}
