// MenuItemDAL.cs — Data Access Layer for Menu Items
//
// Handles all database operations for the 'menu_items' table.
// Uses DbHelper.cs for connections. Items are linked to categories via category_id.
// Called by: MenuManagementForm.cs (CRUD), OrderForm.cs (browsing/selecting items),
//            OrderService.cs (price lookup when adding to orders)

using CoffeeShopPOS.Models;
using Npgsql;

namespace CoffeeShopPOS.Database
{
    /// <summary>
    /// CRUD operations for menu items with category JOIN support.
    /// </summary>
    public static class MenuItemDAL
    {
        /// <summary>
        /// Retrieves all menu items with their category names.
        /// Called by: MenuManagementForm.cs to populate the items DataGridView.
        /// </summary>
        public static List<MenuItem> GetAll(bool includeUnavailable = false)
        {
            var items = new List<MenuItem>();

            // JOIN with categories to get category name for display
            // This JOIN connects menu_items.category_id → categories.category_id
            string query = @"SELECT m.*, c.name AS category_name 
                            FROM menu_items m 
                            JOIN categories c ON m.category_id = c.category_id
                            " + (includeUnavailable ? "" : "WHERE m.is_available = 1") +
                            " ORDER BY c.name, m.name";

            DbHelper.ExecuteReader(query, null, reader =>
            {
                while (reader.Read()) items.Add(MapMenuItem(reader));
            });

            return items;
        }

        /// <summary>
        /// Retrieves menu items filtered by category.
        /// Called by: OrderForm.cs when a category tab/button is clicked.
        /// Only returns available items for the POS screen.
        /// </summary>
        public static List<MenuItem> GetByCategory(int categoryId)
        {
            var items = new List<MenuItem>();
            string query = @"SELECT m.*, c.name AS category_name 
                            FROM menu_items m 
                            JOIN categories c ON m.category_id = c.category_id
                            WHERE m.category_id = @catId AND m.is_available = 1 
                            ORDER BY m.name";

            var parameters = new Dictionary<string, object?> { { "@catId", categoryId } };

            DbHelper.ExecuteReader(query, parameters, reader =>
            {
                while (reader.Read()) items.Add(MapMenuItem(reader));
            });

            return items;
        }

        /// <summary>
        /// Searches menu items by name (partial match).
        /// Called by: OrderForm.cs search textbox for quick item lookup.
        /// </summary>
        public static List<MenuItem> Search(string searchTerm)
        {
            var items = new List<MenuItem>();
            string query = @"SELECT m.*, c.name AS category_name 
                            FROM menu_items m 
                            JOIN categories c ON m.category_id = c.category_id
                            WHERE m.name LIKE @term AND m.is_available = 1 
                            ORDER BY m.name";

            // Use % wildcards for partial matching (e.g., "%latte%" matches "Iced Latte")
            var parameters = new Dictionary<string, object?> { { "@term", $"%{searchTerm}%" } };

            DbHelper.ExecuteReader(query, parameters, reader =>
            {
                while (reader.Read()) items.Add(MapMenuItem(reader));
            });

            return items;
        }

        /// <summary>
        /// Gets a single menu item by ID.
        /// Called by: OrderService.cs when adding an item to an order.
        /// </summary>
        public static MenuItem? GetById(int itemId)
        {
            MenuItem? item = null;
            string query = @"SELECT m.*, c.name AS category_name 
                            FROM menu_items m 
                            JOIN categories c ON m.category_id = c.category_id
                            WHERE m.item_id = @id";

            var parameters = new Dictionary<string, object?> { { "@id", itemId } };

            DbHelper.ExecuteReader(query, parameters, reader =>
            {
                if (reader.Read()) item = MapMenuItem(reader);
            });

            return item;
        }

        /// <summary>
        /// Inserts a new menu item.
        /// Called by: MenuManagementForm.cs "Add Item" button.
        /// </summary>
        public static int Insert(MenuItem item)
        {
            string query = @"INSERT INTO menu_items (category_id, name, description, price, is_available, image_path) 
                            VALUES (@catId, @name, @desc, @price, @avail, @img);
                            SELECT LAST_INSERT_ID();";

            var parameters = new Dictionary<string, object?>
            {
                { "@catId", item.CategoryId },
                { "@name", item.Name },
                { "@desc", item.Description },
                { "@price", item.Price },
                { "@avail", item.IsAvailable },
                { "@img", item.ImagePath }
            };

            var result = DbHelper.ExecuteScalar(query, parameters);
            return Convert.ToInt32(result);
        }

        /// <summary>
        /// Updates an existing menu item.
        /// Called by: MenuManagementForm.cs "Update" button.
        /// </summary>
        public static bool Update(MenuItem item)
        {
            string query = @"UPDATE menu_items 
                            SET category_id = @catId, name = @name, description = @desc, 
                                price = @price, is_available = @avail, image_path = @img 
                            WHERE item_id = @id";

            var parameters = new Dictionary<string, object?>
            {
                { "@id", item.ItemId },
                { "@catId", item.CategoryId },
                { "@name", item.Name },
                { "@desc", item.Description },
                { "@price", item.Price },
                { "@avail", item.IsAvailable },
                { "@img", item.ImagePath }
            };

            return DbHelper.ExecuteNonQuery(query, parameters) > 0;
        }

        /// <summary>
        /// Toggles an item's availability (available ↔ unavailable).
        /// Called by: MenuManagementForm.cs toggle button.
        /// </summary>
        public static bool ToggleAvailability(int itemId)
        {
            // XOR toggle: if is_available is 1, set to 0; if 0, set to 1
            string query = "UPDATE menu_items SET is_available = NOT is_available WHERE item_id = @id";
            var parameters = new Dictionary<string, object?> { { "@id", itemId } };

            return DbHelper.ExecuteNonQuery(query, parameters) > 0;
        }

        // ══════════════════════════════════════════════════════════════════════
        // PRIVATE HELPER — Maps a database reader row to a MenuItem model
        // Centralised mapping avoids code duplication across all Get* methods
        // ══════════════════════════════════════════════════════════════════════
        private static MenuItem MapMenuItem(NpgsqlDataReader reader)
        {
            return new MenuItem
            {
                ItemId = DbHelper.GetInt32(reader, "item_id"),
                CategoryId = DbHelper.GetInt32(reader, "category_id"),
                Name = DbHelper.GetString(reader, "name"),
                Description = DbHelper.GetNullableString(reader, "description"),
                Price = DbHelper.GetDecimal(reader, "price"),
                IsAvailable = DbHelper.GetBoolean(reader, "is_available"),
                ImagePath = DbHelper.GetNullableString(reader, "image_path"),
                CreatedAt = DbHelper.GetDateTime(reader, "created_at"),
                // category_name comes from the JOIN with categories table
                CategoryName = DbHelper.GetNullableString(reader, "category_name")
            };
        }
    }
}
