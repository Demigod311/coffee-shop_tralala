// CategoryManagementForm.cs — Category CRUD Management
//
// Simple CRUD form for managing menu categories.
// Connects to: CategoryDAL.cs (CRUD operations), MainForm.cs (embedded in content panel)

using CoffeeShopPOS.Database;
using CoffeeShopPOS.Models;
using CoffeeShopPOS.Helpers;

namespace CoffeeShopPOS.Forms
{
    /// <summary>
    /// CRUD form for managing menu categories (Hot Coffee, Pastries, etc.).
    /// </summary>
    public class CategoryManagementForm : Form
    {
        private DataGridView dgvCategories = null!;
        private TextBox txtName = null!, txtDescription = null!;
        private Button btnAdd = null!, btnUpdate = null!, btnDelete = null!, btnClear = null!;
        private int? selectedCategoryId;

        private readonly Color bgColor = Color.FromArgb(245, 242, 238);
        private readonly Color cardBg = Color.White;
        private readonly Color textDark = Color.FromArgb(50, 40, 35);
        private readonly Color accentGold = Color.FromArgb(193, 154, 107);
        private readonly Color successGreen = Color.FromArgb(76, 175, 80);
        private readonly Color dangerRed = Color.FromArgb(244, 67, 54);

        public CategoryManagementForm() { InitializeComponent(); }

        private void InitializeComponent()
        {
            this.Text = "Category Management";
            this.BackColor = bgColor;
            this.Dock = DockStyle.Fill;
            this.Padding = new Padding(20);

            var lblTitle = new Label
            { Text = "📁 Category Management", Font = new Font("Segoe UI", 20, FontStyle.Bold), ForeColor = textDark, Dock = DockStyle.Top, Height = 50 };
            this.Controls.Add(lblTitle);

            // Input panel
            var inputPanel = new Panel { Dock = DockStyle.Top, Height = 100, BackColor = cardBg, Padding = new Padding(15) };

            inputPanel.Controls.Add(new Label { Text = "Name:", Font = new Font("Segoe UI", 10), ForeColor = textDark, Location = new Point(10, 12), AutoSize = true });
            txtName = new TextBox { Font = new Font("Segoe UI", 10), Location = new Point(80, 10), Size = new Size(200, 25), BackColor = bgColor, BorderStyle = BorderStyle.FixedSingle };
            inputPanel.Controls.Add(txtName);

            inputPanel.Controls.Add(new Label { Text = "Description:", Font = new Font("Segoe UI", 10), ForeColor = textDark, Location = new Point(300, 12), AutoSize = true });
            txtDescription = new TextBox { Font = new Font("Segoe UI", 10), Location = new Point(395, 10), Size = new Size(300, 25), BackColor = bgColor, BorderStyle = BorderStyle.FixedSingle };
            inputPanel.Controls.Add(txtDescription);

            btnAdd = CreateBtn("➕ Add", successGreen, 10, 50); btnAdd.Click += BtnAdd_Click; inputPanel.Controls.Add(btnAdd);
            btnUpdate = CreateBtn("✏ Update", accentGold, 130, 50); btnUpdate.Click += BtnUpdate_Click; btnUpdate.Enabled = false; inputPanel.Controls.Add(btnUpdate);
            btnDelete = CreateBtn("🗑 Delete", dangerRed, 250, 50); btnDelete.Click += BtnDelete_Click; inputPanel.Controls.Add(btnDelete);
            btnClear = CreateBtn("✕ Clear", Color.Gray, 370, 50); btnClear.Click += (s, e) => ClearForm(); inputPanel.Controls.Add(btnClear);

            this.Controls.Add(inputPanel);

            // Grid
            dgvCategories = CreateGrid();
            dgvCategories.Dock = DockStyle.Fill;
            dgvCategories.CellClick += (s, e) =>
            {
                if (e.RowIndex < 0) return;
                var row = dgvCategories.Rows[e.RowIndex];
                selectedCategoryId = Convert.ToInt32(row.Cells["Id"].Value);
                txtName.Text = row.Cells["Name"].Value?.ToString();
                txtDescription.Text = row.Cells["Desc"].Value?.ToString();
                btnUpdate.Enabled = true;
            };
            this.Controls.Add(dgvCategories);

            this.Controls.SetChildIndex(lblTitle, 2);
            this.Controls.SetChildIndex(inputPanel, 1);
            this.Controls.SetChildIndex(dgvCategories, 0);

            this.Load += (s, e) => LoadData();
        }

        private Button CreateBtn(string text, Color color, int x, int y)
        {
            var btn = new Button { Text = text, Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.White, BackColor = color, FlatStyle = FlatStyle.Flat, Location = new Point(x, y), Size = new Size(110, 35), Cursor = Cursors.Hand };
            btn.FlatAppearance.BorderSize = 0; return btn;
        }

        private DataGridView CreateGrid()
        {
            var dgv = new DataGridView { ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, BackgroundColor = cardBg, BorderStyle = BorderStyle.None, RowHeadersVisible = false, EnableHeadersVisualStyles = false, Font = new Font("Segoe UI", 10), CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal, GridColor = Color.FromArgb(230, 225, 220) };
            dgv.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(45, 38, 32), ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            dgv.ColumnHeadersHeight = 40; dgv.RowTemplate.Height = 36;
            dgv.DefaultCellStyle = new DataGridViewCellStyle { SelectionBackColor = Color.FromArgb(240, 230, 218), SelectionForeColor = textDark };
            dgv.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(252, 250, 248) };
            return dgv;
        }

        // Loads categories from CategoryDAL.GetAll() into the grid
        private void LoadData()
        {
            try
            {
                dgvCategories.Columns.Clear();
                dgvCategories.Columns.Add("Id", "ID"); dgvCategories.Columns["Id"]!.Width = 50;
                dgvCategories.Columns.Add("Name", "Name");
                dgvCategories.Columns.Add("Desc", "Description");
                dgvCategories.Columns.Add("Active", "Active"); dgvCategories.Columns["Active"]!.Width = 70;

                dgvCategories.Rows.Clear();
                var categories = CategoryDAL.GetAll(includeInactive: true);
                foreach (var cat in categories)
                    dgvCategories.Rows.Add(cat.CategoryId, cat.Name, cat.Description ?? "", cat.IsActive ? "Yes" : "No");
            }
            catch (Exception ex) { ValidationHelper.ShowCriticalError($"Error: {ex.Message}"); }
        }

        private void BtnAdd_Click(object? sender, EventArgs e)
        {
            try
            {
                if (!ValidationHelper.RequiredField(txtName.Text, "Name")) return;
                CategoryDAL.Insert(new Category { Name = txtName.Text.Trim(), Description = txtDescription.Text.Trim() });
                ValidationHelper.ShowSuccess("Category added!"); ClearForm(); LoadData();
            }
            catch (Exception ex) { ValidationHelper.ShowCriticalError($"Error: {ex.Message}"); }
        }

        private void BtnUpdate_Click(object? sender, EventArgs e)
        {
            try
            {
                if (!selectedCategoryId.HasValue) return;
                if (!ValidationHelper.RequiredField(txtName.Text, "Name")) return;
                CategoryDAL.Update(new Category { CategoryId = selectedCategoryId.Value, Name = txtName.Text.Trim(), Description = txtDescription.Text.Trim(), IsActive = true });
                ValidationHelper.ShowSuccess("Category updated!"); ClearForm(); LoadData();
            }
            catch (Exception ex) { ValidationHelper.ShowCriticalError($"Error: {ex.Message}"); }
        }

        private void BtnDelete_Click(object? sender, EventArgs e)
        {
            try
            {
                if (!selectedCategoryId.HasValue) { ValidationHelper.ShowError("Select a category."); return; }
                if (!ValidationHelper.Confirm("Deactivate this category?")) return;
                CategoryDAL.Delete(selectedCategoryId.Value);
                ValidationHelper.ShowSuccess("Category deactivated."); ClearForm(); LoadData();
            }
            catch (Exception ex) { ValidationHelper.ShowCriticalError($"Error: {ex.Message}"); }
        }

        private void ClearForm() { selectedCategoryId = null; txtName.Clear(); txtDescription.Clear(); btnUpdate.Enabled = false; }
    }
}
