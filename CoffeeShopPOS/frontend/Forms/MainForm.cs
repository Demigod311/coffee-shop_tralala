using CoffeeShopPOS.Services;
using CoffeeShopPOS.Database;
using System.Drawing.Drawing2D;

namespace CoffeeShopPOS.Forms
{
    public class MainForm : Form
    {
        // ── Sidebar palette ───────────────────────────────────────────
        private static readonly Color SidebarBg    = Color.FromArgb(28, 20, 14);
        private static readonly Color NavHover      = Color.FromArgb(50, 38, 26);
        private static readonly Color NavActiveClr  = Color.FromArgb(200, 150, 85);
        private static readonly Color NavText       = Color.FromArgb(195, 185, 168);
        private static readonly Color NavIconClr    = Color.FromArgb(150, 138, 122);

        private Panel  sidebarPanel  = null!;
        private Panel  contentPanel  = null!;
        private Label  lblPageTitle  = null!;
        private Label  lblDate       = null!;
        private Label  lblTime       = null!;
        private System.Windows.Forms.Timer clockTimer = null!;
        private Panel? activeNavPanel;

        public MainForm() => InitializeComponent();

        private void InitializeComponent()
        {
            this.Text           = "Brew & Co. POS";
            this.Size           = new Size(1440, 900);
            this.MinimumSize    = new Size(1200, 700);
            this.StartPosition  = FormStartPosition.CenterScreen;
            this.BackColor      = UIHelper.ContentBg;
            this.DoubleBuffered = true;
            this.WindowState    = FormWindowState.Maximized;

            BuildSidebar();
            BuildMainArea();

            clockTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            clockTimer.Tick += (s, e) => UpdateClock();
            clockTimer.Start();
            UpdateClock();

            this.Load += MainForm_Load;
        }

        // ═════════════════════════════════════════════════════════════
        // SIDEBAR
        // ═════════════════════════════════════════════════════════════
        private void BuildSidebar()
        {
            sidebarPanel = new Panel
            {
                Dock      = DockStyle.Left,
                Width     = 268,
                BackColor = SidebarBg,
            };
            this.Controls.Add(sidebarPanel);

            // ── Logo ──
            var logoPanel = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 72,
                BackColor = Color.Transparent,
            };
            logoPanel.Controls.Add(new Label
            {
                Text      = "☕",
                Font      = new Font("Segoe UI", 22),
                ForeColor = NavActiveClr,
                Location  = new Point(18, 16),
                AutoSize  = true,
                BackColor = Color.Transparent,
            });
            logoPanel.Controls.Add(new Label
            {
                Text      = "Brew & Co.",
                Font      = new Font("Segoe UI", 15, FontStyle.Bold),
                ForeColor = NavActiveClr,
                Location  = new Point(60, 22),
                AutoSize  = true,
                BackColor = Color.Transparent,
            });
            sidebarPanel.Controls.Add(logoPanel);

            // ── Top separator ──
            sidebarPanel.Controls.Add(new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 1,
                BackColor = Color.FromArgb(52, 40, 28),
            });

            // ── Bottom user + logout (added before Fill so it docks correctly) ──
            BuildSidebarBottom();

            // ── Nav container (Fill) ──
            var navContainer = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.Transparent,
                AutoScroll = false,
                Padding   = new Padding(0, 8, 0, 8),
            };
            sidebarPanel.Controls.Add(navContainer);

            // Build nav items
            int y = 8;

            void AddNav(string icon, string text, bool show, Func<Form> factory, string title)
            {
                if (!show) return;
                var item = MakeNavItem(icon, text);
                item.Location = new Point(0, y);
                item.Click += (s, e) => OpenForm(item, factory(), title);
                foreach (Control c in item.Controls)
                    c.Click += (s, e) => OpenForm(item, factory(), title);
                navContainer.Controls.Add(item);
                y += 48;
            }

            AddNav("📊", "Dashboard",    AuthService.HasAccess("Dashboard"),         () => new DashboardForm(),          "Dashboard");
            AddNav("🛒", "New Order",    true,                                        () => new OrderForm(),              "New Order");
            AddNav("🪑", "Tables",       true,                                        () => new TableManagementForm(),    "Tables");
            AddNav("📋", "Order History",true,                                        () => new OrderHistoryForm(),       "Order History");
            AddNav("🍽", "Menu Items",   AuthService.HasAccess("MenuManagement"),     () => new MenuManagementForm(),     "Menu Items");
            AddNav("📁", "Categories",   AuthService.HasAccess("CategoryManagement"), () => new CategoryManagementForm(), "Categories");
            AddNav("📦", "Inventory",    AuthService.HasAccess("Inventory"),          () => new InventoryForm(),          "Inventory");
            AddNav("📈", "Sales Report", AuthService.HasAccess("Reports"),            () => new SalesReportForm(),        "Sales Report");
            AddNav("👥", "Staff",        AuthService.HasAccess("UserManagement"),     () => new UserManagementForm(),     "Staff");
            if (AuthService.HasAccess("UserManagement"))
            {
                navContainer.Controls.Add(new Panel
                {
                    Location  = new Point(16, y + 4),
                    Size      = new Size(236, 1),
                    BackColor = Color.FromArgb(52, 40, 28),
                });
                y += 14;
                AddNav("🗄", "Backup",   true, () => new BackupRestoreForm(), "Backup & Restore");
            }
        }

        private Panel MakeNavItem(string icon, string text)
        {
            var pnl = new Panel
            {
                Size      = new Size(268, 44),
                BackColor = Color.Transparent,
                Cursor    = Cursors.Hand,
            };

            pnl.Paint += (s, e) =>
            {
                if (pnl != activeNavPanel) return;
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var path  = UIHelper.RoundedPath(new Rectangle(12, 3, pnl.Width - 24, pnl.Height - 6), 8);
                using var brush = new SolidBrush(NavActiveClr);
                g.FillPath(brush, path);
            };

            var iconLbl = new Label
            {
                Text      = icon,
                Font      = new Font("Segoe UI", 12),
                ForeColor = NavIconClr,
                Location  = new Point(24, 10),
                Size      = new Size(28, 24),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter,
                Tag       = "icon",
            };
            var textLbl = new Label
            {
                Text      = text,
                Font      = new Font("Segoe UI", 10.5f),
                ForeColor = NavText,
                Location  = new Point(60, 12),
                AutoSize  = true,
                BackColor = Color.Transparent,
                Tag       = "text",
            };

            pnl.MouseEnter += (s, e) => { if (pnl != activeNavPanel) pnl.BackColor = NavHover; };
            pnl.MouseLeave += (s, e) => { if (pnl != activeNavPanel) pnl.BackColor = Color.Transparent; };

            pnl.Controls.Add(iconLbl);
            pnl.Controls.Add(textLbl);
            return pnl;
        }

        private void BuildSidebarBottom()
        {
            var bottomPanel = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 118,
                BackColor = Color.Transparent,
            };

            // Separator line
            bottomPanel.Controls.Add(new Panel
            {
                Location  = new Point(16, 0),
                Size      = new Size(236, 1),
                BackColor = Color.FromArgb(52, 40, 28),
            });

            // Avatar + name + role
            var avatarPanel = new Panel
            {
                Location  = new Point(16, 12),
                Size      = new Size(42, 42),
                BackColor = Color.Transparent,
            };
            avatarPanel.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var brush = new SolidBrush(NavActiveClr);
                g.FillEllipse(brush, 0, 0, 41, 41);
                var initials = GetInitials(AuthService.CurrentUser?.FullName ?? "U");
                using var font = new Font("Segoe UI", 13, FontStyle.Bold);
                TextRenderer.DrawText(g, initials, font, avatarPanel.ClientRectangle,
                    Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            bottomPanel.Controls.Add(avatarPanel);

            bottomPanel.Controls.Add(new Label
            {
                Text      = AuthService.CurrentUser?.FullName ?? "User",
                Font      = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(225, 215, 200),
                Location  = new Point(68, 14),
                Size      = new Size(184, 20),
                BackColor = Color.Transparent,
            });
            bottomPanel.Controls.Add(new Label
            {
                Text      = AuthService.CurrentUser?.Role ?? "",
                Font      = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(130, 118, 102),
                Location  = new Point(68, 34),
                Size      = new Size(184, 18),
                BackColor = Color.Transparent,
            });

            // Logout row
            var logoutRow = new Panel
            {
                Location  = new Point(0, 66),
                Size      = new Size(268, 44),
                BackColor = Color.Transparent,
                Cursor    = Cursors.Hand,
            };
            logoutRow.Controls.Add(new Label
            {
                Text      = "→",
                Font      = new Font("Segoe UI", 13),
                ForeColor = Color.FromArgb(210, 85, 75),
                Location  = new Point(24, 10),
                Size      = new Size(26, 22),
                BackColor = Color.Transparent,
            });
            logoutRow.Controls.Add(new Label
            {
                Text      = "Logout",
                Font      = new Font("Segoe UI", 10.5f),
                ForeColor = Color.FromArgb(210, 85, 75),
                Location  = new Point(58, 12),
                AutoSize  = true,
                BackColor = Color.Transparent,
            });

            void DoLogout(object? s, EventArgs e)
            {
                if (MessageBox.Show("Are you sure you want to logout?", "Logout",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    AuthService.Logout();
                    this.Close();
                }
            }
            logoutRow.Click += DoLogout;
            foreach (Control c in logoutRow.Controls) c.Click += DoLogout;
            logoutRow.MouseEnter += (s, e) => logoutRow.BackColor = NavHover;
            logoutRow.MouseLeave += (s, e) => logoutRow.BackColor = Color.Transparent;
            bottomPanel.Controls.Add(logoutRow);

            sidebarPanel.Controls.Add(bottomPanel);
        }

        // ═════════════════════════════════════════════════════════════
        // MAIN AREA
        // ═════════════════════════════════════════════════════════════
        private void BuildMainArea()
        {
            var mainContainer = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = UIHelper.ContentBg,
            };
            this.Controls.Add(mainContainer);

            // Header
            var headerPanel = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 64,
                BackColor = Color.White,
                Padding   = new Padding(28, 0, 24, 0),
            };

            lblPageTitle = new Label
            {
                Text      = "",
                Font      = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = UIHelper.TextDark,
                Dock      = DockStyle.Left,
                AutoSize  = false,
                Width     = 500,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent,
            };
            headerPanel.Controls.Add(lblPageTitle);

            // Date/time stacked on the right
            var dtPanel = new Panel
            {
                Dock      = DockStyle.Right,
                Width     = 230,
                BackColor = Color.Transparent,
                Padding   = new Padding(0, 10, 0, 0),
            };
            lblDate = new Label
            {
                Font      = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = UIHelper.TextDark,
                TextAlign = ContentAlignment.TopRight,
                Dock      = DockStyle.Top,
                Height    = 24,
                BackColor = Color.Transparent,
            };
            lblTime = new Label
            {
                Font      = new Font("Segoe UI", 9.5f),
                ForeColor = UIHelper.TextMuted,
                TextAlign = ContentAlignment.TopRight,
                Dock      = DockStyle.Top,
                Height    = 20,
                BackColor = Color.Transparent,
            };
            dtPanel.Controls.Add(lblTime);
            dtPanel.Controls.Add(lblDate);
            headerPanel.Controls.Add(dtPanel);

            headerPanel.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(235, 230, 224));
                e.Graphics.DrawLine(pen, 0, headerPanel.Height - 1, headerPanel.Width, headerPanel.Height - 1);
            };
            mainContainer.Controls.Add(headerPanel);

            // Content panel
            contentPanel = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = UIHelper.ContentBg,
                Padding   = new Padding(24),
            };
            mainContainer.Controls.Add(contentPanel);
        }

        // ═════════════════════════════════════════════════════════════
        // NAVIGATION
        // ═════════════════════════════════════════════════════════════
        private void OpenForm(Panel navItem, Form form, string title)
        {
            // Reset previous active nav
            if (activeNavPanel != null)
            {
                activeNavPanel.BackColor = Color.Transparent;
                foreach (Control c in activeNavPanel.Controls)
                    if (c is Label lbl)
                        lbl.ForeColor = lbl.Tag?.ToString() == "icon" ? NavIconClr : NavText;
                activeNavPanel.Invalidate();
            }

            activeNavPanel = navItem;
            navItem.BackColor = Color.Transparent;
            foreach (Control c in navItem.Controls)
                if (c is Label lbl)
                    lbl.ForeColor = Color.White;
            navItem.Invalidate();

            lblPageTitle.Text = title;

            // Embed form in content panel
            foreach (Control old in contentPanel.Controls)
                old.Dispose();
            contentPanel.Controls.Clear();

            form.TopLevel         = false;
            form.FormBorderStyle  = FormBorderStyle.None;
            form.Dock             = DockStyle.Fill;
            form.BackColor        = UIHelper.ContentBg;
            contentPanel.Controls.Add(form);
            form.Show();
        }

        private void UpdateClock()
        {
            lblDate.Text = DateTime.Now.ToString("dddd, MMMM d, yyyy");
            lblTime.Text = DateTime.Now.ToString("h:mm:ss tt");
        }

        private void MainForm_Load(object? sender, EventArgs e)
        {
            // Trigger first nav item click programmatically
            var navContainer = sidebarPanel.Controls.OfType<Panel>()
                .FirstOrDefault(p => p.Dock == DockStyle.Fill);
            if (navContainer == null) return;

            var firstNav = navContainer.Controls.OfType<Panel>().FirstOrDefault();
            if (firstNav != null)
            {
                // Panels don't have PerformClick(); use reflection to trigger the Click event
                var method = typeof(Control).GetMethod("OnClick", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                method?.Invoke(firstNav, new object[] { EventArgs.Empty });
            }

            if (AuthService.HasAccess("Inventory"))
            {
                var low = InventoryService.GetLowStockCount();
                if (low > 0)
                    MessageBox.Show($"⚠ {low} inventory item(s) are running low on stock.",
                        "Low Stock Alert", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private static string GetInitials(string name)
        {
            var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length >= 2
                ? $"{parts[0][0]}{parts[^1][0]}".ToUpper()
                : name.Length >= 2 ? name[..2].ToUpper() : name.ToUpper();
        }
    }
}
