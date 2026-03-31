// UserDAL.cs — Data Access Layer for Users/Staff
//
// Handles database operations for the 'users' table.
// Uses DbHelper.cs for connections and parameterised queries.
// Called by: AuthService.cs (authentication), UserManagementForm.cs (admin CRUD),
//            LoginForm.cs (indirectly through AuthService)

using CoffeeShopPOS.Models;

namespace CoffeeShopPOS.Database
{
    /// <summary>
    /// CRUD and authentication support for staff users.
    /// Password hashing is handled by AuthService.cs — this DAL stores/retrieves hashes only.
    /// </summary>
    public static class UserDAL
    {
        /// <summary>
        /// Retrieves a user by username for authentication.
        /// Called by: AuthService.Authenticate() during login flow.
        /// Returns null if username not found — AuthService handles the error message.
        /// </summary>
        public static User? GetByUsername(string username)
        {
            User? user = null;
            string query = "SELECT * FROM users WHERE username = @username";
            var parameters = new Dictionary<string, object?> { { "@username", username } };

            DbHelper.ExecuteReader(query, parameters, reader =>
            {
                if (reader.Read())
                {
                    user = MapUser(reader);
                }
            });

            return user;
        }

        /// <summary>
        /// Retrieves all users (optionally including deactivated ones).
        /// Called by: UserManagementForm.cs to populate the users DataGridView.
        /// </summary>
        public static List<User> GetAll(bool includeInactive = false)
        {
            var users = new List<User>();
            string query = includeInactive
                ? "SELECT * FROM users ORDER BY full_name"
                : "SELECT * FROM users WHERE is_active = 1 ORDER BY full_name";

            DbHelper.ExecuteReader(query, null, reader =>
            {
                while (reader.Read()) users.Add(MapUser(reader));
            });

            return users;
        }

        /// <summary>
        /// Retrieves a user by ID.
        /// Called by: OrderHistoryForm.cs to display staff name for orders.
        /// </summary>
        public static User? GetById(int userId)
        {
            User? user = null;
            string query = "SELECT * FROM users WHERE user_id = @id";
            var parameters = new Dictionary<string, object?> { { "@id", userId } };

            DbHelper.ExecuteReader(query, parameters, reader =>
            {
                if (reader.Read()) user = MapUser(reader);
            });

            return user;
        }

        /// <summary>
        /// Inserts a new user with a pre-hashed password.
        /// Called by: UserManagementForm.cs "Add User" button.
        /// The password must be hashed by AuthService.HashPassword() BEFORE calling this.
        /// </summary>
        public static int Insert(User user)
        {
            string query = @"INSERT INTO users (username, password_hash, full_name, role, is_active) 
                            VALUES (@username, @hash, @name, @role, @active);
                            SELECT LAST_INSERT_ID();";

            var parameters = new Dictionary<string, object?>
            {
                { "@username", user.Username },
                { "@hash", user.PasswordHash },
                { "@name", user.FullName },
                { "@role", user.Role },
                { "@active", user.IsActive }
            };

            var result = DbHelper.ExecuteScalar(query, parameters);
            return Convert.ToInt32(result);
        }

        /// <summary>
        /// Updates user details (not including password).
        /// Called by: UserManagementForm.cs "Update" button.
        /// </summary>
        public static bool Update(User user)
        {
            string query = @"UPDATE users 
                            SET username = @username, full_name = @name, role = @role, is_active = @active 
                            WHERE user_id = @id";

            var parameters = new Dictionary<string, object?>
            {
                { "@id", user.UserId },
                { "@username", user.Username },
                { "@name", user.FullName },
                { "@role", user.Role },
                { "@active", user.IsActive }
            };

            return DbHelper.ExecuteNonQuery(query, parameters) > 0;
        }

        /// <summary>
        /// Updates a user's password hash.
        /// Called by: UserManagementForm.cs "Reset Password" button.
        /// The new password must be hashed by AuthService.HashPassword() first.
        /// </summary>
        public static bool UpdatePassword(int userId, string newPasswordHash)
        {
            string query = "UPDATE users SET password_hash = @hash WHERE user_id = @id";
            var parameters = new Dictionary<string, object?>
            {
                { "@id", userId },
                { "@hash", newPasswordHash }
            };

            return DbHelper.ExecuteNonQuery(query, parameters) > 0;
        }

        /// <summary>
        /// Soft-deletes (deactivates) a user.
        /// Called by: UserManagementForm.cs "Deactivate" button.
        /// Deactivated users cannot log in — checked by AuthService.Authenticate().
        /// </summary>
        public static bool Deactivate(int userId)
        {
            string query = "UPDATE users SET is_active = 0 WHERE user_id = @id";
            var parameters = new Dictionary<string, object?> { { "@id", userId } };

            return DbHelper.ExecuteNonQuery(query, parameters) > 0;
        }

        /// <summary>
        /// Checks if a username already exists in the database.
        /// Called by: UserManagementForm.cs to validate uniqueness before insert.
        /// </summary>
        public static bool UsernameExists(string username, int? excludeUserId = null)
        {
            string query = excludeUserId.HasValue
                ? "SELECT COUNT(*) FROM users WHERE username = @username AND user_id != @id"
                : "SELECT COUNT(*) FROM users WHERE username = @username";

            var parameters = new Dictionary<string, object?> { { "@username", username } };
            if (excludeUserId.HasValue)
                parameters.Add("@id", excludeUserId.Value);

            var count = Convert.ToInt32(DbHelper.ExecuteScalar(query, parameters));
            return count > 0;
        }

        // ══════════════════════════════════════════════════════════════════════
        // PRIVATE HELPER — Maps a reader row to a User model
        // ══════════════════════════════════════════════════════════════════════
        private static User MapUser(MySql.Data.MySqlClient.MySqlDataReader reader)
        {
            return new User
            {
                UserId = reader.GetInt32("user_id"),
                Username = reader.GetString("username"),
                PasswordHash = reader.GetString("password_hash"),
                FullName = reader.GetString("full_name"),
                Role = reader.GetString("role"),
                IsActive = reader.GetBoolean("is_active"),
                CreatedAt = reader.GetDateTime("created_at")
            };
        }
    }
}
