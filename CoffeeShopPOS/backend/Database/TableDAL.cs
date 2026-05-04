// TableDAL.cs — Data Access Layer for Restaurant Tables
//
// Handles database operations for the 'tables' table.
// Uses DbHelper.cs for connections and parameterised queries.
// Called by: TableManagementForm.cs (CRUD and visual grid),
//            OrderForm.cs (table selection for dine-in orders),
//            OrderService.cs (auto-update status on order events)

using CoffeeShopPOS.Models;

namespace CoffeeShopPOS.Database
{
    /// <summary>
    /// CRUD and status management for restaurant tables.
    /// </summary>
    public static class TableDAL
    {
        /// <summary>
        /// Retrieves all tables from the database.
        /// Called by: TableManagementForm.cs (to display the visual table grid),
        ///            OrderForm.cs (to populate the table selection dropdown)
        /// </summary>
        public static List<Table> GetAll()
        {
            var tables = new List<Table>();
            string query = "SELECT * FROM tables ORDER BY table_number";

            DbHelper.ExecuteReader(query, null, reader =>
            {
                while (reader.Read())
                {
                    tables.Add(new Table
                    {
                        TableId = DbHelper.GetInt32(reader, "table_id"),
                        TableNumber = DbHelper.GetString(reader, "table_number"),
                        Capacity = DbHelper.GetInt32(reader, "capacity"),
                        Status = DbHelper.GetString(reader, "status")
                    });
                }
            });

            return tables;
        }

        /// <summary>
        /// Retrieves only available tables (not occupied or reserved).
        /// Called by: OrderForm.cs to show only bookable tables in the dropdown.
        /// </summary>
        public static List<Table> GetAvailable()
        {
            var tables = new List<Table>();
            string query = "SELECT * FROM tables WHERE status = 'Available' ORDER BY table_number";

            DbHelper.ExecuteReader(query, null, reader =>
            {
                while (reader.Read())
                {
                    tables.Add(new Table
                    {
                        TableId = DbHelper.GetInt32(reader, "table_id"),
                        TableNumber = DbHelper.GetString(reader, "table_number"),
                        Capacity = DbHelper.GetInt32(reader, "capacity"),
                        Status = DbHelper.GetString(reader, "status")
                    });
                }
            });

            return tables;
        }

        /// <summary>
        /// Updates the status of a table (Available, Occupied, Reserved).
        /// Called by: OrderService.cs — automatically sets "Occupied" when order placed,
        ///            "Available" when order completed.
        ///            TableManagementForm.cs — manual status changes by staff.
        /// </summary>
        public static bool UpdateStatus(int tableId, string status)
        {
            string query = "UPDATE tables SET status = @status WHERE table_id = @id";
            var parameters = new Dictionary<string, object?>
            {
                { "@id", tableId },
                { "@status", status }
            };

            return DbHelper.ExecuteNonQuery(query, parameters) > 0;
        }

        /// <summary>
        /// Inserts a new table.
        /// Called by: TableManagementForm.cs "Add Table" button.
        /// </summary>
        public static int Insert(Table table)
        {
            string query = @"INSERT INTO tables (table_number, capacity, status)
                            VALUES (@num, @cap, @status)
                            RETURNING table_id;";

            var parameters = new Dictionary<string, object?>
            {
                { "@num", table.TableNumber },
                { "@cap", table.Capacity },
                { "@status", table.Status }
            };

            var result = DbHelper.ExecuteScalar(query, parameters);
            return Convert.ToInt32(result);
        }

        /// <summary>
        /// Updates table details (number and capacity).
        /// Called by: TableManagementForm.cs "Update" button.
        /// </summary>
        public static bool Update(Table table)
        {
            string query = @"UPDATE tables 
                            SET table_number = @num, capacity = @cap, status = @status 
                            WHERE table_id = @id";

            var parameters = new Dictionary<string, object?>
            {
                { "@id", table.TableId },
                { "@num", table.TableNumber },
                { "@cap", table.Capacity },
                { "@status", table.Status }
            };

            return DbHelper.ExecuteNonQuery(query, parameters) > 0;
        }

        /// <summary>
        /// Deletes a table. Only allowed if the table has no active orders.
        /// Called by: TableManagementForm.cs "Delete" button with confirmation.
        /// </summary>
        public static bool Delete(int tableId)
        {
            // First check if the table has any active orders to prevent orphaned data
            string checkQuery = @"SELECT COUNT(*) FROM orders 
                                 WHERE table_id = @id AND status NOT IN ('Completed', 'Cancelled')";
            var checkParams = new Dictionary<string, object?> { { "@id", tableId } };
            var count = Convert.ToInt32(DbHelper.ExecuteScalar(checkQuery, checkParams));

            if (count > 0)
            {
                throw new InvalidOperationException(
                    "Cannot delete this table — it has active orders. Complete or cancel them first.");
            }

            string query = "DELETE FROM tables WHERE table_id = @id";
            var parameters = new Dictionary<string, object?> { { "@id", tableId } };

            return DbHelper.ExecuteNonQuery(query, parameters) > 0;
        }
    }
}
