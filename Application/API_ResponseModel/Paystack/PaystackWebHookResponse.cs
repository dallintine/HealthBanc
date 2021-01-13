using System;
using System.Collections.Generic;
using System.Text;

namespace Application.API_ResponseModel.Paystack
{
    public class PaystackWebHookResponse
    {
        public string @event { get; set; }
        public Data data { get; set; }

        public class Data
        {
            public int amount { get; set; }
            public string currency { get; set; }
            public string domain { get; set; }
            public object failures { get; set; }
            public int id { get; set; }
            public Integration integration { get; set; }
            public string reason { get; set; }
            public string reference { get; set; }
            public string source { get; set; }
            public object source_details { get; set; }
            public string status { get; set; }
            public object titan_code { get; set; }
            public string transfer_code { get; set; }
            public DateTime transferred_at { get; set; }
            public Recipient recipient { get; set; }
            public Session session { get; set; }
            public DateTime created_at { get; set; }
            public DateTime updated_at { get; set; }
        }
    }
    public class Integration
    {
        public int id { get; set; }
        public bool is_live { get; set; }
        public string business_name { get; set; }
    }

    public class Details
    {
        public object authorization_code { get; set; }
        public string account_number { get; set; }
        public object account_name { get; set; }
        public string bank_code { get; set; }
        public string bank_name { get; set; }
    }

    public class Recipient
    {
        public bool active { get; set; }
        public string currency { get; set; }
        public object description { get; set; }
        public string domain { get; set; }
        public string email { get; set; }
        public int id { get; set; }
        public int integration { get; set; }
        public object metadata { get; set; }
        public string name { get; set; }
        public string recipient_code { get; set; }
        public string type { get; set; }
        public bool is_deleted { get; set; }
        public Details details { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
    }

    public class Session
    {
        public string provider { get; set; }
        public string id { get; set; }
    }
}
