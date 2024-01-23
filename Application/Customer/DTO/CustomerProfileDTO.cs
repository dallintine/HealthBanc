namespace Application.Customer.DTO
{
    public class CustomerProfileDTO
    {
        public string ReferralCode { get; set; }
        public string ProfileImageURL { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
        public string State { get; set; }
        public DateTime DateOfBirth { get; set; }
        public DateTime CreatedAt { get; set; }
        public HealthDetailDTO HealthDetailDTO { get; set; }
    }
}
