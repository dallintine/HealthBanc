using HealthBanc.Response;
using HealthBanc.ViewModels.AxaMansard;
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
        public InsuranceService()
        {

        }

        public string AxaMansardCreateUserProfile(UserProfileviewModel userProfile,string token)
        {
            const string url = "https://online.axamansard.com/eSalesTest/webservice/axamdemo.asmx";
            const string action = "http://tempuri.org/SaveHealth";

            XmlDocument soapEnvelopXml = CreateSoapEnvelope(userProfile,token);
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
            return result;
        }

        public string AxaMansardGetStates(string token)
        {
            const string url = "https://online.axamansard.com/eSalesTest/webservice/axamdemo.asmx";
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
            return result;
        }

        public string AxaMansardGetTowns(string state,string token)
        {
            const string url = "https://online.axamansard.com/eSalesTest/webservice/axamdemo.asmx";
            const string action = "http://tempuri.org/GetTowns";

            XmlDocument soapEnvelopXml = CreateTownEnvelope(state,token);
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
            return result;
        }

        public string AxaMansardGetMedicalCondition(string token)
        {
            const string url = "https://online.axamansard.com/eSalesTest/webservice/axamdemo.asmx";
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
            return result;
        }

        public ResponseInsure AxaMansardGetToken()
        {
            var responseMessage = new ResponseInsure();
            try
            {
                const string url = "https://online.axamansard.com/eSalesTest/webservice/axamdemo.asmx";
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
            webRequest.Headers.Add("SOAPAction:" + action);
            webRequest.ContentType = "text/xml;charset=\"utf-8\"";
            webRequest.Accept = "text/xml";
            webRequest.Method = "POST";
            return webRequest;
        }

        private static XmlDocument CreateSoapEnvelope(UserProfileviewModel userProfile,string token)
        {
            XmlDocument soapEnvelopeXml = new XmlDocument();
            soapEnvelopeXml.LoadXml($@"<?xml version=""1.0"" encoding=""utf-8""?>
            <soap:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
          <soap:Body>
            <SaveHealth xmlns = ""http://tempuri.org/"" >
              <HealthObject>
                <TransId></ TransId >
                <Gender>{userProfile.Gender}</Gender>
                <CustomerNo>{userProfile.CustomerNo}</CustomerNo>
                <Surname>{userProfile.Surname}</Surname>
                <Othernames>{userProfile.Othernames}</Othernames>
                <MaidenName>{userProfile.MaidenName}</MaidenName>
                <DateOfBirth>{userProfile.DateOfBirth}</DateOfBirth>
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
                <CPAddress>{userProfile.CPAddress}<CPAddress>
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
              </ HealthObject>
              <token>{token}</token>
            </SaveHealth>
          </soap:Body>
        </soap:Envelope >");
            return soapEnvelopeXml;
        }

        private static XmlDocument CreateStateEnvelope(string token)
        {
            XmlDocument soapEnvelopeXml = new XmlDocument();
            soapEnvelopeXml.LoadXml(@"<?xml version=""1.0"" encoding=""utf-8""?>
            <soap:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
                <soap:Body>
                    <GetStates xmlns=""http://tempuri.org/"">
                    <token>9K7RB35D2EiOVccPx9+PTpsKg0uZB9+rUwBUAEIAQQBOAEsAMAAxAA==</token>
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
                        <State>{state}</ State >
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
