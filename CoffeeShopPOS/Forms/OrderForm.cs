// OrderForm.cs — POS Order Screen (Core Workflow)
//
// The heart of the POS system — where orders are created.
// Workflow: Select table → Browse menu → Add items → Place order
// Connects to: OrderService.cs (place order), MenuItemDAL.cs (browse items),
//              CategoryDAL.cs (category tabs), TableDAL.cs (table selection),
//              BillingForm.cs (opened after placing order), AuthService.cs (user ID)

using CoffeeShopPOS.Database;
using CoffeeShopPOS.Models;
using CoffeeShopPOS.Services;
using CoffeeShopPOS.Helpers;

namespace CoffeeShopPOS.Forms
{
    /// <summary>
    /// Main POS order screen with menu browsing, cart management, and order placement.
    /// This form handles the core business workflow of the coffee shop.
    /// </summary>
    public class OrderForm : Form
    {
        // ── Color scheme ──
        private readonly Color bgColor = Color.FromArgb(245, 242, 238);
        private readonly Color cardBg = Color.White;
        private readonly Color textDark = Color.FromArgb(50, 40, 35);
        private readonly Color textMuted = Color.FromArgb(140, 130, 120);
        private readonly Color accentGold = Color.FromArgb(193, 154, 107);
        private readonly Color successGreen = Color.FromArgb(76, 175, 80);
        private readonly Color dangerRed = Color.FromArgb(244, 67, 54);

        // ── UI Controls ──
        private ComboBox cmbTable = null!;
        private ComboBox cmbOrderType = null!;
        private FlowLayoutPanel categoryPanel = null!;
        private FlowLayoutPanel menuItemsPanel = null!;
        private DataGridView dgvCart = null!;
        private TextBox txtSearch = null!;
        private Label lblSubtotal = null!;
        private Label lblTax = null!;
        private Label lblTotal = null!;
        private Button btnPlaceOrder = null!;
        private Button btnClearCart = null!;

        // ── Cart state — stores items added to the current order ──
        // This list is populated from the menu item buttons and displayed in dgvCart
        private List<OrderItem> cartItems = new List<OrderItem>();

        public OrderForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "New Order";
            this.BackColor = bgColor;
            this.Dock = DockStyle.Fill;
            this.Padding = new Padding(15);

            // ══════════════════════════════════════════════════════════════
            // LEFT PANEL — Menu browsing (categories + items)
            // ══════════════════════════════════════════════════════════════
            var leftPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 0, 10, 0),
            };

            // ── Header with title and search ──
            var menuHeader = new Panel { Dock = DockStyle.Top, Height = 50 };

            var lblTitle = new Label
            {
                Text = "🛒 New Order",
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = textDark,
                AutoSize = true,
                Location = new Point(5, 8),
            };
            menuHeader.Controls.Add(lblTitle);

            // Search box for quick item lookup — searches MenuItemDAL.Search()
            txtSearch = new TextBox
            {
                Font = new Font("Segoe UI", 11),
                PlaceholderText = "🔍 Search menu items...",
                Size = new Size(250, 30),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BackColor = cardBg,
                BorderStyle = BorderStyle.FixedSingle,
            };
            txtSearch.TextChanged += TxtSearch_TextChanged;  // Live search as user types
            menuHeader.Controls.Add(txtSearch);

            leftPanel.Controls.Add(menuHeader);

            // ── Order type and table selection ──
            var orderConfigPanel = new Panel { Dock = DockStyle.Top, Height = 50 };

            // Order type dropdown — determines if a table is needed
            cmbOrderType = new ComboBox
            {
                Font = new Font("Segoe UI", 11),
                Location = new Point(5, 8),
                Size = new Size(150, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = cardBg,
            };
            cmbOrderType.Items.AddRange(new[] { "Dine-In", "Takeaway" });
            cmbOrderType.SelectedIndex = 0;
            // When order type changes, toggle table selection visibility
            cmbOrderType.SelectedIndexChanged += (s, e) =>
            {
                cmbTable.Enabled = cmbOrderType.SelectedItem?.ToString() == "Dine-In";
            };
            orderConfigPanel.Controls.Add(cmbOrderType);

            // Table selection dropdown — populated from TableDAL.GetAvailable()
            cmbTable = new ComboBox
            {
                Font = new Font("Segoe UI", 11),
                Location = new Point(170, 8),
                Size = new Size(200, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = cardBg,
            };
            orderConfigPanel.Controls.Add(cmbTable);

            leftPanel.Controls.Add(orderConfigPanel);

            // ── Category filter buttons — loaded from CategoryDAL.GetAll() ──
            categoryPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 50,
                AutoScroll = true,
                WrapContents = false,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(0, 5, 0, 5),
            };
            leftPanel.Controls.Add(categoryPanel);

            // ── Menu items grid — displays available items as clickable cards ──
            menuItemsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                WrapContents = true,
                BackColor = bgColor,
                Padding = new Padding(5),
            };
            leftPanel.Controls.Add(menuItemsPanel);

            // ══════════════════════════════════════════════════════════════
            // RIGHT PANEL — Cart / order summary
            // ══════════════════════════════════════════════════════════════
            var rightPanel = new Panel
            {
                Dock = DockStyle.Right,
                Width = 380,
                BackColor = cardBg,
                Padding = new Padding(15),
            };

            var cartTitle = new Label
            {
                Text = "🛍 Order Cart",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = textDark,
                Dock = DockStyle.Top,
                Height = 40,
            };
            rightPanel.Controls.Add(cartTitle);

            // Cart grid — shows items added to the current order
            dgvCart = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = false,  // Allow quantity editing
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = cardBg,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Color.FromArgb(230, 225, 220),
                RowHeadersVisible = false,
                EnableHeadersVisualStyles = false,
                Font = new Font("Segoe UI", 10),
            };

            dgvCart.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(45, 38, 32),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
            };
            dgvCart.ColumnHeadersHeight = 36;
            dgvCart.RowTemplate.Height = 34;
            dgvCart.DefaultCellStyle = new DataGridViewCellStyle
            {
                SelectionBackColor = Color.FromArgb(240, 230, 218),
                SelectionForeColor = textDark,
            };

            // Handle quantity changes in the cart
            dgvCart.CellValueChanged += DgvCart_CellValueChanged;
            dgvCart.UserDeletedRow += (s, e) => UpdateCartTotals();
            rightPanel.Controls.Add(dgvCart);

            // ── Totals section ──
            var totalsPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 180,
                BackColor = cardBg,
                Padding = new Padding(5),
            };

            lblSubtotal = new Label
            {
                Text = "Subtotal: $0.00",
                Font = new Font("Segoe UI", 11),
                ForeColor = textMuted,
                Dock = DockStyle.Top,
                Height = 28,
                TextAlign = ContentAlignment.MiddleRight,
            };
            totalsPanel.Controls.Add(lblSubtotal);

            lblTax = new Label
            {
                Text = "Tax (10%): $0.00",
                Font = new Font("Segoe UI", 11),
                ForeColor = textMuted,
                Dock = DockStyle.Top,
                Height = 28,
                TextAlign = ContentAlignment.MiddleRight,
            };
            totalsPanel.Controls.Add(lblTax);

            lblTotal = new Label
            {
                Text = "Total: $0.00",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = textDark,
                Dock = DockStyle.Top,
                Height = 35,
                TextAlign = ContentAlignment.MiddleRight,
            };
            totalsPanel.Controls.Add(lblTotal);

            // Place Order button — triggers OrderService.PlaceOrder()
            btnPlaceOrder = new Button
            {
                Text = "✓  Place Order",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = successGreen,
                FlatStyle = FlatStyle.Flat,
                Dock = DockStyle.Bottom,
                Height = 45,
                Cursor = Cursors.Hand,
            };
            btnPlaceOrder.FlatAppearance.BorderSize = 0;
            btnPlaceOrder.Click += BtnPlaceOrder_Click;
            totalsPanel.Controls.Add(btnPlaceOrder);

            // Clear Cart button
            btnClearCart = new Button
            {
                Text = "🗑  Clear Cart",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.White,
                BackColor = dangerRed,
                FlatStyle = FlatStyle.Flat,
                Dock = DockStyle.Bottom,
                Height = 35,
                Cursor = Cursors.Hand,
            };
            btnClearCart.FlatAppearance.BorderSize = 0;
            btnClearCart.Click += (s, e) =>
            {
                if (cartItems.Count > 0 && ValidationHelper.Confirm("Clear all items from the cart?"))
                {
                    cartItems.Clear();
                    RefreshCartDisplay();
                }
            };
            totalsPanel.Controls.Add(btnClearCart);

            rightPanel.Controls.Add(totalsPanel);

            // Add panels to form (right panel first so it docks properly)
            this.Controls.Add(leftPanel);
            this.Controls.Add(rightPanel);

            this.Load += OrderForm_Load;
        }

        /// <summary>
        /// Form load — loads categories, tables, and initial menu items.
        /// </summary>
        private void OrderForm_Load(object? sender, EventArgs e)
        {
            try
            {
                LoadCategories();
                LoadTables();
                LoadMenuItems();  // Load all items initially
                InitializeCartColumns();

                // Position search box
                txtSearch.Location = new Point(
                    txtSearch.Parent!.Width - txtSearch.Width - 10, 8);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading order form: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Loads category filter buttons from CategoryDAL.GetAll().
        /// Each button filters the menu items panel to show only items in that category.
        /// </summary>
        private void LoadCategories()
        {
            categoryPanel.Controls.Clear();

            // "All" button to show all items
            var btnAll = CreateCategoryButton("All", -1);
            categoryPanel.Controls.Add(btnAll);

            // Load categories from database via CategoryDAL.cs
            var categories = CategoryDAL.GetAll();
            foreach (var cat in categories)
            {
                var btn = CreateCategoryButton(cat.Name, cat.CategoryId);
                categoryPanel.Controls.Add(btn);
            }
        }

        /// <summary>
        /// Creates a styled category filter button.
        /// </summary>
        private Button CreateCategoryButton(string name, int categoryId)
        {
            var btn = new Button
            {
                Text = name,
                Font = new Font("Segoe UI", 10),
                ForeColor = textDark,
                BackColor = cardBg,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(name.Length * 10 + 30, 36),
                Cursor = Cursors.Hand,
                Tag = categoryId,  // Store category ID for filtering
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(200, 195, 190);
            btn.FlatAppearance.BorderSize = 1;

            // Hover effects
            btn.MouseEnter += (s, e) => { btn.BackColor = accentGold; btn.ForeColor = Color.White; };
            btn.MouseLeave += (s, e) => { btn.BackColor = cardBg; btn.ForeColor = textDark; };

            // Click — filter menu items by category
            btn.Click += (s, e) =>
            {
                if (categoryId == -1)
                    LoadMenuItems();  // Show all
                else
                    LoadMenuItems(categoryId);  // Filter by category
            };

            return btn;
        }

        /// <summary>
        /// Loads menu items into the items panel as clickable cards.
        /// Items are loaded from MenuItemDAL.cs — either all or filtered by category.
        /// </summary>
        private void LoadMenuItems(int? categoryId = null)
        {
            menuItemsPanel.Controls.Clear();

            List<MenuItem> items;
            if (categoryId.HasValue)
                items = MenuItemDAL.GetByCategory(categoryId.Value);  // Filtered
            else
                items = MenuItemDAL.GetAll();  // All available items

            foreach (var item in items)
            {
                var card = CreateMenuItemCard(item);
                menuItemsPanel.Controls.Add(card);
            }
        }

        /// <summary>
        /// Creates a clickable menu item card for the POS screen.
        /// Clicking adds the item to the cart.
        /// </summary>
        private Panel CreateMenuItemCard(MenuItem item)
        {
            var card = new Panel
            {
                Size = new Size(170, 110),
                BackColor = cardBg,
                Cursor = Cursors.Hand,
                Margin = new Padding(5),
                Tag = item,  // Store the MenuItem for click handler
            };

            var lblName = new Label
            {
                Text = item.Name,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = textDark,
                Location = new Point(10, 10),
                Size = new Size(150, 40),
                BackColor = Color.Transparent,
            };
            card.Controls.Add(lblName);

            var lblCategory = new Label
            {
                Text = item.CategoryName ?? "",
                Font = new Font("Segoe UI", 8),
                ForeColor = textMuted,
                Location = new Point(10, 52),
                Size = new Size(150, 18),
                BackColor = Color.Transparent,
            };
            card.Controls.Add(lblCategory);

            var lblPrice = new Label
            {
                Text = $"${item.Price:F2}",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = accentGold,
                Location = new Point(10, 72),
                Size = new Size(150, 30),
                BackColor = Color.Transparent,
            };
            card.Controls.Add(lblPrice);

            // Click handler — add item to cart
            void addToCart(object? s, EventArgs e) => AddItemToCart(item);
            card.Click += addToCart;
            lblName.Click += addToCart;
            lblCategory.Click += addToCart;
            lblPrice.Click += addToCart;

            // Hover effects for the card
            void mouseEnter(object? s, EventArgs e) =>
                card.BackColor = Color.FromArgb(252, 248, 242);
            void mouseLeave(object? s, EventArgs e) =>
                card.BackColor = cardBg;

            card.MouseEnter += mouseEnter;
            card.MouseLeave += mouseLeave;
            lblName.MouseEnter += mouseEnter;
            lblName.MouseLeave += mouseLeave;
            lblPrice.MouseEnter += mouseEnter;
            lblPrice.MouseLeave += mouseLeave;

            return card;
        }

        /// <summary>
        /// Adds a menu item to the cart. If already in cart, increments quantity.
        /// Called when a menu item card is clicked.
        /// </summary>
        private void AddItemToCart(MenuItem item)
        {
            // Check if item is already in the cart
            var existing = cartItems.FirstOrDefault(ci => ci.ItemId == item.ItemId);
            if (existing != null)
            {
                existing.Quantity++;  // Increment quantity if already in cart
            }
            else
            {
                // Add new cart item with the current price snapshot
                cartItems.Add(new OrderItem
                {
                    ItemId = item.ItemId,
                    ItemName = item.Name,
                    Quantity = 1,
                    UnitPrice = item.Price,  // Snapshot price at time of adding
                });
            }

            RefreshCartDisplay();
        }

        /// <summary>
        /// Sets up the DataGridView columns for the cart display.
        /// </summary>
        private void InitializeCartColumns()
        {
            dgvCart.Columns.Clear();
            dgvCart.Columns.Add(new DataGridViewTextBoxColumn
            { Name = "Item", HeaderText = "Item", ReadOnly = true, Width = 140 });
            dgvCart.Columns.Add(new DataGridViewTextBoxColumn
            { Name = "Qty", HeaderText = "Qty", Width = 50 });
            dgvCart.Columns.Add(new DataGridViewTextBoxColumn
            { Name = "Price", HeaderText = "Price", ReadOnly = true, Width = 70 });
            dgvCart.Columns.Add(new DataGridViewTextBoxColumn
            { Name = "Total", HeaderText = "Total", ReadOnly = true, Width = 80 });
        }

        /// <summary>
        /// Refreshes the cart DataGridView and updates totals.
        /// Called after adding/removing items or changing quantities.
        /// </summary>
        private void RefreshCartDisplay()
        {
            dgvCart.Rows.Clear();
            foreach (var item in cartItems)
            {
                dgvCart.Rows.Add(item.ItemName, item.Quantity,
                    $"${item.UnitPrice:F2}", $"${item.LineTotal:F2}");
            }
            UpdateCartTotals();
        }

        /// <summary>
        /// Updates the subtotal, tax, and total labels based on cart contents.
        /// Uses the same tax rate as OrderService.TaxRate for consistency.
        /// </summary>
        private void UpdateCartTotals()
        {
            decimal subtotal = cartItems.Sum(i => i.LineTotal);
            decimal tax = Math.Round(subtotal * OrderService.TaxRate, 2);
            decimal total = subtotal + tax;

            lblSubtotal.Text = $"Subtotal: ${subtotal:F2}";
            lblTax.Text = $"Tax ({OrderService.TaxRate:P0}): ${tax:F2}";
            lblTotal.Text = $"Total: ${total:F2}";
        }

        /// <summary>
        /// Handles quantity changes in the cart DataGridView.
        /// </summary>
        private void DgvCart_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == 1 && e.RowIndex < cartItems.Count)
            {
                if (int.TryParse(dgvCart.Rows[e.RowIndex].Cells[1].Value?.ToString(), out int newQty) && newQty > 0)
                {
                    cartItems[e.RowIndex].Quantity = newQty;
                    dgvCart.Rows[e.RowIndex].Cells[3].Value = $"${cartItems[e.RowIndex].LineTotal:F2}";
                    UpdateCartTotals();
                }
            }
        }

        /// <summary>
        /// Search handler — filters menu items as user types.
        /// Calls MenuItemDAL.Search() for database-level search.
        /// </summary>
        private void TxtSearch_TextChanged(object? sender, EventArgs e)
        {
            string searchTerm = txtSearch.Text.Trim();
            if (string.IsNullOrEmpty(searchTerm))
            {
                LoadMenuItems();  // Show all if search is empty
                return;
            }

            menuItemsPanel.Controls.Clear();
            var results = MenuItemDAL.Search(searchTerm);
            foreach (var item in results)
            {
                menuItemsPanel.Controls.Add(CreateMenuItemCard(item));
            }
        }

        /// <summary>
        /// Loads available tables into the table dropdown.
        /// Uses TableDAL.GetAll() to show all tables with their status.
        /// </summary>
        private void LoadTables()
        {
            cmbTable.Items.Clear();
            var tables = TableDAL.GetAll();
            foreach (var table in tables)
            {
                cmbTable.Items.Add(table);
            }
            if (cmbTable.Items.Count > 0)
                cmbTable.SelectedIndex = 0;
        }

        /// <summary>
        /// Place Order button click — creates the order via OrderService.PlaceOrder().
        /// This is the main business action of the POS system.
        /// </summary>
        private void BtnPlaceOrder_Click(object? sender, EventArgs e)
        {
            try
            {
                // Validate cart is not empty
                if (cartItems.Count == 0)
                {
                    ValidationHelper.ShowError("Please add at least one item to the cart.");
                    return;
                }

                // Validate table selection for dine-in orders
                string orderType = cmbOrderType.SelectedItem?.ToString() ?? "Dine-In";
                int? tableId = null;

                if (orderType == "Dine-In")
                {
                    if (cmbTable.SelectedItem == null)
                    {
                        ValidationHelper.ShowError("Please select a table for dine-in orders.");
                        return;
                    }
                    var selectedTable = (Table)cmbTable.SelectedItem;
                    tableId = selectedTable.TableId;

                    // Check if table is available
                    if (selectedTable.Status != "Available")
                    {
                        ValidationHelper.ShowError($"Table {selectedTable.TableNumber} is currently {selectedTable.Status}.");
                        return;
                    }
                }

                // Build the Order object from cart data
                var order = new Order
                {
                    TableId = tableId,
                    // UserId comes from the currently logged-in user via AuthService.cs
                    UserId = AuthService.CurrentUser?.UserId ?? 0,
                    OrderType = orderType,
                    Items = new List<OrderItem>(cartItems),  // Copy cart items
                };

                // Place the order via OrderService.cs → OrderDAL.cs → MySQL
                int orderId = OrderService.PlaceOrder(order);

                // Success — show confirmation and open billing
                ValidationHelper.ShowSuccess($"Order #{orderId} placed successfully!");

                // Clear the cart after successful order
                cartItems.Clear();
                RefreshCartDisplay();
                LoadTables();  // Refresh table availability

                // Open BillingForm for the new order
                if (ValidationHelper.Confirm("Open billing for this order?"))
                {
                    var billingForm = new BillingForm(orderId);
                    billingForm.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                ValidationHelper.ShowCriticalError($"Failed to place order:\n{ex.Message}");
            }
        }
    }
}
