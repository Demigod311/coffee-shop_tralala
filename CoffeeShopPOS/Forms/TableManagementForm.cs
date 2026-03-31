// TableManagementForm.cs — Table Management with Visual Grid
//
// Displays restaurant tables as colour-coded cards and allows CRUD operations.
// Green = Available, Red = Occupied, Yellow = Reserved
// Connects to: TableDAL.cs (CRUD), OrderDAL.cs (check active orders),
//              MainForm.cs (embedded in content panel)

using CoffeeShopPOS.Database;
using CoffeeShopPOS.Models;
using CoffeeShopPOS.Helpers;

namespace CoffeeShopPOS.Forms
{
    /// <summary>
    /// Visual table management with colour-coded status cards.
    /// Shows all tables as a grid of panels with status-based coloring.
    /// </summary>
    public class TableManagementForm : Form
    {
        private FlowLayoutPanel tableGrid = null!;
        private TextBox txtNumber = null!, txtCapacity = null!;
        private ComboBox cmbStatus = null!;
        private Button btnAdd = null!, btnUpdate = null!, btnDelete = null!, btnRefresh = null!;
        private int? selectedTableId;

        private readonly Color bgColor = Color.FromArgb(245, 242, 238);
        private readonly Color cardBg = Color.White;
        private readonly Color textDark = Color.FromArgb(50, 40, 35);
        private readonly Color availableGreen = Color.FromArgb(76, 175, 80);
        private readonly Color occupiedRed = Color.FromArgb(244, 67, 54);
        private readonly Color reservedYellow = Color.FromArgb(255, 193, 7);
        private readonly Color accentGold = Color.FromArgb(193, 154, 107);

        public TableManagementForm() { InitializeComponent(); }

        private void InitializeComponent()
        {
            this.Text = "Table Management";
            this.BackColor = bgColor;
            this.Dock = DockStyle.Fill;
            this.Padding = new Padding(20);

            var lblTitle = new Label
            { Text = "🪑 Table Management", Font = new Font("Segoe UI", 20, FontStyle.Bold), ForeColor = textDark, Dock = DockStyle.Top, Height = 50 };
            this.Controls.Add(lblTitle);

            // ── Legend ──
            var legendPanel = new Panel { Dock = DockStyle.Top, Height = 35 };
            AddLegendItem(legendPanel, "● Available", availableGreen, 0);
            AddLegendItem(legendPanel, "● Occupied", occupiedRed, 130);
            AddLegendItem(legendPanel, "● Reserved", reservedYellow, 250);
            this.Controls.Add(legendPanel);

            // ── Input panel ──
            var inputPanel = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = cardBg, Padding = new Padding(15) };

            inputPanel.Controls.Add(new Label { Text = "Table #:", Font = new Font("Segoe UI", 10), ForeColor = textDark, Location = new Point(10, 18), AutoSize = true });
            txtNumber = new TextBox { Font = new Font("Segoe UI", 10), Location = new Point(80, 15), Size = new Size(80, 25), BackColor = bgColor, BorderStyle = BorderStyle.FixedSingle, PlaceholderText = "e.g. T11" };
            inputPanel.Controls.Add(txtNumber);

            inputPanel.Controls.Add(new Label { Text = "Seats:", Font = new Font("Segoe UI", 10), ForeColor = textDark, Location = new Point(175, 18), AutoSize = true });
            txtCapacity = new TextBox { Font = new Font("Segoe UI", 10), Location = new Point(225, 15), Size = new Size(60, 25), BackColor = bgColor, BorderStyle = BorderStyle.FixedSingle, PlaceholderText = "4" };
            inputPanel.Controls.Add(txtCapacity);

            inputPanel.Controls.Add(new Label { Text = "Status:", Font = new Font("Segoe UI", 10), ForeColor = textDark, Location = new Point(300, 18), AutoSize = true });
            cmbStatus = new ComboBox { Font = new Font("Segoe UI", 10), Location = new Point(355, 15), Size = new Size(120, 25), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = bgColor };
            cmbStatus.Items.AddRange(new[] { "Available", "Occupied", "Reserved" });
            cmbStatus.SelectedIndex = 0;
            inputPanel.Controls.Add(cmbStatus);

            btnAdd = MakeBtn("➕ Add", Color.FromArgb(76, 175, 80), 495, 12); btnAdd.Click += BtnAdd_Click; inputPanel.Controls.Add(btnAdd);
            btnUpdate = MakeBtn("✏ Update", accentGold, 605, 12); btnUpdate.Click += BtnUpdate_Click; btnUpdate.Enabled = false; inputPanel.Controls.Add(btnUpdate);
            btnDelete = MakeBtn("🗑 Delete", occupiedRed, 715, 12); btnDelete.Click += BtnDelete_Click; inputPanel.Controls.Add(btnDelete);
            btnRefresh = MakeBtn("🔄", Color.Gray, 825, 12); btnRefresh.Size = new Size(40, 35); btnRefresh.Click += (s, e) => LoadTables(); inputPanel.Controls.Add(btnRefresh);

            this.Controls.Add(inputPanel);

            // ── Visual table grid ──
            tableGrid = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                WrapContents = true,
                Padding = new Padding(10),
                BackColor = bgColor,
            };
            this.Controls.Add(tableGrid);

            this.Controls.SetChildIndex(lblTitle, 3);
            this.Controls.SetChildIndex(legendPanel, 2);
            this.Controls.SetChildIndex(inputPanel, 1);
            this.Controls.SetChildIndex(tableGrid, 0);

            this.Load += (s, e) => LoadTables();
        }

        private void AddLegendItem(Panel parent, string text, Color color, int x)
        {
            parent.Controls.Add(new Label { Text = text, Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = color, Location = new Point(x, 5), AutoSize = true });
        }

        private Button MakeBtn(string text, Color bg, int x, int y)
        {
            var btn = new Button { Text = text, Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.White, BackColor = bg, FlatStyle = FlatStyle.Flat, Location = new Point(x, y), Size = new Size(100, 35), Cursor = Cursors.Hand };
            btn.FlatAppearance.BorderSize = 0; return btn;
        }

        /// <summary>
        /// Loads all tables from TableDAL.GetAll() and displays them as colour-coded cards.
        /// </summary>
        private void LoadTables()
        {
            try
            {
                tableGrid.Controls.Clear();
                var tables = TableDAL.GetAll();

                foreach (var table in tables)
                {
                    // Create a colour-coded card for each table
                    var card = new Panel
                    {
                        Size = new Size(160, 130),
                        Margin = new Padding(8),
                        Cursor = Cursors.Hand,
                        BackColor = table.Status switch
                        {
                            "Available" => availableGreen,
                            "Occupied" => occupiedRed,
                            "Reserved" => reservedYellow,
                            _ => Color.Gray
                        },
                        Tag = table,
                    };

                    var lblNum = new Label
                    {
                        Text = table.TableNumber,
                        Font = new Font("Segoe UI", 18, FontStyle.Bold),
                        ForeColor = Color.White,
                        TextAlign = ContentAlignment.MiddleCenter,
                        Location = new Point(10, 10),
                        Size = new Size(140, 40),
                        BackColor = Color.Transparent,
                    };
                    card.Controls.Add(lblNum);

                    var lblSeats = new Label
                    {
                        Text = $"👤 {table.Capacity} seats",
                        Font = new Font("Segoe UI", 10),
                        ForeColor = Color.White,
                        TextAlign = ContentAlignment.MiddleCenter,
                        Location = new Point(10, 55),
                        Size = new Size(140, 22),
                        BackColor = Color.Transparent,
                    };
                    card.Controls.Add(lblSeats);

                    var lblStatus = new Label
                    {
                        Text = table.Status,
                        Font = new Font("Segoe UI", 10, FontStyle.Bold),
                        ForeColor = Color.White,
                        TextAlign = ContentAlignment.MiddleCenter,
                        Location = new Point(10, 85),
                        Size = new Size(140, 22),
                        BackColor = Color.Transparent,
                    };
                    card.Controls.Add(lblStatus);

                    // Click to select table for editing
                    void selectTable(object? s, EventArgs e)
                    {
                        selectedTableId = table.TableId;
                        txtNumber.Text = table.TableNumber;
                        txtCapacity.Text = table.Capacity.ToString();
                        cmbStatus.SelectedItem = table.Status;
                        btnUpdate.Enabled = true;
                    }
                    card.Click += selectTable;
                    lblNum.Click += selectTable;
                    lblSeats.Click += selectTable;
                    lblStatus.Click += selectTable;

                    tableGrid.Controls.Add(card);
                }
            }
            catch (Exception ex) { ValidationHelper.ShowCriticalError($"Error: {ex.Message}"); }
        }

        private void BtnAdd_Click(object? sender, EventArgs e)
        {
            try
            {
                if (!ValidationHelper.RequiredField(txtNumber.Text, "Table Number")) return;
                if (!ValidationHelper.IsValidInt(txtCapacity.Text, "Capacity", out int cap)) return;
                if (!ValidationHelper.PositiveNumber(cap, "Capacity")) return;

                TableDAL.Insert(new Table { TableNumber = txtNumber.Text.Trim(), Capacity = cap, Status = cmbStatus.SelectedItem?.ToString() ?? "Available" });
                ValidationHelper.ShowSuccess("Table added!"); ClearForm(); LoadTables();
            }
            catch (Exception ex) { ValidationHelper.ShowCriticalError($"Error: {ex.Message}"); }
        }

        private void BtnUpdate_Click(object? sender, EventArgs e)
        {
            try
            {
                if (!selectedTableId.HasValue) return;
                if (!ValidationHelper.RequiredField(txtNumber.Text, "Table Number")) return;
                if (!ValidationHelper.IsValidInt(txtCapacity.Text, "Capacity", out int cap)) return;

                TableDAL.Update(new Table { TableId = selectedTableId.Value, TableNumber = txtNumber.Text.Trim(), Capacity = cap, Status = cmbStatus.SelectedItem?.ToString() ?? "Available" });
                ValidationHelper.ShowSuccess("Table updated!"); ClearForm(); LoadTables();
            }
            catch (Exception ex) { ValidationHelper.ShowCriticalError($"Error: {ex.Message}"); }
        }

        private void BtnDelete_Click(object? sender, EventArgs e)
        {
            try
            {
                if (!selectedTableId.HasValue) { ValidationHelper.ShowError("Select a table."); return; }
                if (!ValidationHelper.Confirm("Delete this table?")) return;
                TableDAL.Delete(selectedTableId.Value);
                ValidationHelper.ShowSuccess("Table deleted."); ClearForm(); LoadTables();
            }
            catch (Exception ex) { ValidationHelper.ShowCriticalError($"Error: {ex.Message}"); }
        }

        private void ClearForm()
        {
            selectedTableId = null; txtNumber.Clear(); txtCapacity.Clear();
            cmbStatus.SelectedIndex = 0; btnUpdate.Enabled = false;
        }
    }
}
