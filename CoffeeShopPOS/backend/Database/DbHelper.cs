// DbHelper.cs — Central Database Connection Helper
//
// This is the core data access utility used by ALL DAL classes in the Database/ folder.
// It reads the PostgreSQL connection string from appsettings and provides reusable methods
// for executing queries safely with parameterised SQL (preventing SQL injection).
//
// Connected to by: CategoryDAL.cs, MenuItemDAL.cs, OrderDAL.cs, TableDAL.cs,
//                  UserDAL.cs, InventoryDAL.cs, ReportDAL.cs

using Npgsql;

namespace CoffeeShopPOS.Database
{
    /// <summary>
    /// Shared database helper providing connection management and query execution.
    /// All DAL classes use this instead of managing connections directly.
    /// </summary>
    public static class DbHelper
    {
        // ── Connection string loaded once when the app starts ──
        // Default connection string — can be overridden by App.config or appsettings
        // Points to the coffee_shop_pos database created by schema.sql
        private static string _connectionString =
            "Host=localhost;Port=5432;Database=coffee_shop_pos;Username=postgres;Password=admin123;";

        /// <summary>
        /// Gets or sets the PostgreSQL connection string.
        /// Called at application startup to configure database access.
        /// Can be updated from a config file if needed.
        /// </summary>
        public static string ConnectionString
        {
            get => _connectionString;
            set => _connectionString = value;
        }

        /// <summary>
        /// Creates and returns a new PostgreSQL connection.
        /// IMPORTANT: The caller is responsible for disposing the connection (use 'using' blocks).
        /// All DAL classes call this method to open database connections.
        /// </summary>
        /// <returns>An open NpgsqlConnection to the coffee_shop_pos database.</returns>
        public static NpgsqlConnection GetConnection()
        {
            // Create a new connection using the configured connection string
            var connection = new NpgsqlConnection(_connectionString);
            try
            {
                connection.Open();  // Attempt to connect to PostgreSQL
                return connection;
            }
            catch (NpgsqlException ex)
            {
                // Provide a clear error message if PostgreSQL is not running or credentials are wrong
                throw new Exception(
                    $"Failed to connect to the database. Please check:\n" +
                    $"1. PostgreSQL service is running\n" +
                    $"2. Connection string is correct\n" +
                    $"3. Database 'coffee_shop_pos' exists (run schema.sql first)\n\n" +
                    $"Error: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Executes a non-query SQL command (INSERT, UPDATE, DELETE).
        /// Returns the number of rows affected.
        /// Used by DAL methods that modify data (e.g., CategoryDAL.Insert(), UserDAL.Update()).
        /// </summary>
        /// <param name="query">SQL query with @parameter placeholders</param>
        /// <param name="parameters">Dictionary of parameter name-value pairs</param>
        /// <returns>Number of rows affected by the command</returns>
        public static int ExecuteNonQuery(string query, Dictionary<string, object?>? parameters = null)
        {
            // 'using' ensures the connection is properly closed even if an exception occurs
            using var connection = GetConnection();
            using var command = new NpgsqlCommand(query, connection);

            // Add parameters safely — this prevents SQL injection attacks
            AddParameters(command, parameters);

            return command.ExecuteNonQuery();
        }

        /// <summary>
        /// Executes a query that returns a single scalar value (e.g., COUNT, MAX, RETURNING id).
        /// Used by DAL methods like OrderDAL.Create() to get the newly inserted order ID.
        /// </summary>
        /// <param name="query">SQL query expected to return a single value</param>
        /// <param name="parameters">Dictionary of parameter name-value pairs</param>
        /// <returns>The first column of the first row, or null if no results</returns>
        public static object? ExecuteScalar(string query, Dictionary<string, object?>? parameters = null)
        {
            using var connection = GetConnection();
            using var command = new NpgsqlCommand(query, connection);

            AddParameters(command, parameters);

            return command.ExecuteScalar();
        }

        /// <summary>
        /// Executes a query and returns an NpgsqlDataReader for reading multiple rows.
        /// IMPORTANT: The caller must dispose both the reader AND the connection.
        /// Use the overload with the action parameter for automatic cleanup.
        /// </summary>
        /// <param name="query">SQL SELECT query</param>
        /// <param name="parameters">Dictionary of parameter name-value pairs</param>
        /// <param name="readerAction">Action to perform with the reader — connection auto-closes after</param>
        public static void ExecuteReader(string query, Dictionary<string, object?>? parameters,
            Action<NpgsqlDataReader> readerAction)
        {
            using var connection = GetConnection();
            using var command = new NpgsqlCommand(query, connection);

            AddParameters(command, parameters);

            // Execute and pass the reader to the callback
            // The 'using' ensures the reader is closed after the action completes
            using var reader = command.ExecuteReader();
            readerAction(reader);
        }

        /// <summary>
        /// Executes a query and returns results as a List of dictionaries.
        /// Each dictionary represents one row with column name → value mappings.
        /// Convenient for report queries in ReportDAL.cs where the schema varies.
        /// </summary>
        /// <param name="query">SQL SELECT query</param>
        /// <param name="parameters">Dictionary of parameter name-value pairs</param>
        /// <returns>List of row dictionaries</returns>
        public static List<Dictionary<string, object?>> ExecuteQuery(string query,
            Dictionary<string, object?>? parameters = null)
        {
            var results = new List<Dictionary<string, object?>>();

            using var connection = GetConnection();
            using var command = new NpgsqlCommand(query, connection);

            AddParameters(command, parameters);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var row = new Dictionary<string, object?>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    // Store each column value, converting DBNull to C# null
                    row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                }
                results.Add(row);
            }

            return results;
        }

        /// <summary>
        /// Tests the database connection. Returns true if successful, false otherwise.
        /// Called by LoginForm.cs at startup to verify database availability.
        /// </summary>
        public static bool TestConnection()
        {
            try
            {
                using var connection = GetConnection();
                return true;  // If GetConnection() succeeds, the database is reachable
            }
            catch
            {
                return false;
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // NPGSQL READER HELPERS — Accessor methods for clean column access
        // ══════════════════════════════════════════════════════════════════════
        
        /// <summary>
        /// Gets a nullable string from a data reader by column name.
        /// </summary>
        public static string? GetNullableString(NpgsqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
        }

        /// <summary>
        /// Gets a nullable int from a data reader by column name.
        /// </summary>
        public static int? GetNullableInt32(NpgsqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
        }

        /// <summary>
        /// Gets a nullable decimal from a data reader by column name.
        /// </summary>
        public static decimal? GetNullableDecimal(NpgsqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetDecimal(ordinal);
        }

        /// <summary>
        /// Gets a nullable datetime from a data reader by column name.
        /// </summary>
        public static DateTime? GetNullableDateTime(NpgsqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
        }

        /// <summary>
        /// Gets a non-null string from a data reader by column name.
        /// </summary>
        public static string GetString(NpgsqlDataReader reader, string columnName)
        {
            return reader.GetString(reader.GetOrdinal(columnName));
        }

        /// <summary>
        /// Gets a non-null int from a data reader by column name.
        /// </summary>
        public static int GetInt32(NpgsqlDataReader reader, string columnName)
        {
            return reader.GetInt32(reader.GetOrdinal(columnName));
        }

        /// <summary>
        /// Gets a non-null decimal from a data reader by column name.
        /// </summary>
        public static decimal GetDecimal(NpgsqlDataReader reader, string columnName)
        {
            return reader.GetDecimal(reader.GetOrdinal(columnName));
        }

        /// <summary>
        /// Gets a non-null datetime from a data reader by column name.
        /// </summary>
        public static DateTime GetDateTime(NpgsqlDataReader reader, string columnName)
        {
            return reader.GetDateTime(reader.GetOrdinal(columnName));
        }

        /// <summary>
        /// Gets a non-null boolean from a data reader by column name.
        /// </summary>
        public static bool GetBoolean(NpgsqlDataReader reader, string columnName)
        {
            return reader.GetBoolean(reader.GetOrdinal(columnName));
        }

        /// <summary>
        /// Checks if a column value is DBNull by column name.
        /// </summary>
        public static bool IsDBNull(NpgsqlDataReader reader, string columnName)
        {
            return reader.IsDBNull(reader.GetOrdinal(columnName));
        }

        // ══════════════════════════════════════════════════════════════════════
        // PRIVATE HELPER — Adds parameters to a command safely
        // This method is called by all public Execute* methods above.
        // Parameterised queries prevent SQL injection by treating user input as
        // data rather than executable SQL code.
        // ══════════════════════════════════════════════════════════════════════
        private static void AddParameters(NpgsqlCommand command, Dictionary<string, object?>? parameters)
        {
            if (parameters == null) return;

            foreach (var param in parameters)
            {
                // Convert C# null to DBNull.Value for proper PostgreSQL handling
                command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
            }
        }
    }
}
