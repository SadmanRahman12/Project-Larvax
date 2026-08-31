namespace LarvaX.Core.Entities
{
    public class InventoryItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int Threshold { get; set; } // Alert if Quantity < Threshold
        public string Location { get; set; } = string.Empty; // e.g., "Main Storage", "Clinic A"
        public ICollection<InventoryTransaction> Transactions { get; set; } = new List<InventoryTransaction>();
    }
}
