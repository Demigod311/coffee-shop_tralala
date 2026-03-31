// MenuManagementForm.cs — Menu Item CRUD Management
//
// Admin/Manager form for adding, editing, and managing menu items.
// Connects to: MenuItemDAL.cs (CRUD operations), CategoryDAL.cs (category dropdown),
//              MainForm.cs (embedded in content panel)

using CoffeeShopPOS.Database;
using CoffeeShopPOS.Models;
using CoffeeShopPOS.Helpers;

namespace CoffeeShopPOS.Forms
{
    /// <summary>
    /// CRUD form for managing menu items with category association.
    /// Admin and Manager roles only (enforced by MainForm.cs sidebar).
    /// </summary>
    public class MenuManagementForm : Form
    {
        private DataGridView dgvItems = null!;
        private TextBox txtName = null!, txtDescription = null!, txtPrice = null!;
        private ComboBox cmbCategory = null!;
        private CheckBox chkAvailable = null!;
        private Button btnAdd = null!, btnUpdate = null!, btnToggle = null!, btnClear = null!;
        private int? selectedItemId;

        private readonly Color bgColor = Color.FromArgb(245, 242, 238);
        private readonly Color cardBg = Color.White;
        private readonly Color textDark = Color.FromArgb(50, 40, 35);
        private readonly Color accentGold = Color.FromArgb(193, 154, 107);
        private readonly Color successGreen = Color.FromArgb(76, 175, 80);
        private readonly Color warningOrange = Color.FromArgb(255, 152, 0);

        public MenuManagementForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Menu Management";
            this.BackColor = bgColor;
            this.Dock = DockStyle.Fill;
            this.Padding = new Padding(20);

            // ── Title ──
            var lblTitle = new Label
            {
                Text = "🍽 Menu Item Management",
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = textDark,
                Dock = DockStyle.Top,
                Height = 50,
            };
            this.Controls.Add(lblTitle);

            // ══════════════════════════════════════════════════════════
            // INPUT PANEL — Form fields for adding/editing menu items
            // ══════════════════════════════════════════════════════════
            var inputPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 130,
                BackColor = cardBg,
                Padding = new Padding(15),
            };

            // Row 1: Name, Category, Price
            inputPanel.Controls.Add(CreateLabel("Name:", 10, 10));
            txtName = CreateTextBox(70, 8, 200);
            inputPanel.Controls.Add(txtName);

            inputPanel.Controls.Add(CreateLabel("Category:", 285, 10));
            cmbCategory = new ComboBox
            {
                Font = new Font("Segoe UI", 10),
                Location = new Point(360, 8),
                Size = new Size(180, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = bgColor,
            };
            inputPanel.Controls.Add(cmbCategory);

            inputPanel.Controls.Add(CreateLabel("Price ($):", 555, 10));
            txtPrice = CreateTextBox(630, 8, 100);
            inputPanel.Controls.Add(txtPrice);

            // Row 2: Description, Available
            inputPanel.Controls.Add(CreateLabel("Description:", 10, 48));
            txtDescription = CreateTextBox(100, 46, 400);
            inputPanel.Controls.Add(txtDescription);

            chkAvailable = new CheckBox
            {
                Text = "Available",
                Font = new Font("Segoe UI", 10),
                ForeColor = textDark,
                Location = new Point(520, 48),
                Size = new Size(100, 25),
                Checked = true,
            };
            inputPanel.Controls.Add(chkAvailable);

            // Row 3: Action buttons
            btnAdd = CreateButton("➕ Add", successGreen, 10, 85);
            btnAdd.Click += BtnAdd_Click;
            inputPanel.Controls.Add(btnAdd);

            btnUpdate = CreateButton("✏ Update", accentGold, 130, 85);
            btnUpdate.Click += BtnUpdate_Click;
            btnUpdate.Enabled = false;
            inputPanel.Controls.Add(btnUpdate);

            btnToggle = CreateButton("🔄 Toggle", warningOrange, 250, 85);
            btnToggle.Click += BtnToggle_Click;
            inputPanel.Controls.Add(btnToggle);

            btnClear = CreateButton("✕ Clear", Color.Gray, 370, 85);
            btnClear.Click += (s, e) => ClearForm();
            inputPanel.Controls.Add(btnClear);

            this.Controls.Add(inputPanel);

            // ══════════════════════════════════════════════════════════
            // DATA GRID — Displays all menu items
            // ══════════════════════════════════════════════════════════
            dgvItems = CreateStyledGrid();
            dgvItems.Dock = DockStyle.Fill;
            // Click on a row to load it into the edit form
            dgvItems.CellClick += DgvItems_CellClick;
            this.Controls.Add(dgvItems);

            // Fix dock order
            this.Controls.SetChildIndex(lblTitle, 2);
            this.Controls.SetChildIndex(inputPanel, 1);
            this.Controls.SetChildIndex(dgvItems, 0);

            this.Load += (s, e) => { LoadCategories(); LoadItems(); };
        }

        private Label CreateLabel(string text, int x, int y) =>
            new Label { Text = text, Font = new Font("Segoe UI", 10), ForeColor = textDark, Location = new Point(x, y), AutoSize = true };

        private TextBox CreateTextBox(int x, int y, int width) =>
            new TextBox { Font = new Font("Segoe UI", 10), Location = new Point(x, y), Size = new Size(width, 25), BackColor = bgColor, BorderStyle = BorderStyle.FixedSingle };

        private Button CreateButton(string text, Color color, int x, int y)
        {
            var btn = new Button
            {
                Text = text, Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.White, BackColor = color, FlatStyle = FlatStyle.Flat,
                Location = new Point(x, y), Size = new Size(110, 35), Cursor = Cursors.Hand,
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private DataGridView CreateStyledGrid()
        {
            var dgv = new DataGridView
            {
                ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = cardBg, BorderStyle = BorderStyle.None, RowHeadersVisible = false,
                EnableHeadersVisualStyles = false, Font = new Font("Segoe UI", 10),
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal, GridColor = Color.FromArgb(230, 225, 220),
            };
            dgv.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            { BackColor = Color.FromArgb(45, 38, 32), ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            dgv.ColumnHeadersHeight = 40; dgv.RowTemplate.Height = 36;
            dgv.DefaultCellStyle = new DataGridViewCellStyle
            { SelectionBackColor = Color.FromArgb(240, 230, 218), SelectionForeColor = textDark };
            dgv.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(252, 250, 248) };
            return dgv;
        }

        // Loads categories into the dropdown from CategoryDAL.GetAll()
        private void LoadCategories()
        {
            cmbCategory.Items.Clear();
            var categories = CategoryDAL.GetAll();
            foreach (var cat in categories) cmbCategory.Items.Add(cat);
            if (cmbCategory.Items.Count > 0) cmbCategory.SelectedIndex = 0;
        }

        // Loads all menu items into the grid from MenuItemDAL.GetAll()
        private void LoadItems()
        {
            try
            {
                dgvItems.Columns.Clear();
                dgvItems.Columns.Add("Id", "ID"); dgvItems.Columns["Id"]!.Width = 50;
                dgvItems.Columns.Add("Name", "Name");
                dgvItems.Columns.Add("Category", "Category");
                dgvItems.Columns.Add("Price", "Price"); dgvItems.Columns["Price"]!.Width = 80;
                dgvItems.Columns.Add("Available", "Available"); dgvItems.Columns["Available"]!.Width = 80;
                dgvItems.Columns.Add("Description", "Description");

                dgvItems.Rows.Clear();
                var items = MenuItemDAL.GetAll(includeUnavailable: true);
                foreach (var item in items)
                {
                    dgvItems.Rows.Add(item.ItemId, item.Name, item.CategoryName,
                        $"${item.Price:F2}", item.IsAvailable ? "Yes" : "No", item.Description ?? "");
                }
            }
            catch (Exception ex) { ValidationHelper.ShowCriticalError($"Error loading items: {ex.Message}"); }
        }

        // Click on grid row → populate form fields for editing
        private void DgvItems_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var row = dgvItems.Rows[e.RowIndex];
            selectedItemId = Convert.ToInt32(row.Cells["Id"].Value);
            txtName.Text = row.Cells["Name"].Value?.ToString();
            txtDescription.Text = row.Cells["Description"].Value?.ToString();
            txtPrice.Text = row.Cells["Price"].Value?.ToString()?.TrimStart('$');
            chkAvailable.Checked = row.Cells["Available"].Value?.ToString() == "Yes";

            // Find and select the matching category in the dropdown
            string catName = row.Cells["Category"].Value?.ToString() ?? "";
            for (int i = 0; i < cmbCategory.Items.Count; i++)
            {
                if (cmbCategory.Items[i].ToString() == catName) { cmbCategory.SelectedIndex = i; break; }
            }

            btnUpdate.Enabled = true;
        }

        // Add new menu item via MenuItemDAL.Insert()
        private void BtnAdd_Click(object? sender, EventArgs e)
        {
            try
            {
                if (!ValidateInput()) return;
                var item = BuildMenuItemFromForm();
                MenuItemDAL.Insert(item);
                ValidationHelper.ShowSuccess("Menu item added successfully!");
                ClearForm(); LoadItems();
            }
            catch (Exception ex) { ValidationHelper.ShowCriticalError($"Error: {ex.Message}"); }
        }

        // Update existing menu item via MenuItemDAL.Update()
        private void BtnUpdate_Click(object? sender, EventArgs e)
        {
            try
            {
                if (!selectedItemId.HasValue) { ValidationHelper.ShowError("Select an item first."); return; }
                if (!ValidateInput()) return;
                var item = BuildMenuItemFromForm();
                item.ItemId = selectedItemId.Value;
                MenuItemDAL.Update(item);
                ValidationHelper.ShowSuccess("Menu item updated!");
                ClearForm(); LoadItems();
            }
            catch (Exception ex) { ValidationHelper.ShowCriticalError($"Error: {ex.Message}"); }
        }

        // Toggle availability via MenuItemDAL.ToggleAvailability()
        private void BtnToggle_Click(object? sender, EventArgs e)
        {
            try
            {
                if (!selectedItemId.HasValue) { ValidationHelper.ShowError("Select an item first."); return; }
                MenuItemDAL.ToggleAvailability(selectedItemId.Value);
                ValidationHelper.ShowSuccess("Availability toggled!");
                LoadItems();
            }
            catch (Exception ex) { ValidationHelper.ShowCriticalError($"Error: {ex.Message}"); }
        }

        private bool ValidateInput()
        {
            if (!ValidationHelper.RequiredField(txtName.Text, "Name")) return false;
            if (cmbCategory.SelectedItem == null) { ValidationHelper.ShowError("Select a category."); return false; }
            if (!ValidationHelper.IsValidDecimal(txtPrice.Text, "Price", out decimal price)) return false;
            if (!ValidationHelper.PositiveNumber(price, "Price")) return false;
            return true;
        }

        private MenuItem BuildMenuItemFromForm()
        {
            var cat = (Category)cmbCategory.SelectedItem!;
            return new MenuItem
            {
                CategoryId = cat.CategoryId,
                Name = txtName.Text.Trim(),
                Description = txtDescription.Text.Trim(),
                Price = decimal.Parse(txtPrice.Text),
                IsAvailable = chkAvailable.Checked,
            };
        }

        private void ClearForm()
        {
            selectedItemId = null;
            txtName.Clear(); txtDescription.Clear(); txtPrice.Clear();
            chkAvailable.Checked = true; btnUpdate.Enabled = false;
            if (cmbCategory.Items.Count > 0) cmbCategory.SelectedIndex = 0;
        }
    }
}
