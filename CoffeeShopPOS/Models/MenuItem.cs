// MenuItem.cs — Menu Item Model
//
// Plain data class representing a product available for sale.
// Maps directly to the 'menu_items' table in MySQL.
// Used by: MenuItemDAL.cs (data access), OrderForm.cs (item selection), 
//          MenuManagementForm.cs (CRUD), BillingForm.cs (receipt display)

namespace CoffeeShopPOS.Models
{
    /// <summary>
    /// Represents a single product on the coffee shop menu.
    /// Populated by MenuItemDAL.cs from the menu_items table.
    /// </summary>
    public class MenuItem
    {
        // Primary key — auto-incremented in MySQL (menu_items.item_id)
        public int ItemId { get; set; }

        // Foreign key to categories table — links this item to its category
        // Used by MenuItemDAL.GetByCategory() to filter items
        public int CategoryId { get; set; }

        // Product display name shown on POS screen and receipts
        public string Name { get; set; } = string.Empty;

        // Optional product description
        public string? Description { get; set; }

        // Sale price — displayed in OrderForm.cs and used for total calculation
        public decimal Price { get; set; }

        // Availability toggle — items can be marked unavailable without deleting
        // Checked by OrderForm.cs before allowing item to be added to order
        public bool IsAvailable { get; set; } = true;

        // Path to product image (optional, for future UI enhancement)
        public string? ImagePath { get; set; }

        // Timestamp of when item was added to the database
        public DateTime CreatedAt { get; set; }

        // Category name — populated by JOIN queries in MenuItemDAL.cs
        // Not a direct database column, but used for display purposes
        public string? CategoryName { get; set; }

        /// <summary>
        /// Override ToString for display in ListBox and search results.
        /// Shows name and price for quick identification in OrderForm.cs.
        /// </summary>
        public override string ToString() => $"{Name} — ${Price:F2}";
    }
}
