using LarvaX.Core.Entities;
using LarvaX.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LarvaX.Web.Controllers
{
    [Authorize(Roles = "HealthWorker,Administrator")]
    public class InventoryController : Controller
    {
        private readonly IInventoryService _inventoryService;
        private readonly UserManager<ApplicationUser> _userManager;

        public InventoryController(IInventoryService inventoryService, UserManager<ApplicationUser> userManager)
        {
            _inventoryService = inventoryService;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var items = await _inventoryService.GetAllItemsAsync();
            return View(items);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new InventoryItem());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(InventoryItem item)
        {
            if (!ModelState.IsValid) return View(item);
            await _inventoryService.AddItemAsync(item);
            TempData["Success"] = "Inventory item added successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var item = await _inventoryService.GetItemByIdAsync(id);
            if (item == null) return NotFound();
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(InventoryItem item)
        {
            if (!ModelState.IsValid) return View(item);
            await _inventoryService.UpdateItemAsync(item);
            TempData["Success"] = "Item updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Transactions(int id)
        {
            var item = await _inventoryService.GetItemByIdAsync(id);
            if (item == null) return NotFound();
            var transactions = await _inventoryService.GetTransactionsAsync(id);
            ViewBag.Item = item;
            return View(transactions);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecordTransaction(int itemId, int quantityChange, string reason)
        {
            var userId = _userManager.GetUserId(User)!;
            try
            {
                await _inventoryService.RecordTransactionAsync(itemId, quantityChange, reason, userId);
                TempData["Success"] = $"Transaction recorded: {(quantityChange > 0 ? "+" : "")}{quantityChange} units.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction(nameof(Transactions), new { id = itemId });
        }
    }
}
