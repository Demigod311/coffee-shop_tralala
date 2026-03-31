// InventoryForm.cs — Inventory/Stock Management
//
// Displays inventory items with stock levels and allows adjustments.
// Connects to: InventoryDAL.cs (CRUD), InventoryService.cs (stock management),
//              AuthService.cs (current user for audit logging), MainForm.cs (embedded in content)

using CoffeeShopPOS.Database;
using CoffeeShopPOS.Models;
using CoffeeShopPOS.Services;
using CoffeeShopPOS.Helpers;

namespace CoffeeShopPOS.Forms
{
    /// <summary>
    /// Inventory management form for tracking stock levels and making adjustments.
    /// Highlights low-stock items and provides restock/usage adjustment.
    /// </summary>
    public class InventoryForm : Form
    {
        private DataGridView dgvInventory = null!;
        private TextBox txtName = null!, txtUnit = null!, txtQuantity = null!, txtReorder = null!, txtCost = null!;
        private TextBox txtAdjustQty = null!, txtAdjustReason = null!;
        private Button btnAdd = null!, btnUpdate = null!, btnDelete = null!, btnAdjust = null!, btnClear = null!;
        private int? selectedInventoryId;

        private readonly Color bgColor = Color.FromArgb(245, 242, 238);
        private readonly Color cardBg = Color.White;
        private readonly Color textDark = Color.FromArgb(50, 40, 35);
        private readonly Color accentGold = Color.FromArgb(193, 154, 107);
        private readonly Color successGreen = Color.FromArgb(76, 175, 80);
        private readonly Color dangerRed = Color.FromArgb(244, 67, 54);
        private readonly Color warningOrange = Color.FromArgb(255, 152, 0);

        public InventoryForm() { InitializeComponent(); }

        private void InitializeComponent()
        {
            this.Text = "Inventory"; this.BackColor = bgColor; this.Dock = DockStyle.Fill; this.Padding = new Padding(20);

            var lblTitle = new Label { Text = "📦 Inventory Management", Font = new Font("Segoe UI", 20, FontStyle.Bold), ForeColor = textDark, Dock = DockStyle.Top, Height = 50 };
            this.Controls.Add(lblTitle);

            // ═══ Input Panel — CRUD fields ═══
            var ip = new Panel { Dock = DockStyle.Top, Height = 110, BackColor = cardBg, Padding = new Padding(15) };

            ip.Controls.Add(L("Name:", 10, 12)); txtName = T(60, 10, 150); ip.Controls.Add(txtName);
            ip.Controls.Add(L("Unit:", 225, 12)); txtUnit = T(265, 10, 80); txtUnit.PlaceholderText = "kg, liters"; ip.Controls.Add(txtUnit);
            ip.Controls.Add(L("Qty:", 360, 12)); txtQuantity = T(395, 10, 80); ip.Controls.Add(txtQuantity);
            ip.Controls.Add(L("Reorder Lvl:", 490, 12)); txtReorder = T(580, 10, 60); ip.Controls.Add(txtReorder);
            ip.Controls.Add(L("Cost/Unit:", 660, 12)); txtCost = T(740, 10, 70); ip.Controls.Add(txtCost);

            btnAdd = B("➕ Add", successGreen, 10, 50); btnAdd.Click += BtnAdd_Click; ip.Controls.Add(btnAdd);
            btnUpdate = B("✏ Update", accentGold, 125, 50); btnUpdate.Click += BtnUpdate_Click; btnUpdate.Enabled = false; ip.Controls.Add(btnUpdate);
            btnDelete = B("🗑 Delete", dangerRed, 240, 50); btnDelete.Click += BtnDelete_Click; ip.Controls.Add(btnDelete);

            // Stock adjustment fields
            ip.Controls.Add(L("Adjust:", 395, 55)); txtAdjustQty = T(450, 52, 60); txtAdjustQty.PlaceholderText = "+/-"; ip.Controls.Add(txtAdjustQty);
            ip.Controls.Add(L("Reason:", 525, 55)); txtAdjustReason = T(585, 52, 140); txtAdjustReason.PlaceholderText = "Restock / Usage"; ip.Controls.Add(txtAdjustReason);
            btnAdjust = B("⚡ Adjust", warningOrange, 740, 50); btnAdjust.Click += BtnAdjust_Click; ip.Controls.Add(btnAdjust);
            btnClear = B("✕", Color.Gray, 855, 50); btnClear.Size = new Size(40, 35); btnClear.Click += (s, e) => ClearForm(); ip.Controls.Add(btnClear);

            this.Controls.Add(ip);

            // Grid
            dgvInventory = MakeGrid(); dgvInventory.Dock = DockStyle.Fill;
            dgvInventory.CellClick += (s, e) =>
            {
                if (e.RowIndex < 0) return;
                var row = dgvInventory.Rows[e.RowIndex];
                selectedInventoryId = Convert.ToInt32(row.Cells["Id"].Value);
                txtName.Text = row.Cells["Name"].Value?.ToString();
                txtUnit.Text = row.Cells["Unit"].Value?.ToString();
                txtQuantity.Text = row.Cells["Qty"].Value?.ToString();
                txtReorder.Text = row.Cells["Reorder"].Value?.ToString();
                txtCost.Text = row.Cells["Cost"].Value?.ToString()?.TrimStart('$');
                btnUpdate.Enabled = true;
            };
            // Colour low-stock rows in red
            dgvInventory.CellFormatting += (s, e) =>
            {
                if (e.RowIndex >= 0 && dgvInventory.Columns[e.ColumnIndex].Name == "Status")
                {
                    string? status = e.Value?.ToString();
                    if (status == "LOW") { e.CellStyle.ForeColor = dangerRed; e.CellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold); }
                    else if (status == "OK") { e.CellStyle.ForeColor = successGreen; }
                }
            };
            this.Controls.Add(dgvInventory);

            this.Controls.SetChildIndex(lblTitle, 2); this.Controls.SetChildIndex(ip, 1); this.Controls.SetChildIndex(dgvInventory, 0);
            this.Load += (s, e) => LoadData();
        }

        private Label L(string t, int x, int y) => new Label { Text = t, Font = new Font("Segoe UI", 10), ForeColor = textDark, Location = new Point(x, y), AutoSize = true };
        private TextBox T(int x, int y, int w) => new TextBox { Font = new Font("Segoe UI", 10), Location = new Point(x, y), Size = new Size(w, 25), BackColor = bgColor, BorderStyle = BorderStyle.FixedSingle };
        private Button B(string t, Color c, int x, int y)
        { var b = new Button { Text = t, Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.White, BackColor = c, FlatStyle = FlatStyle.Flat, Location = new Point(x, y), Size = new Size(105, 35), Cursor = Cursors.Hand }; b.FlatAppearance.BorderSize = 0; return b; }
        private DataGridView MakeGrid()
        {
            var d = new DataGridView { ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, BackgroundColor = cardBg, BorderStyle = BorderStyle.None, RowHeadersVisible = false, EnableHeadersVisualStyles = false, Font = new Font("Segoe UI", 10), CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal, GridColor = Color.FromArgb(230, 225, 220) };
            d.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(45, 38, 32), ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            d.ColumnHeadersHeight = 40; d.RowTemplate.Height = 36;
            d.DefaultCellStyle = new DataGridViewCellStyle { SelectionBackColor = Color.FromArgb(240, 230, 218), SelectionForeColor = textDark };
            d.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(252, 250, 248) };
            return d;
        }

        // Loads inventory from InventoryDAL.GetAll()
        private void LoadData()
        {
            try
            {
                dgvInventory.Columns.Clear();
                dgvInventory.Columns.Add("Id", "ID"); dgvInventory.Columns["Id"]!.Width = 50;
                dgvInventory.Columns.Add("Name", "Name");
                dgvInventory.Columns.Add("Qty", "Quantity"); dgvInventory.Columns["Qty"]!.Width = 80;
                dgvInventory.Columns.Add("Unit", "Unit"); dgvInventory.Columns["Unit"]!.Width = 60;
                dgvInventory.Columns.Add("Reorder", "Reorder Level"); dgvInventory.Columns["Reorder"]!.Width = 100;
                dgvInventory.Columns.Add("Cost", "Cost/Unit"); dgvInventory.Columns["Cost"]!.Width = 80;
                dgvInventory.Columns.Add("Status", "Status"); dgvInventory.Columns["Status"]!.Width = 70;
                dgvInventory.Columns.Add("Updated", "Last Updated");

                dgvInventory.Rows.Clear();
                var items = InventoryDAL.GetAll();
                foreach (var item in items)
                {
                    dgvInventory.Rows.Add(item.InventoryId, item.Name, item.Quantity, item.Unit,
                        item.ReorderLevel, item.CostPerUnit.HasValue ? $"${item.CostPerUnit:F2}" : "-",
                        item.IsLowStock ? "LOW" : "OK", item.LastUpdated.ToString("g"));
                }
            }
            catch (Exception ex) { ValidationHelper.ShowCriticalError($"Error: {ex.Message}"); }
        }

        private void BtnAdd_Click(object? sender, EventArgs e)
        {
            try
            {
                if (!ValidationHelper.RequiredField(txtName.Text, "Name")) return;
                if (!ValidationHelper.RequiredField(txtUnit.Text, "Unit")) return;
                if (!ValidationHelper.IsValidDecimal(txtQuantity.Text, "Quantity", out decimal qty)) return;

                decimal reorder = 10; if (!string.IsNullOrEmpty(txtReorder.Text)) decimal.TryParse(txtReorder.Text, out reorder);
                decimal? cost = null; if (!string.IsNullOrEmpty(txtCost.Text) && decimal.TryParse(txtCost.Text, out decimal c)) cost = c;

                InventoryDAL.Insert(new InventoryItem { Name = txtName.Text.Trim(), Unit = txtUnit.Text.Trim(), Quantity = qty, ReorderLevel = reorder, CostPerUnit = cost });
                ValidationHelper.ShowSuccess("Inventory item added!"); ClearForm(); LoadData();
            }
            catch (Exception ex) { ValidationHelper.ShowCriticalError($"Error: {ex.Message}"); }
        }

        private void BtnUpdate_Click(object? sender, EventArgs e)
        {
            try
            {
                if (!selectedInventoryId.HasValue) return;
                if (!ValidationHelper.RequiredField(txtName.Text, "Name")) return;
                if (!ValidationHelper.IsValidDecimal(txtQuantity.Text, "Quantity", out decimal qty)) return;

                decimal reorder = 10; if (!string.IsNullOrEmpty(txtReorder.Text)) decimal.TryParse(txtReorder.Text, out reorder);
                decimal? cost = null; if (!string.IsNullOrEmpty(txtCost.Text) && decimal.TryParse(txtCost.Text, out decimal c)) cost = c;

                InventoryDAL.Update(new InventoryItem { InventoryId = selectedInventoryId.Value, Name = txtName.Text.Trim(), Unit = txtUnit.Text.Trim(), Quantity = qty, ReorderLevel = reorder, CostPerUnit = cost });
                ValidationHelper.ShowSuccess("Item updated!"); ClearForm(); LoadData();
            }
            catch (Exception ex) { ValidationHelper.ShowCriticalError($"Error: {ex.Message}"); }
        }

        private void BtnDelete_Click(object? sender, EventArgs e)
        {
            try
            {
                if (!selectedInventoryId.HasValue) { ValidationHelper.ShowError("Select an item."); return; }
                if (!ValidationHelper.Confirm("Delete this inventory item and its history?")) return;
                InventoryDAL.Delete(selectedInventoryId.Value);
                ValidationHelper.ShowSuccess("Item deleted."); ClearForm(); LoadData();
            }
            catch (Exception ex) { ValidationHelper.ShowCriticalError($"Error: {ex.Message}"); }
        }

        // Stock adjustment via InventoryService.AdjustStock()
        private void BtnAdjust_Click(object? sender, EventArgs e)
        {
            try
            {
                if (!selectedInventoryId.HasValue) { ValidationHelper.ShowError("Select an item to adjust."); return; }
                if (!ValidationHelper.IsValidDecimal(txtAdjustQty.Text, "Adjust Quantity", out decimal adjustQty)) return;
                if (adjustQty == 0) { ValidationHelper.ShowError("Adjustment quantity cannot be zero."); return; }

                string reason = string.IsNullOrWhiteSpace(txtAdjustReason.Text) ? "Manual adjustment" : txtAdjustReason.Text.Trim();
                int userId = AuthService.CurrentUser?.UserId ?? 0;

                // Adjust stock via InventoryService.cs → InventoryDAL.AdjustStock()
                InventoryService.AdjustStock(selectedInventoryId.Value, adjustQty, reason, userId);
                ValidationHelper.ShowSuccess($"Stock adjusted by {adjustQty:+#.##;-#.##;0}!");
                txtAdjustQty.Clear(); txtAdjustReason.Clear(); LoadData();
            }
            catch (Exception ex) { ValidationHelper.ShowCriticalError($"Error: {ex.Message}"); }
        }

        private void ClearForm() { selectedInventoryId = null; txtName.Clear(); txtUnit.Clear(); txtQuantity.Clear(); txtReorder.Clear(); txtCost.Clear(); txtAdjustQty.Clear(); txtAdjustReason.Clear(); btnUpdate.Enabled = false; }
    }
}
