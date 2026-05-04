// AuthService.cs — Authentication & Authorization Service
//
// Handles user login, password hashing/verification, and role-based access control.
// Uses BCrypt.Net-Next NuGet package for secure password hashing.
// Connects to: UserDAL.cs (database queries), LoginForm.cs (login UI),
//              MainForm.cs (role-based menu visibility), Program.cs (entry point)

using BCrypt.Net;
using CoffeeShopPOS.Database;
using CoffeeShopPOS.Models;

namespace CoffeeShopPOS.Services
{
    /// <summary>
    /// Provides authentication and authorization services.
    /// Manages the currently logged-in user and role-based access checks.
    /// </summary>
    public static class AuthService
    {
        // ── Static property holding the currently logged-in user ──
        // Set by Authenticate() on successful login, checked throughout the app
        // MainForm.cs reads this to determine which sidebar buttons to show
        // OrderForm.cs reads this to set the user_id on new orders
        public static User? CurrentUser { get; private set; }

        /// <summary>
        /// Authenticates a user by username and password.
        /// Called by: LoginForm.cs when the "Login" button is clicked.
        /// Returns the User object on success, null on failure.
        /// </summary>
        /// <param name="username">The username entered in LoginForm</param>
        /// <param name="password">The plain-text password entered in LoginForm</param>
        /// <returns>Authenticated User or null if credentials are invalid</returns>
        public static User? Authenticate(string username, string password)
        {
            try
            {
                // Look up the user in the database via UserDAL.cs
                var user = UserDAL.GetByUsername(username);

                // Check if user exists
                if (user == null)
                    return null;

                // Check if account is active (soft-deleted accounts cannot login)
                if (!user.IsActive)
                    return null;

                // Verify the password against the stored BCrypt hash
                // BCrypt.Verify automatically handles salt extraction from the hash
                if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                    return null;

                // Authentication successful — store the user as the current session user
                // This is referenced by OrderForm.cs, MainForm.cs, and other forms
                CurrentUser = user;
                return user;
            }
            catch (Exception ex)
            {
                // Handle BCrypt verification errors (e.g., invalid hash format in seed data)
                // This can happen when seed_data.sql has placeholder hashes
                throw new Exception($"Authentication error: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Hashes a plain-text password using BCrypt.
        /// Called by: UserManagementForm.cs when creating a new user or resetting a password.
        /// The hash includes a random salt, making each hash unique even for the same password.
        /// </summary>
        /// <param name="password">Plain-text password to hash</param>
        /// <returns>BCrypt hash string safe for database storage</returns>
        public static string HashPassword(string password)
        {
            // Work factor of 11 — good balance between security and performance
            // Higher values make brute-force attacks slower but also slow down login
            return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 11);
        }

        /// <summary>
        /// Checks if the currently logged-in user has Admin privileges.
        /// Called by: MainForm.cs to show/hide the "User Management" button.
        /// </summary>
        public static bool IsAdmin()
        {
            return CurrentUser?.Role == "Admin";
        }

        /// <summary>
        /// Checks if the current user has Manager or Admin privileges.
        /// Called by: MainForm.cs to show/hide "Menu Management", "Inventory", "Reports".
        /// </summary>
        public static bool IsManagerOrAbove()
        {
            return CurrentUser?.Role == "Admin" || CurrentUser?.Role == "Manager";
        }

        /// <summary>
        /// Checks if a specific role has access to a feature.
        /// Called by: MainForm.cs for granular feature access control.
        /// Maps to the role-based access matrix in the implementation plan (Section 5.2).
        /// </summary>
        /// <param name="feature">Feature name to check access for</param>
        /// <returns>True if current user's role can access the feature</returns>
        public static bool HasAccess(string feature)
        {
            if (CurrentUser == null) return false;

            return feature switch
            {
                // User Management — Admin only
                "UserManagement" => CurrentUser.Role == "Admin",

                // Menu & Category Management — Admin and Manager
                "MenuManagement" => IsManagerOrAbove(),
                "CategoryManagement" => IsManagerOrAbove(),

                // Inventory — Admin and Manager
                "Inventory" => IsManagerOrAbove(),

                // Reports — Admin and Manager
                "Reports" => IsManagerOrAbove(),

                // Orders, Billing, Tables — All roles
                "Orders" => true,
                "Tables" => true,
                "Billing" => true,

                // Dashboard — Admin and Manager
                "Dashboard" => IsManagerOrAbove(),

                // Default: deny access for unknown features
                _ => false
            };
        }

        /// <summary>
        /// Logs out the current user.
        /// Called by: MainForm.cs "Logout" button.
        /// Clears the CurrentUser so all role checks return false.
        /// </summary>
        public static void Logout()
        {
            CurrentUser = null;
        }

        /// <summary>
        /// Creates the initial admin user if no users exist.
        /// Called at application startup to ensure there's always at least one admin.
        /// </summary>
        public static void EnsureAdminExists()
        {
            try
            {
                var users = UserDAL.GetAll(includeInactive: true);
                if (users.Count == 0)
                {
                    // Create a default admin account
                    var admin = new User
                    {
                        Username = "admin",
                        PasswordHash = HashPassword("admin123"),
                        FullName = "System Administrator",
                        Role = "Admin",
                        IsActive = true
                    };
                    UserDAL.Insert(admin);
                }
            }
            catch
            {
                // Silently fail — will be handled by LoginForm showing connection error
            }
        }
    }
}
