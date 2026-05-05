using CoffeeShopPOS.Database;
using CoffeeShopPOS.Models;
using CoffeeShopPOS.Services;
using CoffeeShopPOS.Helpers;
using System.Drawing.Drawing2D;

namespace CoffeeShopPOS.Forms
{
    public class OrderForm : Form
    {
        // ── UI controls ───────────────────────────────────────────────
        private Button          btnDineIn       = null!;
        private Button          btnTakeaway     = null!;
        private ComboBox        cmbTable        = null!;
        private FlowLayoutPanel categoryPanel   = null!;
        private FlowLayoutPanel menuPanel       = null!;
        private TextBox         txtSearch       = null!;
        private Panel           cartItemsPanel  = null!;
        private Label           lblEmpty        = null!;
        private Label           lblSubtotal     = null!;
        private Label           lblTax          = null!;
        private Label           lblTotal        = null!;
        private TextBox         txtDiscount     = null!;
        private Button          btnPlaceOrder   = null!;
        private Button          btnClearCart    = null!;

        private readonly List<OrderItem> cartItems = new();
        private Button? activeCategoryBtn;

        public OrderForm() => InitializeComponent();

        private void InitializeComponent()
        {
            this.Text           = "New Order";
            this.BackColor      = UIHelper.ContentBg;
            this.Dock           = DockStyle.Fill;
            this.DoubleBuffered = true;

            BuildLayout();
            this.Load += OrderForm_Load;
        }

        // ═════════════════════════════════════════════════════════════
        // LAYOUT
        // ═════════════════════════════════════════════════════════════
        private void BuildLayout()
        {
            // ── RIGHT PANEL — Cart (built first so it docks to right) ──
            var cartCard = UIHelper.MakeDockedCard();
            cartCard.Dock    = DockStyle.Right;
            cartCard.Width   = 360;
            cartCard.Padding = new Padding(20, 18, 20, 18);
            BuildCartPanel(cartCard);
            this.Controls.Add(cartCard);

            // ── Spacer between panels ──
            this.Controls.Add(new Panel { Dock = DockStyle.Right, Width = 16, BackColor = Color.Transparent });

            // ── LEFT PANEL — Menu area ──
            var menuCard = UIHelper.MakeDockedCard();
            menuCard.Dock    = DockStyle.Fill;
            menuCard.Padding = new Padding(20, 18, 20, 18);
            BuildMenuPanel(menuCard);
            this.Controls.Add(menuCard);

            // Enforce dock order: menu fill on left, spacer, cart fixed right.
            this.Controls.SetChildIndex(menuCard, 0);
            this.Controls.SetChildIndex(cartCard, 2);
        }

        // ─────────────────────────────────────────────────────────────
        // MENU PANEL (left card)
        // ─────────────────────────────────────────────────────────────
        private void BuildMenuPanel(Panel card)
        {
            // Order type + table row
            var typeRow = new Panel { Dock = DockStyle.Top, Height = 46, BackColor = Color.Transparent };

            btnDineIn = MakeToggleButton("Dine-In",  true);
            btnDineIn.Location = new Point(0, 4);
            btnDineIn.Size     = new Size(110, 36);
            typeRow.Controls.Add(btnDineIn);

            btnTakeaway = MakeToggleButton("Takeaway", false);
            btnTakeaway.Location = new Point(118, 4);
            btnTakeaway.Size     = new Size(110, 36);
            typeRow.Controls.Add(btnTakeaway);

            cmbTable = new ComboBox
            {
                Font          = new Font("Segoe UI", 10),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location      = new Point(240, 8),
                Size          = new Size(200, 30),
                BackColor     = Color.White,
                FlatStyle     = FlatStyle.Flat,
            };
            typeRow.Controls.Add(cmbTable);

            btnDineIn.Click   += (s, e) => SetOrderType("Dine-In");
            btnTakeaway.Click += (s, e) => SetOrderType("Takeaway");
            card.Controls.Add(typeRow);

            // Category pills
            categoryPanel = new FlowLayoutPanel
            {
                Dock          = DockStyle.Top,
                Height        = 46,
                AutoScroll    = false,
                WrapContents  = false,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor     = Color.Transparent,
                Padding       = new Padding(0, 6, 0, 0),
            };
            card.Controls.Add(categoryPanel);

            // Search bar
            var searchRow = new Panel { Dock = DockStyle.Top, Height = 46, BackColor = Color.Transparent };
            var searchCard = new Panel
            {
                Location  = new Point(0, 6),
                BackColor = Color.White,
            };
            searchCard.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            searchCard.Size   = new Size(card.Width - 40, 34);
            searchCard.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = UIHelper.RoundedPath(new Rectangle(0, 0, searchCard.Width - 1, searchCard.Height - 1), 8);
                using var bg   = new SolidBrush(Color.White);
                g.FillPath(bg, path);
                using var pen  = new Pen(UIHelper.Border, 1f);
                g.DrawPath(pen, path);
            };
            searchCard.Region = new Region(UIHelper.RoundedPath(new Rectangle(0, 0, searchCard.Width, 34), 8));
            card.Resize += (s, e) =>
            {
                searchCard.Width = card.Width - 40 - card.Padding.Horizontal;
                searchCard.Region = new Region(UIHelper.RoundedPath(new Rectangle(0, 0, searchCard.Width, 34), 8));
            };

            var searchIcon = new Label
            {
                Text      = "🔍",
                Font      = new Font("Segoe UI", 11),
                Location  = new Point(8, 4),
                Size      = new Size(24, 24),
                BackColor = Color.Transparent,
            };
            searchCard.Controls.Add(searchIcon);

            txtSearch = new TextBox
            {
                Font        = new Font("Segoe UI", 10),
                BorderStyle = BorderStyle.None,
                Location    = new Point(36, 8),
                BackColor   = Color.White,
                ForeColor   = UIHelper.TextDark,
                PlaceholderText = "Search menu...",
            };
            txtSearch.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtSearch.Width  = searchCard.Width - 44;
            searchCard.Resize += (s, e) => txtSearch.Width = searchCard.Width - 44;
            txtSearch.TextChanged += TxtSearch_TextChanged;
            searchCard.Controls.Add(txtSearch);
            searchRow.Controls.Add(searchCard);
            card.Controls.Add(searchRow);

            // Menu items grid
            menuPanel = new FlowLayoutPanel
            {
                Dock          = DockStyle.Fill,
                AutoScroll    = true,
                WrapContents  = true,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor     = Color.Transparent,
                Padding       = new Padding(0, 4, 0, 0),
            };
            card.Controls.Add(menuPanel);

            // Fix stacking order (Dock=Top items)
            var items = card.Controls.Cast<Control>().ToList();
            for (int i = 0; i < items.Count; i++)
                card.Controls.SetChildIndex(items[i], i);
        }

        // ─────────────────────────────────────────────────────────────
        // CART PANEL (right card)
        // ─────────────────────────────────────────────────────────────
        private void BuildCartPanel(Panel card)
        {
            card.Controls.Add(new Label
            {
                Text      = "Order Cart",
                Font      = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = UIHelper.TextDark,
                Dock      = DockStyle.Top,
                Height    = 38,
                BackColor = Color.Transparent,
            });
            card.Controls.Add(new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 1,
                BackColor = UIHelper.Border,
            });

            // Totals + buttons at bottom
            var bottomBar = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 222,
                BackColor = Color.Transparent,
            };
            BuildCartBottom(bottomBar);
            card.Controls.Add(bottomBar);

            // Cart items (scrollable, fills remaining space)
            cartItemsPanel = new Panel
            {
                Dock      = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.Transparent,
            };

            // Empty state label
            lblEmpty = new Label
            {
                Text      = "Cart is empty",
                Font      = new Font("Segoe UI", 11),
                ForeColor = UIHelper.TextMuted,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
            };
            cartItemsPanel.Controls.Add(lblEmpty);
            card.Controls.Add(cartItemsPanel);

            // Fix stacking order
            var items = card.Controls.Cast<Control>().ToList();
            for (int i = 0; i < items.Count; i++)
                card.Controls.SetChildIndex(items[i], i);
        }

        private void BuildCartBottom(Panel bar)
        {
            // Place Order button
            btnPlaceOrder = UIHelper.MakeButton("Place Order", UIHelper.Green, Color.White, 320, 42);
            btnPlaceOrder.Dock   = DockStyle.Bottom;
            btnPlaceOrder.Click += BtnPlaceOrder_Click;
            bar.Controls.Add(btnPlaceOrder);

            // Spacer
            bar.Controls.Add(new Panel { Dock = DockStyle.Bottom, Height = 8, BackColor = Color.Transparent });

            // Clear Cart button
            btnClearCart = UIHelper.MakeOutlineButton("Clear Cart", UIHelper.Red, 320, 36);
            btnClearCart.Dock   = DockStyle.Bottom;
            btnClearCart.Click += (s, e) =>
            {
                if (cartItems.Count > 0 && ValidationHelper.Confirm("Clear all items from the cart?"))
                {
                    cartItems.Clear();
                    RefreshCart();
                }
            };
            bar.Controls.Add(btnClearCart);

            // Spacer
            bar.Controls.Add(new Panel { Dock = DockStyle.Bottom, Height = 8, BackColor = Color.Transparent });

            // Totals
            lblTotal = new Label
            {
                Font      = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = UIHelper.Gold,
                Dock      = DockStyle.Bottom,
                Height    = 28,
                TextAlign = ContentAlignment.MiddleRight,
                BackColor = Color.Transparent,
            };
            bar.Controls.Add(lblTotal);

            // Discount input field (between Tax and Total)
            var discountPanel = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 36,
                BackColor = Color.FromArgb(242, 237, 228),
            };
            discountPanel.Resize += (s, e) =>
            {
                if (discountPanel.Width > 0)
                    discountPanel.Region = new Region(UIHelper.RoundedPath(
                        new Rectangle(0, 0, discountPanel.Width, discountPanel.Height), 8));
            };
            txtDiscount = new TextBox
            {
                Font        = new Font("Segoe UI", 11),
                BorderStyle = BorderStyle.None,
                BackColor   = Color.FromArgb(242, 237, 228),
                ForeColor   = UIHelper.TextDark,
                Text        = "0",
                TextAlign   = HorizontalAlignment.Center,
                Dock        = DockStyle.Fill,
            };
            txtDiscount.TextChanged += (s, e) => UpdateTotals();
            discountPanel.Controls.Add(txtDiscount);
            bar.Controls.Add(discountPanel);

            lblTax = new Label
            {
                Font      = new Font("Segoe UI", 10),
                ForeColor = UIHelper.TextMuted,
                Dock      = DockStyle.Bottom,
                Height    = 22,
                TextAlign = ContentAlignment.MiddleRight,
                BackColor = Color.Transparent,
            };
            bar.Controls.Add(lblTax);

            lblSubtotal = new Label
            {
                Font      = new Font("Segoe UI", 10),
                ForeColor = UIHelper.TextMuted,
                Dock      = DockStyle.Bottom,
                Height    = 22,
                TextAlign = ContentAlignment.MiddleRight,
                BackColor = Color.Transparent,
            };
            bar.Controls.Add(lblSubtotal);

            // Fix stacking
            var items = bar.Controls.Cast<Control>().ToList();
            for (int i = 0; i < items.Count; i++)
                bar.Controls.SetChildIndex(items[i], i);

            UpdateTotals();
        }

        // ═════════════════════════════════════════════════════════════
        // LOAD
        // ═════════════════════════════════════════════════════════════
        private void OrderForm_Load(object? sender, EventArgs e)
        {
            try
            {
                LoadTables();
                LoadCategories();
                LoadMenuItems();
                SetOrderType("Dine-In");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading order form:\n{ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadTables()
        {
            cmbTable.Items.Clear();
            foreach (var t in TableDAL.GetAll())
                cmbTable.Items.Add(t);
            if (cmbTable.Items.Count > 0)
                cmbTable.SelectedIndex = 0;
        }

        private void LoadCategories()
        {
            categoryPanel.Controls.Clear();
            activeCategoryBtn = null;

            var allBtn = MakeCategoryPill("All", -1);
            categoryPanel.Controls.Add(allBtn);
            SetActiveCategory(allBtn);

            foreach (var cat in CategoryDAL.GetAll())
                categoryPanel.Controls.Add(MakeCategoryPill(cat.Name, cat.CategoryId));
        }

        private void LoadMenuItems(int? categoryId = null)
        {
            menuPanel.Controls.Clear();
            var items = categoryId.HasValue
                ? MenuItemDAL.GetByCategory(categoryId.Value)
                : MenuItemDAL.GetAll();

            foreach (var item in items)
                menuPanel.Controls.Add(MakeMenuItemCard(item));
        }

        // ═════════════════════════════════════════════════════════════
        // CATEGORY PILLS
        // ═════════════════════════════════════════════════════════════
        private Button MakeCategoryPill(string name, int categoryId)
        {
            var btn = new Button
            {
                Text      = name,
                Font      = new Font("Segoe UI", 9.5f),
                ForeColor = UIHelper.TextDark,
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Height    = 32,
                Width     = TextRenderer.MeasureText(name, new Font("Segoe UI", 9.5f)).Width + 28,
                Cursor    = Cursors.Hand,
                Margin    = new Padding(0, 0, 8, 0),
                Tag       = categoryId,
            };
            btn.FlatAppearance.BorderSize  = 1;
            btn.FlatAppearance.BorderColor = UIHelper.Border;
            btn.Region = new Region(UIHelper.RoundedPath(new Rectangle(0, 0, btn.Width, btn.Height), 16));

            btn.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                bool active  = (btn == activeCategoryBtn);
                using var path  = UIHelper.RoundedPath(new Rectangle(0, 0, btn.Width - 1, btn.Height - 1), 16);
                using var bg    = new SolidBrush(active ? UIHelper.Gold : Color.White);
                g.FillPath(bg, path);
                if (!active)
                {
                    using var pen = new Pen(UIHelper.Border, 1f);
                    g.DrawPath(pen, path);
                }
                TextRenderer.DrawText(e.Graphics, btn.Text, btn.Font, btn.ClientRectangle,
                    active ? Color.White : UIHelper.TextDark,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };

            btn.Click += (s, e) =>
            {
                SetActiveCategory(btn);
                int id = (int)btn.Tag!;
                LoadMenuItems(id == -1 ? null : id);
            };

            return btn;
        }

        private void SetActiveCategory(Button btn)
        {
            activeCategoryBtn?.Invalidate();
            activeCategoryBtn = btn;
            btn.Invalidate();
        }

        // ═════════════════════════════════════════════════════════════
        // MENU ITEM CARDS
        // ═════════════════════════════════════════════════════════════
        private Panel MakeMenuItemCard(MenuItem item)
        {
            int colorIdx  = Math.Abs((item.CategoryName ?? "").GetHashCode()) % UIHelper.CategoryColors.Length;
            var stripe    = UIHelper.CategoryColors[colorIdx];

            var card = new Panel
            {
                Size      = new Size(186, 110),
                BackColor = Color.Transparent,
                Cursor    = Cursors.Hand,
                Margin    = new Padding(0, 0, 12, 12),
            };
            card.Region = new Region(UIHelper.RoundedPath(new Rectangle(0, 0, 186, 110), 10));

            card.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var path  = UIHelper.RoundedPath(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 10);
                using var bg    = new SolidBrush(Color.White);
                g.FillPath(bg, path);
                using var pen   = new Pen(UIHelper.Border, 1f);
                g.DrawPath(pen, path);
                // Top stripe
                using var stripeClip = UIHelper.RoundedPath(new Rectangle(0, 0, card.Width, 14), 10);
                g.SetClip(stripeClip);
                g.FillRectangle(new SolidBrush(stripe), 0, 0, card.Width, 5);
                g.ResetClip();
            };

            // Item name
            card.Controls.Add(new Label
            {
                Text      = item.Name,
                Font      = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = UIHelper.TextDark,
                Location  = new Point(10, 14),
                Size      = new Size(145, 38),
                BackColor = Color.Transparent,
            });

            // Price
            card.Controls.Add(new Label
            {
                Text      = $"${item.Price:F2}",
                Font      = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = UIHelper.Gold,
                Location  = new Point(10, 76),
                Size      = new Size(80, 22),
                BackColor = Color.Transparent,
            });

            // "+" button (gold circle, bottom-right)
            var addBtn = new Panel
            {
                Location  = new Point(148, 72),
                Size      = new Size(30, 30),
                BackColor = Color.Transparent,
                Cursor    = Cursors.Hand,
            };
            addBtn.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var brush = new SolidBrush(UIHelper.Gold);
                g.FillEllipse(brush, 0, 0, 29, 29);
                TextRenderer.DrawText(e.Graphics, "+", new Font("Segoe UI", 14, FontStyle.Bold),
                    addBtn.ClientRectangle, Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };

            // Hover effect on the card
            void OnEnter(object? s, EventArgs e) => card.Invalidate();
            void OnLeave(object? s, EventArgs e) => card.Invalidate();
            void AddItem(object? s, EventArgs e) => AddToCart(item);

            card.MouseEnter += OnEnter;
            card.MouseLeave += OnLeave;
            addBtn.Click    += AddItem;
            card.Click      += AddItem;
            foreach (Control c in card.Controls) c.Click += AddItem;

            card.Controls.Add(addBtn);
            return card;
        }

        // ═════════════════════════════════════════════════════════════
        // CART LOGIC
        // ═════════════════════════════════════════════════════════════
        private void AddToCart(MenuItem item)
        {
            var existing = cartItems.FirstOrDefault(ci => ci.ItemId == item.ItemId);
            if (existing != null)
                existing.Quantity++;
            else
                cartItems.Add(new OrderItem
                {
                    ItemId    = item.ItemId,
                    ItemName  = item.Name,
                    Quantity  = 1,
                    UnitPrice = item.Price,
                });
            RefreshCart();
        }

        private void RefreshCart()
        {
            cartItemsPanel.Controls.Clear();
            lblEmpty.Visible = cartItems.Count == 0;

            if (cartItems.Count == 0)
            {
                cartItemsPanel.Controls.Add(lblEmpty);
            }
            else
            {
                int y = 0;
                foreach (var ci in cartItems)
                {
                    var row = MakeCartRow(ci);
                    row.Location = new Point(0, y);
                    cartItemsPanel.Controls.Add(row);
                    y += row.Height;
                }
            }
            UpdateTotals();
        }

        private Panel MakeCartRow(OrderItem item)
        {
            var row = new Panel
            {
                Size      = new Size(cartItemsPanel.Width - 4, 52),
                BackColor = Color.Transparent,
                Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            };
            row.Paint += (s, e) =>
            {
                using var pen = new Pen(UIHelper.Border);
                e.Graphics.DrawLine(pen, 0, row.Height - 1, row.Width, row.Height - 1);
            };

            // Item name
            row.Controls.Add(new Label
            {
                Text      = item.ItemName,
                Font      = new Font("Segoe UI", 9.5f),
                ForeColor = UIHelper.TextDark,
                Location  = new Point(0, 8),
                Size      = new Size(120, 18),
                BackColor = Color.Transparent,
            });
            row.Controls.Add(new Label
            {
                Text      = $"${item.UnitPrice:F2}",
                Font      = new Font("Segoe UI", 8.5f),
                ForeColor = UIHelper.TextMuted,
                Location  = new Point(0, 28),
                Size      = new Size(80, 16),
                BackColor = Color.Transparent,
            });

            // Quantity stepper — | - | qty | + |
            var minus = MakeQtyBtn("−");
            minus.Location = new Point(130, 14);
            minus.Click += (s, e) =>
            {
                if (item.Quantity > 1) { item.Quantity--; RefreshCart(); }
                else { cartItems.Remove(item); RefreshCart(); }
            };
            row.Controls.Add(minus);

            var qtyLbl = new Label
            {
                Text      = item.Quantity.ToString(),
                Font      = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = UIHelper.TextDark,
                Location  = new Point(156, 14),
                Size      = new Size(26, 24),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
            };
            row.Controls.Add(qtyLbl);

            var plus = MakeQtyBtn("+");
            plus.Location = new Point(184, 14);
            plus.Click += (s, e) => { item.Quantity++; RefreshCart(); };
            row.Controls.Add(plus);

            // Line total
            row.Controls.Add(new Label
            {
                Text      = $"${item.LineTotal:F2}",
                Font      = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = UIHelper.TextDark,
                Location  = new Point(220, 15),
                Size      = new Size(70, 22),
                TextAlign = ContentAlignment.MiddleRight,
                BackColor = Color.Transparent,
            });

            // Remove (×)
            var remove = new Label
            {
                Text      = "×",
                Font      = new Font("Segoe UI", 14),
                ForeColor = Color.FromArgb(200, 190, 180),
                Location  = new Point(295, 12),
                Size      = new Size(22, 26),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor    = Cursors.Hand,
                BackColor = Color.Transparent,
            };
            remove.Click      += (s, e) => { cartItems.Remove(item); RefreshCart(); };
            remove.MouseEnter += (s, e) => remove.ForeColor = UIHelper.Red;
            remove.MouseLeave += (s, e) => remove.ForeColor = Color.FromArgb(200, 190, 180);
            row.Controls.Add(remove);

            return row;
        }

        private static Label MakeQtyBtn(string text)
        {
            var lbl = new Label
            {
                Text      = text,
                Font      = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = UIHelper.Gold,
                Size      = new Size(24, 24),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor    = Cursors.Hand,
                BackColor = Color.Transparent,
            };
            lbl.MouseEnter += (s, e) => lbl.ForeColor = ControlPaint.Dark(UIHelper.Gold, 0.1f);
            lbl.MouseLeave += (s, e) => lbl.ForeColor = UIHelper.Gold;
            return lbl;
        }

        private void UpdateTotals()
        {
            decimal subtotal = cartItems.Sum(i => i.LineTotal);
            decimal tax      = Math.Round(subtotal * OrderService.TaxRate, 2);
            decimal discount = decimal.TryParse(txtDiscount?.Text, out decimal d) && d >= 0 ? d : 0;
            decimal total    = Math.Max(0, subtotal + tax - discount);

            lblSubtotal.Text = $"Subtotal    ${subtotal:F2}";
            lblTax.Text      = $"Tax (10%)   ${tax:F2}";
            lblTotal.Text    = $"Total   ${total:F2}";
        }

        // ═════════════════════════════════════════════════════════════
        // ORDER TYPE TOGGLE
        // ═════════════════════════════════════════════════════════════
        private void SetOrderType(string type)
        {
            bool dineIn = type == "Dine-In";
            cmbTable.Enabled = dineIn;
            btnDineIn.Tag    = dineIn ? "active" : "";
            btnTakeaway.Tag  = dineIn ? "" : "active";
            btnDineIn.Invalidate();
            btnTakeaway.Invalidate();
        }

        private Button MakeToggleButton(string text, bool active)
        {
            var btn = new Button
            {
                Text      = text,
                Font      = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.Transparent,
                ForeColor = UIHelper.TextDark,
                FlatStyle = FlatStyle.Flat,
                Cursor    = Cursors.Hand,
                Tag       = active ? "active" : "",
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Region = new Region(UIHelper.RoundedPath(new Rectangle(0, 0, 110, 36), 8));
            btn.Resize += (s, e) =>
                btn.Region = new Region(UIHelper.RoundedPath(new Rectangle(0, 0, btn.Width, btn.Height), 8));
            btn.Paint += (s, e) =>
            {
                bool isActive = btn.Tag?.ToString() == "active";
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var path  = UIHelper.RoundedPath(new Rectangle(0, 0, btn.Width - 1, btn.Height - 1), 8);
                using var bg    = new SolidBrush(isActive ? UIHelper.Gold : Color.White);
                g.FillPath(bg, path);
                if (!isActive)
                {
                    using var pen = new Pen(UIHelper.Border, 1f);
                    g.DrawPath(pen, path);
                }
                TextRenderer.DrawText(e.Graphics, btn.Text, btn.Font, btn.ClientRectangle,
                    isActive ? Color.White : UIHelper.TextDark,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            return btn;
        }

        // ═════════════════════════════════════════════════════════════
        // SEARCH
        // ═════════════════════════════════════════════════════════════
        private void TxtSearch_TextChanged(object? sender, EventArgs e)
        {
            string term = txtSearch.Text.Trim();
            if (string.IsNullOrEmpty(term)) { LoadMenuItems(); return; }
            menuPanel.Controls.Clear();
            foreach (var item in MenuItemDAL.Search(term))
                menuPanel.Controls.Add(MakeMenuItemCard(item));
        }

        // ═════════════════════════════════════════════════════════════
        // PLACE ORDER
        // ═════════════════════════════════════════════════════════════
        private void BtnPlaceOrder_Click(object? sender, EventArgs e)
        {
            try
            {
                if (cartItems.Count == 0)
                { ValidationHelper.ShowError("Add at least one item to the cart."); return; }

                string orderType = btnDineIn.Tag?.ToString() == "active" ? "Dine-In" : "Takeaway";
                int? tableId = null;

                if (orderType == "Dine-In")
                {
                    if (cmbTable.SelectedItem is not Table selectedTable)
                    { ValidationHelper.ShowError("Please select a table."); return; }
                    if (selectedTable.Status != "Available")
                    { ValidationHelper.ShowError($"Table {selectedTable.TableNumber} is {selectedTable.Status}."); return; }
                    tableId = selectedTable.TableId;
                }

                var order = new Order
                {
                    TableId   = tableId,
                    UserId    = AuthService.CurrentUser?.UserId ?? 0,
                    OrderType = orderType,
                    Items     = new List<OrderItem>(cartItems),
                };

                int orderId = OrderService.PlaceOrder(order);
                ValidationHelper.ShowSuccess($"Order #{orderId} placed successfully!");

                cartItems.Clear();
                RefreshCart();
                LoadTables();

                if (ValidationHelper.Confirm("Open billing for this order?"))
                    new BillingForm(orderId).ShowDialog();
            }
            catch (Exception ex)
            {
                ValidationHelper.ShowCriticalError($"Failed to place order:\n{ex.Message}");
            }
        }
    }
}
