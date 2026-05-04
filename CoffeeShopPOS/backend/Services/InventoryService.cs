// InventoryService.cs — Inventory Business Logic Service
//
// Manages stock levels, low-stock alerts, and stock adjustments.
// Connects to: InventoryDAL.cs (database), InventoryForm.cs (UI),
//              InventoryReportForm.cs (low stock reports), DashboardForm.cs (alerts)

using CoffeeShopPOS.Database;
using CoffeeShopPOS.Models;

namespace CoffeeShopPOS.Services
{
    /// <summary>
    /// Business logic for inventory/stock management.
    /// Provides stock adjustment, low-stock detection, and alert functionality.
    /// </summary>
    public static class InventoryService
    {
        /// <summary>
        /// Adjusts stock for an inventory item (restock or usage).
        /// Called by: InventoryForm.cs (manual adjustment button),
        ///            OrderService.cs (auto-deduction on order completion, future enhancement)
        /// </summary>
        /// <param name="inventoryId">ID of the item to adjust</param>
        /// <param name="changeQty">Positive for restock, negative for usage</param>
        /// <param name="reason">Reason for the change (logged in inventory_log table)</param>
        /// <param name="userId">ID of the staff member making the change</param>
        public static bool AdjustStock(int inventoryId, decimal changeQty, string reason, int userId)
        {
            // Validate the adjustment
            var item = InventoryDAL.GetById(inventoryId);
            if (item == null)
                throw new InvalidOperationException("Inventory item not found.");

            // Prevent stock from going negative
            if (item.Quantity + changeQty < 0)
                throw new InvalidOperationException(
                    $"Insufficient stock. Current: {item.Quantity} {item.Unit}, " +
                    $"Requested change: {changeQty} {item.Unit}");

            // Perform the adjustment via InventoryDAL.cs
            // This updates the inventory table AND logs the change in inventory_log
            return InventoryDAL.AdjustStock(inventoryId, changeQty, reason, userId);
        }

        /// <summary>
        /// Gets all items that are at or below their reorder level.
        /// Called by: InventoryReportForm.cs (low stock list),
        ///            DashboardForm.cs (low stock count KPI card)
        /// </summary>
        public static List<InventoryItem> GetLowStockItems()
        {
            return InventoryDAL.GetLowStock();
        }

        /// <summary>
        /// Gets the count of low-stock items for dashboard display.
        /// Called by: DashboardForm.cs for the "Low Stock Items" KPI card.
        /// </summary>
        public static int GetLowStockCount()
        {
            return InventoryDAL.GetLowStock().Count;
        }

        /// <summary>
        /// Checks if any items are critically low (at zero or below).
        /// Called by: MainForm.cs to show a warning notification on login.
        /// </summary>
        public static List<InventoryItem> GetCriticalStockItems()
        {
            return InventoryDAL.GetLowStock()
                .Where(i => i.Quantity <= 0)
                .ToList();
        }

        /// <summary>
        /// Generates a formatted low-stock alert message.
        /// Called by: DashboardForm.cs and MainForm.cs for notification display.
        /// </summary>
        public static string GetLowStockAlertMessage()
        {
            var lowStockItems = GetLowStockItems();
            if (lowStockItems.Count == 0)
                return "All inventory levels are healthy.";

            var message = $"⚠ {lowStockItems.Count} item(s) need restocking:\n\n";
            foreach (var item in lowStockItems)
            {
                string status = item.Quantity <= 0 ? "OUT OF STOCK" : "LOW STOCK";
                message += $"• {item.Name}: {item.Quantity} {item.Unit} [{status}]\n";
            }

            return message;
        }
    }
}
