// ReportDAL.cs — Data Access Layer for Reports and Analytics
//
// Provides aggregation queries for the reporting and dashboard features.
// Uses DbHelper.cs for connections. Returns data as dictionaries for flexible display.
// Called by: ReportService.cs (data formatting), SalesReportForm.cs (sales reports),
//            InventoryReportForm.cs (inventory reports), DashboardForm.cs (KPI cards)

namespace CoffeeShopPOS.Database
{
    /// <summary>
    /// Aggregation queries for sales reports, inventory summaries, and dashboard KPIs.
    /// Returns results as List of Dictionary for flexible binding to DataGridView.
    /// </summary>
    public static class ReportDAL
    {
        /// <summary>
        /// Gets daily sales totals for a date range.
        /// Called by: SalesReportForm.cs and DashboardForm.cs for sales chart.
        /// Groups completed orders by date and sums their totals.
        /// </summary>
        public static List<Dictionary<string, object?>> DailySales(DateTime startDate, DateTime endDate)
        {
            string query = @"SELECT
                                created_at::date AS sale_date,
                                COUNT(*) AS total_orders,
                                SUM(subtotal) AS subtotal,
                                SUM(tax) AS tax_total,
                                SUM(discount) AS discount_total,
                                SUM(total) AS revenue
                            FROM orders
                            WHERE status <> 'Cancelled'
                              AND created_at BETWEEN @start AND @end
                            GROUP BY created_at::date
                            ORDER BY sale_date DESC";

            var parameters = new Dictionary<string, object?>
            {
                { "@start", startDate },
                { "@end", endDate }
            };

            return DbHelper.ExecuteQuery(query, parameters);
        }

        /// <summary>
        /// Gets monthly sales totals.
        /// Called by: SalesReportForm.cs for the monthly view.
        /// </summary>
        public static List<Dictionary<string, object?>> MonthlySales(int year)
        {
            string query = @"SELECT
                                EXTRACT(MONTH FROM created_at)::int AS month_num,
                                TO_CHAR(created_at, 'Month') AS month_name,
                                COUNT(*) AS total_orders,
                                SUM(total) AS revenue
                            FROM orders
                            WHERE status <> 'Cancelled' AND EXTRACT(YEAR FROM created_at)::int = @year
                            GROUP BY EXTRACT(MONTH FROM created_at)::int, TO_CHAR(created_at, 'Month')
                            ORDER BY month_num";

            var parameters = new Dictionary<string, object?> { { "@year", year } };
            return DbHelper.ExecuteQuery(query, parameters);
        }

        /// <summary>
        /// Gets the top-selling menu items by quantity ordered.
        /// Called by: DashboardForm.cs (top items KPI), SalesReportForm.cs (best sellers).
        /// JOINs order_items with menu_items to get item names.
        /// </summary>
        public static List<Dictionary<string, object?>> TopSellingItems(int limit = 10,
            DateTime? startDate = null, DateTime? endDate = null)
        {
            string dateFilter = "";
            var parameters = new Dictionary<string, object?> { { "@limit", limit } };

            if (startDate.HasValue && endDate.HasValue)
            {
                dateFilter = "AND o.created_at BETWEEN @start AND @end";
                parameters.Add("@start", startDate.Value);
                parameters.Add("@end", endDate.Value);
            }

            string query = $@"SELECT 
                                m.name AS item_name,
                                c.name AS category_name,
                                SUM(oi.quantity) AS total_qty,
                                SUM(oi.quantity * oi.unit_price) AS total_revenue
                            FROM order_items oi
                            JOIN menu_items m ON oi.item_id = m.item_id
                            JOIN categories c ON m.category_id = c.category_id
                            JOIN orders o ON oi.order_id = o.order_id
                            WHERE o.status <> 'Cancelled' {dateFilter}
                            GROUP BY m.item_id, m.name, c.name
                            ORDER BY total_qty DESC
                            LIMIT @limit";

            return DbHelper.ExecuteQuery(query, parameters);
        }

        /// <summary>
        /// Gets today's sales summary for the dashboard.
        /// Called by: DashboardForm.cs for the KPI cards at the top.
        /// Returns a single row with today's order count, revenue, and average order value.
        /// </summary>
        public static Dictionary<string, object?> TodaySummary()
        {
            string query = @"SELECT 
                                COUNT(*) AS total_orders,
                                COALESCE(SUM(total), 0) AS total_revenue,
                                COALESCE(AVG(total), 0) AS avg_order_value,
                                COALESCE(SUM(CASE WHEN payment_method = 'Cash' THEN total ELSE 0 END), 0) AS cash_revenue,
                                COALESCE(SUM(CASE WHEN payment_method = 'Card' THEN total ELSE 0 END), 0) AS card_revenue,
                                COALESCE(SUM(CASE WHEN payment_method = 'Online' THEN total ELSE 0 END), 0) AS online_revenue
                            FROM orders 
                            WHERE status <> 'Cancelled' AND created_at::date = CURRENT_DATE";

            var results = DbHelper.ExecuteQuery(query, null);
            return results.Count > 0 ? results[0] : new Dictionary<string, object?>();
        }

        /// <summary>
        /// Gets inventory summary with stock status.
        /// Called by: InventoryReportForm.cs for the full inventory report.
        /// </summary>
        public static List<Dictionary<string, object?>> InventorySummary()
        {
            string query = @"SELECT 
                                i.name,
                                i.unit,
                                i.quantity,
                                i.reorder_level,
                                i.cost_per_unit,
                                (i.quantity * COALESCE(i.cost_per_unit, 0)) AS stock_value,
                                CASE 
                                    WHEN i.quantity <= 0 THEN 'Out of Stock'
                                    WHEN i.quantity <= i.reorder_level THEN 'Low Stock'
                                    ELSE 'In Stock'
                                END AS stock_status
                            FROM inventory i
                            ORDER BY stock_status DESC, i.name";

            return DbHelper.ExecuteQuery(query, null);
        }

        /// <summary>
        /// Gets sales breakdown by payment method.
        /// Called by: SalesReportForm.cs for the payment method chart.
        /// </summary>
        public static List<Dictionary<string, object?>> SalesByPaymentMethod(DateTime startDate, DateTime endDate)
        {
            string query = @"SELECT 
                                payment_method,
                                COUNT(*) AS order_count,
                                SUM(total) AS total_amount
                            FROM orders 
                            WHERE status <> 'Cancelled' 
                              AND created_at BETWEEN @start AND @end
                            GROUP BY payment_method
                            ORDER BY total_amount DESC";

            var parameters = new Dictionary<string, object?>
            {
                { "@start", startDate },
                { "@end", endDate }
            };

            return DbHelper.ExecuteQuery(query, parameters);
        }

        /// <summary>
        /// Gets sales breakdown by category.
        /// Called by: SalesReportForm.cs for category-wise analysis.
        /// </summary>
        public static List<Dictionary<string, object?>> SalesByCategory(DateTime startDate, DateTime endDate)
        {
            string query = @"SELECT 
                                c.name AS category_name,
                                COUNT(DISTINCT o.order_id) AS order_count,
                                SUM(oi.quantity) AS items_sold,
                                SUM(oi.quantity * oi.unit_price) AS revenue
                            FROM order_items oi
                            JOIN menu_items m ON oi.item_id = m.item_id
                            JOIN categories c ON m.category_id = c.category_id
                            JOIN orders o ON oi.order_id = o.order_id
                            WHERE o.status <> 'Cancelled' 
                              AND o.created_at BETWEEN @start AND @end
                            GROUP BY c.category_id, c.name
                            ORDER BY revenue DESC";

            var parameters = new Dictionary<string, object?>
            {
                { "@start", startDate },
                { "@end", endDate }
            };

            return DbHelper.ExecuteQuery(query, parameters);
        }

        /// <summary>
        /// Gets hourly sales distribution for today.
        /// Called by: DashboardForm.cs for the "Sales by Hour" chart.
        /// </summary>
        public static List<Dictionary<string, object?>> HourlySalesToday()
        {
            string query = @"SELECT
                                EXTRACT(HOUR FROM created_at)::int AS hour,
                                COUNT(*) AS order_count,
                                SUM(total) AS revenue
                            FROM orders
                            WHERE status <> 'Cancelled' AND created_at::date = CURRENT_DATE
                            GROUP BY EXTRACT(HOUR FROM created_at)::int
                            ORDER BY hour";

            return DbHelper.ExecuteQuery(query, null);
        }
    }
}
