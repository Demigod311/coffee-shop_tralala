// CategoryDAL.cs — Data Access Layer for Categories
//
// Handles all database operations (CRUD) for the 'categories' table.
// Uses DbHelper.cs for database connections and parameterised queries.
// Called by: MenuManagementForm.cs, CategoryManagementForm.cs, OrderForm.cs

using CoffeeShopPOS.Models;

namespace CoffeeShopPOS.Database
{
    /// <summary>
    /// Provides CRUD operations for menu categories.
    /// All methods use parameterised queries via DbHelper to prevent SQL injection.
    /// </summary>
    public static class CategoryDAL
    {
        /// <summary>
        /// Retrieves all active categories from the database.
        /// Called by: OrderForm.cs (to populate category filter buttons),
        ///            MenuManagementForm.cs (category dropdown for menu items),
        ///            CategoryManagementForm.cs (to display all categories)
        /// </summary>
        /// <param name="includeInactive">If true, returns inactive categories too (for admin view)</param>
        /// <returns>List of Category model objects</returns>
        public static List<Category> GetAll(bool includeInactive = false)
        {
            var categories = new List<Category>();
            // Build query — optionally filter out inactive categories
            string query = includeInactive
                ? "SELECT * FROM categories ORDER BY name"
                : "SELECT * FROM categories WHERE is_active = 1 ORDER BY name";

            // Use DbHelper.ExecuteReader to safely execute the query
            // The reader callback populates our Category model objects
            DbHelper.ExecuteReader(query, null, reader =>
            {
                while (reader.Read())
                {
                    categories.Add(new Category
                    {
                        CategoryId = reader.GetInt32("category_id"),
                        Name = reader.GetString("name"),
                        // Handle nullable description column
                        Description = reader.IsDBNull(reader.GetOrdinal("description"))
                            ? null : reader.GetString("description"),
                        IsActive = reader.GetBoolean("is_active")
                    });
                }
            });

            return categories;
        }

        /// <summary>
        /// Retrieves a single category by its ID.
        /// Called by: MenuManagementForm.cs when editing a category.
        /// </summary>
        public static Category? GetById(int categoryId)
        {
            Category? category = null;
            string query = "SELECT * FROM categories WHERE category_id = @id";
            var parameters = new Dictionary<string, object?> { { "@id", categoryId } };

            DbHelper.ExecuteReader(query, parameters, reader =>
            {
                if (reader.Read())
                {
                    category = new Category
                    {
                        CategoryId = reader.GetInt32("category_id"),
                        Name = reader.GetString("name"),
                        Description = reader.IsDBNull(reader.GetOrdinal("description"))
                            ? null : reader.GetString("description"),
                        IsActive = reader.GetBoolean("is_active")
                    };
                }
            });

            return category;
        }

        /// <summary>
        /// Inserts a new category into the database.
        /// Called by: CategoryManagementForm.cs when the "Add" button is clicked.
        /// Returns the auto-generated category_id from MySQL.
        /// </summary>
        public static int Insert(Category category)
        {
            string query = @"INSERT INTO categories (name, description, is_active) 
                            VALUES (@name, @desc, @active);
                            SELECT LAST_INSERT_ID();";

            var parameters = new Dictionary<string, object?>
            {
                { "@name", category.Name },
                { "@desc", category.Description },
                { "@active", category.IsActive }
            };

            // ExecuteScalar returns the LAST_INSERT_ID() — the new category's primary key
            var result = DbHelper.ExecuteScalar(query, parameters);
            return Convert.ToInt32(result);
        }

        /// <summary>
        /// Updates an existing category's name and description.
        /// Called by: CategoryManagementForm.cs when the "Update" button is clicked.
        /// </summary>
        public static bool Update(Category category)
        {
            string query = @"UPDATE categories 
                            SET name = @name, description = @desc, is_active = @active 
                            WHERE category_id = @id";

            var parameters = new Dictionary<string, object?>
            {
                { "@id", category.CategoryId },
                { "@name", category.Name },
                { "@desc", category.Description },
                { "@active", category.IsActive }
            };

            // Returns true if exactly one row was updated (the target category)
            return DbHelper.ExecuteNonQuery(query, parameters) > 0;
        }

        /// <summary>
        /// Soft-deletes a category by setting is_active to 0.
        /// Called by: CategoryManagementForm.cs when the "Delete" button is clicked.
        /// We don't hard-delete because existing menu items may reference this category.
        /// </summary>
        public static bool Delete(int categoryId)
        {
            string query = "UPDATE categories SET is_active = 0 WHERE category_id = @id";
            var parameters = new Dictionary<string, object?> { { "@id", categoryId } };

            return DbHelper.ExecuteNonQuery(query, parameters) > 0;
        }
    }
}
