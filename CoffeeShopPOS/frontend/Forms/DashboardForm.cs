using CoffeeShopPOS.Services;
using CoffeeShopPOS.Database;
using System.Drawing.Drawing2D;

namespace CoffeeShopPOS.Forms
{
    public class DashboardForm : Form
    {
        private Panel   kpiRow      = null!;
        private Panel   bottomRow   = null!;
        private Panel   topItemsCard = null!;
        private Panel   recentCard  = null!;

        public DashboardForm() => InitializeComponent();

        private void InitializeComponent()
        {
            this.Text           = "Dashboard";
            this.BackColor      = UIHelper.ContentBg;
            this.Dock           = DockStyle.Fill;
            this.DoubleBuffered = true;
            this.AutoScroll     = true;
            this.Padding        = new Padding(0);

            // ── KPI row ──
            kpiRow = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 138,
                BackColor = Color.Transparent,
            };
            this.Controls.Add(kpiRow);

            // ── Spacer ──
            this.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 18, BackColor = Color.Transparent });

            // ── Bottom row (top items left, recent orders right) ──
            bottomRow = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.Transparent,
            };
            this.Controls.Add(bottomRow);

            // Fix top-docked stacking order (last-added is visually on top with Dock=Top)
            this.Controls.SetChildIndex(kpiRow, 0);
            this.Controls.SetChildIndex(this.Controls.Cast<Control>().First(c => c is Panel p && p.Height == 18), 1);
            this.Controls.SetChildIndex(bottomRow, 2);

            BuildBottomRow();
            this.Load += (s, e) => LoadData();
            this.Resize += (s, e) => { kpiRow.Invalidate(); RebuildKpiCards(); };
        }

        // ─────────────────────────────────────────────────────────────
        // BOTTOM ROW  (Top Selling left | Recent Orders right)
        // ─────────────────────────────────────────────────────────────
        private void BuildBottomRow()
        {
            // Recent Orders — right panel, fixed width
            recentCard = UIHelper.MakeDockedCard();
            recentCard.Dock  = DockStyle.Right;
            recentCard.Width = 380;
            bottomRow.Controls.Add(recentCard);

            // Spacer
            bottomRow.Controls.Add(new Panel { Dock = DockStyle.Right, Width = 16, BackColor = Color.Transparent });

            // Top Selling Items — fills remaining space
            topItemsCard = UIHelper.MakeDockedCard();
            topItemsCard.Dock = DockStyle.Fill;
            bottomRow.Controls.Add(topItemsCard);
        }

        // ─────────────────────────────────────────────────────────────
        // DATA LOAD
        // ─────────────────────────────────────────────────────────────
        private void LoadData()
        {
            try
            {
                var summary       = ReportService.GetTodaySummary();
                int totalOrders   = Convert.ToInt32(summary.GetValueOrDefault("total_orders",    0));
                decimal revenue   = Convert.ToDecimal(summary.GetValueOrDefault("total_revenue", 0m));
                decimal avgOrder  = Convert.ToDecimal(summary.GetValueOrDefault("avg_order_value", 0m));
                int lowStock      = InventoryService.GetLowStockCount();

                RebuildKpiCards(totalOrders, revenue, avgOrder, lowStock);
                BuildTopSellingItems();
                BuildRecentOrders();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading dashboard:\n{ex.Message}",
                    "Dashboard Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // KPI CARDS
        // ─────────────────────────────────────────────────────────────
        private int _orders; private decimal _revenue, _avg; private int _low;

        private void RebuildKpiCards(int orders = -1, decimal revenue = -1, decimal avg = -1, int low = -1)
        {
            if (orders  >= 0) _orders  = orders;
            if (revenue >= 0) _revenue = revenue;
            if (avg     >= 0) _avg     = avg;
            if (low     >= 0) _low     = low;

            kpiRow.Controls.Clear();
            if (kpiRow.Width < 100) return;

            int gap = 16;
            int w   = (kpiRow.Width - gap * 3) / 4;
            int h   = kpiRow.Height - 8;

            CreateKpiCard("Today's Orders", _orders.ToString(),    "🛒", UIHelper.Blue,   gap,                 w, h);
            CreateKpiCard("Revenue",        $"${_revenue:N2}",     "💰", UIHelper.Green,  gap + (w + gap),     w, h);
            CreateKpiCard("Avg Order Value",$"${_avg:N2}",         "📊", UIHelper.Gold,   gap + (w + gap) * 2, w, h);
            CreateKpiCard("Low Stock Alerts",_low.ToString(),       "⚠", _low > 0 ? UIHelper.Red : UIHelper.Green,
                                                                          gap + (w + gap) * 3, w, h);
        }

        private void CreateKpiCard(string label, string value, string icon, Color accent, int x, int w, int h)
        {
            // Shadow
            var shadow = new Panel
            {
                Location  = new Point(x + 3, 5),
                Size      = new Size(w, h),
                BackColor = Color.FromArgb(218, 212, 206),
            };
            shadow.Region = new Region(UIHelper.RoundedPath(new Rectangle(0, 0, w, h), 10));
            kpiRow.Controls.Add(shadow);

            // Card
            var card = UIHelper.MakeCard(x, 2, w, h);
            kpiRow.Controls.Add(card);

            // Colored top stripe
            card.Controls.Add(new Panel
            {
                Location  = new Point(0, 0),
                Size      = new Size(w, 4),
                BackColor = accent,
            });

            // Icon circle
            var iconCircle = new Panel
            {
                Location  = new Point(20, 18),
                Size      = new Size(48, 48),
                BackColor = Color.Transparent,
            };
            iconCircle.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var brush = new SolidBrush(Color.FromArgb(30, accent.R, accent.G, accent.B));
                g.FillEllipse(brush, 0, 0, 47, 47);
                using var accBrush = new SolidBrush(accent);
                // Draw a smaller accent circle overlay
                g.FillEllipse(new SolidBrush(Color.FromArgb(50, accent.R, accent.G, accent.B)), 4, 4, 39, 39);
                TextRenderer.DrawText(e.Graphics, icon, new Font("Segoe UI", 18),
                    iconCircle.ClientRectangle, accent,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            card.Controls.Add(iconCircle);

            // Value
            card.Controls.Add(new Label
            {
                Text      = value,
                Font      = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = UIHelper.TextDark,
                Location  = new Point(20, 72),
                Size      = new Size(w - 24, 34),
                BackColor = Color.Transparent,
            });

            // Label
            card.Controls.Add(new Label
            {
                Text      = label,
                Font      = new Font("Segoe UI", 9.5f),
                ForeColor = UIHelper.TextMuted,
                Location  = new Point(20, 108),
                Size      = new Size(w - 24, 20),
                BackColor = Color.Transparent,
            });
        }

        // ─────────────────────────────────────────────────────────────
        // TOP SELLING ITEMS
        // ─────────────────────────────────────────────────────────────
        private void BuildTopSellingItems()
        {
            topItemsCard.Controls.Clear();
            topItemsCard.Padding = new Padding(20, 16, 20, 16);

            // Header row
            var headerRow = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 38,
                BackColor = Color.Transparent,
            };
            headerRow.Controls.Add(new Label
            {
                Text      = "Top Selling Items",
                Font      = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = UIHelper.TextDark,
                AutoSize  = true,
                Location  = new Point(0, 6),
                BackColor = Color.Transparent,
            });

            // Refresh button
            var btnRefresh = new Label
            {
                Text      = "↻",
                Font      = new Font("Segoe UI", 16),
                ForeColor = UIHelper.TextMuted,
                Location  = new Point(topItemsCard.Width - 60, 4),
                Size      = new Size(32, 30),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor    = Cursors.Hand,
                Anchor    = AnchorStyles.Top | AnchorStyles.Right,
                BackColor = Color.Transparent,
            };
            btnRefresh.Click += (s, e) => LoadData();
            headerRow.Controls.Add(btnRefresh);
            topItemsCard.Controls.Add(headerRow);

            // Separator
            topItemsCard.Controls.Add(new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 1,
                BackColor = UIHelper.Border,
            });

            // Column header row
            var colHeader = BuildTopItemsColHeader();
            topItemsCard.Controls.Add(colHeader);

            // Data rows
            try
            {
                var items = ReportService.GetTopSellingItems(8);
                int rank = 1;
                foreach (var row in items.Rows.Cast<System.Data.DataRow>())
                {
                    var dataRow = BuildTopItemRow(
                        rank++,
                        row["item_name"]?.ToString() ?? "",
                        row["category_name"]?.ToString() ?? "",
                        row["total_qty"]?.ToString() ?? "0",
                        Convert.ToDecimal(row["total_revenue"] ?? 0));
                    topItemsCard.Controls.Add(dataRow);
                }
            }
            catch { /* no data — show nothing */ }

            // Fix stacking order
            var controls = topItemsCard.Controls.Cast<Control>().ToList();
            for (int i = 0; i < controls.Count; i++)
                topItemsCard.Controls.SetChildIndex(controls[i], i);
        }

        private static Panel BuildTopItemsColHeader()
        {
            var row = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 34,
                BackColor = Color.FromArgb(250, 247, 243),
            };
            void Col(string t, int x, int w, ContentAlignment align = ContentAlignment.MiddleLeft) =>
                row.Controls.Add(new Label
                {
                    Text      = t,
                    Font      = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                    ForeColor = UIHelper.TextMuted,
                    Location  = new Point(x, 0),
                    Size      = new Size(w, 34),
                    TextAlign = align,
                    BackColor = Color.Transparent,
                });
            Col("Rank",     8,   52);
            Col("Item",     68,  160);
            Col("Category", 236, 110);
            Col("Qty",      354, 60, ContentAlignment.MiddleRight);
            Col("Revenue",  422, 90, ContentAlignment.MiddleRight);
            return row;
        }

        private static Panel BuildTopItemRow(int rank, string item, string category, string qty, decimal revenue)
        {
            var row = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 44,
                BackColor = Color.Transparent,
            };
            row.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(240, 235, 230));
                e.Graphics.DrawLine(pen, 0, row.Height - 1, row.Width, row.Height - 1);
            };

            // Rank circle
            var rankCircle = new Panel
            {
                Location  = new Point(8, 8),
                Size      = new Size(28, 28),
                BackColor = Color.Transparent,
            };
            rankCircle.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var brush = new SolidBrush(UIHelper.Gold);
                g.FillEllipse(brush, 0, 0, 27, 27);
                TextRenderer.DrawText(e.Graphics, rank.ToString(), new Font("Segoe UI", 9, FontStyle.Bold),
                    rankCircle.ClientRectangle, Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            row.Controls.Add(rankCircle);

            row.Controls.Add(new Label { Text = item,     Font = new Font("Segoe UI", 10),              ForeColor = UIHelper.TextDark,  Location = new Point(46, 12), Size = new Size(180, 20), BackColor = Color.Transparent });
            row.Controls.Add(new Label { Text = category, Font = new Font("Segoe UI", 9.5f),            ForeColor = UIHelper.TextMuted, Location = new Point(236, 12), Size = new Size(110, 20), BackColor = Color.Transparent });
            row.Controls.Add(new Label { Text = qty,      Font = new Font("Segoe UI", 10),              ForeColor = UIHelper.TextDark,  Location = new Point(346, 12), Size = new Size(60, 20), TextAlign = ContentAlignment.MiddleRight, BackColor = Color.Transparent });
            row.Controls.Add(new Label { Text = $"${revenue:N2}", Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = UIHelper.Green, Location = new Point(414, 12), Size = new Size(90, 20), TextAlign = ContentAlignment.MiddleRight, BackColor = Color.Transparent });
            return row;
        }

        // ─────────────────────────────────────────────────────────────
        // RECENT ORDERS
        // ─────────────────────────────────────────────────────────────
        private void BuildRecentOrders()
        {
            recentCard.Controls.Clear();
            recentCard.Padding = new Padding(20, 16, 20, 16);

            recentCard.Controls.Add(new Label
            {
                Text      = "Recent Orders",
                Font      = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = UIHelper.TextDark,
                Dock      = DockStyle.Top,
                Height    = 38,
                BackColor = Color.Transparent,
            });
            recentCard.Controls.Add(new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 1,
                BackColor = UIHelper.Border,
            });

            try
            {
                var orders = OrderDAL.GetActiveOrders().Take(5).ToList();
                foreach (var order in orders)
                    recentCard.Controls.Add(BuildRecentOrderRow(order));

                if (orders.Count == 0)
                {
                    recentCard.Controls.Add(new Label
                    {
                        Text      = "No active orders",
                        Font      = new Font("Segoe UI", 10),
                        ForeColor = UIHelper.TextMuted,
                        Dock      = DockStyle.Top,
                        Height    = 44,
                        TextAlign = ContentAlignment.MiddleCenter,
                        BackColor = Color.Transparent,
                    });
                }
            }
            catch { /* silently skip */ }

            // Fix stacking order
            var controls = recentCard.Controls.Cast<Control>().ToList();
            for (int i = 0; i < controls.Count; i++)
                recentCard.Controls.SetChildIndex(controls[i], i);
        }

        private static Panel BuildRecentOrderRow(CoffeeShopPOS.Models.Order order)
        {
            var row = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 56,
                BackColor = Color.Transparent,
            };
            row.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(240, 235, 230));
                e.Graphics.DrawLine(pen, 0, row.Height - 1, row.Width, row.Height - 1);
            };

            // Order ID + time (stacked)
            row.Controls.Add(new Label
            {
                Text      = $"#{order.OrderId:D4}",
                Font      = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = UIHelper.TextDark,
                Location  = new Point(0, 8),
                Size      = new Size(80, 18),
                BackColor = Color.Transparent,
            });
            row.Controls.Add(new Label
            {
                Text      = order.CreatedAt.ToString("h:mm tt"),
                Font      = new Font("Segoe UI", 8.5f),
                ForeColor = UIHelper.TextMuted,
                Location  = new Point(0, 28),
                Size      = new Size(80, 16),
                BackColor = Color.Transparent,
            });

            // Table/Takeaway
            row.Controls.Add(new Label
            {
                Text      = order.OrderType == "Takeaway" ? "Takeaway" : $"Table {order.TableNumber}",
                Font      = new Font("Segoe UI", 9.5f),
                ForeColor = UIHelper.TextMuted,
                Location  = new Point(90, 18),
                AutoSize  = true,
                BackColor = Color.Transparent,
            });

            // Total
            row.Controls.Add(new Label
            {
                Text      = $"${order.Total:N2}",
                Font      = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = UIHelper.TextDark,
                Location  = new Point(210, 18),
                Size      = new Size(70, 20),
                TextAlign = ContentAlignment.MiddleRight,
                BackColor = Color.Transparent,
            });

            // Status badge
            var badge = UIHelper.MakeBadge(order.Status, UIHelper.StatusColor(order.Status), 80, 24);
            badge.Location = new Point(288, 16);
            row.Controls.Add(badge);

            return row;
        }
    }
}
