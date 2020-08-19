using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace HealthBanc.Response.AxaMansard
{
    [XmlRoot(ElementName = "GetStatesResponse", Namespace = "http://tempuri.org/")]
    public class GetStatesResponse
    {
        [XmlElement(ElementName = "GetStatesResult", Namespace = "http://tempuri.org/")]
        public GetStatesResult GetStatesResult { get; set; }
        [XmlAttribute(AttributeName = "xmlns")]
        public string Xmlns { get; set; }
    }
	[XmlRoot(ElementName = "Body", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
	public class GetStatesBody
	{
		[XmlElement(ElementName = "GetStatesResponse", Namespace = "http://tempuri.org/")]
		public GetStatesResponse GetStatesResponse { get; set; }
	}

	[XmlRoot(ElementName = "Envelope", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
	public class EnvelopeGetStates
	{
		[XmlElement(ElementName = "Body", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
		public GetStatesBody Body { get; set; }
		[XmlAttribute(AttributeName = "soap", Namespace = "http://www.w3.org/2000/xmlns/")]
		public string Soap { get; set; }
		[XmlAttribute(AttributeName = "xsi", Namespace = "http://www.w3.org/2000/xmlns/")]
		public string Xsi { get; set; }
		[XmlAttribute(AttributeName = "xsd", Namespace = "http://www.w3.org/2000/xmlns/")]
		public string Xsd { get; set; }
	}

	public class StateArray
	{
		public string Code { get; set; }
		public string Text { get; set; }
	}

	[XmlRoot(ElementName = "GetStatesResult", Namespace = "http://tempuri.org/")]
	public class GetStatesResult
	{
		public List<StateArray> MyArray { get; set; }
	}
}
