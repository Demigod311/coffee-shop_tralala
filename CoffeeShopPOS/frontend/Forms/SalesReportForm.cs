// SalesReportForm.cs — Sales Report with Date Filtering and Export
//
// Displays sales data with date range filtering and CSV export.
// Connects to: ReportService.cs (data), ExportHelper.cs (CSV),
//              MainForm.cs (embedded in content panel)

using CoffeeShopPOS.Services;
using CoffeeShopPOS.Helpers;
using System.Data;

namespace CoffeeShopPOS.Forms
{
    /// <summary>
    /// Sales report form with date range filter, multiple views (daily, category, payment),
    /// and CSV export functionality.
    /// </summary>
    public class SalesReportForm : Form
    {
        private DateTimePicker dtpStart = null!, dtpEnd = null!;
        private ComboBox cmbReportType = null!;
        private DataGridView dgvReport = null!;
        private Button btnGenerate = null!, btnExport = null!;
        private Label lblSummary = null!;

        private readonly Color bgColor = Color.FromArgb(245, 242, 238);
        private readonly Color cardBg = Color.White;
        private readonly Color textDark = Color.FromArgb(50, 40, 35);
        private readonly Color accentGold = Color.FromArgb(193, 154, 107);
        private readonly Color successGreen = Color.FromArgb(76, 175, 80);

        public SalesReportForm() { InitializeComponent(); }

        private void InitializeComponent()
        {
            this.Text = "Sales Report"; this.BackColor = bgColor; this.Dock = DockStyle.Fill; this.Padding = new Padding(20);

            var lblTitle = new Label { Text = "📈 Sales Report", Font = new Font("Segoe UI", 20, FontStyle.Bold), ForeColor = textDark, Dock = DockStyle.Top, Height = 50 };
            this.Controls.Add(lblTitle);

            // ── Filter bar ──
            var filterPanel = new Panel { Dock = DockStyle.Top, Height = 55, BackColor = cardBg, Padding = new Padding(15) };

            filterPanel.Controls.Add(new Label { Text = "From:", Font = new Font("Segoe UI", 10), ForeColor = textDark, Location = new Point(10, 15), AutoSize = true });
            dtpStart = new DateTimePicker { Font = new Font("Segoe UI", 10), Location = new Point(55, 12), Size = new Size(180, 25), Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(-30) };
            filterPanel.Controls.Add(dtpStart);

            filterPanel.Controls.Add(new Label { Text = "To:", Font = new Font("Segoe UI", 10), ForeColor = textDark, Location = new Point(250, 15), AutoSize = true });
            dtpEnd = new DateTimePicker { Font = new Font("Segoe UI", 10), Location = new Point(280, 12), Size = new Size(180, 25), Format = DateTimePickerFormat.Short, Value = DateTime.Today };
            filterPanel.Controls.Add(dtpEnd);

            filterPanel.Controls.Add(new Label { Text = "View:", Font = new Font("Segoe UI", 10), ForeColor = textDark, Location = new Point(480, 15), AutoSize = true });
            cmbReportType = new ComboBox { Font = new Font("Segoe UI", 10), Location = new Point(520, 12), Size = new Size(150, 25), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = bgColor };
            cmbReportType.Items.AddRange(new[] { "Daily Sales", "Top Items", "By Category", "By Payment" });
            cmbReportType.SelectedIndex = 0;
            filterPanel.Controls.Add(cmbReportType);

            // Generate button → calls ReportService methods
            btnGenerate = new Button { Text = "📊 Generate", Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.White, BackColor = accentGold, FlatStyle = FlatStyle.Flat, Location = new Point(690, 9), Size = new Size(110, 35), Cursor = Cursors.Hand };
            btnGenerate.FlatAppearance.BorderSize = 0;
            btnGenerate.Click += BtnGenerate_Click;
            filterPanel.Controls.Add(btnGenerate);

            // Export button → calls ExportHelper.cs
            btnExport = new Button { Text = "📥 Export CSV", Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.White, BackColor = successGreen, FlatStyle = FlatStyle.Flat, Location = new Point(810, 9), Size = new Size(120, 35), Cursor = Cursors.Hand };
            btnExport.FlatAppearance.BorderSize = 0;
            btnExport.Click += BtnExport_Click;
            filterPanel.Controls.Add(btnExport);

            this.Controls.Add(filterPanel);

            // Summary label
            lblSummary = new Label { Text = "Click 'Generate' to view the report.", Font = new Font("Segoe UI", 11), ForeColor = textDark, Dock = DockStyle.Top, Height = 35, Padding = new Padding(5, 8, 0, 0) };
            this.Controls.Add(lblSummary);

            // Data grid
            dgvReport = MakeGrid(); dgvReport.Dock = DockStyle.Fill;
            this.Controls.Add(dgvReport);

            this.Controls.SetChildIndex(lblTitle, 3); this.Controls.SetChildIndex(filterPanel, 2);
            this.Controls.SetChildIndex(lblSummary, 1); this.Controls.SetChildIndex(dgvReport, 0);

            this.Load += (s, e) => BtnGenerate_Click(s, e);
        }

        private DataGridView MakeGrid()
        {
            var d = new DataGridView { ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, BackgroundColor = cardBg, BorderStyle = BorderStyle.None, RowHeadersVisible = false, EnableHeadersVisualStyles = false, Font = new Font("Segoe UI", 10), CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal, GridColor = Color.FromArgb(230, 225, 220) };
            d.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(45, 38, 32), ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            d.ColumnHeadersHeight = 40; d.RowTemplate.Height = 36;
            d.DefaultCellStyle = new DataGridViewCellStyle { SelectionBackColor = Color.FromArgb(240, 230, 218), SelectionForeColor = textDark };
            d.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(252, 250, 248) };
            return d;
        }

        /// <summary>
        /// Generates the selected report type using ReportService.
        /// </summary>
        private void BtnGenerate_Click(object? sender, EventArgs e)
        {
            try
            {
                DateTime start = dtpStart.Value.Date;
                DateTime end = dtpEnd.Value.Date.AddDays(1).AddSeconds(-1);  // End of day

                DataTable? data = cmbReportType.SelectedIndex switch
                {
                    0 => ReportService.GetDailySalesReport(start, end),       // Daily Sales from ReportDAL.DailySales()
                    1 => ReportService.GetTopSellingItems(20, start, end),    // Top Items from ReportDAL.TopSellingItems()
                    2 => ReportService.GetSalesByCategory(start, end),        // By Category from ReportDAL.SalesByCategory()
                    3 => ReportService.GetSalesByPaymentMethod(start, end),   // By Payment from ReportDAL.SalesByPaymentMethod()
                    _ => null
                };

                if (data != null)
                {
                    dgvReport.DataSource = data;
                    lblSummary.Text = $"📊 {cmbReportType.SelectedItem} — {data.Rows.Count} record(s) | {start:d} to {dtpEnd.Value:d}";
                }
            }
            catch (Exception ex) { ValidationHelper.ShowCriticalError($"Error generating report: {ex.Message}"); }
        }

        /// <summary>
        /// Exports current report to CSV via ExportHelper.cs.
        /// </summary>
        private void BtnExport_Click(object? sender, EventArgs e)
        {
            try
            {
                if (dgvReport.DataSource is not DataTable dt || dt.Rows.Count == 0)
                { ValidationHelper.ShowError("No data to export. Generate a report first."); return; }

                string name = $"SalesReport_{cmbReportType.SelectedItem?.ToString()?.Replace(" ", "")}";
                string? path = ExportHelper.ShowSaveDialog(name);
                if (path != null)
                {
                    ExportHelper.ExportToCsv(dt, path);
                    ValidationHelper.ShowSuccess($"Report exported to:\n{path}");
                }
            }
            catch (Exception ex) { ValidationHelper.ShowCriticalError($"Error exporting: {ex.Message}"); }
        }
    }
}
