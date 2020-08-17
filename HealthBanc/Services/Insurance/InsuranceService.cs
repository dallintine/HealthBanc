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

        public string AxaMansardCreateUserProfile(UseProfileviewModel userProfile)
        {
            const string url = "https://online.axamansard.com/eSalesTest/webservice/axamdemo.asmx";
            const string action = "http://tempuri.org/SaveHealth";

            XmlDocument soapEnvelopXml = CreateSoapEnvelope(userProfile);
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

        public string AxaMansardGetToken(string userName,string password)
        {
            try
            {
                const string url = "https://online.axamansard.com/eSalesTest/webservice/axamdemo.asmx";
                const string action = "http://tempuri.org/getToken";

                XmlDocument soapEnvelopXml = CreateTokenEnvelope(userName, password);
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
            catch(Exception ex)
            {
                string result = ex.ToString();
                return result;
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

        private static XmlDocument CreateSoapEnvelope(UseProfileviewModel userProfile)
        {
            XmlDocument soapEnvelopeXml = new XmlDocument();
            soapEnvelopeXml.LoadXml(@"<?xml version=""1.0"" encoding=""utf-8""?>
            <soap:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
          <soap:Body>
            <SaveHealth xmlns = ""http://tempuri.org/"" >
              <HealthObject>
                <TransId > string </ TransId >
                <Gender>{userProfile.Gender}</Gender>
                <CustomerNo> string </ CustomerNo >
                <Surname> string </ Surname >
                <Othernames> string </ Othernames >
                <MaidenName> string </ MaidenName >
                <DateOfBirth> dateTime </ DateOfBirth >
                <PhoneNumber> string </ PhoneNumber >
                <Email> string </ Email >
                <ContactAddress> string </ ContactAddress >
                <Occupation> string </ Occupation >
                <MaritalStatus> string </ MaritalStatus >
                <BankName> string </ BankName >
                <AccountNo> string </ AccountNo >
                <BVN> string </ BVN >
                <Weight> decimal </ Weight >
                <Height> decimal </ Height >
                <BloodGroup> string </ BloodGroup >
                <Genotype> string </ Genotype >
                <Identification> string </ Identification >
                <Religion> int </ Religion >
                <Hobbies> string </ Hobbies >
                <CareProviderName> string </ CareProviderName >
                <CPPhone> string </ CPPhone >
                <CPAddress> string </ CPAddress >
                <CPCity> string </CPCity >
                <CPEmail> string </CPEmail>
                <AlternateHospital> string </ AlternateHospital>
                <MedicalCondition> string </MedicalCondition>
                <PlanCode> string </PlanCode>
                <Premium> decimal </Premium>
                <CustomerPhoto> string </CustomerPhoto>
                <IdentityPhoto> string </IdentityPhoto>
                <StateOfResidence> string </StateOfResidence>
                <TownOfResidence> string </TownOfResidence>
              </ HealthObject>
              <token> string </token>
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
                    < token > string </token>
                    </ GetStates >
                </ soap:Body >
            </soap:Envelope >");
            return soapEnvelopeXml;
        }

        private static XmlDocument CreateTownEnvelope(string state,string token)
        {
            XmlDocument soapEnvelopeXml = new XmlDocument();
            soapEnvelopeXml.LoadXml(@"<?xml version=""1.0"" encoding=""utf-8""?>
            <soap:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
                <soap:Body>
                   <GetTowns xmlns=""http://tempuri.org/"">
                        < State > string </ State >
                        < token > string </ token >
                    </ GetTowns >
                </ soap:Body >
            </soap:Envelope >");
            return soapEnvelopeXml;
        }

        private static XmlDocument CreateMedicalConditionEnvelope(string token)
        {
            XmlDocument soapEnvelopeXml = new XmlDocument();
            soapEnvelopeXml.LoadXml(@"<?xml version=""1.0"" encoding=""utf-8""?>
            <soap:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
                <soap:Body>
                    <GetMedicalConditions xmlns=""http://tempuri.org/"">
                    < token > string </token>
                    </GetMedicalConditions>
                </ soap:Body >
            </soap:Envelope >");
            return soapEnvelopeXml;
        }

        private static XmlDocument CreateTokenEnvelope(string userName, string password)
        {
            XmlDocument soapEnvelopeXml = new XmlDocument();
            soapEnvelopeXml.LoadXml(@"<?xml version=""1.0"" encoding=""utf-8""?>
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
