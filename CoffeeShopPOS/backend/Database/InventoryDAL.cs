// InventoryDAL.cs — Data Access Layer for Inventory
//
// Handles database operations for the 'inventory' and 'inventory_log' tables.
// Uses DbHelper.cs for connections and parameterised queries.
// Called by: InventoryService.cs (stock management), InventoryForm.cs (CRUD UI),
//            InventoryReportForm.cs (low stock alerts), DashboardForm.cs (stock warnings)

using CoffeeShopPOS.Models;
using Npgsql;

namespace CoffeeShopPOS.Database
{
    /// <summary>
    /// CRUD operations for inventory items and stock adjustment logging.
    /// </summary>
    public static class InventoryDAL
    {
        /// <summary>
        /// Retrieves all inventory items.
        /// Called by: InventoryForm.cs (main inventory list), 
        ///            InventoryReportForm.cs (stock report)
        /// </summary>
        public static List<InventoryItem> GetAll()
        {
            var items = new List<InventoryItem>();
            string query = "SELECT * FROM inventory ORDER BY name";

            DbHelper.ExecuteReader(query, null, reader =>
            {
                while (reader.Read()) items.Add(MapInventoryItem(reader));
            });

            return items;
        }

        /// <summary>
        /// Retrieves items that are at or below their reorder level.
        /// Called by: InventoryService.GetLowStockItems() to trigger alerts,
        ///            InventoryReportForm.cs (low stock warning list),
        ///            DashboardForm.cs (low stock count KPI)
        /// </summary>
        public static List<InventoryItem> GetLowStock()
        {
            var items = new List<InventoryItem>();
            // Compare current quantity against the reorder_level threshold
            string query = "SELECT * FROM inventory WHERE quantity <= reorder_level ORDER BY quantity ASC";

            DbHelper.ExecuteReader(query, null, reader =>
            {
                while (reader.Read()) items.Add(MapInventoryItem(reader));
            });

            return items;
        }

        /// <summary>
        /// Retrieves a single inventory item by ID.
        /// Called by: InventoryForm.cs when editing an item.
        /// </summary>
        public static InventoryItem? GetById(int inventoryId)
        {
            InventoryItem? item = null;
            string query = "SELECT * FROM inventory WHERE inventory_id = @id";
            var parameters = new Dictionary<string, object?> { { "@id", inventoryId } };

            DbHelper.ExecuteReader(query, parameters, reader =>
            {
                if (reader.Read()) item = MapInventoryItem(reader);
            });

            return item;
        }

        /// <summary>
        /// Inserts a new inventory item.
        /// Called by: InventoryForm.cs "Add Item" button.
        /// </summary>
        public static int Insert(InventoryItem item)
        {
            string query = @"INSERT INTO inventory (name, unit, quantity, reorder_level, cost_per_unit)
                            VALUES (@name, @unit, @qty, @reorder, @cost)
                            RETURNING inventory_id;";

            var parameters = new Dictionary<string, object?>
            {
                { "@name", item.Name },
                { "@unit", item.Unit },
                { "@qty", item.Quantity },
                { "@reorder", item.ReorderLevel },
                { "@cost", item.CostPerUnit }
            };

            var result = DbHelper.ExecuteScalar(query, parameters);
            return Convert.ToInt32(result);
        }

        /// <summary>
        /// Updates an existing inventory item's details.
        /// Called by: InventoryForm.cs "Update" button.
        /// </summary>
        public static bool Update(InventoryItem item)
        {
            string query = @"UPDATE inventory 
                            SET name = @name, unit = @unit, quantity = @qty, 
                                reorder_level = @reorder, cost_per_unit = @cost 
                            WHERE inventory_id = @id";

            var parameters = new Dictionary<string, object?>
            {
                { "@id", item.InventoryId },
                { "@name", item.Name },
                { "@unit", item.Unit },
                { "@qty", item.Quantity },
                { "@reorder", item.ReorderLevel },
                { "@cost", item.CostPerUnit }
            };

            return DbHelper.ExecuteNonQuery(query, parameters) > 0;
        }

        /// <summary>
        /// Adjusts the stock quantity and logs the change.
        /// Used for both restocking (positive changeQty) and usage (negative changeQty).
        /// Called by: InventoryService.AdjustStock() when orders are completed,
        ///            InventoryForm.cs for manual stock adjustments.
        /// </summary>
        /// <param name="inventoryId">ID of the inventory item to adjust</param>
        /// <param name="changeQty">Amount to add (positive) or subtract (negative)</param>
        /// <param name="reason">Why the adjustment was made (e.g., "Order #42", "Manual restock")</param>
        /// <param name="changedByUserId">User who made the change (for audit trail)</param>
        public static bool AdjustStock(int inventoryId, decimal changeQty, string reason, int changedByUserId)
        {
            // Update the inventory quantity
            string updateQuery = @"UPDATE inventory 
                                  SET quantity = quantity + @change 
                                  WHERE inventory_id = @id";

            var updateParams = new Dictionary<string, object?>
            {
                { "@id", inventoryId },
                { "@change", changeQty }
            };

            int rowsAffected = DbHelper.ExecuteNonQuery(updateQuery, updateParams);

            if (rowsAffected > 0)
            {
                // Log the change in inventory_log for audit trail
                // This connects to the inventory_log table for tracking
                LogChange(inventoryId, changeQty, reason, changedByUserId);
            }

            return rowsAffected > 0;
        }

        /// <summary>
        /// Logs a stock change in the inventory_log audit trail table.
        /// Called by: AdjustStock() above — every stock change is logged.
        /// </summary>
        public static void LogChange(int inventoryId, decimal changeQty, string reason, int changedByUserId)
        {
            string query = @"INSERT INTO inventory_log (inventory_id, change_qty, reason, changed_by) 
                            VALUES (@invId, @qty, @reason, @userId)";

            var parameters = new Dictionary<string, object?>
            {
                { "@invId", inventoryId },
                { "@qty", changeQty },
                { "@reason", reason },
                { "@userId", changedByUserId }
            };

            DbHelper.ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Gets the inventory change log for a specific item.
        /// Called by: InventoryForm.cs to show the history of stock changes.
        /// </summary>
        public static List<Dictionary<string, object?>> GetLog(int inventoryId)
        {
            string query = @"SELECT il.*, u.full_name 
                            FROM inventory_log il 
                            LEFT JOIN users u ON il.changed_by = u.user_id 
                            WHERE il.inventory_id = @id 
                            ORDER BY il.changed_at DESC 
                            LIMIT 100";

            var parameters = new Dictionary<string, object?> { { "@id", inventoryId } };
            return DbHelper.ExecuteQuery(query, parameters);
        }

        /// <summary>
        /// Deletes an inventory item. Only if it has no usage history.
        /// Called by: InventoryForm.cs "Delete" button.
        /// </summary>
        public static bool Delete(int inventoryId)
        {
            // Delete log entries first (cascade), then the item itself
            string deleteLogQuery = "DELETE FROM inventory_log WHERE inventory_id = @id";
            var parameters = new Dictionary<string, object?> { { "@id", inventoryId } };
            DbHelper.ExecuteNonQuery(deleteLogQuery, parameters);

            string deleteQuery = "DELETE FROM inventory WHERE inventory_id = @id";
            return DbHelper.ExecuteNonQuery(deleteQuery, parameters) > 0;
        }

        // ══════════════════════════════════════════════════════════════════════
        // PRIVATE HELPER — Maps a reader row to an InventoryItem model
        // ══════════════════════════════════════════════════════════════════════
        private static InventoryItem MapInventoryItem(NpgsqlDataReader reader)
        {
            return new InventoryItem
            {
                InventoryId = DbHelper.GetInt32(reader, "inventory_id"),
                Name = DbHelper.GetString(reader, "name"),
                Unit = DbHelper.GetString(reader, "unit"),
                Quantity = DbHelper.GetDecimal(reader, "quantity"),
                ReorderLevel = DbHelper.GetDecimal(reader, "reorder_level"),
                CostPerUnit = DbHelper.GetNullableDecimal(reader, "cost_per_unit"),
                LastUpdated = DbHelper.GetDateTime(reader, "last_updated")
            };
        }
    }
}
