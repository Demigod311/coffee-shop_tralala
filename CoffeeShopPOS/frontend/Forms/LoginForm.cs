// LoginForm.cs — User Login Screen
using CoffeeShopPOS.Services;
using CoffeeShopPOS.Database;
using System.Drawing.Drawing2D;

namespace CoffeeShopPOS.Forms
{
    public class LoginForm : Form
    {
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

        private System.Windows.Forms.Timer fadeTimer = null!;
        private double currentOpacity = 0;

        // Color scheme — white card with warm cream/gold accents
        private readonly Color bgDark     = Color.FromArgb(20, 12, 7);
        private readonly Color cardBg     = Color.White;
        private readonly Color accentColor = Color.FromArgb(193, 154, 107);
        private readonly Color accentHover = Color.FromArgb(212, 175, 130);
        private readonly Color textDark   = Color.FromArgb(40, 28, 18);
        private readonly Color textMuted  = Color.FromArgb(150, 140, 130);
        private readonly Color inputBg    = Color.FromArgb(242, 237, 228);
        private readonly Color inputFocus = Color.FromArgb(228, 220, 206);
        private readonly Color errorColor = Color.FromArgb(220, 80, 60);

        public LoginForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Brew & Co. POS — Login";
            this.Size = new Size(500, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = bgDark;
            this.DoubleBuffered = true;
            this.Opacity = 0;

            // ── White card panel ──
            panelMain = new Panel
            {
                Size = new Size(400, 490),
                Location = new Point(50, 55),
                BackColor = cardBg,
            };
            SetRoundedRegion(panelMain, 16);
            this.Controls.Add(panelMain);

            // ── Logo circle ──
            panelLogo = new Panel
            {
                Size = new Size(80, 80),
                Location = new Point(160, 22),
                BackColor = Color.Transparent,
            };
            panelLogo.Paint += PanelLogo_Paint;
            panelMain.Controls.Add(panelLogo);

            // ── Title ──
            lblTitle = new Label
            {
                Text = "Brew & Co. POS",
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = accentColor,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(360, 40),
                Location = new Point(20, 115),
                BackColor = Color.Transparent,
            };
            panelMain.Controls.Add(lblTitle);

            // ── Username label ──
            lblUsername = new Label
            {
                Text = "Username",
                Font = new Font("Segoe UI", 10),
                ForeColor = textDark,
                Location = new Point(35, 176),
                Size = new Size(330, 20),
                BackColor = Color.Transparent,
            };
            panelMain.Controls.Add(lblUsername);

            // ── Username input ──
            txtUsername = new TextBox
            {
                Font = new Font("Segoe UI", 12),
                ForeColor = textDark,
                BackColor = inputBg,
                BorderStyle = BorderStyle.None,
                PlaceholderText = "Enter your username",
            };
            var userPanel = CreateRoundedInputPanel(txtUsername, 35, 201);
            panelMain.Controls.Add(userPanel);

            // ── Password label ──
            lblPassword = new Label
            {
                Text = "Password",
                Font = new Font("Segoe UI", 10),
                ForeColor = textDark,
                Location = new Point(35, 267),
                Size = new Size(330, 20),
                BackColor = Color.Transparent,
            };
            panelMain.Controls.Add(lblPassword);

            // ── Password input with eye toggle ──
            txtPassword = new TextBox
            {
                Font = new Font("Segoe UI", 12),
                ForeColor = textDark,
                BackColor = inputBg,
                BorderStyle = BorderStyle.None,
                UseSystemPasswordChar = true,
                PlaceholderText = "Enter your password",
            };
            var passPanel = CreateRoundedPasswordPanel(txtPassword, 35, 292);
            panelMain.Controls.Add(passPanel);

            // ── Sign In button ──
            btnLogin = new Button
            {
                Text = "Sign In",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = accentColor,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(330, 50),
                Location = new Point(35, 368),
                Cursor = Cursors.Hand,
            };
            btnLogin.FlatAppearance.BorderSize = 0;
            SetRoundedRegion(btnLogin, 25);
            btnLogin.MouseEnter += (s, e) => btnLogin.BackColor = accentHover;
            btnLogin.MouseLeave += (s, e) => btnLogin.BackColor = accentColor;
            btnLogin.Click += BtnLogin_Click;
            panelMain.Controls.Add(btnLogin);

            // ── Status label ──
            lblStatus = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 9),
                ForeColor = errorColor,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(330, 20),
                Location = new Point(35, 426),
                BackColor = Color.Transparent,
            };
            panelMain.Controls.Add(lblStatus);

            // ── Subtitle at bottom of card ──
            lblSubtitle = new Label
            {
                Text = "Coffee Shop Management System",
                Font = new Font("Segoe UI", 10),
                ForeColor = textMuted,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(360, 25),
                Location = new Point(20, 454),
                BackColor = Color.Transparent,
            };
            panelMain.Controls.Add(lblSubtitle);

            // ── Close button (✕) at top-right of card ──
            var btnClose = new Label
            {
                Text = "✕",
                Font = new Font("Segoe UI", 13),
                ForeColor = textMuted,
                Size = new Size(30, 30),
                Location = new Point(362, 8),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand,
                BackColor = Color.Transparent,
            };
            btnClose.Click += (s, e) => Application.Exit();
            btnClose.MouseEnter += (s, e) => btnClose.ForeColor = errorColor;
            btnClose.MouseLeave += (s, e) => btnClose.ForeColor = textMuted;
            panelMain.Controls.Add(btnClose);
            btnClose.BringToFront();

            // ── Help (?) button at bottom-right of form ──
            var btnHelp = new Button
            {
                Text = "?",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = textMuted,
                BackColor = Color.FromArgb(50, 40, 30),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(36, 36),
                Location = new Point(452, 552),
                Cursor = Cursors.Hand,
            };
            btnHelp.FlatAppearance.BorderSize = 0;
            SetRoundedRegion(btnHelp, 18);
            this.Controls.Add(btnHelp);

            // ── Keyboard shortcuts ──
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
                    txtPassword.Focus();
                }
            };

            // ── Fade-in animation ──
            fadeTimer = new System.Windows.Forms.Timer { Interval = 16 };
            fadeTimer.Tick += FadeTimer_Tick;

            panelMain.MouseDown += Form_MouseDown;
            lblTitle.MouseDown += Form_MouseDown;

            this.Load += LoginForm_Load;
        }

        private Panel CreateRoundedInputPanel(TextBox textBox, int x, int y)
        {
            var panel = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(330, 48),
                BackColor = inputBg,
            };
            textBox.Location = new Point(16, 12);
            textBox.Size = new Size(298, 24);
            panel.Controls.Add(textBox);

            textBox.GotFocus  += (s, e) => panel.BackColor = inputFocus;
            textBox.LostFocus += (s, e) => panel.BackColor = inputBg;

            SetRoundedRegion(panel, 24);
            return panel;
        }

        private Panel CreateRoundedPasswordPanel(TextBox textBox, int x, int y)
        {
            var panel = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(330, 48),
                BackColor = inputBg,
            };
            textBox.Location = new Point(16, 12);
            textBox.Size = new Size(270, 24);
            panel.Controls.Add(textBox);

            // Eye icon toggle
            var eyeLabel = new Label
            {
                Text = "◉",
                Font = new Font("Segoe UI Symbol", 14),
                ForeColor = textMuted,
                Size = new Size(36, 36),
                Location = new Point(288, 6),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand,
                BackColor = Color.Transparent,
            };
            eyeLabel.Click += (s, e) =>
            {
                textBox.UseSystemPasswordChar = !textBox.UseSystemPasswordChar;
                eyeLabel.ForeColor = textBox.UseSystemPasswordChar ? textMuted : accentColor;
            };
            panel.Controls.Add(eyeLabel);

            textBox.GotFocus  += (s, e) => panel.BackColor = inputFocus;
            textBox.LostFocus += (s, e) => panel.BackColor = inputBg;

            SetRoundedRegion(panel, 24);
            return panel;
        }

        private static void SetRoundedRegion(Control control, int radius)
        {
            int w = control.Width, h = control.Height, d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(0, 0, d, d, 180, 90);
            path.AddArc(w - d, 0, d, d, 270, 90);
            path.AddArc(w - d, h - d, d, d, 0, 90);
            path.AddArc(0, h - d, d, d, 90, 90);
            path.CloseFigure();
            control.Region = new Region(path);
        }

        private void LoginForm_Load(object? sender, EventArgs e)
        {
            fadeTimer.Start();

            if (!DbHelper.TestConnection())
            {
                lblStatus.Text = "⚠ Cannot connect to database. Check PostgreSQL.";
                lblStatus.ForeColor = errorColor;
                btnLogin.Enabled = false;
            }
            else
            {
                AuthService.EnsureAdminExists();
            }

            txtUsername.Focus();
        }

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

        private void BtnLogin_Click(object? sender, EventArgs e)
        {
            lblStatus.Text = "";

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
                btnLogin.Text = "Signing in...";
                btnLogin.Enabled = false;
                Application.DoEvents();

                var user = AuthService.Authenticate(username, password);

                if (user != null)
                {
                    lblStatus.Text = $"Welcome, {user.FullName}!";
                    lblStatus.ForeColor = Color.FromArgb(100, 200, 100);

                    var transitionTimer = new System.Windows.Forms.Timer { Interval = 500 };
                    transitionTimer.Tick += (s2, e2) =>
                    {
                        transitionTimer.Stop();
                        transitionTimer.Dispose();
                        this.Hide();
                        var mainForm = new MainForm();
                        mainForm.FormClosed += (s3, e3) => Application.Exit();
                        mainForm.Show();
                    };
                    transitionTimer.Start();
                }
                else
                {
                    lblStatus.Text = "Invalid username or password.";
                    lblStatus.ForeColor = errorColor;
                    txtPassword.Clear();
                    txtPassword.Focus();
                    ShakeForm();
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Error: {ex.Message}";
                lblStatus.ForeColor = errorColor;
            }
            finally
            {
                btnLogin.Text = "Sign In";
                btnLogin.Enabled = true;
            }
        }

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

        private void PanelLogo_Paint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Gold circle
            using var goldBrush = new SolidBrush(accentColor);
            g.FillEllipse(goldBrush, 0, 0, 80, 80);

            using var whiteBrush = new SolidBrush(Color.White);
            using var whitePen   = new Pen(Color.White, 2.5f);

            // Steam lines
            g.DrawBezier(whitePen, 28, 32, 25, 23, 31, 19, 28, 12);
            g.DrawBezier(whitePen, 38, 32, 35, 22, 41, 18, 38, 10);
            g.DrawBezier(whitePen, 48, 32, 45, 23, 51, 19, 48, 12);

            // Cup body (filled trapezoid)
            var cup = new PointF[]
            {
                new PointF(18, 36), new PointF(56, 36),
                new PointF(52, 62), new PointF(22, 62),
            };
            g.FillPolygon(whiteBrush, cup);

            // Cup rim
            g.FillRectangle(whiteBrush, 16, 32, 42, 7);

            // Handle
            g.DrawArc(whitePen, 52, 40, 14, 14, -90, 180);
        }

        private Point dragStart;
        private void Form_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                dragStart = e.Location;
                if (sender is Control ctrl)
                {
                    ctrl.MouseMove += Form_MouseMove;
                    ctrl.MouseUp   += Form_MouseUp;
                }
            }
        }

        private void Form_MouseMove(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                this.Location = new Point(
                    this.Location.X + e.X - dragStart.X,
                    this.Location.Y + e.Y - dragStart.Y);
        }

        private void Form_MouseUp(object? sender, MouseEventArgs e)
        {
            if (sender is Control ctrl)
            {
                ctrl.MouseMove -= Form_MouseMove;
                ctrl.MouseUp   -= Form_MouseUp;
            }
        }
    }
}
