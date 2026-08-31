namespace LarvaX.Core.Entities
{
    public class InventoryTransaction
    {
        public int Id { get; set; }
        public int InventoryItemId { get; set; }
        public InventoryItem? InventoryItem { get; set; }
        public int QuantityChange { get; set; } // Positive for adding, negative for removing
        public DateTime Date { get; set; } = DateTime.UtcNow;
        public string Reason { get; set; } = string.Empty;
        public string? UserId { get; set; } // HealthWorker Id
        public ApplicationUser? User { get; set; }
    }
}
