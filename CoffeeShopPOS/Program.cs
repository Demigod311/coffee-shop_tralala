// Program.cs — Application Entry Point
//
// This is the starting point of the Coffee Shop POS application.
// It initializes Windows Forms, sets up visual styles for a modern look,
// and launches the LoginForm as the first screen the user sees.
// Upon successful login, the MainForm (MDI parent) is opened.

namespace CoffeeShopPOS
{
    /// <summary>
    /// Main entry point for the Coffee Shop POS application.
    /// Configures application-wide settings and launches the login screen.
    /// </summary>
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Enable modern visual styles (rounded controls, themed buttons)
            ApplicationConfiguration.Initialize();

            // Set up a global unhandled exception handler to prevent crashes
            // and show user-friendly error messages instead of raw stack traces
            Application.ThreadException += (sender, e) =>
            {
                MessageBox.Show(
                    $"An unexpected error occurred:\n\n{e.Exception.Message}\n\nPlease contact the administrator.",
                    "Coffee Shop POS — Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            };

            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                var ex = e.ExceptionObject as Exception;
                MessageBox.Show(
                    $"A critical error occurred:\n\n{ex?.Message}\n\nThe application will close.",
                    "Coffee Shop POS — Critical Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            };

            // Launch the LoginForm — this is the gateway to the entire application.
            // LoginForm connects to AuthService.cs for authentication,
            // and on success opens MainForm.cs (the MDI parent shell).
            Application.Run(new Forms.LoginForm());
        }
    }
}
