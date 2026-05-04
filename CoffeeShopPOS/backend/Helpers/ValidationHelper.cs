// ValidationHelper.cs — Input Validation Utility
//
// Provides reusable validation methods for form inputs across all UI forms.
// Called by: All Forms that accept user input (UserManagementForm, MenuManagementForm,
//            OrderForm, InventoryForm, etc.)

namespace CoffeeShopPOS.Helpers
{
    /// <summary>
    /// Centralized input validation for the entire application.
    /// Shows user-friendly error messages when validation fails.
    /// </summary>
    public static class ValidationHelper
    {
        /// <summary>
        /// Validates that a text field is not empty or whitespace.
        /// Called by: All forms when validating required fields before save.
        /// </summary>
        /// <param name="value">The input value to validate</param>
        /// <param name="fieldName">Name of the field for the error message</param>
        /// <returns>True if valid, false if empty</returns>
        public static bool RequiredField(string? value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                ShowError($"{fieldName} is required and cannot be empty.");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Validates that a numeric value is positive.
        /// Called by: MenuManagementForm.cs (price), InventoryForm.cs (quantity),
        ///            TableManagementForm.cs (capacity)
        /// </summary>
        public static bool PositiveNumber(decimal value, string fieldName)
        {
            if (value <= 0)
            {
                ShowError($"{fieldName} must be greater than zero.");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Validates that a numeric value is zero or positive.
        /// Called by: BillingForm.cs (discount), InventoryForm.cs (reorder level)
        /// </summary>
        public static bool NonNegativeNumber(decimal value, string fieldName)
        {
            if (value < 0)
            {
                ShowError($"{fieldName} cannot be negative.");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Validates that a string can be parsed as a decimal.
        /// Called by: All forms with numeric text boxes.
        /// </summary>
        /// <param name="text">The text to validate</param>
        /// <param name="fieldName">Name of the field for error message</param>
        /// <param name="result">The parsed decimal value if valid</param>
        /// <returns>True if valid decimal, false otherwise</returns>
        public static bool IsValidDecimal(string text, string fieldName, out decimal result)
        {
            if (!decimal.TryParse(text, out result))
            {
                ShowError($"{fieldName} must be a valid number.");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Validates that a string can be parsed as an integer.
        /// Called by: TableManagementForm.cs (capacity), OrderForm.cs (quantity)
        /// </summary>
        public static bool IsValidInt(string text, string fieldName, out int result)
        {
            if (!int.TryParse(text, out result))
            {
                ShowError($"{fieldName} must be a valid whole number.");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Validates username format (alphanumeric, 3-50 chars).
        /// Called by: UserManagementForm.cs when creating/editing users.
        /// </summary>
        public static bool ValidUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                ShowError("Username is required.");
                return false;
            }

            if (username.Length < 3 || username.Length > 50)
            {
                ShowError("Username must be between 3 and 50 characters.");
                return false;
            }

            if (!username.All(c => char.IsLetterOrDigit(c) || c == '_'))
            {
                ShowError("Username can only contain letters, numbers, and underscores.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validates password strength (minimum 6 chars).
        /// Called by: UserManagementForm.cs when creating/resetting passwords.
        /// </summary>
        public static bool ValidPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                ShowError("Password is required.");
                return false;
            }

            if (password.Length < 6)
            {
                ShowError("Password must be at least 6 characters long.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Shows a confirmation dialog and returns the user's choice.
        /// Called by: All forms before delete operations.
        /// </summary>
        public static bool Confirm(string message, string title = "Confirm")
        {
            return MessageBox.Show(message, title,
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
        }

        /// <summary>
        /// Shows a success notification.
        /// Called by: All forms after successful CRUD operations.
        /// </summary>
        public static void ShowSuccess(string message)
        {
            MessageBox.Show(message, "Success",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Shows an error message dialog.
        /// Called by: All validation methods above and form error handlers.
        /// </summary>
        public static void ShowError(string message)
        {
            MessageBox.Show(message, "Validation Error",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        /// <summary>
        /// Shows a critical error message dialog.
        /// Called by: Forms when database operations fail.
        /// </summary>
        public static void ShowCriticalError(string message)
        {
            MessageBox.Show(message, "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
