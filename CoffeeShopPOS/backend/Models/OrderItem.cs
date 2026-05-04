// OrderItem.cs — Order Line Item Model
//
// Represents a single line item within an order (one menu item with quantity).
// Maps directly to the 'order_items' table in PostgreSQL.
// Used by: OrderDAL.cs (data access), OrderForm.cs (adding items),
//          BillingForm.cs (displaying itemized bill), OrderService.cs (total calc)

namespace CoffeeShopPOS.Models
{
    /// <summary>
    /// Represents one line item in an order — links a menu item to an order with quantity.
    /// Populated by OrderDAL.cs from the order_items table.
    /// </summary>
    public class OrderItem
    {
        // Primary key — auto-incremented in PostgreSQL
        public int OrderItemId { get; set; }

        // Foreign key to orders table — which order this item belongs to
        public int OrderId { get; set; }

        // Foreign key to menu_items table — which product was ordered
        public int ItemId { get; set; }

        // How many of this item were ordered
        public int Quantity { get; set; } = 1;

        // Price at the time of order — snapshot to handle future price changes
        // This is copied from MenuItem.Price when the item is added in OrderForm.cs
        public decimal UnitPrice { get; set; }

        // Special instructions (e.g., "No sugar", "Extra hot", "Almond milk")
        // Entered by cashier in OrderForm.cs
        public string? Notes { get; set; }

        // ── Display properties (populated by JOINs, not direct DB columns) ──

        // Menu item name — for display in OrderForm.cs and BillingForm.cs
        public string? ItemName { get; set; }

        /// <summary>
        /// Calculated line total for this item (quantity × unit price).
        /// Used by BillingForm.cs to show line totals and by OrderService.cs for subtotal.
        /// </summary>
        public decimal LineTotal => Quantity * UnitPrice;
    }
}

