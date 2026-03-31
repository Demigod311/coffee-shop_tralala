// DashboardForm.cs — Main Dashboard with KPIs and Charts
//
// Displays key performance indicators and summary statistics.
// First screen shown to Admin/Manager users after login.
// Connects to: ReportService.cs (data), ReportDAL.cs (queries),
//              InventoryService.cs (low stock count), MainForm.cs (embedded in content panel)

using CoffeeShopPOS.Services;
using CoffeeShopPOS.Database;
using System.Drawing.Drawing2D;

namespace CoffeeShopPOS.Forms
{
    /// <summary>
    /// Dashboard with KPI cards, top-selling items, and daily summary.
    /// Provides at-a-glance business overview for managers and admins.
    /// </summary>
    public class DashboardForm : Form
    {
        // ── Color scheme ──
        private readonly Color bgColor = Color.FromArgb(245, 242, 238);
        private readonly Color cardBg = Color.White;
        private readonly Color textDark = Color.FromArgb(50, 40, 35);
        private readonly Color textMuted = Color.FromArgb(140, 130, 120);
        private readonly Color accentGold = Color.FromArgb(193, 154, 107);
        private readonly Color successGreen = Color.FromArgb(76, 175, 80);
        private readonly Color warningOrange = Color.FromArgb(255, 152, 0);
        private readonly Color dangerRed = Color.FromArgb(244, 67, 54);
        private readonly Color infoBlue = Color.FromArgb(33, 150, 243);

        private Panel kpiPanel = null!;
        private DataGridView dgvTopItems = null!;
        private DataGridView dgvRecentOrders = null!;
        private Button btnRefresh = null!;

        public DashboardForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Dashboard";
            this.BackColor = bgColor;
            this.Dock = DockStyle.Fill;
            this.AutoScroll = true;
            this.Padding = new Padding(20);

            // ── Page title ──
            var lblTitle = new Label
            {
                Text = "📊 Dashboard",
                Font = new Font("Segoe UI", 22, FontStyle.Bold),
                ForeColor = textDark,
                Dock = DockStyle.Top,
                Height = 50,
                Padding = new Padding(5, 5, 0, 0),
            };
            this.Controls.Add(lblTitle);

            var lblSubtitle = new Label
            {
                Text = $"Today's overview — {DateTime.Now:dddd, MMMM dd, yyyy}",
                Font = new Font("Segoe UI", 11),
                ForeColor = textMuted,
                Dock = DockStyle.Top,
                Height = 30,
                Padding = new Padding(5, 0, 0, 0),
            };
            this.Controls.Add(lblSubtitle);

            // ── Refresh button ──
            btnRefresh = new Button
            {
                Text = "🔄 Refresh",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.White,
                BackColor = accentGold,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(120, 35),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
            };
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.Click += (s, e) => LoadDashboardData();
            this.Controls.Add(btnRefresh);

            // ── KPI Cards Panel ──
            kpiPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 140,
                Padding = new Padding(0, 15, 0, 15),
            };
            this.Controls.Add(kpiPanel);

            // ── Top-selling items grid ──
            var lblTopItems = new Label
            {
                Text = "🏆 Top Selling Items",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = textDark,
                Dock = DockStyle.Top,
                Height = 40,
                Padding = new Padding(5, 10, 0, 0),
            };
            this.Controls.Add(lblTopItems);

            dgvTopItems = CreateStyledDataGridView();
            dgvTopItems.Dock = DockStyle.Top;
            dgvTopItems.Height = 250;
            this.Controls.Add(dgvTopItems);

            // Reverse the control order so they display top-to-bottom properly
            // (Dock=Top adds from bottom up)
            this.Controls.SetChildIndex(lblTitle, this.Controls.Count - 1);
            this.Controls.SetChildIndex(lblSubtitle, this.Controls.Count - 1);
            this.Controls.SetChildIndex(kpiPanel, this.Controls.Count - 1);
            this.Controls.SetChildIndex(lblTopItems, this.Controls.Count - 1);
            this.Controls.SetChildIndex(dgvTopItems, this.Controls.Count - 1);

            this.Load += (s, e) => LoadDashboardData();
        }

        /// <summary>
        /// Loads dashboard data from ReportService and InventoryService.
        /// Called on form load and when the Refresh button is clicked.
        /// </summary>
        private void LoadDashboardData()
        {
            try
            {
                // Get today's summary from ReportService → ReportDAL.TodaySummary()
                var summary = ReportService.GetTodaySummary();

                // Clear existing KPI cards
                kpiPanel.Controls.Clear();

                // Extract values from the summary dictionary
                int totalOrders = Convert.ToInt32(summary.GetValueOrDefault("total_orders", 0));
                decimal totalRevenue = Convert.ToDecimal(summary.GetValueOrDefault("total_revenue", 0m));
                decimal avgOrder = Convert.ToDecimal(summary.GetValueOrDefault("avg_order_value", 0m));
                int lowStockCount = InventoryService.GetLowStockCount();

                // Create KPI cards with data
                int cardWidth = (kpiPanel.Width - 80) / 4;
                CreateKpiCard("Today's Orders", totalOrders.ToString(), "📦", infoBlue, 10, cardWidth);
                CreateKpiCard("Revenue", $"${totalRevenue:N2}", "💰", successGreen, cardWidth + 25, cardWidth);
                CreateKpiCard("Avg Order", $"${avgOrder:N2}", "📊", accentGold, (cardWidth + 25) * 2, cardWidth);
                CreateKpiCard("Low Stock", lowStockCount.ToString(), "⚠",
                    lowStockCount > 0 ? dangerRed : successGreen, (cardWidth + 25) * 3, cardWidth);

                // Load top-selling items from ReportService → ReportDAL.TopSellingItems()
                var topItems = ReportService.GetTopSellingItems(10);
                dgvTopItems.DataSource = topItems;

                // Auto-size columns for better display
                if (dgvTopItems.Columns.Count > 0)
                {
                    dgvTopItems.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading dashboard data:\n{ex.Message}",
                    "Dashboard Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Creates a styled KPI card with icon, value, and label.
        /// Cards use a clean white background with colored accent stripe.
        /// </summary>
        private void CreateKpiCard(string title, string value, string icon, Color accentColor, int x, int width)
        {
            var card = new Panel
            {
                Location = new Point(x, 5),
                Size = new Size(width, 110),
                BackColor = cardBg,
            };

            // Left accent stripe (colored bar on the left edge)
            var stripe = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(5, 110),
                BackColor = accentColor,
            };
            card.Controls.Add(stripe);

            // Icon
            var lblIcon = new Label
            {
                Text = icon,
                Font = new Font("Segoe UI", 28),
                Location = new Point(15, 15),
                Size = new Size(55, 50),
                TextAlign = ContentAlignment.MiddleCenter,
            };
            card.Controls.Add(lblIcon);

            // Value (large number)
            var lblValue = new Label
            {
                Text = value,
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = textDark,
                Location = new Point(70, 10),
                Size = new Size(width - 85, 40),
                TextAlign = ContentAlignment.MiddleLeft,
            };
            card.Controls.Add(lblValue);

            // Title (below value)
            var lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 10),
                ForeColor = textMuted,
                Location = new Point(70, 55),
                Size = new Size(width - 85, 25),
            };
            card.Controls.Add(lblTitle);

            kpiPanel.Controls.Add(card);
        }

        /// <summary>
        /// Creates a styled DataGridView matching the application theme.
        /// Used throughout the application for consistent table styling.
        /// </summary>
        private DataGridView CreateStyledDataGridView()
        {
            var dgv = new DataGridView
            {
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = cardBg,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Color.FromArgb(230, 225, 220),
                RowHeadersVisible = false,
                EnableHeadersVisualStyles = false,
                Font = new Font("Segoe UI", 10),
            };

            // Header styling
            dgv.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(45, 38, 32),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                Padding = new Padding(8, 4, 4, 4),
            };
            dgv.ColumnHeadersHeight = 42;

            // Row styling
            dgv.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.White,
                ForeColor = textDark,
                SelectionBackColor = Color.FromArgb(240, 230, 218),
                SelectionForeColor = textDark,
                Padding = new Padding(8, 4, 4, 4),
            };
            dgv.RowTemplate.Height = 38;

            // Alternate row color for readability
            dgv.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(252, 250, 248),
            };

            return dgv;
        }
    }
}
