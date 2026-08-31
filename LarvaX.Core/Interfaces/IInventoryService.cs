using LarvaX.Core.Entities;

namespace LarvaX.Core.Interfaces
{
    public interface IInventoryService
    {
        Task<IEnumerable<InventoryItem>> GetAllItemsAsync();
        Task<InventoryItem?> GetItemByIdAsync(int id);
        Task AddItemAsync(InventoryItem item);
        Task UpdateItemAsync(InventoryItem item);
        Task RecordTransactionAsync(int itemId, int quantityChange, string reason, string userId);
        Task<IEnumerable<InventoryTransaction>> GetTransactionsAsync(int itemId);
    }
}
