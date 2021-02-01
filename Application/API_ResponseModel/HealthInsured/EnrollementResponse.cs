using System;
using System.Collections.Generic;
using System.Text;

namespace Application.API_ResponseModel.HealthInsured 
{ 
    public class EnrollementResponse
    {
        public object content { get; set; }
        public bool success { get; set; }
        public int type { get; set; }
        public object title { get; set; }
        public object status { get; set; }
        public object detail { get; set; }
        public object instance { get; set; }
        public object validationErrors { get; set; }
        public string message { get; set; }
        public object code { get; set; }
    }
}
