// LoginForm.cs — User Login Screen
//
// The first screen users see when launching the application.
// Features a polished dark-themed UI with animated transitions.
// Connects to: AuthService.cs (authentication), MainForm.cs (opens on success),
//              DbHelper.cs (connection test on load), Program.cs (launched from here)

using CoffeeShopPOS.Services;
using CoffeeShopPOS.Database;
using System.Drawing.Drawing2D;

namespace CoffeeShopPOS.Forms
{
    /// <summary>
    /// Login form with modern dark theme, smooth transitions, and error handling.
    /// This form is the gateway — users must authenticate before accessing the POS.
    /// </summary>
    public class LoginForm : Form
    {
        // ── UI Controls ──
        private TextBox txtUsername = null!;
        private TextBox txtPassword = null!;
        private Button btnLogin = null!;
        private Label lblTitle = null!;
        private Label lblSubtitle = null!;
        private Label lblUsername = null!;
        private Label lblPassword = null!;
        private Label lblStatus = null!;
        private Panel panelMain = null!;
        private Panel panelLogo = null!;
        private CheckBox chkShowPassword = null!;

        // ── Animation timer for smooth fade-in effect ──
        // Creates a polished entrance animation when the form loads
        private System.Windows.Forms.Timer fadeTimer = null!;
        private double currentOpacity = 0;

        // ── Color scheme — modern dark coffee theme ──
        private readonly Color bgDark = Color.FromArgb(30, 25, 20);           // Deep coffee brown
        private readonly Color panelBg = Color.FromArgb(45, 38, 32);          // Lighter brown panel
        private readonly Color accentColor = Color.FromArgb(193, 154, 107);   // Warm gold/coffee
        private readonly Color accentHover = Color.FromArgb(212, 175, 130);   // Lighter gold hover
        private readonly Color textLight = Color.FromArgb(240, 235, 228);     // Warm white
        private readonly Color textMuted = Color.FromArgb(160, 150, 140);     // Muted text
        private readonly Color inputBg = Color.FromArgb(60, 52, 44);          // Input field bg
        private readonly Color errorColor = Color.FromArgb(220, 80, 60);      // Error red

        public LoginForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Initializes all UI controls with the modern dark coffee theme.
        /// This method is equivalent to the .Designer.cs file but done programmatically
        /// for better control over styling and animations.
        /// </summary>
        private void InitializeComponent()
        {
            // ── Form settings ──
            this.Text = "Coffee Shop POS — Login";
            this.Size = new Size(500, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.None;  // Borderless for modern look
            this.BackColor = bgDark;
            this.DoubleBuffered = true;  // Prevent flickering during animations
            this.Opacity = 0;  // Start invisible for fade-in animation

            // ── Main content panel with rounded corners ──
            panelMain = new Panel
            {
                Size = new Size(400, 500),
                Location = new Point(50, 50),
                BackColor = panelBg,
            };
            this.Controls.Add(panelMain);

            // ── Coffee cup icon area ──
            panelLogo = new Panel
            {
                Size = new Size(80, 80),
                Location = new Point(160, 20),
                BackColor = Color.Transparent,
            };
            panelLogo.Paint += PanelLogo_Paint;  // Custom paint for the coffee icon
            panelMain.Controls.Add(panelLogo);

            // ── Title label ──
            lblTitle = new Label
            {
                Text = "☕ Coffee Shop POS",
                Font = new Font("Segoe UI", 22, FontStyle.Bold),
                ForeColor = accentColor,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(380, 45),
                Location = new Point(10, 110),
                BackColor = Color.Transparent,
            };
            panelMain.Controls.Add(lblTitle);

            // ── Subtitle ──
            lblSubtitle = new Label
            {
                Text = "Sign in to your account",
                Font = new Font("Segoe UI", 11),
                ForeColor = textMuted,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(380, 25),
                Location = new Point(10, 158),
                BackColor = Color.Transparent,
            };
            panelMain.Controls.Add(lblSubtitle);

            // ── Username field ──
            lblUsername = new Label
            {
                Text = "USERNAME",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = textMuted,
                Location = new Point(40, 210),
                Size = new Size(320, 18),
                BackColor = Color.Transparent,
            };
            panelMain.Controls.Add(lblUsername);

            txtUsername = new TextBox
            {
                Font = new Font("Segoe UI", 13),
                ForeColor = textLight,
                BackColor = inputBg,
                BorderStyle = BorderStyle.None,
                Location = new Point(40, 232),
                Size = new Size(320, 32),
                PlaceholderText = "Enter username",
            };
            // Add padding by wrapping in a panel with borders
            var userPanel = CreateInputPanel(txtUsername, 40, 228);
            panelMain.Controls.Add(userPanel);

            // ── Password field ──
            lblPassword = new Label
            {
                Text = "PASSWORD",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = textMuted,
                Location = new Point(40, 290),
                Size = new Size(320, 18),
                BackColor = Color.Transparent,
            };
            panelMain.Controls.Add(lblPassword);

            txtPassword = new TextBox
            {
                Font = new Font("Segoe UI", 13),
                ForeColor = textLight,
                BackColor = inputBg,
                BorderStyle = BorderStyle.None,
                Location = new Point(40, 312),
                Size = new Size(320, 32),
                UseSystemPasswordChar = true,  // Hide password characters
                PlaceholderText = "Enter password",
            };
            var passPanel = CreateInputPanel(txtPassword, 40, 308);
            panelMain.Controls.Add(passPanel);

            // ── Show password checkbox ──
            chkShowPassword = new CheckBox
            {
                Text = "Show password",
                Font = new Font("Segoe UI", 9),
                ForeColor = textMuted,
                Location = new Point(40, 358),
                Size = new Size(150, 22),
                BackColor = Color.Transparent,
            };
            // Toggle password visibility when checkbox changes
            chkShowPassword.CheckedChanged += (s, e) =>
                txtPassword.UseSystemPasswordChar = !chkShowPassword.Checked;
            panelMain.Controls.Add(chkShowPassword);

            // ── Login button with hover animation ──
            btnLogin = new Button
            {
                Text = "SIGN IN",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = accentColor,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(320, 48),
                Location = new Point(40, 400),
                Cursor = Cursors.Hand,
            };
            btnLogin.FlatAppearance.BorderSize = 0;
            // Hover effects for button — visual feedback on mouse over
            btnLogin.MouseEnter += (s, e) => btnLogin.BackColor = accentHover;
            btnLogin.MouseLeave += (s, e) => btnLogin.BackColor = accentColor;
            // Click handler — triggers authentication via AuthService.cs
            btnLogin.Click += BtnLogin_Click;
            panelMain.Controls.Add(btnLogin);

            // ── Status label for error/success messages ──
            lblStatus = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 9),
                ForeColor = errorColor,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(320, 20),
                Location = new Point(40, 456),
                BackColor = Color.Transparent,
            };
            panelMain.Controls.Add(lblStatus);

            // ── Close button (top-right X) ──
            var btnClose = new Label
            {
                Text = "✕",
                Font = new Font("Segoe UI", 14),
                ForeColor = textMuted,
                Size = new Size(30, 30),
                Location = new Point(365, 5),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand,
                BackColor = Color.Transparent,
            };
            btnClose.Click += (s, e) => Application.Exit();
            btnClose.MouseEnter += (s, e) => btnClose.ForeColor = errorColor;
            btnClose.MouseLeave += (s, e) => btnClose.ForeColor = textMuted;
            panelMain.Controls.Add(btnClose);

            // ── Enter key support — pressing Enter triggers login ──
            txtPassword.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    BtnLogin_Click(this, EventArgs.Empty);
                }
            };
            txtUsername.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    txtPassword.Focus();  // Move to password field
                }
            };

            // ── Fade-in animation timer ──
            fadeTimer = new System.Windows.Forms.Timer { Interval = 16 };  // ~60fps
            fadeTimer.Tick += FadeTimer_Tick;

            // Allow dragging the borderless form by clicking the main panel
            panelMain.MouseDown += Form_MouseDown;
            lblTitle.MouseDown += Form_MouseDown;

            this.Load += LoginForm_Load;
        }

        /// <summary>
        /// Creates a styled input panel with border effect around a TextBox.
        /// Used to give text inputs a modern bordered appearance.
        /// </summary>
        private Panel CreateInputPanel(TextBox textBox, int x, int y)
        {
            var panel = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(320, 38),
                BackColor = inputBg,
                Padding = new Padding(8, 5, 8, 5),
            };

            textBox.Location = new Point(8, 5);
            textBox.Size = new Size(304, 28);
            panel.Controls.Add(textBox);

            // Add focus highlight — border changes color when focused
            textBox.GotFocus += (s, e) => panel.BackColor = Color.FromArgb(70, 60, 50);
            textBox.LostFocus += (s, e) => panel.BackColor = inputBg;

            return panel;
        }

        /// <summary>
        /// Form load handler — starts fade-in animation and tests database connection.
        /// Tests the database connection via DbHelper.TestConnection() to show an
        /// immediate error if MySQL is not running.
        /// </summary>
        private void LoginForm_Load(object? sender, EventArgs e)
        {
            // Start the fade-in animation
            fadeTimer.Start();

            // Test database connection and ensure admin exists
            // DbHelper.TestConnection() tries to open a MySQL connection
            if (!DbHelper.TestConnection())
            {
                lblStatus.Text = "⚠ Cannot connect to database. Check MySQL.";
                lblStatus.ForeColor = errorColor;
                btnLogin.Enabled = false;
            }
            else
            {
                // Ensure at least one admin user exists in the database
                // AuthService.EnsureAdminExists() creates a default admin if needed
                AuthService.EnsureAdminExists();
            }

            txtUsername.Focus();
        }

        /// <summary>
        /// Fade-in animation tick — gradually increases form opacity.
        /// Creates a smooth entrance effect when the login form appears.
        /// </summary>
        private void FadeTimer_Tick(object? sender, EventArgs e)
        {
            currentOpacity += 0.05;
            if (currentOpacity >= 1.0)
            {
                currentOpacity = 1.0;
                fadeTimer.Stop();
            }
            this.Opacity = currentOpacity;
        }

        /// <summary>
        /// Login button click handler — authenticates the user via AuthService.
        /// On success: hides this form and opens MainForm.cs (the MDI parent).
        /// On failure: shows an error message in lblStatus.
        /// </summary>
        private void BtnLogin_Click(object? sender, EventArgs e)
        {
            // Clear previous status message
            lblStatus.Text = "";

            // Basic input validation using ValidationHelper.cs
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text;

            if (string.IsNullOrEmpty(username))
            {
                lblStatus.Text = "Please enter your username.";
                lblStatus.ForeColor = errorColor;
                txtUsername.Focus();
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                lblStatus.Text = "Please enter your password.";
                lblStatus.ForeColor = errorColor;
                txtPassword.Focus();
                return;
            }

            try
            {
                // Show loading state
                btnLogin.Text = "Signing in...";
                btnLogin.Enabled = false;
                Application.DoEvents();  // Allow UI to update

                // Authenticate via AuthService.cs → UserDAL.cs → MySQL
                var user = AuthService.Authenticate(username, password);

                if (user != null)
                {
                    // Success — show green confirmation briefly
                    lblStatus.Text = $"Welcome, {user.FullName}!";
                    lblStatus.ForeColor = Color.FromArgb(100, 200, 100);

                    // Small delay for visual feedback before transitioning to MainForm
                    var transitionTimer = new System.Windows.Forms.Timer { Interval = 500 };
                    transitionTimer.Tick += (s2, e2) =>
                    {
                        transitionTimer.Stop();
                        transitionTimer.Dispose();

                        // Hide login form and open the MainForm (MDI parent)
                        // MainForm.cs reads AuthService.CurrentUser for role-based UI
                        this.Hide();
                        var mainForm = new MainForm();
                        mainForm.FormClosed += (s3, e3) => Application.Exit();
                        mainForm.Show();
                    };
                    transitionTimer.Start();
                }
                else
                {
                    // Authentication failed — show error
                    lblStatus.Text = "Invalid username or password.";
                    lblStatus.ForeColor = errorColor;
                    txtPassword.Clear();
                    txtPassword.Focus();

                    // Shake animation effect for visual feedback on failed login
                    ShakeForm();
                }
            }
            catch (Exception ex)
            {
                // Handle database or other errors
                lblStatus.Text = $"Error: {ex.Message}";
                lblStatus.ForeColor = errorColor;
            }
            finally
            {
                btnLogin.Text = "SIGN IN";
                btnLogin.Enabled = true;
            }
        }

        /// <summary>
        /// Shake animation — gives visual feedback for failed login attempts.
        /// Rapidly moves the form left and right to simulate a "wrong" shake.
        /// </summary>
        private async void ShakeForm()
        {
            var original = this.Location;
            for (int i = 0; i < 6; i++)
            {
                int offset = (i % 2 == 0) ? 10 : -10;
                this.Location = new Point(original.X + offset, original.Y);
                await Task.Delay(40);
            }
            this.Location = original;
        }

        /// <summary>
        /// Custom paint for the logo panel — draws a coffee cup icon.
        /// </summary>
        private void PanelLogo_Paint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Draw a simple coffee cup icon
            using var brush = new SolidBrush(accentColor);
            using var pen = new Pen(accentColor, 3);

            // Cup body
            g.FillEllipse(brush, 20, 30, 40, 40);
            g.FillRectangle(brush, 20, 25, 40, 30);

            // Steam lines
            using var steamPen = new Pen(accentColor, 2);
            g.DrawBezier(steamPen, 30, 22, 28, 12, 35, 8, 32, 0);
            g.DrawBezier(steamPen, 40, 22, 38, 10, 45, 6, 42, 0);
            g.DrawBezier(steamPen, 50, 22, 48, 12, 55, 8, 52, 0);
        }

        // ══════════════════════════════════════════════════════════════════════
        // Form dragging support — allows moving the borderless form
        // Since FormBorderStyle is None, users need to drag from the panel
        // ══════════════════════════════════════════════════════════════════════
        private Point dragStart;
        private void Form_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                dragStart = e.Location;
                if (sender is Control ctrl)
                {
                    ctrl.MouseMove += Form_MouseMove;
                    ctrl.MouseUp += Form_MouseUp;
                }
            }
        }

        private void Form_MouseMove(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                this.Location = new Point(
                    this.Location.X + e.X - dragStart.X,
                    this.Location.Y + e.Y - dragStart.Y);
            }
        }

        private void Form_MouseUp(object? sender, MouseEventArgs e)
        {
            if (sender is Control ctrl)
            {
                ctrl.MouseMove -= Form_MouseMove;
                ctrl.MouseUp -= Form_MouseUp;
            }
        }
    }
}
