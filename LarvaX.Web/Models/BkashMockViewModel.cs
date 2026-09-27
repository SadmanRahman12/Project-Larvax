namespace LarvaX.Web.Models
{
    public class BkashMockViewModel
    {
        public string TransactionReference { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "BDT";
        public int PlanId { get; set; }
        public string? PlanName { get; set; }
    }
}
