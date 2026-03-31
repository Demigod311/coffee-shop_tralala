// InventoryItem.cs — Inventory/Stock Model
//
// Represents a raw material or supply item tracked in inventory.
// Maps directly to the 'inventory' table in MySQL.
// Used by: InventoryDAL.cs (data access), InventoryService.cs (stock management),
//          InventoryForm.cs (CRUD UI), InventoryReportForm.cs (low stock alerts)

namespace CoffeeShopPOS.Models
{
    /// <summary>
    /// Represents an inventory/stock item (ingredient, supply, or material).
    /// Populated by InventoryDAL.cs from the inventory database table.
    /// </summary>
    public class InventoryItem
    {
        // Primary key — auto-incremented in MySQL
        public int InventoryId { get; set; }

        // Item name (e.g., "Coffee Beans", "Whole Milk", "Paper Cups")
        public string Name { get; set; } = string.Empty;

        // Unit of measurement (e.g., "kg", "liters", "pcs")
        // Displayed alongside quantity in InventoryForm.cs
        public string Unit { get; set; } = string.Empty;

        // Current stock level — decremented by InventoryService.cs on order completion
        public decimal Quantity { get; set; }

        // Minimum stock threshold — when Quantity falls below this,
        // InventoryService.cs triggers a low-stock alert shown in InventoryReportForm.cs
        public decimal ReorderLevel { get; set; } = 10;

        // Cost per unit for financial tracking and reports
        public decimal? CostPerUnit { get; set; }

        // Last time this record was modified (auto-updated by MySQL)
        public DateTime LastUpdated { get; set; }

        /// <summary>
        /// Checks if current stock is at or below the reorder threshold.
        /// Used by InventoryService.GetLowStockItems() and InventoryReportForm.cs
        /// to highlight items that need restocking.
        /// </summary>
        public bool IsLowStock => Quantity <= ReorderLevel;

        /// <summary>
        /// Override ToString for display purposes.
        /// </summary>
        public override string ToString() => $"{Name} — {Quantity} {Unit}";
    }
}
