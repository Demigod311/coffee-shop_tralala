// InventoryReportForm.cs — Inventory Summary Report
//
// Displays inventory summary with stock status and low-stock alerts.
// Connects to: ReportService.cs (data), InventoryService.cs (low stock),
//              ExportHelper.cs (CSV export), MainForm.cs (embedded in content)

using CoffeeShopPOS.Services;
using CoffeeShopPOS.Helpers;
using System.Data;

namespace CoffeeShopPOS.Forms
{
    /// <summary>
    /// Inventory report showing stock levels, values, and low-stock alerts.
    /// </summary>
    public class InventoryReportForm : Form
    {
        private DataGridView dgvReport = null!;
        private Label lblAlerts = null!;
        private Button btnRefresh = null!, btnExport = null!;

        private readonly Color bgColor = Color.FromArgb(245, 242, 238);
        private readonly Color cardBg = Color.White;
        private readonly Color textDark = Color.FromArgb(50, 40, 35);
        private readonly Color accentGold = Color.FromArgb(193, 154, 107);
        private readonly Color successGreen = Color.FromArgb(76, 175, 80);
        private readonly Color dangerRed = Color.FromArgb(244, 67, 54);

        public InventoryReportForm() { InitializeComponent(); }

        private void InitializeComponent()
        {
            this.Text = "Inventory Report"; this.BackColor = bgColor; this.Dock = DockStyle.Fill; this.Padding = new Padding(20);

            var lblTitle = new Label { Text = "📊 Inventory Report", Font = new Font("Segoe UI", 20, FontStyle.Bold), ForeColor = textDark, Dock = DockStyle.Top, Height = 50 };
            this.Controls.Add(lblTitle);

            // Buttons
            var btnPanel = new Panel { Dock = DockStyle.Top, Height = 50 };
            btnRefresh = new Button { Text = "🔄 Refresh", Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.White, BackColor = accentGold, FlatStyle = FlatStyle.Flat, Location = new Point(5, 8), Size = new Size(110, 35), Cursor = Cursors.Hand };
            btnRefresh.FlatAppearance.BorderSize = 0; btnRefresh.Click += (s, e) => LoadData();
            btnPanel.Controls.Add(btnRefresh);

            btnExport = new Button { Text = "📥 Export CSV", Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.White, BackColor = successGreen, FlatStyle = FlatStyle.Flat, Location = new Point(125, 8), Size = new Size(120, 35), Cursor = Cursors.Hand };
            btnExport.FlatAppearance.BorderSize = 0;
            btnExport.Click += (s, e) =>
            {
                try
                {
                    if (dgvReport.DataSource is not DataTable dt || dt.Rows.Count == 0) { ValidationHelper.ShowError("No data."); return; }
                    string? path = ExportHelper.ShowSaveDialog("InventoryReport");
                    if (path != null) { ExportHelper.ExportToCsv(dt, path); ValidationHelper.ShowSuccess($"Exported to {path}"); }
                }
                catch (Exception ex) { ValidationHelper.ShowCriticalError($"Error: {ex.Message}"); }
            };
            btnPanel.Controls.Add(btnExport);
            this.Controls.Add(btnPanel);

            // Low stock alerts section
            lblAlerts = new Label
            {
                Text = "Loading...",
                Font = new Font("Segoe UI", 10),
                ForeColor = dangerRed,
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = Color.FromArgb(255, 248, 246),
                Padding = new Padding(10),
                AutoSize = false,
            };
            this.Controls.Add(lblAlerts);

            // Data grid
            dgvReport = MakeGrid(); dgvReport.Dock = DockStyle.Fill;
            // Highlight low stock and out of stock rows
            dgvReport.CellFormatting += (s, e) =>
            {
                if (e.RowIndex >= 0 && dgvReport.Columns[e.ColumnIndex].Name == "Status")
                {
                    string? val = e.Value?.ToString();
                    if (val == "Out of Stock") { e.CellStyle.ForeColor = dangerRed; e.CellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold); }
                    else if (val == "Low Stock") { e.CellStyle.ForeColor = Color.FromArgb(255, 152, 0); e.CellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold); }
                    else { e.CellStyle.ForeColor = successGreen; }
                }
            };
            this.Controls.Add(dgvReport);

            this.Controls.SetChildIndex(lblTitle, 3); this.Controls.SetChildIndex(btnPanel, 2);
            this.Controls.SetChildIndex(lblAlerts, 1); this.Controls.SetChildIndex(dgvReport, 0);

            this.Load += (s, e) => LoadData();
        }

        private DataGridView MakeGrid()
        {
            var d = new DataGridView { ReadOnly = true, AllowUserToAddRows = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, BackgroundColor = cardBg, BorderStyle = BorderStyle.None, RowHeadersVisible = false, EnableHeadersVisualStyles = false, Font = new Font("Segoe UI", 10), CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal, GridColor = Color.FromArgb(230, 225, 220) };
            d.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(45, 38, 32), ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            d.ColumnHeadersHeight = 40; d.RowTemplate.Height = 36;
            d.DefaultCellStyle = new DataGridViewCellStyle { SelectionBackColor = Color.FromArgb(240, 230, 218), SelectionForeColor = textDark };
            d.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(252, 250, 248) };
            return d;
        }

        /// <summary>
        /// Loads inventory report from ReportService and low stock alerts from InventoryService.
        /// </summary>
        private void LoadData()
        {
            try
            {
                // Get inventory summary from ReportService → ReportDAL.InventorySummary()
                var data = ReportService.GetInventorySummary();
                dgvReport.DataSource = data;

                // Get low stock alert message from InventoryService.cs
                lblAlerts.Text = InventoryService.GetLowStockAlertMessage();
                var lowCount = InventoryService.GetLowStockCount();
                lblAlerts.ForeColor = lowCount > 0 ? dangerRed : successGreen;
                lblAlerts.BackColor = lowCount > 0 ? Color.FromArgb(255, 248, 246) : Color.FromArgb(246, 255, 246);
            }
            catch (Exception ex) { ValidationHelper.ShowCriticalError($"Error: {ex.Message}"); }
        }
    }
}
