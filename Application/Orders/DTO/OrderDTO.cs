namespace Application.Orders.DTO
{
    public class OrderDTO
    {
        public long UserId { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        public decimal Amount { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; }
        public bool IsSuccessful { get; set; }
        public string Vendor { get; set; }
        public string Plan { get; set; }
        public string PaymentReference { get; set; }
    }
}
