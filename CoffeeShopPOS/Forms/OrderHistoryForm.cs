// OrderHistoryForm.cs — Past Order History with Search and Details
//
// Displays a searchable list of past orders with status filtering.
// Click on an order to view its details or open billing.
// Connects to: OrderDAL.cs (queries), OrderService.cs (status updates),
//              BillingForm.cs (view receipt), ExportHelper.cs (CSV export),
//              MainForm.cs (embedded in content panel)

using CoffeeShopPOS.Database;
using CoffeeShopPOS.Models;
using CoffeeShopPOS.Services;
using CoffeeShopPOS.Helpers;

namespace CoffeeShopPOS.Forms
{
    /// <summary>
    /// Order history with date filtering, status filter, and order detail viewer.
    /// Allows staff to review past orders and update active order statuses.
    /// </summary>
    public class OrderHistoryForm : Form
    {
        private DateTimePicker dtpStart = null!, dtpEnd = null!;
        private ComboBox cmbStatus = null!;
        private DataGridView dgvOrders = null!;
        private DataGridView dgvDetails = null!;
        private Button btnSearch = null!, btnViewBill = null!, btnUpdateStatus = null!, btnExport = null!;

        private readonly Color bgColor = Color.FromArgb(245, 242, 238);
        private readonly Color cardBg = Color.White;
        private readonly Color textDark = Color.FromArgb(50, 40, 35);
        private readonly Color accentGold = Color.FromArgb(193, 154, 107);
        private readonly Color successGreen = Color.FromArgb(76, 175, 80);

        public OrderHistoryForm() { InitializeComponent(); }

        private void InitializeComponent()
        {
            this.Text = "Order History"; this.BackColor = bgColor; this.Dock = DockStyle.Fill; this.Padding = new Padding(20);

            var lblTitle = new Label { Text = "📋 Order History", Font = new Font("Segoe UI", 20, FontStyle.Bold), ForeColor = textDark, Dock = DockStyle.Top, Height = 50 };
            this.Controls.Add(lblTitle);

            // ── Filter bar ──
            var fp = new Panel { Dock = DockStyle.Top, Height = 55, BackColor = cardBg, Padding = new Padding(15) };

            fp.Controls.Add(new Label { Text = "From:", Font = new Font("Segoe UI", 10), ForeColor = textDark, Location = new Point(10, 15), AutoSize = true });
            dtpStart = new DateTimePicker { Font = new Font("Segoe UI", 10), Location = new Point(55, 12), Size = new Size(150, 25), Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(-7) };
            fp.Controls.Add(dtpStart);

            fp.Controls.Add(new Label { Text = "To:", Font = new Font("Segoe UI", 10), ForeColor = textDark, Location = new Point(220, 15), AutoSize = true });
            dtpEnd = new DateTimePicker { Font = new Font("Segoe UI", 10), Location = new Point(245, 12), Size = new Size(150, 25), Format = DateTimePickerFormat.Short };
            fp.Controls.Add(dtpEnd);

            fp.Controls.Add(new Label { Text = "Status:", Font = new Font("Segoe UI", 10), ForeColor = textDark, Location = new Point(410, 15), AutoSize = true });
            cmbStatus = new ComboBox { Font = new Font("Segoe UI", 10), Location = new Point(465, 12), Size = new Size(120, 25), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = bgColor };
            cmbStatus.Items.AddRange(new object[] { "All", "Pending", "Preparing", "Served", "Completed", "Cancelled" }); cmbStatus.SelectedIndex = 0;
            fp.Controls.Add(cmbStatus);

            btnSearch = new Button { Text = "🔍 Search", Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.White, BackColor = accentGold, FlatStyle = FlatStyle.Flat, Location = new Point(600, 9), Size = new Size(100, 35), Cursor = Cursors.Hand };
            btnSearch.FlatAppearance.BorderSize = 0; btnSearch.Click += (s, e) => LoadOrders(); fp.Controls.Add(btnSearch);

            btnExport = new Button { Text = "📥 Export", Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.White, BackColor = successGreen, FlatStyle = FlatStyle.Flat, Location = new Point(710, 9), Size = new Size(100, 35), Cursor = Cursors.Hand };
            btnExport.FlatAppearance.BorderSize = 0;
            btnExport.Click += (s, e) =>
            {
                try
                {
                    string? path = ExportHelper.ShowSaveDialog("OrderHistory");
                    if (path != null) { ExportHelper.ExportDataGridViewToCsv(dgvOrders, path); ValidationHelper.ShowSuccess($"Exported to {path}"); }
                }
                catch (Exception ex) { ValidationHelper.ShowCriticalError($"Error: {ex.Message}"); }
            };
            fp.Controls.Add(btnExport);
            this.Controls.Add(fp);

            // ── Action buttons ──
            var actionPanel = new Panel { Dock = DockStyle.Top, Height = 45 };

            btnViewBill = new Button { Text = "🧾 View Bill", Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.White, BackColor = accentGold, FlatStyle = FlatStyle.Flat, Location = new Point(5, 5), Size = new Size(110, 35), Cursor = Cursors.Hand };
            btnViewBill.FlatAppearance.BorderSize = 0;
            btnViewBill.Click += (s, e) =>
            {
                if (dgvOrders.SelectedRows.Count == 0) { ValidationHelper.ShowError("Select an order."); return; }
                int orderId = Convert.ToInt32(dgvOrders.SelectedRows[0].Cells["OrderId"].Value);
                // Open BillingForm.cs for the selected order
                new BillingForm(orderId).ShowDialog();
            };
            actionPanel.Controls.Add(btnViewBill);

            btnUpdateStatus = new Button { Text = "🔄 Update Status", Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.White, BackColor = Color.FromArgb(33, 150, 243), FlatStyle = FlatStyle.Flat, Location = new Point(125, 5), Size = new Size(140, 35), Cursor = Cursors.Hand };
            btnUpdateStatus.FlatAppearance.BorderSize = 0;
            btnUpdateStatus.Click += BtnUpdateStatus_Click;
            actionPanel.Controls.Add(btnUpdateStatus);

            this.Controls.Add(actionPanel);

            // ── Orders grid ──
            dgvOrders = MakeGrid(); dgvOrders.Dock = DockStyle.Fill;
            dgvOrders.CellClick += DgvOrders_CellClick;
            // Colour-code status cells
            dgvOrders.CellFormatting += (s, e) =>
            {
                if (e.RowIndex >= 0 && dgvOrders.Columns[e.ColumnIndex].Name == "Status")
                {
                    e.CellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
                    e.CellStyle.ForeColor = e.Value?.ToString() switch
                    {
                        "Completed" => Color.FromArgb(76, 175, 80),
                        "Cancelled" => Color.FromArgb(244, 67, 54),
                        "Pending" => Color.FromArgb(255, 152, 0),
                        "Preparing" => Color.FromArgb(33, 150, 243),
                        "Served" => Color.FromArgb(0, 150, 136),
                        _ => textDark
                    };
                }
            };
            this.Controls.Add(dgvOrders);

            // ── Order details grid (bottom panel) ──
            dgvDetails = MakeGrid(); dgvDetails.Dock = DockStyle.Bottom; dgvDetails.Height = 200;
            this.Controls.Add(dgvDetails);

            var detailLabel = new Label { Text = "📦 Order Items (click an order above to view)", Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = textDark, Dock = DockStyle.Bottom, Height = 30, Padding = new Padding(5, 5, 0, 0) };
            this.Controls.Add(detailLabel);

            this.Controls.SetChildIndex(lblTitle, 6); this.Controls.SetChildIndex(fp, 5);
            this.Controls.SetChildIndex(actionPanel, 4); this.Controls.SetChildIndex(dgvOrders, 3);
            this.Controls.SetChildIndex(detailLabel, 2); this.Controls.SetChildIndex(dgvDetails, 1);

            this.Load += (s, e) => LoadOrders();
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
        /// Loads orders from OrderDAL.GetByDateRange() with optional status filter.
        /// </summary>
        private void LoadOrders()
        {
            try
            {
                dgvOrders.Columns.Clear();
                dgvOrders.Columns.Add("OrderId", "Order #"); dgvOrders.Columns["OrderId"]!.Width = 70;
                dgvOrders.Columns.Add("Table", "Table"); dgvOrders.Columns["Table"]!.Width = 70;
                dgvOrders.Columns.Add("Type", "Type"); dgvOrders.Columns["Type"]!.Width = 80;
                dgvOrders.Columns.Add("Status", "Status"); dgvOrders.Columns["Status"]!.Width = 90;
                dgvOrders.Columns.Add("Total", "Total"); dgvOrders.Columns["Total"]!.Width = 80;
                dgvOrders.Columns.Add("Payment", "Payment"); dgvOrders.Columns["Payment"]!.Width = 80;
                dgvOrders.Columns.Add("Staff", "Staff");
                dgvOrders.Columns.Add("Date", "Date"); dgvOrders.Columns["Date"]!.Width = 130;

                dgvOrders.Rows.Clear();
                var orders = OrderDAL.GetByDateRange(dtpStart.Value.Date, dtpEnd.Value.Date.AddDays(1));

                string statusFilter = cmbStatus.SelectedItem?.ToString() ?? "All";
                foreach (var o in orders)
                {
                    if (statusFilter != "All" && o.Status != statusFilter) continue;
                    dgvOrders.Rows.Add(o.OrderId, o.TableNumber ?? "Takeaway", o.OrderType,
                        o.Status, $"${o.Total:F2}", o.PaymentMethod, o.StaffName, o.CreatedAt.ToString("g"));
                }
            }
            catch (Exception ex) { ValidationHelper.ShowCriticalError($"Error: {ex.Message}"); }
        }

        /// <summary>
        /// Click on order row → load its line items into the details grid.
        /// Items loaded from OrderDAL.GetOrderItems().
        /// </summary>
        private void DgvOrders_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            try
            {
                int orderId = Convert.ToInt32(dgvOrders.Rows[e.RowIndex].Cells["OrderId"].Value);
                var items = OrderDAL.GetOrderItems(orderId);

                dgvDetails.Columns.Clear();
                dgvDetails.Columns.Add("Item", "Item");
                dgvDetails.Columns.Add("Qty", "Qty"); dgvDetails.Columns["Qty"]!.Width = 60;
                dgvDetails.Columns.Add("Price", "Unit Price"); dgvDetails.Columns["Price"]!.Width = 80;
                dgvDetails.Columns.Add("Total", "Total"); dgvDetails.Columns["Total"]!.Width = 80;
                dgvDetails.Columns.Add("Notes", "Notes");

                dgvDetails.Rows.Clear();
                foreach (var item in items)
                    dgvDetails.Rows.Add(item.ItemName, item.Quantity, $"${item.UnitPrice:F2}", $"${item.LineTotal:F2}", item.Notes ?? "");
            }
            catch (Exception ex) { ValidationHelper.ShowCriticalError($"Error: {ex.Message}"); }
        }

        /// <summary>
        /// Update order status via OrderService.UpdateStatus().
        /// Shows a dropdown to select the new status.
        /// </summary>
        private void BtnUpdateStatus_Click(object? sender, EventArgs e)
        {
            if (dgvOrders.SelectedRows.Count == 0) { ValidationHelper.ShowError("Select an order."); return; }

            int orderId = Convert.ToInt32(dgvOrders.SelectedRows[0].Cells["OrderId"].Value);
            string currentStatus = dgvOrders.SelectedRows[0].Cells["Status"].Value?.ToString() ?? "";

            // Build status selection dialog
            using var dialog = new Form { Text = "Update Status", Size = new Size(300, 180), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, BackColor = cardBg };

            dialog.Controls.Add(new Label { Text = $"Order #{orderId} — Current: {currentStatus}", Font = new Font("Segoe UI", 10), Location = new Point(15, 15), AutoSize = true });

            var cmbNew = new ComboBox { Font = new Font("Segoe UI", 10), Location = new Point(15, 45), Size = new Size(250, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbNew.Items.AddRange(new[] { "Pending", "Preparing", "Served", "Completed", "Cancelled" }); cmbNew.SelectedItem = currentStatus;
            dialog.Controls.Add(cmbNew);

            var btnConfirm = new Button { Text = "✓ Update", Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.White, BackColor = accentGold, FlatStyle = FlatStyle.Flat, Location = new Point(15, 85), Size = new Size(120, 35), Cursor = Cursors.Hand, DialogResult = DialogResult.OK };
            btnConfirm.FlatAppearance.BorderSize = 0;
            dialog.Controls.Add(btnConfirm);

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    string newStatus = cmbNew.SelectedItem?.ToString() ?? "Pending";
                    OrderService.UpdateStatus(orderId, newStatus);
                    ValidationHelper.ShowSuccess($"Order #{orderId} status updated to {newStatus}.");
                    LoadOrders();
                }
                catch (Exception ex) { ValidationHelper.ShowCriticalError($"Error: {ex.Message}"); }
            }
        }
    }
}
