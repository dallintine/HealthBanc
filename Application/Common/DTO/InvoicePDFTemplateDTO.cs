using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Common.DTO
{
    public class InvoicePDFTemplateDTO
    {
        public string Name { get; set; }
        public decimal SubTotal { get; set; }
        public decimal TransactionCharge { get; set; }
        public decimal Total { get; set; }
        public List<InvoiceItem> InvoiceItems { get; set; }
    }

    public class InvoiceItem
    {
        public string ServiceName { get; set; }
        public string PlanName { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal TotalPrice { get; set; }
    }
}
