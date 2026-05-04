// Category.cs — Category Model
//
// Plain data class representing a menu category (e.g., "Hot Coffee", "Pastries").
// Maps directly to the 'categories' table in PostgreSQL.
// Used by: CategoryDAL.cs (data access), MenuManagementForm.cs, OrderForm.cs (UI display)

namespace CoffeeShopPOS.Models
{
    /// <summary>
    /// Represents a menu category that groups related menu items together.
    /// This model is populated by CategoryDAL.cs from the categories table.
    /// </summary>
    public class Category
    {
        // Primary key — auto-incremented in PostgreSQL (categories.category_id)
        public int CategoryId { get; set; }

        // Display name shown in the UI and on receipts
        public string Name { get; set; } = string.Empty;

        // Optional description for admin reference
        public string? Description { get; set; }

        // Soft delete flag — when false, category is hidden from menus
        // Used by CategoryDAL.GetAll() to filter active categories
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Override ToString for display in ComboBox and ListBox controls.
        /// ComboBox in OrderForm.cs and MenuManagementForm.cs uses this.
        /// </summary>
        public override string ToString() => Name;
    }
}

