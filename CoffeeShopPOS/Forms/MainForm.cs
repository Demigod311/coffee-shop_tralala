// MainForm.cs — Main MDI Parent / Navigation Shell
//
// The central hub of the application after login.
// Features a sidebar with role-based navigation and a content panel for child forms.
// Connects to: AuthService.cs (role checks for sidebar buttons),
//              All child Forms (opened in the content panel),
//              LoginForm.cs (returns here on logout)

using CoffeeShopPOS.Services;
using System.Drawing.Drawing2D;

namespace CoffeeShopPOS.Forms
{
    /// <summary>
    /// Main application shell with sidebar navigation and content area.
    /// Child forms are displayed in the content panel rather than using MDI child windows.
    /// Sidebar buttons are shown/hidden based on the current user's role (from AuthService.cs).
    /// </summary>
    public class MainForm : Form
    {
        // ── Layout panels ──
        private Panel sidebarPanel = null!;    // Left navigation sidebar
        private Panel contentPanel = null!;    // Right content area for child forms
        private Panel headerPanel = null!;     // Top header bar
        private Label lblWelcome = null!;      // Shows current user name and role
        private Label lblDateTime = null!;     // Live clock display
        private System.Windows.Forms.Timer clockTimer = null!;

        // ── Currently active sidebar button (for highlighting) ──
        private Button? activeButton;

        // ── Color scheme — matching the login form's coffee theme ──
        private readonly Color sidebarBg = Color.FromArgb(35, 28, 22);
        private readonly Color sidebarHover = Color.FromArgb(55, 45, 38);
        private readonly Color sidebarActive = Color.FromArgb(193, 154, 107);
        private readonly Color contentBg = Color.FromArgb(245, 242, 238);
        private readonly Color headerBg = Color.FromArgb(45, 38, 32);
        private readonly Color textLight = Color.FromArgb(240, 235, 228);
        private readonly Color textDark = Color.FromArgb(60, 50, 40);
        private readonly Color accentColor = Color.FromArgb(193, 154, 107);

        // ── Fade animation for content transitions ──
        private System.Windows.Forms.Timer fadeTimer = null!;
        private Form? pendingForm;

        public MainForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            // ── Form settings ──
            this.Text = "Coffee Shop POS System";
            this.Size = new Size(1400, 850);
            this.MinimumSize = new Size(1200, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = contentBg;
            this.DoubleBuffered = true;
            this.WindowState = FormWindowState.Maximized;

            // ═══════════════════════════════════════════════════════════════
            // SIDEBAR PANEL — Left navigation with coffee-themed styling
            // Contains buttons for each module, shown based on user role
            // ═══════════════════════════════════════════════════════════════
            sidebarPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 250,
                BackColor = sidebarBg,
            };
            this.Controls.Add(sidebarPanel);

            // ── Sidebar branding ──
            var brandLabel = new Label
            {
                Text = "☕ Coffee Shop",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = accentColor,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 10, 0, 0),
            };
            sidebarPanel.Controls.Add(brandLabel);

            var separator = new Panel
            {
                Dock = DockStyle.Top,
                Height = 2,
                BackColor = Color.FromArgb(60, 50, 42),
            };
            sidebarPanel.Controls.Add(separator);

            // ── Add navigation buttons based on user role ──
            // AuthService.HasAccess() checks the current user's role permissions
            int buttonY = 85;

            // Dashboard — Admin and Manager only
            if (AuthService.HasAccess("Dashboard"))
            {
                AddSidebarButton("📊  Dashboard", buttonY, () => ShowForm(new DashboardForm()));
                buttonY += 48;
            }

            // Orders — All roles
            AddSidebarButton("🛒  New Order", buttonY, () => ShowForm(new OrderForm()));
            buttonY += 48;

            // Tables — All roles
            AddSidebarButton("🪑  Tables", buttonY, () => ShowForm(new TableManagementForm()));
            buttonY += 48;

            // Order History — All roles
            AddSidebarButton("📋  Order History", buttonY, () => ShowForm(new OrderHistoryForm()));
            buttonY += 48;

            // Menu Management — Admin and Manager
            if (AuthService.HasAccess("MenuManagement"))
            {
                AddSidebarButton("🍽  Menu Items", buttonY, () => ShowForm(new MenuManagementForm()));
                buttonY += 48;
            }

            // Category Management — Admin and Manager
            if (AuthService.HasAccess("CategoryManagement"))
            {
                AddSidebarButton("📁  Categories", buttonY, () => ShowForm(new CategoryManagementForm()));
                buttonY += 48;
            }

            // Inventory — Admin and Manager
            if (AuthService.HasAccess("Inventory"))
            {
                AddSidebarButton("📦  Inventory", buttonY, () => ShowForm(new InventoryForm()));
                buttonY += 48;
            }

            // Reports — Admin and Manager
            if (AuthService.HasAccess("Reports"))
            {
                AddSidebarButton("📈  Sales Report", buttonY, () => ShowForm(new SalesReportForm()));
                buttonY += 48;
                AddSidebarButton("📊  Inventory Report", buttonY, () => ShowForm(new InventoryReportForm()));
                buttonY += 48;
            }

            // User Management — Admin only
            if (AuthService.HasAccess("UserManagement"))
            {
                AddSidebarButton("👥  Staff", buttonY, () => ShowForm(new UserManagementForm()));
                buttonY += 48;
            }

            // ── Logout button at bottom of sidebar ──
            var btnLogout = new Button
            {
                Text = "🚪  Logout",
                Font = new Font("Segoe UI", 11),
                ForeColor = Color.FromArgb(220, 100, 80),
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(250, 45),
                Dock = DockStyle.Bottom,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(20, 0, 0, 0),
                Cursor = Cursors.Hand,
            };
            btnLogout.FlatAppearance.BorderSize = 0;
            // Logout click — clears session via AuthService and returns to LoginForm
            btnLogout.Click += (s, e) =>
            {
                if (MessageBox.Show("Are you sure you want to logout?",
                    "Confirm Logout", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    AuthService.Logout();  // Clears AuthService.CurrentUser
                    this.Close();           // Closes MainForm → returns to LoginForm via FormClosed event
                }
            };
            btnLogout.MouseEnter += (s, e) => btnLogout.BackColor = Color.FromArgb(60, 45, 38);
            btnLogout.MouseLeave += (s, e) => btnLogout.BackColor = Color.Transparent;
            sidebarPanel.Controls.Add(btnLogout);

            // ═══════════════════════════════════════════════════════════════
            // HEADER PANEL — Top bar with welcome message and clock
            // ═══════════════════════════════════════════════════════════════
            headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = headerBg,
                Padding = new Padding(20, 0, 20, 0),
            };
            this.Controls.Add(headerPanel);

            // Welcome message shows the logged-in user's name and role
            // AuthService.CurrentUser is set by LoginForm after successful authentication
            lblWelcome = new Label
            {
                Text = $"Welcome, {AuthService.CurrentUser?.FullName ?? "User"} ({AuthService.CurrentUser?.Role})",
                Font = new Font("Segoe UI", 12),
                ForeColor = textLight,
                AutoSize = true,
                Location = new Point(20, 18),
                BackColor = Color.Transparent,
            };
            headerPanel.Controls.Add(lblWelcome);

            // Live clock display updated every second
            lblDateTime = new Label
            {
                Text = DateTime.Now.ToString("dddd, MMMM dd, yyyy  |  hh:mm:ss tt"),
                Font = new Font("Segoe UI", 11),
                ForeColor = accentColor,
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BackColor = Color.Transparent,
            };
            headerPanel.Controls.Add(lblDateTime);

            // ═══════════════════════════════════════════════════════════════
            // CONTENT PANEL — Main area where child forms are displayed
            // ═══════════════════════════════════════════════════════════════
            contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = contentBg,
                Padding = new Padding(15),
            };
            this.Controls.Add(contentPanel);

            // ── Clock timer — updates the time display every second ──
            clockTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            clockTimer.Tick += (s, e) =>
            {
                lblDateTime.Text = DateTime.Now.ToString("dddd, MMMM dd, yyyy  |  hh:mm:ss tt");
            };
            clockTimer.Start();

            // ── Content transition timer ──
            fadeTimer = new System.Windows.Forms.Timer { Interval = 16 };
            fadeTimer.Tick += FadeTimer_Tick;

            this.Load += MainForm_Load;
            this.Resize += MainForm_Resize;
        }

        /// <summary>
        /// Creates a styled sidebar navigation button with hover effects.
        /// Each button opens a different child form in the content panel.
        /// </summary>
        private void AddSidebarButton(string text, int top, Action onClick)
        {
            var btn = new Button
            {
                Text = text,
                Font = new Font("Segoe UI", 11),
                ForeColor = textLight,
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(250, 44),
                Location = new Point(0, top),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(20, 0, 0, 0),
                Cursor = Cursors.Hand,
            };
            btn.FlatAppearance.BorderSize = 0;

            // Hover effects — highlight on mouse over
            btn.MouseEnter += (s, e) =>
            {
                if (btn != activeButton)
                    btn.BackColor = sidebarHover;
            };
            btn.MouseLeave += (s, e) =>
            {
                if (btn != activeButton)
                    btn.BackColor = Color.Transparent;
            };

            // Click — open the associated form and highlight this button
            btn.Click += (s, e) =>
            {
                // Reset previous active button
                if (activeButton != null)
                {
                    activeButton.BackColor = Color.Transparent;
                    activeButton.ForeColor = textLight;
                }

                // Set new active button
                activeButton = btn;
                btn.BackColor = sidebarActive;
                btn.ForeColor = Color.White;

                onClick();
            };

            sidebarPanel.Controls.Add(btn);
        }

        /// <summary>
        /// Shows a child form inside the content panel.
        /// Disposes any previous form and embeds the new one as a Panel-like control.
        /// This creates a seamless, modern single-window experience.
        /// </summary>
        private void ShowForm(Form form)
        {
            try
            {
                // Clear existing content
                contentPanel.Controls.Clear();

                // Configure the child form to embed inside the content panel
                form.TopLevel = false;           // Not a standalone window
                form.FormBorderStyle = FormBorderStyle.None;  // Remove window chrome
                form.Dock = DockStyle.Fill;       // Fill the content area
                form.BackColor = contentBg;

                contentPanel.Controls.Add(form);
                form.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening form: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void FadeTimer_Tick(object? sender, EventArgs e)
        {
            // Content transition effect (simplified)
            fadeTimer.Stop();
        }

        /// <summary>
        /// Form load — shows the dashboard (if user has access) or a welcome screen.
        /// </summary>
        private void MainForm_Load(object? sender, EventArgs e)
        {
            // Position the clock label
            MainForm_Resize(sender, e);

            // Auto-open dashboard for Admin/Manager, or show welcome for others
            if (AuthService.HasAccess("Dashboard"))
            {
                ShowForm(new DashboardForm());
            }
            else
            {
                ShowWelcomeScreen();
            }

            // Check for low stock alerts if user has inventory access
            if (AuthService.HasAccess("Inventory"))
            {
                var lowStockCount = InventoryService.GetLowStockCount();
                if (lowStockCount > 0)
                {
                    MessageBox.Show(
                        $"⚠ {lowStockCount} inventory item(s) are running low on stock.\nCheck the Inventory Report for details.",
                        "Low Stock Alert", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        /// <summary>
        /// Shows a welcome screen for users who don't have dashboard access.
        /// </summary>
        private void ShowWelcomeScreen()
        {
            var welcomePanel = new Panel { Dock = DockStyle.Fill, BackColor = contentBg };

            var welcomeLabel = new Label
            {
                Text = $"☕ Welcome to Coffee Shop POS\n\n{AuthService.CurrentUser?.FullName}\n\nSelect an option from the sidebar to get started.",
                Font = new Font("Segoe UI", 18),
                ForeColor = textDark,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
            };

            welcomePanel.Controls.Add(welcomeLabel);
            contentPanel.Controls.Clear();
            contentPanel.Controls.Add(welcomePanel);
        }

        /// <summary>
        /// Handles form resize to reposition the clock label.
        /// </summary>
        private void MainForm_Resize(object? sender, EventArgs e)
        {
            if (lblDateTime != null)
            {
                lblDateTime.Location = new Point(
                    headerPanel.Width - lblDateTime.Width - 40,
                    18);
            }
        }
    }
}
