using CoffeeShopPOS.Database;
using CoffeeShopPOS.Models;
using CoffeeShopPOS.Helpers;
using System.Drawing.Drawing2D;

namespace CoffeeShopPOS.Forms
{
    public class TableManagementForm : Form
    {
        private FlowLayoutPanel tableGrid = null!;
        private Label lblAvailCount = null!, lblOccupCount = null!, lblReserCount = null!;

        public TableManagementForm() => InitializeComponent();

        private void InitializeComponent()
        {
            this.Text           = "Tables";
            this.BackColor      = UIHelper.ContentBg;
            this.Dock           = DockStyle.Fill;
            this.DoubleBuffered = true;

            BuildSummaryBar();
            BuildTableGridCard();
            BuildHeaderCard();

            this.Load += (s, e) => LoadTables();
        }

        // ═══════════════════════════════════════════════════════
        // HEADER
        // ═══════════════════════════════════════════════════════
        private void BuildHeaderCard()
        {
            var header = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 62,
                BackColor = Color.White,
                Padding   = new Padding(0, 0, 0, 0),
            };
            header.Paint += (s, e) =>
            {
                using var pen = new Pen(UIHelper.Border);
                e.Graphics.DrawLine(pen, 0, header.Height - 1, header.Width, header.Height - 1);
            };

            // Legend dots
            var legendPanel = new FlowLayoutPanel
            {
                Dock          = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor     = Color.Transparent,
                Padding       = new Padding(16, 0, 0, 0),
                WrapContents  = false,
            };
            foreach (var (status, clr) in new[] {
                ("Available", UIHelper.Green),
                ("Occupied",  UIHelper.Red),
                ("Reserved",  UIHelper.Orange),
            })
            {
                var dot = new Label
                {
                    Text      = "●",
                    Font      = new Font("Segoe UI", 10),
                    ForeColor = clr,
                    AutoSize  = false,
                    Size      = new Size(18, 62),
                    TextAlign = ContentAlignment.MiddleCenter,
                    BackColor = Color.Transparent,
                    Margin    = new Padding(0, 0, 2, 0),
                };
                var lbl = new Label
                {
                    Text      = status,
                    Font      = new Font("Segoe UI", 10),
                    ForeColor = UIHelper.TextMuted,
                    AutoSize  = false,
                    Size      = new Size(80, 62),
                    TextAlign = ContentAlignment.MiddleLeft,
                    BackColor = Color.Transparent,
                    Margin    = new Padding(0, 0, 16, 0),
                };
                legendPanel.Controls.Add(dot);
                legendPanel.Controls.Add(lbl);
            }
            header.Controls.Add(legendPanel);

            // Add Table button (right side)
            var btnAdd = UIHelper.MakeButton("+ Add Table", UIHelper.Gold, Color.White, 130, 40);
            btnAdd.Location = new Point(0, 0); // will be positioned on resize
            var btnContainer = new Panel
            {
                Dock      = DockStyle.Right,
                Width     = 158,
                BackColor = Color.Transparent,
                Padding   = new Padding(0, 11, 16, 11),
            };
            btnAdd.Dock = DockStyle.Fill;
            btnContainer.Controls.Add(btnAdd);
            btnAdd.Click += (s, e) => ShowTableDialog(null);
            header.Controls.Add(btnContainer);

            this.Controls.Add(header);
        }

        // ═══════════════════════════════════════════════════════
        // TABLE GRID
        // ═══════════════════════════════════════════════════════
        private void BuildTableGridCard()
        {
            var card = UIHelper.MakeDockedCard(10);
            card.Dock    = DockStyle.Fill;
            card.Padding = new Padding(16);
            card.Margin  = new Padding(0);

            tableGrid = new FlowLayoutPanel
            {
                Dock         = DockStyle.Fill,
                AutoScroll   = true,
                WrapContents = true,
                Padding      = new Padding(0),
                BackColor    = Color.Transparent,
            };
            card.Controls.Add(tableGrid);

            var wrapper = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = UIHelper.ContentBg,
                Padding   = new Padding(0, 8, 0, 8),
            };
            wrapper.Controls.Add(card);
            this.Controls.Add(wrapper);
        }

        // ═══════════════════════════════════════════════════════
        // SUMMARY BAR
        // ═══════════════════════════════════════════════════════
        private void BuildSummaryBar()
        {
            var bar = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 76,
                BackColor = Color.White,
                Padding   = new Padding(16, 10, 16, 10),
            };
            bar.Paint += (s, e) =>
            {
                using var pen = new Pen(UIHelper.Border);
                e.Graphics.DrawLine(pen, 0, 0, bar.Width, 0);
            };

            var flow = new FlowLayoutPanel
            {
                Dock          = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = false,
                BackColor     = Color.Transparent,
            };

            lblAvailCount = new Label();
            lblOccupCount = new Label();
            lblReserCount = new Label();

            flow.Controls.Add(MakeSummaryCard("Available", UIHelper.Green,  lblAvailCount));
            flow.Controls.Add(MakeSummaryCard("Occupied",  UIHelper.Red,    lblOccupCount));
            flow.Controls.Add(MakeSummaryCard("Reserved",  UIHelper.Orange, lblReserCount));

            bar.Controls.Add(flow);
            this.Controls.Add(bar);
        }

        private Panel MakeSummaryCard(string status, Color clr, Label countLabel)
        {
            var card = new Panel
            {
                Size      = new Size(190, 56),
                BackColor = Color.Transparent,
                Margin    = new Padding(0, 0, 16, 0),
            };
            card.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var path  = UIHelper.RoundedPath(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 8);
                using var bg    = new SolidBrush(Color.White);
                g.FillPath(bg, path);
                using var pen   = new Pen(UIHelper.Border);
                g.DrawPath(pen, path);
            };

            var dot = new Label
            {
                Text      = "●",
                Font      = new Font("Segoe UI", 12),
                ForeColor = clr,
                Location  = new Point(12, 16),
                Size      = new Size(20, 22),
                BackColor = Color.Transparent,
            };
            countLabel.Text      = "0";
            countLabel.Font      = new Font("Segoe UI", 13, FontStyle.Bold);
            countLabel.ForeColor = UIHelper.TextDark;
            countLabel.Location  = new Point(36, 14);
            countLabel.Size      = new Size(44, 24);
            countLabel.BackColor = Color.Transparent;

            var lbl = new Label
            {
                Text      = status,
                Font      = new Font("Segoe UI", 9),
                ForeColor = UIHelper.TextMuted,
                Location  = new Point(84, 18),
                AutoSize  = true,
                BackColor = Color.Transparent,
            };

            card.Controls.Add(dot);
            card.Controls.Add(countLabel);
            card.Controls.Add(lbl);
            return card;
        }

        // ═══════════════════════════════════════════════════════
        // LOAD DATA
        // ═══════════════════════════════════════════════════════
        private void LoadTables()
        {
            try
            {
                tableGrid.Controls.Clear();
                var tables = TableDAL.GetAll();

                int avail = 0, occup = 0, reser = 0;
                foreach (var t in tables)
                {
                    tableGrid.Controls.Add(MakeTableCard(t));
                    if (t.Status == "Available") avail++;
                    else if (t.Status == "Occupied") occup++;
                    else if (t.Status == "Reserved") reser++;
                }

                lblAvailCount.Text = avail.ToString();
                lblOccupCount.Text = occup.ToString();
                lblReserCount.Text = reser.ToString();
            }
            catch (Exception ex) { MessageBox.Show($"Error loading tables: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        // ═══════════════════════════════════════════════════════
        // TABLE CARD
        // ═══════════════════════════════════════════════════════
        private Panel MakeTableCard(Table table)
        {
            var statusClr   = UIHelper.StatusColor(table.Status);
            var statusLight = UIHelper.StatusLight(table.Status);

            var card = new Panel
            {
                Size      = new Size(170, 190),
                Margin    = new Padding(0, 0, 12, 12),
                BackColor = Color.Transparent,
                Cursor    = Cursors.Hand,
                Tag       = table,
            };
            card.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var rect = new Rectangle(0, 0, card.Width - 1, card.Height - 1);
                using var path   = UIHelper.RoundedPath(rect, 10);
                using var bgBrush = new SolidBrush(Color.White);
                g.FillPath(bgBrush, path);
                using var borderPen = new Pen(statusClr, 1.5f);
                g.DrawPath(borderPen, path);
            };

            // Number circle
            var circle = new Panel
            {
                Location  = new Point(59, 24),
                Size      = new Size(52, 52),
                BackColor = Color.Transparent,
            };
            circle.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var brush = new SolidBrush(statusLight);
                g.FillEllipse(brush, 0, 0, 51, 51);
                using var font = new Font("Segoe UI", 15, FontStyle.Bold);
                var num = table.TableNumber.Replace("T", "").Replace("Table ", "");
                TextRenderer.DrawText(g, num, font, circle.ClientRectangle,
                    statusClr, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            card.Controls.Add(circle);

            // Table label
            card.Controls.Add(new Label
            {
                Text      = $"Table {table.TableNumber.Replace("T", "")}",
                Font      = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = UIHelper.TextDark,
                TextAlign = ContentAlignment.MiddleCenter,
                Location  = new Point(0, 84),
                Size      = new Size(170, 24),
                BackColor = Color.Transparent,
            });

            // Capacity
            card.Controls.Add(new Label
            {
                Text      = $"👥  {table.Capacity} seats",
                Font      = new Font("Segoe UI", 9.5f),
                ForeColor = UIHelper.TextMuted,
                TextAlign = ContentAlignment.MiddleCenter,
                Location  = new Point(0, 110),
                Size      = new Size(170, 22),
                BackColor = Color.Transparent,
            });

            // Status badge
            var badge = UIHelper.MakeBadge(table.Status, statusClr, 100, 26);
            badge.Location = new Point(35, 142);
            card.Controls.Add(badge);

            // Context menu
            var ctx = new ContextMenuStrip();
            ctx.Items.Add("Change Status →").Click += (s, e) => ShowStatusMenu(table, ctx);
            ctx.Items.Add(new ToolStripSeparator());
            ctx.Items.Add("✏  Edit").Click   += (s, e) => ShowTableDialog(table);
            ctx.Items.Add("🗑  Delete").Click += (s, e) => DeleteTable(table);

            void ShowCtx(object? s, EventArgs e) => ctx.Show(card, card.PointToClient(Cursor.Position));
            card.MouseClick += (s, e) => { if (e.Button == MouseButtons.Right) ShowCtx(s, e); };
            foreach (Control c in card.Controls)
                c.MouseClick += (s, e) => { if (e.Button == MouseButtons.Right) ShowCtx(s, e); };

            // Left-click: open edit dialog
            card.Click += (s, e) => { if (e is MouseEventArgs me && me.Button == MouseButtons.Left) ShowTableDialog(table); };

            return card;
        }

        private void ShowStatusMenu(Table table, ContextMenuStrip parent)
        {
            var sub = new ContextMenuStrip();
            foreach (var st in new[] { "Available", "Occupied", "Reserved" })
            {
                var item = sub.Items.Add(st);
                item.Click += (s, e) =>
                {
                    try
                    {
                        TableDAL.Update(new Table { TableId = table.TableId, TableNumber = table.TableNumber, Capacity = table.Capacity, Status = st });
                        LoadTables();
                    }
                    catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                };
            }
            sub.Show(Cursor.Position);
        }

        // ═══════════════════════════════════════════════════════
        // ADD / EDIT DIALOG
        // ═══════════════════════════════════════════════════════
        private void ShowTableDialog(Table? existing)
        {
            var dlg = new Form
            {
                Text            = existing == null ? "Add Table" : "Edit Table",
                Size            = new Size(360, 280),
                StartPosition   = FormStartPosition.CenterParent,
                BackColor       = Color.White,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox     = false,
                MinimizeBox     = false,
            };

            int y = 24;
            Label MakeLbl(string text) => new Label
            {
                Text      = text,
                Font      = new Font("Segoe UI", 9.5f),
                ForeColor = UIHelper.TextMuted,
                Location  = new Point(28, y),
                AutoSize  = true,
            };
            TextBox MakeTxt(string ph) { var t = new TextBox { Font = new Font("Segoe UI", 11), Location = new Point(28, y + 22), Size = new Size(296, 30), BorderStyle = BorderStyle.FixedSingle, PlaceholderText = ph }; y += 66; return t; }

            dlg.Controls.Add(MakeLbl("Table Number"));
            var txtNum = MakeTxt("e.g. T11");
            dlg.Controls.Add(txtNum);

            dlg.Controls.Add(MakeLbl("Capacity (seats)"));
            var txtCap = MakeTxt("e.g. 4");
            dlg.Controls.Add(txtCap);

            dlg.Controls.Add(MakeLbl("Status"));
            var cmb = new ComboBox { Font = new Font("Segoe UI", 11), Location = new Point(28, y + 22), Size = new Size(296, 30), DropDownStyle = ComboBoxStyle.DropDownList };
            cmb.Items.AddRange(new[] { "Available", "Occupied", "Reserved" });
            cmb.SelectedIndex = 0;
            dlg.Controls.Add(cmb);
            y += 66;

            if (existing != null)
            {
                txtNum.Text        = existing.TableNumber;
                txtCap.Text        = existing.Capacity.ToString();
                cmb.SelectedItem   = existing.Status;
            }

            var btnSave = UIHelper.MakeButton(existing == null ? "Add Table" : "Save Changes",
                UIHelper.Gold, Color.White, 150, 40);
            btnSave.Location = new Point(28, y + 10);
            dlg.Controls.Add(btnSave);

            var btnCancel = UIHelper.MakeOutlineButton("Cancel", UIHelper.TextMuted, 120, 40);
            btnCancel.Location = new Point(192, y + 10);
            btnCancel.Click   += (s, e) => dlg.Close();
            dlg.Controls.Add(btnCancel);

            btnSave.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtNum.Text)) { MessageBox.Show("Table number is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                if (!int.TryParse(txtCap.Text, out int cap) || cap <= 0) { MessageBox.Show("Enter a valid capacity.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

                try
                {
                    var t = new Table
                    {
                        TableId     = existing?.TableId ?? 0,
                        TableNumber = txtNum.Text.Trim(),
                        Capacity    = cap,
                        Status      = cmb.SelectedItem?.ToString() ?? "Available",
                    };
                    if (existing == null) TableDAL.Insert(t);
                    else                  TableDAL.Update(t);
                    dlg.Close();
                    LoadTables();
                }
                catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            };

            dlg.ShowDialog(this);
        }

        private void DeleteTable(Table table)
        {
            if (MessageBox.Show($"Delete Table {table.TableNumber}?", "Confirm",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            try { TableDAL.Delete(table.TableId); LoadTables(); }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }
    }
}
