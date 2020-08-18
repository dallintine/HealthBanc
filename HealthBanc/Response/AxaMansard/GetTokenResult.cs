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
		public ReturnedObject ReturnedObject { get; set; }
		[XmlElement(ElementName = "ReturnCode", Namespace = "http://tempuri.org/")]
		public string ReturnCode { get; set; }
	}

	[XmlRoot(ElementName = "ReturnedObject", Namespace = "http://tempuri.org/")]
	public class ReturnedObject
	{
		[XmlAttribute(AttributeName = "type", Namespace = "http://www.w3.org/2001/XMLSchema-instance")]
		public string Type { get; set; }
		[XmlText]
		public string Text { get; set; }
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

	[XmlRoot(ElementName = "Envelope", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
	public class Envelope
	{
		[XmlElement(ElementName = "Body", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
		public Body Body { get; set; }
		[XmlAttribute(AttributeName = "soap", Namespace = "http://www.w3.org/2000/xmlns/")]
		public string Soap { get; set; }
		[XmlAttribute(AttributeName = "xsi", Namespace = "http://www.w3.org/2000/xmlns/")]
		public string Xsi { get; set; }
		[XmlAttribute(AttributeName = "xsd", Namespace = "http://www.w3.org/2000/xmlns/")]
		public string Xsd { get; set; }
	}
}
