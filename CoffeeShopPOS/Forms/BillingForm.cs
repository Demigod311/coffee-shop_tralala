// BillingForm.cs — Invoice / Receipt / Payment Screen
//
// Displays an itemized bill for an order and handles payment processing.
// Opened from OrderForm.cs after placing an order, or from OrderHistoryForm.cs.
// Connects to: OrderDAL.cs (load order details), OrderService.cs (apply discount, complete payment),
//              ExportHelper.cs (future: print receipt)

using CoffeeShopPOS.Database;
using CoffeeShopPOS.Models;
using CoffeeShopPOS.Services;
using CoffeeShopPOS.Helpers;

namespace CoffeeShopPOS.Forms
{
    /// <summary>
    /// Invoice/receipt form showing itemized bill with payment processing.
    /// Displays order items, totals, and allows discount/payment method selection.
    /// </summary>
    public class BillingForm : Form
    {
        private readonly int _orderId;
        private Order? _order;

        // ── UI Controls ──
        private DataGridView dgvItems = null!;
        private Label lblOrderId = null!;
        private Label lblOrderType = null!;
        private Label lblTable = null!;
        private Label lblStaff = null!;
        private Label lblDate = null!;
        private Label lblSubtotal = null!;
        private Label lblTax = null!;
        private Label lblDiscount = null!;
        private Label lblTotal = null!;
        private TextBox txtDiscount = null!;
        private ComboBox cmbPayment = null!;
        private Button btnApplyDiscount = null!;
        private Button btnComplete = null!;
        private Button btnPrint = null!;

        // ── Color scheme ──
        private readonly Color bgColor = Color.FromArgb(250, 248, 245);
        private readonly Color cardBg = Color.White;
        private readonly Color textDark = Color.FromArgb(50, 40, 35);
        private readonly Color textMuted = Color.FromArgb(140, 130, 120);
        private readonly Color accentGold = Color.FromArgb(193, 154, 107);
        private readonly Color successGreen = Color.FromArgb(76, 175, 80);

        /// <summary>
        /// Creates the billing form for a specific order.
        /// The orderId is used to load order details from OrderDAL.GetById().
        /// </summary>
        /// <param name="orderId">ID of the order to display (from OrderForm.cs)</param>
        public BillingForm(int orderId)
        {
            _orderId = orderId;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = $"Bill — Order #{_orderId}";
            this.Size = new Size(550, 750);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = bgColor;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = cardBg,
                Padding = new Padding(25),
            };
            this.Controls.Add(mainPanel);

            // ── Receipt Header ──
            var lblTitle = new Label
            {
                Text = "☕ Coffee Shop",
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = accentGold,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 45,
            };
            mainPanel.Controls.Add(lblTitle);

            var lblReceiptTitle = new Label
            {
                Text = "— RECEIPT —",
                Font = new Font("Segoe UI", 10),
                ForeColor = textMuted,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 25,
            };
            mainPanel.Controls.Add(lblReceiptTitle);

            // ── Order Info Section ──
            var infoPanel = new Panel { Dock = DockStyle.Top, Height = 100, Padding = new Padding(0, 10, 0, 10) };

            lblOrderId = CreateInfoLabel("Order #:", 0);
            lblOrderType = CreateInfoLabel("Type:", 20);
            lblTable = CreateInfoLabel("Table:", 40);
            lblStaff = CreateInfoLabel("Staff:", 60);
            lblDate = CreateInfoLabel("Date:", 80);

            infoPanel.Controls.AddRange(new Control[] { lblOrderId, lblOrderType, lblTable, lblStaff, lblDate });
            mainPanel.Controls.Add(infoPanel);

            // ── Separator ──
            mainPanel.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 2, BackColor = Color.FromArgb(230, 225, 220) });

            // ── Items Grid ──
            dgvItems = new DataGridView
            {
                Dock = DockStyle.Top,
                Height = 200,
                ReadOnly = true,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                BackgroundColor = cardBg,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Color.FromArgb(240, 238, 235),
                EnableHeadersVisualStyles = false,
                Font = new Font("Segoe UI", 10),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            };
            dgvItems.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(45, 38, 32),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
            };
            dgvItems.ColumnHeadersHeight = 35;
            dgvItems.RowTemplate.Height = 32;
            mainPanel.Controls.Add(dgvItems);

            // ── Totals ──
            var totalsPanel = new Panel { Dock = DockStyle.Top, Height = 130, Padding = new Padding(0, 10, 0, 0) };

            lblSubtotal = new Label
            {
                Text = "Subtotal: $0.00",
                Font = new Font("Segoe UI", 11),
                ForeColor = textDark,
                TextAlign = ContentAlignment.MiddleRight,
                Dock = DockStyle.Top, Height = 26,
            };
            lblTax = new Label
            {
                Text = "Tax: $0.00",
                Font = new Font("Segoe UI", 11),
                ForeColor = textDark,
                TextAlign = ContentAlignment.MiddleRight,
                Dock = DockStyle.Top, Height = 26,
            };
            lblDiscount = new Label
            {
                Text = "Discount: $0.00",
                Font = new Font("Segoe UI", 11),
                ForeColor = textDark,
                TextAlign = ContentAlignment.MiddleRight,
                Dock = DockStyle.Top, Height = 26,
            };
            totalsPanel.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 2, BackColor = Color.FromArgb(230, 225, 220) });
            lblTotal = new Label
            {
                Text = "TOTAL: $0.00",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = accentGold,
                TextAlign = ContentAlignment.MiddleRight,
                Dock = DockStyle.Top, Height = 40,
            };

            totalsPanel.Controls.AddRange(new Control[] { lblTotal, lblDiscount, lblTax, lblSubtotal });
            mainPanel.Controls.Add(totalsPanel);

            // ── Payment section ──
            var paymentPanel = new Panel { Dock = DockStyle.Top, Height = 110, Padding = new Padding(0, 10, 0, 0) };

            // Discount input
            var lblDiscountLabel = new Label
            {
                Text = "Discount ($):",
                Font = new Font("Segoe UI", 10),
                ForeColor = textDark,
                Location = new Point(0, 5),
                Size = new Size(100, 25),
            };
            paymentPanel.Controls.Add(lblDiscountLabel);

            txtDiscount = new TextBox
            {
                Font = new Font("Segoe UI", 10),
                Location = new Point(105, 3),
                Size = new Size(100, 25),
                Text = "0",
                BackColor = bgColor,
            };
            paymentPanel.Controls.Add(txtDiscount);

            btnApplyDiscount = new Button
            {
                Text = "Apply",
                Font = new Font("Segoe UI", 9),
                BackColor = accentGold,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(215, 2),
                Size = new Size(70, 28),
                Cursor = Cursors.Hand,
            };
            btnApplyDiscount.FlatAppearance.BorderSize = 0;
            btnApplyDiscount.Click += BtnApplyDiscount_Click;
            paymentPanel.Controls.Add(btnApplyDiscount);

            // Payment method selection
            var lblPaymentLabel = new Label
            {
                Text = "Payment:",
                Font = new Font("Segoe UI", 10),
                ForeColor = textDark,
                Location = new Point(0, 42),
                Size = new Size(100, 25),
            };
            paymentPanel.Controls.Add(lblPaymentLabel);

            cmbPayment = new ComboBox
            {
                Font = new Font("Segoe UI", 10),
                Location = new Point(105, 40),
                Size = new Size(150, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = bgColor,
            };
            cmbPayment.Items.AddRange(new[] { "Cash", "Card", "Online" });
            cmbPayment.SelectedIndex = 0;
            paymentPanel.Controls.Add(cmbPayment);

            // Complete Payment button — triggers OrderService.CompletePayment()
            btnComplete = new Button
            {
                Text = "✓ Complete Payment",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                BackColor = successGreen,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(0, 75),
                Size = new Size(250, 40),
                Cursor = Cursors.Hand,
            };
            btnComplete.FlatAppearance.BorderSize = 0;
            btnComplete.Click += BtnComplete_Click;
            paymentPanel.Controls.Add(btnComplete);

            // Close button
            btnPrint = new Button
            {
                Text = "Close",
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(158, 158, 158),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(260, 75),
                Size = new Size(100, 40),
                Cursor = Cursors.Hand,
            };
            btnPrint.FlatAppearance.BorderSize = 0;
            btnPrint.Click += (s, e) => this.Close();
            paymentPanel.Controls.Add(btnPrint);

            mainPanel.Controls.Add(paymentPanel);

            // Reverse control order for proper Dock=Top rendering
            var controls = new List<Control>();
            foreach (Control c in mainPanel.Controls) controls.Add(c);
            controls.Reverse();
            mainPanel.Controls.Clear();
            foreach (var c in controls) mainPanel.Controls.Add(c);

            this.Load += (s, e) => LoadOrderData();
        }

        /// <summary>
        /// Creates a styled info label for the order details section.
        /// </summary>
        private Label CreateInfoLabel(string prefix, int top)
        {
            return new Label
            {
                Text = prefix,
                Font = new Font("Segoe UI", 10),
                ForeColor = textDark,
                Location = new Point(0, top),
                Size = new Size(460, 20),
            };
        }

        /// <summary>
        /// Loads order data from OrderDAL.GetById() and populates the form.
        /// </summary>
        private void LoadOrderData()
        {
            try
            {
                // Load the complete order with items from OrderDAL.cs
                _order = OrderDAL.GetById(_orderId);
                if (_order == null)
                {
                    MessageBox.Show("Order not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.Close();
                    return;
                }

                // Populate order info labels
                lblOrderId.Text = $"Order #: {_order.OrderId}";
                lblOrderType.Text = $"Type: {_order.OrderType}";
                lblTable.Text = $"Table: {_order.TableNumber ?? "N/A (Takeaway)"}";
                lblStaff.Text = $"Staff: {_order.StaffName}";
                lblDate.Text = $"Date: {_order.CreatedAt:g}";

                // Populate items grid
                dgvItems.Columns.Clear();
                dgvItems.Columns.Add("Item", "Item");
                dgvItems.Columns.Add("Qty", "Qty");
                dgvItems.Columns.Add("Price", "Price");
                dgvItems.Columns.Add("Total", "Total");

                foreach (var item in _order.Items)
                {
                    dgvItems.Rows.Add(item.ItemName, item.Quantity,
                        $"${item.UnitPrice:F2}", $"${item.LineTotal:F2}");
                }

                // Update totals
                UpdateTotalsDisplay();

                // Disable payment controls if order is already completed
                if (_order.Status == "Completed" || _order.Status == "Cancelled")
                {
                    btnComplete.Enabled = false;
                    btnApplyDiscount.Enabled = false;
                    txtDiscount.Enabled = false;
                    cmbPayment.Enabled = false;
                    btnComplete.Text = $"✓ {_order.Status}";
                    btnComplete.BackColor = Color.Gray;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading order: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Updates the totals display section.
        /// </summary>
        private void UpdateTotalsDisplay()
        {
            if (_order == null) return;
            lblSubtotal.Text = $"Subtotal: ${_order.Subtotal:F2}";
            lblTax.Text = $"Tax (10%): ${_order.Tax:F2}";
            lblDiscount.Text = $"Discount: -${_order.Discount:F2}";
            lblTotal.Text = $"TOTAL: ${_order.Total:F2}";
        }

        /// <summary>
        /// Apply discount button — uses OrderService.ApplyDiscount().
        /// </summary>
        private void BtnApplyDiscount_Click(object? sender, EventArgs e)
        {
            try
            {
                if (!ValidationHelper.IsValidDecimal(txtDiscount.Text, "Discount", out decimal discount))
                    return;

                if (!ValidationHelper.NonNegativeNumber(discount, "Discount"))
                    return;

                // Apply via OrderService.cs → OrderDAL.cs
                OrderService.ApplyDiscount(_orderId, discount);

                // Reload the order to reflect changes
                _order = OrderDAL.GetById(_orderId);
                UpdateTotalsDisplay();

                ValidationHelper.ShowSuccess("Discount applied successfully.");
            }
            catch (Exception ex)
            {
                ValidationHelper.ShowCriticalError($"Error applying discount: {ex.Message}");
            }
        }

        /// <summary>
        /// Complete Payment button — finalizes the order via OrderService.CompletePayment().
        /// This marks the order as Completed, sets payment method, and releases the table.
        /// </summary>
        private void BtnComplete_Click(object? sender, EventArgs e)
        {
            try
            {
                string paymentMethod = cmbPayment.SelectedItem?.ToString() ?? "Cash";

                if (!ValidationHelper.Confirm($"Complete payment of ${_order?.Total:F2} via {paymentMethod}?"))
                    return;

                // Complete payment via OrderService.cs → OrderDAL.cs + TableDAL.cs
                OrderService.CompletePayment(_orderId, paymentMethod);

                ValidationHelper.ShowSuccess("Payment completed successfully! Thank you.");

                // Update UI to reflect completed state
                btnComplete.Enabled = false;
                btnComplete.Text = "✓ Completed";
                btnComplete.BackColor = Color.Gray;
                btnApplyDiscount.Enabled = false;
            }
            catch (Exception ex)
            {
                ValidationHelper.ShowCriticalError($"Payment error: {ex.Message}");
            }
        }
    }
}
