using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace HealthBanc.Response.AxaMansard
{
	[XmlRoot(ElementName = "getTokenResult", Namespace = "http://tempuri.org/")]
	public class GetTokenResult
    {
		[XmlElement(ElementName = "IsSuccessful", Namespace = "http://tempuri.org/")]
		public string IsSuccessful { get; set; }
		[XmlElement(ElementName = "message", Namespace = "http://tempuri.org/")]
		public string Message { get; set; }
		[XmlElement(ElementName = "ReturnedObject", Namespace = "http://tempuri.org/")]
		public string ReturnedObject { get; set; }
		[XmlElement(ElementName = "ReturnCode", Namespace = "http://tempuri.org/")]
		public string ReturnCode { get; set; }
	}

	[XmlRoot(ElementName = "getTokenResponse", Namespace = "http://tempuri.org/")]
	public class GetTokenResponse
	{
		[XmlElement(ElementName = "getTokenResult", Namespace = "http://tempuri.org/")]
		public GetTokenResult GetTokenResult { get; set; }
		[XmlAttribute(AttributeName = "xmlns")]
		public string Xmlns { get; set; }
	}

	[XmlRoot(ElementName = "Body", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
	public class Body
	{
		[XmlElement(ElementName = "getTokenResponse", Namespace = "http://tempuri.org/")]
		public GetTokenResponse GetTokenResponse { get; set; }
	}
}
