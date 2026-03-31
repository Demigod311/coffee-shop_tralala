// Order.cs — Order Model
//
// Represents a customer order with payment and status tracking.
// Maps directly to the 'orders' table in MySQL.
// Used by: OrderDAL.cs (data access), OrderService.cs (business logic),
//          OrderForm.cs (creating orders), BillingForm.cs (billing),
//          OrderHistoryForm.cs (viewing past orders), DashboardForm.cs (KPIs)

namespace CoffeeShopPOS.Models
{
    /// <summary>
    /// Represents a complete customer order including payment info and status.
    /// Populated by OrderDAL.cs from the orders table.
    /// </summary>
    public class Order
    {
        // Primary key — auto-incremented in MySQL (orders.order_id)
        public int OrderId { get; set; }

        // Foreign key to tables — NULL for takeaway orders
        // Used by OrderService.cs to update table status when order is placed/completed
        public int? TableId { get; set; }

        // Foreign key to users — the staff member who created this order
        // Set from the currently logged-in user in OrderForm.cs
        public int UserId { get; set; }

        // Dine-In or Takeaway — determines whether a table is required
        // Set in OrderForm.cs and affects table status management
        public string OrderType { get; set; } = "Dine-In";

        // Order lifecycle: Pending → Preparing → Served → Completed / Cancelled
        // Managed by OrderService.cs, displayed in OrderHistoryForm.cs
        public string Status { get; set; } = "Pending";

        // Financial breakdown — calculated by OrderService.CalculateTotal()
        public decimal Subtotal { get; set; }   // Sum of (unit_price * quantity) for all items
        public decimal Tax { get; set; }         // Subtotal * tax rate (default 10%)
        public decimal Discount { get; set; }    // Applied in BillingForm.cs
        public decimal Total { get; set; }       // Subtotal + Tax - Discount

        // Payment method selected in BillingForm.cs
        public string PaymentMethod { get; set; } = "Cash";

        // Timestamps
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }  // Set when status = "Completed"

        // ── Navigation / display properties (not direct DB columns) ──

        // Table number string for display — populated by JOINs in OrderDAL.cs
        public string? TableNumber { get; set; }

        // Staff name who placed the order — populated by JOINs in OrderDAL.cs
        public string? StaffName { get; set; }

        // Collection of line items in this order
        // Populated by OrderDAL.GetOrderWithItems() and used by BillingForm.cs
        public List<OrderItem> Items { get; set; } = new List<OrderItem>();
    }
}
