// BackupRestoreForm.cs — Database Backup and Restore (Feature #20)
//
// Allows admins to export all PostgreSQL data to a .sql file and restore from one.
// Backup writes INSERT statements in FK-safe order with a leading TRUNCATE CASCADE.
// Restore executes the entire backup file in a single transaction.
// Connects to: DbHelper.cs (connection string), Npgsql (direct ADO.NET access)

using CoffeeShopPOS.Database;
using Npgsql;

namespace CoffeeShopPOS.Forms
{
    public class BackupRestoreForm : Form
    {
        // ── Color scheme ──
        private readonly Color bgColor    = Color.FromArgb(245, 242, 238);
        private readonly Color cardBg     = Color.White;
        private readonly Color textDark   = Color.FromArgb(50, 40, 35);
        private readonly Color textMuted  = Color.FromArgb(140, 130, 120);
        private readonly Color accentGold = Color.FromArgb(193, 154, 107);
        private readonly Color successGreen = Color.FromArgb(76, 175, 80);
        private readonly Color dangerRed  = Color.FromArgb(244, 67, 54);
        private readonly Color terminalBg = Color.FromArgb(22, 18, 14);

        private RichTextBox rtbLog  = null!;
        private ProgressBar progressBar = null!;
        private Button btnBackup  = null!;
        private Button btnRestore = null!;
        private Label  lblStatus  = null!;

        // Tables backed up in FK-safe INSERT order.
        // Restore uses TRUNCATE … CASCADE so order doesn't matter for deletion.
        private static readonly (string Table, string PkColumn)[] BackupOrder =
        {
            ("users",         "user_id"),
            ("categories",    "category_id"),
            ("tables",        "table_id"),
            ("menu_items",    "item_id"),
            ("inventory",     "inventory_id"),
            ("orders",        "order_id"),
            ("order_items",   "order_item_id"),
            ("inventory_log", "log_id"),
        };

        public BackupRestoreForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text      = "Backup & Restore";
            this.BackColor = bgColor;
            this.Dock      = DockStyle.Fill;
            this.Padding   = new Padding(24, 20, 24, 20);

            // ── Page title ──
            var lblTitle = new Label
            {
                Text      = "🗄  Backup & Restore",
                Font      = new Font("Segoe UI", 22, FontStyle.Bold),
                ForeColor = textDark,
                Dock      = DockStyle.Top,
                Height    = 52,
            };
            this.Controls.Add(lblTitle);

            var lblSub = new Label
            {
                Text      = "Create a full data backup or restore from an existing .sql backup file. Admin access only.",
                Font      = new Font("Segoe UI", 10),
                ForeColor = textMuted,
                Dock      = DockStyle.Top,
                Height    = 28,
            };
            this.Controls.Add(lblSub);

            // ── Action cards row ──
            var cardsRow = new Panel
            {
                Dock    = DockStyle.Top,
                Height  = 145,
                Padding = new Padding(0, 14, 0, 0),
                BackColor = bgColor,
            };
            this.Controls.Add(cardsRow);

            var backupCard  = BuildActionCard("📤  Create Backup",
                "Export all data to a portable SQL file.", accentGold, 0);
            var restoreCard = BuildActionCard("📥  Restore Backup",
                "Replace all data from a .sql backup file.", dangerRed, 320);

            btnBackup  = (Button)backupCard.Controls[backupCard.Controls.Count - 1];
            btnRestore = (Button)restoreCard.Controls[restoreCard.Controls.Count - 1];
            btnBackup.Click  += BtnBackup_Click;
            btnRestore.Click += BtnRestore_Click;

            cardsRow.Controls.Add(backupCard);
            cardsRow.Controls.Add(restoreCard);

            // ── Status line ──
            lblStatus = new Label
            {
                Text      = "Ready.",
                Font      = new Font("Segoe UI", 10),
                ForeColor = textMuted,
                Dock      = DockStyle.Top,
                Height    = 28,
            };
            this.Controls.Add(lblStatus);

            // ── Progress bar ──
            progressBar = new ProgressBar
            {
                Dock    = DockStyle.Top,
                Height  = 6,
                Visible = false,
                Style   = ProgressBarStyle.Continuous,
            };
            this.Controls.Add(progressBar);

            // ── Log heading ──
            var logLabel = new Label
            {
                Text      = "Operation Log",
                Font      = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = textDark,
                Dock      = DockStyle.Top,
                Height    = 34,
                Padding   = new Padding(0, 8, 0, 0),
            };
            this.Controls.Add(logLabel);

            // ── Terminal-style log box ──
            rtbLog = new RichTextBox
            {
                Dock        = DockStyle.Fill,
                Font        = new Font("Consolas", 9.5f),
                BackColor   = terminalBg,
                ForeColor   = Color.FromArgb(193, 154, 107),
                ReadOnly    = true,
                BorderStyle = BorderStyle.None,
                Padding     = new Padding(10),
            };
            this.Controls.Add(rtbLog);

            // Controls added with Dock=Top are stacked bottom-to-top internally;
            // reverse so they display in the order written above.
            int count = this.Controls.Count;
            this.Controls.SetChildIndex(lblTitle,    count - 1);
            this.Controls.SetChildIndex(lblSub,      count - 1);
            this.Controls.SetChildIndex(cardsRow,    count - 1);
            this.Controls.SetChildIndex(lblStatus,   count - 1);
            this.Controls.SetChildIndex(progressBar, count - 1);
            this.Controls.SetChildIndex(logLabel,    count - 1);
            this.Controls.SetChildIndex(rtbLog,      count - 1);

            AppendLog("System ready.  Select Backup or Restore to begin.", textMuted);
        }

        // ─────────────────────────────────────────────
        // Card builder
        // ─────────────────────────────────────────────
        private Panel BuildActionCard(string title, string description, Color accent, int x)
        {
            var card = new Panel
            {
                Location  = new Point(x, 0),
                Size      = new Size(300, 128),
                BackColor = cardBg,
            };

            var stripe = new Panel
            {
                Location  = Point.Empty,
                Size      = new Size(300, 5),
                BackColor = accent,
            };
            card.Controls.Add(stripe);

            card.Controls.Add(new Label
            {
                Text      = title,
                Font      = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = textDark,
                Location  = new Point(14, 14),
                Size      = new Size(272, 28),
                BackColor = Color.Transparent,
            });

            card.Controls.Add(new Label
            {
                Text      = description,
                Font      = new Font("Segoe UI", 9),
                ForeColor = textMuted,
                Location  = new Point(14, 46),
                Size      = new Size(272, 22),
                BackColor = Color.Transparent,
            });

            var btn = new Button
            {
                Text      = title.Contains("Create") ? "Create Backup" : "Restore Now",
                Font      = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = accent,
                FlatStyle = FlatStyle.Flat,
                Location  = new Point(14, 82),
                Size      = new Size(140, 34),
                Cursor    = Cursors.Hand,
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.MouseEnter += (s, e) => btn.BackColor = ControlPaint.Light(accent, 0.15f);
            btn.MouseLeave += (s, e) => btn.BackColor = accent;
            card.Controls.Add(btn);   // always last — BtnBackup/BtnRestore resolved via index

            return card;
        }

        // ─────────────────────────────────────────────
        // BACKUP
        // ─────────────────────────────────────────────
        private async void BtnBackup_Click(object? sender, EventArgs e)
        {
            using var dlg = new SaveFileDialog
            {
                Title       = "Save Database Backup",
                Filter      = "SQL Files (*.sql)|*.sql|All Files (*.*)|*.*",
                FileName    = $"coffeeshop_backup_{DateTime.Now:yyyyMMdd_HHmmss}.sql",
                DefaultExt  = "sql",
            };
            if (dlg.ShowDialog() != DialogResult.OK) return;

            SetBusy(true);
            progressBar.Maximum = BackupOrder.Length;
            progressBar.Value   = 0;

            try
            {
                AppendLog($"\n[{DateTime.Now:HH:mm:ss}]  Starting backup → {dlg.FileName}", successGreen);
                await Task.Run(() => RunBackup(dlg.FileName));
                SetStatus("Backup completed successfully.", successGreen);
                AppendLog("✓  Backup complete.", successGreen);
                MessageBox.Show($"Backup saved to:\n{dlg.FileName}",
                    "Backup Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                SetStatus("Backup failed.", dangerRed);
                AppendLog($"✗  Error: {ex.Message}", dangerRed);
                MessageBox.Show($"Backup failed:\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void RunBackup(string path)
        {
            using var writer = new StreamWriter(path, false, System.Text.Encoding.UTF8);
            using var conn   = new NpgsqlConnection(DbHelper.ConnectionString);
            conn.Open();

            // ── Header ──
            writer.WriteLine("-- Coffee Shop POS  |  PostgreSQL Backup");
            writer.WriteLine($"-- Created : {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            writer.WriteLine($"-- Tables  : {BackupOrder.Length}");
            writer.WriteLine();
            writer.WriteLine("BEGIN;");
            writer.WriteLine();

            // ── Clear all tables in safe order using CASCADE ──
            var tableList = string.Join(", ", BackupOrder.Reverse().Select(t => t.Table));
            writer.WriteLine($"TRUNCATE TABLE {tableList} RESTART IDENTITY CASCADE;");
            writer.WriteLine();

            // ── INSERT rows per table ──
            foreach (var (table, pkCol) in BackupOrder)
            {
                InvokeLog($"  Exporting  {table} …", accentGold);

                using var cmd    = new NpgsqlCommand($"SELECT * FROM {table} ORDER BY 1", conn);
                using var reader = cmd.ExecuteReader();

                var cols = Enumerable.Range(0, reader.FieldCount)
                                     .Select(i => reader.GetName(i))
                                     .ToList();

                writer.WriteLine($"-- {table}");
                int rows = 0;
                while (reader.Read())
                {
                    var vals = cols.Select((_, i) => PgLiteral(reader, i));
                    writer.WriteLine(
                        $"INSERT INTO {table} ({string.Join(", ", cols)}) " +
                        $"VALUES ({string.Join(", ", vals)});");
                    rows++;
                }
                writer.WriteLine();
                InvokeLog($"    → {rows} rows", Color.FromArgb(120, 180, 120));
                this.Invoke(() => { if (progressBar.Value < progressBar.Maximum) progressBar.Value++; });
            }

            // ── Fix sequences so next auto-insert doesn't collide ──
            writer.WriteLine("-- Sync sequences");
            foreach (var (table, pkCol) in BackupOrder)
                writer.WriteLine(
                    $"SELECT setval(pg_get_serial_sequence('{table}', '{pkCol}'), " +
                    $"COALESCE((SELECT MAX({pkCol}) FROM {table}), 0) + 1, false);");

            writer.WriteLine();
            writer.WriteLine("COMMIT;");
        }

        // ─────────────────────────────────────────────
        // RESTORE
        // ─────────────────────────────────────────────
        private async void BtnRestore_Click(object? sender, EventArgs e)
        {
            if (MessageBox.Show(
                    "⚠  Warning: This will REPLACE all current data with the backup.\n\nContinue?",
                    "Confirm Restore", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)
                != DialogResult.Yes)
                return;

            using var dlg = new OpenFileDialog
            {
                Title  = "Select Backup File",
                Filter = "SQL Files (*.sql)|*.sql|All Files (*.*)|*.*",
            };
            if (dlg.ShowDialog() != DialogResult.OK) return;

            SetBusy(true);
            progressBar.Style = ProgressBarStyle.Marquee;

            try
            {
                AppendLog($"\n[{DateTime.Now:HH:mm:ss}]  Restoring from → {dlg.FileName}", Color.FromArgb(255, 152, 0));
                await Task.Run(() => RunRestore(dlg.FileName));
                SetStatus("Restore completed successfully.", successGreen);
                AppendLog("✓  Restore complete.", successGreen);
                MessageBox.Show("Database restored successfully!",
                    "Restore Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                SetStatus("Restore failed.", dangerRed);
                AppendLog($"✗  Error: {ex.Message}", dangerRed);
                MessageBox.Show($"Restore failed:\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                progressBar.Style = ProgressBarStyle.Continuous;
                SetBusy(false);
            }
        }

        private void RunRestore(string path)
        {
            string sql = File.ReadAllText(path, System.Text.Encoding.UTF8);
            InvokeLog("  Executing backup script …", accentGold);

            using var conn = new NpgsqlConnection(DbHelper.ConnectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.CommandTimeout = 300;
            cmd.ExecuteNonQuery();

            InvokeLog("  Script executed successfully.", Color.FromArgb(120, 180, 120));
        }

        // ─────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────

        // Converts a column value to a PostgreSQL-safe SQL literal.
        private static string PgLiteral(NpgsqlDataReader reader, int ordinal)
        {
            if (reader.IsDBNull(ordinal)) return "NULL";
            var v = reader.GetValue(ordinal);
            return v switch
            {
                bool   b  => b ? "TRUE" : "FALSE",
                DateTime d => $"'{d:yyyy-MM-dd HH:mm:ss}'",
                string s   => $"'{s.Replace("'", "''")}'",
                int or long or short or decimal or double or float => v.ToString()!,
                _          => $"'{v.ToString()!.Replace("'", "''")}'",
            };
        }

        private void SetBusy(bool busy)
        {
            this.Invoke(() =>
            {
                btnBackup.Enabled    = !busy;
                btnRestore.Enabled   = !busy;
                progressBar.Visible  = busy;
            });
        }

        private void SetStatus(string text, Color color)
        {
            this.Invoke(() =>
            {
                lblStatus.Text      = text;
                lblStatus.ForeColor = color;
            });
        }

        private void AppendLog(string message, Color color)
        {
            if (rtbLog.InvokeRequired) { rtbLog.Invoke(() => AppendLog(message, color)); return; }
            rtbLog.SelectionStart  = rtbLog.TextLength;
            rtbLog.SelectionLength = 0;
            rtbLog.SelectionColor  = color;
            rtbLog.AppendText(message + "\n");
            rtbLog.ScrollToCaret();
        }

        private void InvokeLog(string message, Color color) =>
            this.Invoke(() => AppendLog(message, color));
    }
}
