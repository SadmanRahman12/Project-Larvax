using LarvaX.Core.Entities;
using LarvaX.Core.Interfaces;
using LarvaX.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LarvaX.Infrastructure.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly ApplicationDbContext _db;

        public InventoryService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IEnumerable<InventoryItem>> GetAllItemsAsync()
        {
            return await _db.InventoryItems.ToListAsync();
        }

        public async Task<InventoryItem?> GetItemByIdAsync(int id)
        {
            return await _db.InventoryItems.FindAsync(id);
        }

        public async Task AddItemAsync(InventoryItem item)
        {
            _db.InventoryItems.Add(item);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateItemAsync(InventoryItem item)
        {
            _db.InventoryItems.Update(item);
            await _db.SaveChangesAsync();
        }

        public async Task RecordTransactionAsync(int itemId, int quantityChange, string reason, string userId)
        {
            var item = await _db.InventoryItems.FindAsync(itemId)
                ?? throw new InvalidOperationException("Inventory item not found.");

            item.Quantity += quantityChange;
            if (item.Quantity < 0)
                throw new InvalidOperationException("Insufficient stock. Cannot reduce below zero.");

            var transaction = new InventoryTransaction
            {
                InventoryItemId = itemId,
                QuantityChange = quantityChange,
                Reason = reason,
                UserId = userId,
                Date = DateTime.UtcNow
            };

            _db.InventoryTransactions.Add(transaction);
            _db.InventoryItems.Update(item);
            await _db.SaveChangesAsync();
        }

        public async Task<IEnumerable<InventoryTransaction>> GetTransactionsAsync(int itemId)
        {
            return await _db.InventoryTransactions
                .Where(t => t.InventoryItemId == itemId)
                .Include(t => t.User)
                .OrderByDescending(t => t.Date)
                .ToListAsync();
        }
    }
}
