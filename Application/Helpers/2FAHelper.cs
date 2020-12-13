using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Helpers
{
    public class OtpValidations
    {
       public OtpValidations OtpValidation { get; set; }
    }

    public class  _2FAHelper
    {
        public string Otp { get; set; }
        public string Username { get; set; }
        public string HashKey { get; set; }
    }
}

//// NOTE: Generated code may require at least .NET Framework 4.5 or .NET Core/Standard 2.0.
///// <remarks/>
//[System.SerializableAttribute()]
//[System.ComponentModel.DesignerCategoryAttribute("code")]
//[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://tempuri.org/")]
//[System.Xml.Serialization.XmlRootAttribute(Namespace = "http://tempuri.org/", IsNullable = false)]
//public partial class OtpValidation
//{

//    private string otpField;

//    private string usernameField;

//    private string hashkeyField;

//    /// <remarks/>
//    public string otp
//    {
//        get
//        {
//            return this.otpField;
//        }
//        set
//        {
//            this.otpField = value;
//        }
//    }

//    /// <remarks/>
//    public string username
//    {
//        get
//        {
//            return this.usernameField;
//        }
//        set
//        {
//            this.usernameField = value;
//        }
//    }

//    /// <remarks/>
//    public string hashkey
//    {
//        get
//        {
//            return this.hashkeyField;
//        }
//        set
//        {
//            this.hashkeyField = value;
//        }
//    }
//}

