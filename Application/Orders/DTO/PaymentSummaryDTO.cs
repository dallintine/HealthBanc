namespace Application.Orders.DTO
{
    public class PaymentSummaryDTO
    {
        public decimal TotalRevenue { get; set; }
        public decimal TotalIncome { get; set; }
        public long TransactionCount { get; set; }
        public long SuccessfulTransactionCount { get; set; }
    }
}
