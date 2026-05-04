using System.Drawing.Drawing2D;

namespace CoffeeShopPOS.Forms
{
    internal static class UIHelper
    {
        // ── Brand palette ─────────────────────────────────────────────
        public static readonly Color ContentBg   = Color.FromArgb(245, 240, 234);
        public static readonly Color CardBg      = Color.White;
        public static readonly Color Border      = Color.FromArgb(232, 226, 218);
        public static readonly Color TextDark    = Color.FromArgb( 44,  33,  24);
        public static readonly Color TextMuted   = Color.FromArgb(140, 124, 107);
        public static readonly Color Gold        = Color.FromArgb(200, 150,  85);
        public static readonly Color GoldLight   = Color.FromArgb(252, 248, 240);
        public static readonly Color Green       = Color.FromArgb( 76, 175,  80);
        public static readonly Color GreenLight  = Color.FromArgb(232, 245, 233);
        public static readonly Color Red         = Color.FromArgb(229,  57,  53);
        public static readonly Color RedLight    = Color.FromArgb(255, 235, 238);
        public static readonly Color Orange      = Color.FromArgb(255, 152,   0);
        public static readonly Color OrangeLight = Color.FromArgb(255, 248, 225);
        public static readonly Color Blue        = Color.FromArgb( 33, 150, 243);
        public static readonly Color BlueLight   = Color.FromArgb(227, 242, 253);
        public static readonly Color Purple      = Color.FromArgb(156,  39, 176);
        public static readonly Color PurpleLight = Color.FromArgb(243, 229, 245);

        // Category stripe colors (cycles by category hash)
        public static readonly Color[] CategoryColors =
        {
            Color.FromArgb(229,  57,  53),  // red
            Color.FromArgb( 33, 150, 243),  // blue
            Color.FromArgb(255, 152,   0),  // orange
            Color.FromArgb(156,  39, 176),  // purple
            Color.FromArgb( 76, 175,  80),  // green
            Color.FromArgb(  0, 188, 212),  // cyan
            Color.FromArgb(233,  30,  99),  // pink
        };

        // ── Rounded GraphicsPath ──────────────────────────────────────
        public static GraphicsPath RoundedPath(Rectangle b, int r)
        {
            int d = r * 2;
            var p = new GraphicsPath();
            p.AddArc(b.X,           b.Y,            d, d, 180, 90);
            p.AddArc(b.Right - d,   b.Y,            d, d, 270, 90);
            p.AddArc(b.Right - d,   b.Bottom - d,   d, d,   0, 90);
            p.AddArc(b.X,           b.Bottom - d,   d, d,  90, 90);
            p.CloseAllFigures();
            return p;
        }

        // ── Card factory ──────────────────────────────────────────────
        // Creates a white rounded-corner Panel.
        public static Panel MakeCard(int x, int y, int w, int h, int radius = 10)
        {
            var card = new Panel
            {
                Location  = new Point(x, y),
                Size      = new Size(w, h),
                BackColor = Color.Transparent,
            };
            RefreshCardRegion(card, radius);
            card.Resize  += (s, e) => RefreshCardRegion(card, radius);
            card.Paint   += (s, e) => PaintCard(e.Graphics, card, radius);
            return card;
        }

        // Docked card variant (fill/top/etc.) — call after setting Dock
        public static Panel MakeDockedCard(int radius = 10)
        {
            var card = new Panel { BackColor = Color.Transparent };
            card.Resize += (s, e) => RefreshCardRegion(card, radius);
            card.Paint  += (s, e) => PaintCard(e.Graphics, card, radius);
            return card;
        }

        private static void RefreshCardRegion(Panel card, int radius)
        {
            if (card.Width > 1 && card.Height > 1)
                card.Region = new Region(RoundedPath(new Rectangle(0, 0, card.Width, card.Height), radius));
        }

        private static void PaintCard(Graphics g, Panel card, int radius)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var path  = RoundedPath(new Rectangle(0, 0, card.Width - 1, card.Height - 1), radius);
            using var bg    = new SolidBrush(CardBg);
            g.FillPath(bg, path);
            using var pen   = new Pen(Border, 1f);
            g.DrawPath(pen, path);
        }

        // ── Pill badge factory ────────────────────────────────────────
        public static Label MakeBadge(string text, Color bg, int w = 88, int h = 24)
        {
            var lbl = new Label
            {
                Text      = text,
                Font      = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Size      = new Size(w, h),
                TextAlign = ContentAlignment.MiddleCenter,
            };
            lbl.Region = new Region(RoundedPath(new Rectangle(0, 0, w, h), h / 2));
            lbl.Paint  += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var path  = RoundedPath(new Rectangle(0, 0, lbl.Width - 1, lbl.Height - 1), lbl.Height / 2);
                using var brush = new SolidBrush(bg);
                g.FillPath(brush, path);
                TextRenderer.DrawText(e.Graphics, lbl.Text, lbl.Font,
                    lbl.ClientRectangle, Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            return lbl;
        }

        // ── Status helpers ────────────────────────────────────────────
        public static Color StatusColor(string status) => status switch
        {
            "Completed" or "Available" => Green,
            "Preparing" or "Reserved"  => Orange,
            "Served"                   => Purple,
            "Pending"                  => Blue,
            "Cancelled" or "Occupied"  => Red,
            _                          => Color.Gray,
        };

        public static Color StatusLight(string status) => status switch
        {
            "Completed" or "Available" => GreenLight,
            "Preparing" or "Reserved"  => OrangeLight,
            "Served"                   => PurpleLight,
            "Pending"                  => BlueLight,
            "Cancelled" or "Occupied"  => RedLight,
            _                          => Color.FromArgb(245, 245, 245),
        };

        // ── Styled DataGridView ───────────────────────────────────────
        public static DataGridView MakeGrid()
        {
            var dgv = new DataGridView
            {
                ReadOnly                = true,
                AllowUserToAddRows      = false,
                AllowUserToDeleteRows   = false,
                SelectionMode           = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode     = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor         = CardBg,
                BorderStyle             = BorderStyle.None,
                CellBorderStyle         = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor               = Color.FromArgb(235, 230, 225),
                RowHeadersVisible       = false,
                EnableHeadersVisualStyles = false,
                Font                    = new Font("Segoe UI", 10),
            };
            dgv.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(250, 247, 243),
                ForeColor = TextMuted,
                Font      = new Font("Segoe UI", 9, FontStyle.Bold),
                Padding   = new Padding(8, 4, 4, 4),
                Alignment = DataGridViewContentAlignment.MiddleLeft,
            };
            dgv.ColumnHeadersHeight = 38;
            dgv.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor           = CardBg,
                ForeColor           = TextDark,
                SelectionBackColor  = Color.FromArgb(252, 248, 240),
                SelectionForeColor  = TextDark,
                Padding             = new Padding(8, 4, 4, 4),
            };
            dgv.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor           = Color.FromArgb(252, 250, 248),
                SelectionBackColor  = Color.FromArgb(252, 248, 240),
                SelectionForeColor  = TextDark,
            };
            dgv.RowTemplate.Height = 38;
            return dgv;
        }

        // ── Styled rounded Button ─────────────────────────────────────
        public static Button MakeButton(string text, Color bg, Color fg, int w, int h, int radius = 8)
        {
            var btn = new Button
            {
                Text      = text,
                Font      = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = fg,
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Size      = new Size(w, h),
                Cursor    = Cursors.Hand,
                Tag       = bg,
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Region = new Region(RoundedPath(new Rectangle(0, 0, w, h), radius));
            btn.Resize += (s, e) =>
                btn.Region = new Region(RoundedPath(new Rectangle(0, 0, btn.Width, btn.Height), radius));
            btn.Paint += (s, e) =>
            {
                var g   = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var clr = (Color)(btn.Tag ?? bg);
                using var path  = RoundedPath(new Rectangle(0, 0, btn.Width - 1, btn.Height - 1), radius);
                using var brush = new SolidBrush(clr);
                g.FillPath(brush, path);
                TextRenderer.DrawText(e.Graphics, btn.Text, btn.Font,
                    btn.ClientRectangle, fg,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            btn.MouseEnter += (s, e) => { btn.Tag = ControlPaint.Light(bg, 0.1f); btn.Invalidate(); };
            btn.MouseLeave += (s, e) => { btn.Tag = bg;                            btn.Invalidate(); };
            return btn;
        }

        // ── Grid cell helpers (used by multiple forms) ────────────────
        public static void DrawGridToggle(Graphics g, Rectangle bounds, bool isOn)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int tw = 40, th = 20;
            int tx = bounds.X + (bounds.Width  - tw) / 2;
            int ty = bounds.Y + (bounds.Height - th) / 2;
            using var path = RoundedPath(new Rectangle(tx, ty, tw, th), th / 2);
            using var bg   = new SolidBrush(isOn ? Green : Color.FromArgb(189, 189, 189));
            g.FillPath(bg, path);
            int cx = isOn ? tx + tw - th + 1 : tx + 1;
            using var wh = new SolidBrush(Color.White);
            g.FillEllipse(wh, cx, ty + 1, th - 2, th - 2);
        }

        public static void DrawGridBadge(Graphics g, Rectangle bounds, string text, Color bg, int bw = 88, int bh = 24)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int bx = bounds.X + (bounds.Width  - bw) / 2;
            int by = bounds.Y + (bounds.Height - bh) / 2;
            var rect = new Rectangle(bx, by, bw, bh);
            using var path  = RoundedPath(rect, bh / 2);
            using var brush = new SolidBrush(bg);
            g.FillPath(brush, path);
            using var font  = new Font("Segoe UI", 8f, FontStyle.Bold);
            TextRenderer.DrawText(g, text, font, rect, Color.White,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        // ── Outlined button variant ───────────────────────────────────
        public static Button MakeOutlineButton(string text, Color accent, int w, int h, int radius = 8)
        {
            var btn = new Button
            {
                Text      = text,
                Font      = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = accent,
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Size      = new Size(w, h),
                Cursor    = Cursors.Hand,
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Region = new Region(RoundedPath(new Rectangle(0, 0, w, h), radius));
            btn.Resize += (s, e) =>
                btn.Region = new Region(RoundedPath(new Rectangle(0, 0, btn.Width, btn.Height), radius));
            btn.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var path  = RoundedPath(new Rectangle(0, 0, btn.Width - 1, btn.Height - 1), radius);
                using var bg    = new SolidBrush(Color.White);
                g.FillPath(bg, path);
                using var pen   = new Pen(accent, 1.5f);
                g.DrawPath(pen, path);
                TextRenderer.DrawText(e.Graphics, btn.Text, btn.Font,
                    btn.ClientRectangle, accent,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            return btn;
        }
    }
}
