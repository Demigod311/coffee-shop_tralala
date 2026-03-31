// ReportService.cs — Report Business Logic Service
//
// Aggregates and formats data from ReportDAL.cs for display in report forms.
// Connects to: ReportDAL.cs (raw data), SalesReportForm.cs (sales UI),
//              InventoryReportForm.cs (inventory UI), DashboardForm.cs (KPIs),
//              ExportHelper.cs (CSV export)

using CoffeeShopPOS.Database;
using System.Data;

namespace CoffeeShopPOS.Services
{
    /// <summary>
    /// Transforms raw report data from ReportDAL into DataTables for display.
    /// DataTable format is used because DataGridView binds directly to it.
    /// </summary>
    public static class ReportService
    {
        /// <summary>
        /// Gets daily sales report as a DataTable for DataGridView binding.
        /// Called by: SalesReportForm.cs when "Generate Report" is clicked.
        /// </summary>
        public static DataTable GetDailySalesReport(DateTime startDate, DateTime endDate)
        {
            // Get raw data from ReportDAL.cs
            var data = ReportDAL.DailySales(startDate, endDate);
            return ConvertToDataTable(data, new[] { "sale_date", "total_orders", "subtotal", "tax_total", "discount_total", "revenue" },
                new[] { "Date", "Orders", "Subtotal", "Tax", "Discounts", "Revenue" });
        }

        /// <summary>
        /// Gets monthly sales report as a DataTable.
        /// Called by: SalesReportForm.cs monthly view toggle.
        /// </summary>
        public static DataTable GetMonthlySalesReport(int year)
        {
            var data = ReportDAL.MonthlySales(year);
            return ConvertToDataTable(data, new[] { "month_name", "total_orders", "revenue" },
                new[] { "Month", "Orders", "Revenue" });
        }

        /// <summary>
        /// Gets top-selling items as a DataTable.
        /// Called by: SalesReportForm.cs and DashboardForm.cs.
        /// </summary>
        public static DataTable GetTopSellingItems(int limit = 10, DateTime? start = null, DateTime? end = null)
        {
            var data = ReportDAL.TopSellingItems(limit, start, end);
            return ConvertToDataTable(data, new[] { "item_name", "category_name", "total_qty", "total_revenue" },
                new[] { "Item", "Category", "Qty Sold", "Revenue" });
        }

        /// <summary>
        /// Gets today's summary for dashboard KPI cards.
        /// Called by: DashboardForm.cs on load and refresh.
        /// Returns a dictionary with: total_orders, total_revenue, avg_order_value, etc.
        /// </summary>
        public static Dictionary<string, object?> GetTodaySummary()
        {
            return ReportDAL.TodaySummary();
        }

        /// <summary>
        /// Gets inventory summary as a DataTable.
        /// Called by: InventoryReportForm.cs.
        /// </summary>
        public static DataTable GetInventorySummary()
        {
            var data = ReportDAL.InventorySummary();
            return ConvertToDataTable(data, new[] { "name", "unit", "quantity", "reorder_level", "stock_value", "stock_status" },
                new[] { "Item", "Unit", "Qty", "Reorder Level", "Stock Value", "Status" });
        }

        /// <summary>
        /// Gets sales by payment method as a DataTable.
        /// Called by: SalesReportForm.cs for payment breakdown.
        /// </summary>
        public static DataTable GetSalesByPaymentMethod(DateTime startDate, DateTime endDate)
        {
            var data = ReportDAL.SalesByPaymentMethod(startDate, endDate);
            return ConvertToDataTable(data, new[] { "payment_method", "order_count", "total_amount" },
                new[] { "Payment Method", "Orders", "Total Amount" });
        }

        /// <summary>
        /// Gets sales by category as a DataTable.
        /// Called by: SalesReportForm.cs for category analysis.
        /// </summary>
        public static DataTable GetSalesByCategory(DateTime startDate, DateTime endDate)
        {
            var data = ReportDAL.SalesByCategory(startDate, endDate);
            return ConvertToDataTable(data, new[] { "category_name", "order_count", "items_sold", "revenue" },
                new[] { "Category", "Orders", "Items Sold", "Revenue" });
        }

        // ══════════════════════════════════════════════════════════════════════
        // PRIVATE HELPER — Converts List<Dictionary> to DataTable
        // Maps raw database column names to user-friendly display names.
        // DataGridView in WinForms binds directly to DataTable objects.
        // ══════════════════════════════════════════════════════════════════════
        private static DataTable ConvertToDataTable(List<Dictionary<string, object?>> data,
            string[] sourceColumns, string[] displayNames)
        {
            var table = new DataTable();

            // Create columns with friendly display names
            for (int i = 0; i < displayNames.Length; i++)
            {
                table.Columns.Add(displayNames[i]);
            }

            // Populate rows
            foreach (var row in data)
            {
                var dataRow = table.NewRow();
                for (int i = 0; i < sourceColumns.Length; i++)
                {
                    if (row.TryGetValue(sourceColumns[i], out var value))
                    {
                        // Format decimal values to 2 decimal places for currency display
                        if (value is decimal decVal)
                            dataRow[displayNames[i]] = decVal.ToString("N2");
                        else if (value is DateTime dateVal)
                            dataRow[displayNames[i]] = dateVal.ToString("yyyy-MM-dd");
                        else
                            dataRow[displayNames[i]] = value?.ToString() ?? "";
                    }
                }
                table.Rows.Add(dataRow);
            }

            return table;
        }
    }
}
