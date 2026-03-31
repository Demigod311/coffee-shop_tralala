// ExportHelper.cs — CSV Export Utility
//
// Provides CSV export functionality for DataGridView and DataTable data.
// Called by: SalesReportForm.cs, InventoryReportForm.cs, OrderHistoryForm.cs
// when the "Export to CSV" button is clicked.

using System.Data;
using System.Text;

namespace CoffeeShopPOS.Helpers
{
    /// <summary>
    /// Utility class for exporting data to CSV files.
    /// Used by report forms to save data for external analysis (e.g., in Excel).
    /// </summary>
    public static class ExportHelper
    {
        /// <summary>
        /// Exports a DataTable to a CSV file.
        /// Called by: SalesReportForm.cs and InventoryReportForm.cs export buttons.
        /// DataTables come from ReportService.cs methods.
        /// </summary>
        /// <param name="dataTable">The DataTable to export (from ReportService methods)</param>
        /// <param name="filePath">Full path where the CSV file will be saved</param>
        public static void ExportToCsv(DataTable dataTable, string filePath)
        {
            if (dataTable == null || dataTable.Rows.Count == 0)
                throw new InvalidOperationException("No data to export.");

            var sb = new StringBuilder();

            // Write column headers as the first CSV line
            var headers = dataTable.Columns.Cast<DataColumn>()
                .Select(c => EscapeCsvField(c.ColumnName));
            sb.AppendLine(string.Join(",", headers));

            // Write each data row
            foreach (DataRow row in dataTable.Rows)
            {
                var fields = row.ItemArray.Select(f => EscapeCsvField(f?.ToString() ?? ""));
                sb.AppendLine(string.Join(",", fields));
            }

            // Write to file with UTF-8 encoding (supports special characters)
            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }

        /// <summary>
        /// Exports a DataGridView's contents to CSV.
        /// Called by: OrderHistoryForm.cs export button.
        /// Uses the DataGridView's visible columns and rows directly.
        /// </summary>
        public static void ExportDataGridViewToCsv(DataGridView dgv, string filePath)
        {
            if (dgv.Rows.Count == 0)
                throw new InvalidOperationException("No data to export.");

            var sb = new StringBuilder();

            // Write column headers from the DataGridView
            var headers = new List<string>();
            foreach (DataGridViewColumn col in dgv.Columns)
            {
                if (col.Visible)
                    headers.Add(EscapeCsvField(col.HeaderText));
            }
            sb.AppendLine(string.Join(",", headers));

            // Write each row
            foreach (DataGridViewRow row in dgv.Rows)
            {
                if (row.IsNewRow) continue;  // Skip the empty "new row" at the bottom

                var fields = new List<string>();
                foreach (DataGridViewColumn col in dgv.Columns)
                {
                    if (col.Visible)
                    {
                        var value = row.Cells[col.Index].Value?.ToString() ?? "";
                        fields.Add(EscapeCsvField(value));
                    }
                }
                sb.AppendLine(string.Join(",", fields));
            }

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }

        /// <summary>
        /// Shows a SaveFileDialog and returns the selected path.
        /// Called by: Report forms before calling export methods.
        /// Suggests a default filename based on the report type and date.
        /// </summary>
        /// <param name="defaultFileName">Suggested filename (e.g., "SalesReport_2026-03-30")</param>
        /// <returns>Selected file path, or null if user cancelled</returns>
        public static string? ShowSaveDialog(string defaultFileName = "export")
        {
            using var dialog = new SaveFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                FileName = $"{defaultFileName}_{DateTime.Now:yyyy-MM-dd}.csv",
                Title = "Export to CSV"
            };

            return dialog.ShowDialog() == DialogResult.OK ? dialog.FileName : null;
        }

        // ══════════════════════════════════════════════════════════════════════
        // PRIVATE HELPER — Escapes a field for proper CSV formatting
        // Wraps fields in quotes if they contain commas, quotes, or newlines
        // ══════════════════════════════════════════════════════════════════════
        private static string EscapeCsvField(string field)
        {
            if (field.Contains(',') || field.Contains('"') || field.Contains('\n'))
            {
                // Double any existing quotes and wrap the field in quotes
                return $"\"{field.Replace("\"", "\"\"")}\"";
            }
            return field;
        }
    }
}
