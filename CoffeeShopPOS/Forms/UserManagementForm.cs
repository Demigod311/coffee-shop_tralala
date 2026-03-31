// UserManagementForm.cs — Staff/User CRUD Management
//
// Admin-only form for creating and managing staff accounts.
// Connects to: UserDAL.cs (CRUD), AuthService.cs (password hashing, role checks),
//              MainForm.cs (embedded in content panel, visible to Admin role only)

using CoffeeShopPOS.Database;
using CoffeeShopPOS.Models;
using CoffeeShopPOS.Services;
using CoffeeShopPOS.Helpers;

namespace CoffeeShopPOS.Forms
{
    /// <summary>
    /// Admin CRUD form for managing staff users with role assignment.
    /// Only visible to Admin role — enforced by MainForm.cs sidebar via AuthService.HasAccess().
    /// </summary>
    public class UserManagementForm : Form
    {
        private DataGridView dgvUsers = null!;
        private TextBox txtUsername = null!, txtFullName = null!, txtPassword = null!;
        private ComboBox cmbRole = null!;
        private Button btnAdd = null!, btnUpdate = null!, btnResetPwd = null!, btnDeactivate = null!, btnClear = null!;
        private int? selectedUserId;

        private readonly Color bgColor = Color.FromArgb(245, 242, 238);
        private readonly Color cardBg = Color.White;
        private readonly Color textDark = Color.FromArgb(50, 40, 35);
        private readonly Color accentGold = Color.FromArgb(193, 154, 107);
        private readonly Color successGreen = Color.FromArgb(76, 175, 80);
        private readonly Color dangerRed = Color.FromArgb(244, 67, 54);

        public UserManagementForm() { InitializeComponent(); }

        private void InitializeComponent()
        {
            this.Text = "Staff Management"; this.BackColor = bgColor; this.Dock = DockStyle.Fill; this.Padding = new Padding(20);

            var lblTitle = new Label { Text = "👥 Staff Management", Font = new Font("Segoe UI", 20, FontStyle.Bold), ForeColor = textDark, Dock = DockStyle.Top, Height = 50 };
            this.Controls.Add(lblTitle);

            // Input panel
            var ip = new Panel { Dock = DockStyle.Top, Height = 110, BackColor = cardBg, Padding = new Padding(15) };

            ip.Controls.Add(Lbl("Username:", 10, 12)); txtUsername = Tb(90, 10, 150); ip.Controls.Add(txtUsername);
            ip.Controls.Add(Lbl("Full Name:", 260, 12)); txtFullName = Tb(345, 10, 200); ip.Controls.Add(txtFullName);
            ip.Controls.Add(Lbl("Role:", 565, 12));
            cmbRole = new ComboBox { Font = new Font("Segoe UI", 10), Location = new Point(610, 10), Size = new Size(130, 25), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = bgColor };
            cmbRole.Items.AddRange(new[] { "Admin", "Manager", "Cashier", "Waiter" }); cmbRole.SelectedIndex = 2;
            ip.Controls.Add(cmbRole);

            ip.Controls.Add(Lbl("Password:", 10, 50)); txtPassword = Tb(90, 48, 150); txtPassword.UseSystemPasswordChar = true; ip.Controls.Add(txtPassword);

            btnAdd = Btn("➕ Add", successGreen, 260, 45); btnAdd.Click += BtnAdd_Click; ip.Controls.Add(btnAdd);
            btnUpdate = Btn("✏ Update", accentGold, 380, 45); btnUpdate.Click += BtnUpdate_Click; btnUpdate.Enabled = false; ip.Controls.Add(btnUpdate);
            btnResetPwd = Btn("🔑 Reset Pwd", Color.FromArgb(33, 150, 243), 500, 45); btnResetPwd.Click += BtnResetPwd_Click; ip.Controls.Add(btnResetPwd);
            btnDeactivate = Btn("⛔ Deactivate", dangerRed, 650, 45); btnDeactivate.Click += BtnDeactivate_Click; ip.Controls.Add(btnDeactivate);
            btnClear = Btn("✕", Color.Gray, 800, 45); btnClear.Size = new Size(40, 35); btnClear.Click += (s, e) => ClearForm(); ip.Controls.Add(btnClear);

            this.Controls.Add(ip);

            // Grid
            dgvUsers = MakeGrid(); dgvUsers.Dock = DockStyle.Fill;
            dgvUsers.CellClick += (s, e) =>
            {
                if (e.RowIndex < 0) return;
                var row = dgvUsers.Rows[e.RowIndex];
                selectedUserId = Convert.ToInt32(row.Cells["Id"].Value);
                txtUsername.Text = row.Cells["Username"].Value?.ToString();
                txtFullName.Text = row.Cells["Name"].Value?.ToString();
                cmbRole.SelectedItem = row.Cells["Role"].Value?.ToString();
                txtPassword.Clear();
                btnUpdate.Enabled = true;
            };
            this.Controls.Add(dgvUsers);

            this.Controls.SetChildIndex(lblTitle, 2); this.Controls.SetChildIndex(ip, 1); this.Controls.SetChildIndex(dgvUsers, 0);
            this.Load += (s, e) => LoadData();
        }

        private Label Lbl(string t, int x, int y) => new Label { Text = t, Font = new Font("Segoe UI", 10), ForeColor = textDark, Location = new Point(x, y), AutoSize = true };
        private TextBox Tb(int x, int y, int w) => new TextBox { Font = new Font("Segoe UI", 10), Location = new Point(x, y), Size = new Size(w, 25), BackColor = bgColor, BorderStyle = BorderStyle.FixedSingle };
        private Button Btn(string t, Color c, int x, int y)
        {
            var b = new Button { Text = t, Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.White, BackColor = c, FlatStyle = FlatStyle.Flat, Location = new Point(x, y), Size = new Size(110, 35), Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0; return b;
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

        // Loads users from UserDAL.GetAll() into the grid
        private void LoadData()
        {
            try
            {
                dgvUsers.Columns.Clear();
                dgvUsers.Columns.Add("Id", "ID"); dgvUsers.Columns["Id"]!.Width = 50;
                dgvUsers.Columns.Add("Username", "Username");
                dgvUsers.Columns.Add("Name", "Full Name");
                dgvUsers.Columns.Add("Role", "Role"); dgvUsers.Columns["Role"]!.Width = 90;
                dgvUsers.Columns.Add("Active", "Active"); dgvUsers.Columns["Active"]!.Width = 70;
                dgvUsers.Columns.Add("Created", "Created"); dgvUsers.Columns["Created"]!.Width = 130;

                dgvUsers.Rows.Clear();
                var users = UserDAL.GetAll(includeInactive: true);
                foreach (var u in users)
                    dgvUsers.Rows.Add(u.UserId, u.Username, u.FullName, u.Role, u.IsActive ? "Yes" : "No", u.CreatedAt.ToString("g"));
            }
            catch (Exception ex) { ValidationHelper.ShowCriticalError($"Error: {ex.Message}"); }
        }

        // Add new user with hashed password via AuthService.HashPassword() → UserDAL.Insert()
        private void BtnAdd_Click(object? sender, EventArgs e)
        {
            try
            {
                if (!ValidationHelper.ValidUsername(txtUsername.Text)) return;
                if (!ValidationHelper.RequiredField(txtFullName.Text, "Full Name")) return;
                if (!ValidationHelper.ValidPassword(txtPassword.Text)) return;

                // Check for duplicate username via UserDAL.UsernameExists()
                if (UserDAL.UsernameExists(txtUsername.Text.Trim()))
                { ValidationHelper.ShowError("Username already exists."); return; }

                var user = new User
                {
                    Username = txtUsername.Text.Trim(),
                    // Hash password via AuthService.cs before storing
                    PasswordHash = AuthService.HashPassword(txtPassword.Text),
                    FullName = txtFullName.Text.Trim(),
                    Role = cmbRole.SelectedItem?.ToString() ?? "Cashier"
                };

                UserDAL.Insert(user);
                ValidationHelper.ShowSuccess("User created!"); ClearForm(); LoadData();
            }
            catch (Exception ex) { ValidationHelper.ShowCriticalError($"Error: {ex.Message}"); }
        }

        private void BtnUpdate_Click(object? sender, EventArgs e)
        {
            try
            {
                if (!selectedUserId.HasValue) return;
                if (!ValidationHelper.ValidUsername(txtUsername.Text)) return;
                if (!ValidationHelper.RequiredField(txtFullName.Text, "Full Name")) return;

                if (UserDAL.UsernameExists(txtUsername.Text.Trim(), selectedUserId))
                { ValidationHelper.ShowError("Username already taken."); return; }

                UserDAL.Update(new User { UserId = selectedUserId.Value, Username = txtUsername.Text.Trim(), FullName = txtFullName.Text.Trim(), Role = cmbRole.SelectedItem?.ToString() ?? "Cashier", IsActive = true });
                ValidationHelper.ShowSuccess("User updated!"); ClearForm(); LoadData();
            }
            catch (Exception ex) { ValidationHelper.ShowCriticalError($"Error: {ex.Message}"); }
        }

        // Reset password via AuthService.HashPassword() → UserDAL.UpdatePassword()
        private void BtnResetPwd_Click(object? sender, EventArgs e)
        {
            try
            {
                if (!selectedUserId.HasValue) { ValidationHelper.ShowError("Select a user."); return; }
                if (!ValidationHelper.ValidPassword(txtPassword.Text)) return;
                if (!ValidationHelper.Confirm("Reset this user's password?")) return;

                UserDAL.UpdatePassword(selectedUserId.Value, AuthService.HashPassword(txtPassword.Text));
                ValidationHelper.ShowSuccess("Password reset!"); txtPassword.Clear();
            }
            catch (Exception ex) { ValidationHelper.ShowCriticalError($"Error: {ex.Message}"); }
        }

        // Deactivate user via UserDAL.Deactivate() (soft delete)
        private void BtnDeactivate_Click(object? sender, EventArgs e)
        {
            try
            {
                if (!selectedUserId.HasValue) { ValidationHelper.ShowError("Select a user."); return; }
                if (!ValidationHelper.Confirm("Deactivate this user? They will not be able to login.")) return;
                UserDAL.Deactivate(selectedUserId.Value);
                ValidationHelper.ShowSuccess("User deactivated."); ClearForm(); LoadData();
            }
            catch (Exception ex) { ValidationHelper.ShowCriticalError($"Error: {ex.Message}"); }
        }

        private void ClearForm() { selectedUserId = null; txtUsername.Clear(); txtFullName.Clear(); txtPassword.Clear(); cmbRole.SelectedIndex = 2; btnUpdate.Enabled = false; }
    }
}
