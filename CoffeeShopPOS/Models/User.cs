// User.cs — Staff/User Model
//
// Represents a staff member with authentication and role information.
// Maps directly to the 'users' table in MySQL.
// Used by: UserDAL.cs (data access), AuthService.cs (authentication),
//          UserManagementForm.cs (admin CRUD), LoginForm.cs (login flow)

namespace CoffeeShopPOS.Models
{
    /// <summary>
    /// Represents a staff user with role-based access control.
    /// Populated by UserDAL.cs from the users database table.
    /// </summary>
    public class User
    {
        // Primary key — auto-incremented in MySQL
        public int UserId { get; set; }

        // Unique login username — used in LoginForm.cs
        public string Username { get; set; } = string.Empty;

        // BCrypt password hash — NEVER store or display plain text passwords
        // Generated and verified by AuthService.cs using BCrypt.Net
        public string PasswordHash { get; set; } = string.Empty;

        // Display name — shown in MainForm.cs header, receipts, and reports
        public string FullName { get; set; } = string.Empty;

        // Role determines access level throughout the application:
        //   "Admin"   — Full access (user management, reports, everything)
        //   "Manager" — Menu, inventory, reports, orders
        //   "Cashier" — Orders, billing, tables only
        //   "Waiter"  — Orders and tables only
        // Checked by MainForm.cs to show/hide sidebar buttons
        public string Role { get; set; } = "Cashier";

        // Soft delete — deactivated users cannot log in
        // Checked by AuthService.Authenticate() during login
        public bool IsActive { get; set; } = true;

        // Account creation timestamp
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Override ToString for display in DataGridView and ComboBox controls.
        /// </summary>
        public override string ToString() => $"{FullName} ({Role})";
    }
}
