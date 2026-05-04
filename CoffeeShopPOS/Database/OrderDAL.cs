// OrderDAL.cs — Data Access Layer for Orders
//
// Handles all database operations for the 'orders' and 'order_items' tables.
// This is the most complex DAL as it manages the core POS workflow.
// Uses DbHelper.cs for connections and parameterised queries.
// Called by: OrderService.cs (business logic), OrderForm.cs (creating orders),
//            BillingForm.cs (completing orders), OrderHistoryForm.cs (viewing past orders),
//            DashboardForm.cs (today's order stats), ReportDAL.cs (sales aggregation)

using CoffeeShopPOS.Models;
using Npgsql;

namespace CoffeeShopPOS.Database
{
    /// <summary>
    /// Data access for orders and order line items.
    /// Manages the complete order lifecycle from creation to completion.
    /// </summary>
    public static class OrderDAL
    {
        /// <summary>
        /// Creates a new order and returns the auto-generated order_id.
        /// Called by: OrderService.PlaceOrder() which is triggered from OrderForm.cs.
        /// This inserts into the 'orders' table — line items are added separately via AddItem().
        /// </summary>
        public static int Create(Order order)
        {
            string query = @"INSERT INTO orders (table_id, user_id, order_type, status, 
                            subtotal, tax, discount, total, payment_method)
                            VALUES (@tableId, @userId, @type, @status, 
                            @subtotal, @tax, @discount, @total, @payment);
                            SELECT LAST_INSERT_ID();";

            var parameters = new Dictionary<string, object?>
            {
                { "@tableId", order.TableId.HasValue ? order.TableId.Value : DBNull.Value },
                { "@userId", order.UserId },
                { "@type", order.OrderType },
                { "@status", order.Status },
                { "@subtotal", order.Subtotal },
                { "@tax", order.Tax },
                { "@discount", order.Discount },
                { "@total", order.Total },
                { "@payment", order.PaymentMethod }
            };

            var result = DbHelper.ExecuteScalar(query, parameters);
            return Convert.ToInt32(result);
        }

        /// <summary>
        /// Adds a line item to an existing order.
        /// Called by: OrderService.PlaceOrder() for each item in the order.
        /// Inserts into the 'order_items' table linking order_id to item_id.
        /// </summary>
        public static void AddItem(OrderItem item)
        {
            string query = @"INSERT INTO order_items (order_id, item_id, quantity, unit_price, notes) 
                            VALUES (@orderId, @itemId, @qty, @price, @notes)";

            var parameters = new Dictionary<string, object?>
            {
                { "@orderId", item.OrderId },
                { "@itemId", item.ItemId },
                { "@qty", item.Quantity },
                { "@price", item.UnitPrice },
                { "@notes", item.Notes }
            };

            DbHelper.ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Updates the financial totals of an order.
        /// Called by: OrderService.CalculateTotal() and BillingForm.cs when applying discounts.
        /// </summary>
        public static bool UpdateTotals(int orderId, decimal subtotal, decimal tax, decimal discount, decimal total)
        {
            string query = @"UPDATE orders 
                            SET subtotal = @sub, tax = @tax, discount = @disc, total = @total 
                            WHERE order_id = @id";

            var parameters = new Dictionary<string, object?>
            {
                { "@id", orderId },
                { "@sub", subtotal },
                { "@tax", tax },
                { "@disc", discount },
                { "@total", total }
            };

            return DbHelper.ExecuteNonQuery(query, parameters) > 0;
        }

        /// <summary>
        /// Updates the status of an order (Pending → Preparing → Served → Completed).
        /// Called by: OrderService.UpdateStatus() which manages the order lifecycle.
        /// When status becomes "Completed", also sets the completed_at timestamp.
        /// </summary>
        public static bool UpdateStatus(int orderId, string status)
        {
            // If completing, also record the completion timestamp
            string query = status == "Completed"
                ? "UPDATE orders SET status = @status, completed_at = NOW() WHERE order_id = @id"
                : "UPDATE orders SET status = @status WHERE order_id = @id";

            var parameters = new Dictionary<string, object?>
            {
                { "@id", orderId },
                { "@status", status }
            };

            return DbHelper.ExecuteNonQuery(query, parameters) > 0;
        }

        /// <summary>
        /// Updates the payment method for an order.
        /// Called by: BillingForm.cs when the cashier selects payment type.
        /// </summary>
        public static bool UpdatePaymentMethod(int orderId, string paymentMethod)
        {
            string query = "UPDATE orders SET payment_method = @method WHERE order_id = @id";
            var parameters = new Dictionary<string, object?>
            {
                { "@id", orderId },
                { "@method", paymentMethod }
            };

            return DbHelper.ExecuteNonQuery(query, parameters) > 0;
        }

        /// <summary>
        /// Retrieves an order with all its line items.
        /// Called by: BillingForm.cs (to display itemized bill), OrderHistoryForm.cs (order details).
        /// JOINs with users and tables to get display names.
        /// </summary>
        public static Order? GetById(int orderId)
        {
            Order? order = null;

            // Query the order with JOIN to get table number and staff name
            string query = @"SELECT o.*, t.table_number, u.full_name AS staff_name
                            FROM orders o
                            LEFT JOIN tables t ON o.table_id = t.table_id
                            JOIN users u ON o.user_id = u.user_id
                            WHERE o.order_id = @id";

            var parameters = new Dictionary<string, object?> { { "@id", orderId } };

            DbHelper.ExecuteReader(query, parameters, reader =>
            {
                if (reader.Read())
                {
                    order = MapOrder(reader);
                }
            });

            // If order was found, also load its line items
            if (order != null)
            {
                order.Items = GetOrderItems(orderId);
            }

            return order;
        }

        /// <summary>
        /// Gets line items for a specific order.
        /// Called by: GetById() above, and BillingForm.cs for receipt display.
        /// JOINs with menu_items to get item names.
        /// </summary>
        public static List<OrderItem> GetOrderItems(int orderId)
        {
            var items = new List<OrderItem>();
            // JOIN with menu_items to get item name for display
            string query = @"SELECT oi.*, m.name AS item_name 
                            FROM order_items oi 
                            JOIN menu_items m ON oi.item_id = m.item_id 
                            WHERE oi.order_id = @id";

            var parameters = new Dictionary<string, object?> { { "@id", orderId } };

            DbHelper.ExecuteReader(query, parameters, reader =>
            {
                while (reader.Read())
                {
                    items.Add(new OrderItem
                    {
                        OrderItemId = DbHelper.GetInt32(reader, "order_item_id"),
                        OrderId = DbHelper.GetInt32(reader, "order_id"),
                        ItemId = DbHelper.GetInt32(reader, "item_id"),
                        Quantity = DbHelper.GetInt32(reader, "quantity"),
                        UnitPrice = DbHelper.GetDecimal(reader, "unit_price"),
                        Notes = reader.IsDBNull(reader.GetOrdinal("notes"))
                            ? null : DbHelper.GetString(reader, "notes"),
                        // item_name comes from the JOIN with menu_items
                        ItemName = DbHelper.GetString(reader, "item_name")
                    });
                }
            });

            return items;
        }

        /// <summary>
        /// Retrieves orders filtered by date range.
        /// Called by: OrderHistoryForm.cs and SalesReportForm.cs for date-filtered views.
        /// </summary>
        public static List<Order> GetByDateRange(DateTime startDate, DateTime endDate)
        {
            var orders = new List<Order>();
            string query = @"SELECT o.*, t.table_number, u.full_name AS staff_name
                            FROM orders o
                            LEFT JOIN tables t ON o.table_id = t.table_id
                            JOIN users u ON o.user_id = u.user_id
                            WHERE o.created_at BETWEEN @start AND @end
                            ORDER BY o.created_at DESC";

            var parameters = new Dictionary<string, object?>
            {
                { "@start", startDate },
                { "@end", endDate }
            };

            DbHelper.ExecuteReader(query, parameters, reader =>
            {
                while (reader.Read()) orders.Add(MapOrder(reader));
            });

            return orders;
        }

        /// <summary>
        /// Retrieves orders for a specific table (active orders).
        /// Called by: OrderForm.cs to check if a table already has an active order.
        /// </summary>
        public static List<Order> GetByTable(int tableId)
        {
            var orders = new List<Order>();
            string query = @"SELECT o.*, t.table_number, u.full_name AS staff_name
                            FROM orders o
                            LEFT JOIN tables t ON o.table_id = t.table_id
                            JOIN users u ON o.user_id = u.user_id
                            WHERE o.table_id = @tableId AND o.status NOT IN ('Completed', 'Cancelled')
                            ORDER BY o.created_at DESC";

            var parameters = new Dictionary<string, object?> { { "@tableId", tableId } };

            DbHelper.ExecuteReader(query, parameters, reader =>
            {
                while (reader.Read()) orders.Add(MapOrder(reader));
            });

            return orders;
        }

        /// <summary>
        /// Retrieves all active (non-completed, non-cancelled) orders.
        /// Called by: OrderForm.cs to show pending orders that need attention.
        /// </summary>
        public static List<Order> GetActiveOrders()
        {
            var orders = new List<Order>();
            string query = @"SELECT o.*, t.table_number, u.full_name AS staff_name
                            FROM orders o
                            LEFT JOIN tables t ON o.table_id = t.table_id
                            JOIN users u ON o.user_id = u.user_id
                            WHERE o.status NOT IN ('Completed', 'Cancelled')
                            ORDER BY o.created_at ASC";

            DbHelper.ExecuteReader(query, null, reader =>
            {
                while (reader.Read()) orders.Add(MapOrder(reader));
            });

            return orders;
        }

        // ══════════════════════════════════════════════════════════════════════
        // PRIVATE HELPER — Maps a reader row to an Order model
        // Used by all Get* methods above to avoid code duplication
        // ══════════════════════════════════════════════════════════════════════
        private static Order MapOrder(NpgsqlDataReader reader)
        {
            return new Order
            {
                OrderId = DbHelper.GetInt32(reader, "order_id"),
                TableId = DbHelper.GetNullableInt32(reader, "table_id"),
                UserId = DbHelper.GetInt32(reader, "user_id"),
                OrderType = DbHelper.GetString(reader, "order_type"),
                Status = DbHelper.GetString(reader, "status"),
                Subtotal = DbHelper.GetDecimal(reader, "subtotal"),
                Tax = DbHelper.GetDecimal(reader, "tax"),
                Discount = DbHelper.GetDecimal(reader, "discount"),
                Total = DbHelper.GetDecimal(reader, "total"),
                PaymentMethod = DbHelper.GetString(reader, "payment_method"),
                CreatedAt = DbHelper.GetDateTime(reader, "created_at"),
                CompletedAt = DbHelper.GetNullableDateTime(reader, "completed_at"),
                // Navigation properties from JOINs
                TableNumber = DbHelper.GetNullableString(reader, "table_number"),
                StaffName = DbHelper.GetNullableString(reader, "staff_name")
            };
        }
    }
}
