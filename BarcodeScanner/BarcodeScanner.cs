using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace BarcodeScanner
{
    public class MainForm : Form
    {
        // ── Controls ──────────────────────────────────────────────
        private Panel     panelSidebar;
        private Panel     panelMain;
        private Panel     panelHeader;
        private Panel     panelInputCard;
        private Panel     panelLogCard;
        private Panel     panelListCard;
        private Panel     panelProgress;

        private Label     lblAppTitle;
        private Label     lblAppSubtitle;
        private Label     lblBarcodeID;
        private Label     lblScanned;
        private Label     lblGenerated;
        private Label     lblScanCount;
        private Label     lblProgressInfo;

        private TextBox   txtBarcode;
        private RichTextBox rtbLog;
        private ListView  lvGenerated;

        private Button    btnClear;
        private Button    btnGenerateNow;
        private Button    btnExport;

        private ThemedProgressBar pbScan;

        // ── State ─────────────────────────────────────────────────
        private readonly List<string> _scannedBarcodes = new List<string>();
        private readonly List<string> _generatedBarcodes = new List<string>();
        private readonly Random       _rng              = new Random();
        private const    int          SCAN_TARGET        = 50;

        // ── Palette ───────────────────────────────────────────────
        private static readonly Color C_BG        = Color.FromArgb(10,  14,  26);
        private static readonly Color C_SIDEBAR   = Color.FromArgb(15,  20,  38);
        private static readonly Color C_CARD      = Color.FromArgb(20,  28,  52);
        private static readonly Color C_CARD2     = Color.FromArgb(24,  34,  60);
        private static readonly Color C_ACCENT    = Color.FromArgb(0,   210, 255);
        private static readonly Color C_ACCENT2   = Color.FromArgb(120,  80, 255);
        private static readonly Color C_SUCCESS   = Color.FromArgb(0,   230, 150);
        private static readonly Color C_TEXT      = Color.FromArgb(220, 235, 255);
        private static readonly Color C_MUTED     = Color.FromArgb(100, 130, 180);
        private static readonly Color C_BORDER    = Color.FromArgb(35,  55,  100);

        // ── Constructor ───────────────────────────────────────────
        public MainForm()
        {
            InitializeComponent();
            WireEvents();
        }

        // ═════════════════════════════════════════════════════════
        //  UI BUILD
        // ═════════════════════════════════════════════════════════
        private void InitializeComponent()
        {
            // Form
            this.Text            = "ECH Barcode Scanner Pro";
            this.Size            = new Size(1200, 860);
            this.MinimumSize     = new Size(960, 700);
            this.StartPosition   = FormStartPosition.CenterScreen;
            this.BackColor       = C_BG;
            this.ForeColor       = C_TEXT;
            this.Font            = new Font("Segoe UI", 9.5f);
            this.DoubleBuffered  = true;

            BuildSidebar();
            BuildMain();

            this.Controls.Add(panelMain);
            this.Controls.Add(panelSidebar);
        }

        // ── Sidebar ───────────────────────────────────────────────
        private void BuildSidebar()
        {
            panelSidebar = new Panel
            {
                Dock      = DockStyle.Left,
                Width     = 220,
                BackColor = C_SIDEBAR,
                Padding   = new Padding(0)
            };
            panelSidebar.Paint += PaintSidebarAccent;

            // Logo area
            var pnlLogo = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 90,
                BackColor = Color.Transparent
            };

            lblAppTitle = new Label
            {
                Text      = "ECH",
                Font      = new Font("Segoe UI Black", 28f, FontStyle.Bold),
                ForeColor = C_ACCENT,
                AutoSize  = false,
                Bounds    = new Rectangle(22, 20, 90, 42),
                BackColor = Color.Transparent
            };

            lblAppSubtitle = new Label
            {
                Text      = "BARCODE PRO",
                Font      = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = C_MUTED,
                AutoSize  = false,
                Bounds    = new Rectangle(22, 60, 150, 18),
                BackColor = Color.Transparent
            };

            pnlLogo.Controls.Add(lblAppTitle);
            pnlLogo.Controls.Add(lblAppSubtitle);

            // Divider
            var divider = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 1,
                BackColor = C_BORDER
            };

            // Stats panel
            var pnlStats = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 160,
                BackColor = Color.Transparent,
                Padding   = new Padding(16, 20, 16, 0)
            };

            lblScanned = BuildStatLabel("SCANNED", "0 / 50", Color.FromArgb(30, 40, 70), C_ACCENT);
            lblScanned.Bounds = new Rectangle(16, 20, 188, 55);

            lblGenerated = BuildStatLabel("GENERATED", "0", Color.FromArgb(30, 40, 70), C_SUCCESS);
            lblGenerated.Bounds = new Rectangle(16, 88, 188, 55);

            pnlStats.Controls.Add(lblScanned);
            pnlStats.Controls.Add(lblGenerated);

            // Bottom buttons
            btnClear = BuildSidebarButton("⟳  Clear All", 0);
            btnGenerateNow = BuildSidebarButton("⚡  Generate Now", 1);
            btnExport = BuildSidebarButton("↓  Export List", 2);

            var pnlButtons = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 160,
                BackColor = Color.Transparent
            };
            pnlButtons.Controls.Add(btnExport);
            pnlButtons.Controls.Add(btnGenerateNow);
            pnlButtons.Controls.Add(btnClear);

            panelSidebar.Controls.Add(pnlStats);
            panelSidebar.Controls.Add(divider);
            panelSidebar.Controls.Add(pnlLogo);
            panelSidebar.Controls.Add(pnlButtons);
        }

        private Label BuildStatLabel(string title, string value, Color bg, Color accent)
        {
            var lbl = new Label
            {
                BackColor = bg,
                ForeColor = C_TEXT,
                AutoSize  = false,
                Cursor    = Cursors.Default
            };
            lbl.Paint += (s, e) =>
            {
                var g   = e.Graphics;
                var rc  = lbl.ClientRectangle;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                // rounded rect
                using var path = RoundedRect(rc, 10);
                using var br   = new SolidBrush(bg);
                g.FillPath(br, path);
                // accent bar
                using var barBr = new SolidBrush(accent);
                g.FillRectangle(barBr, 0, 0, 4, rc.Height);
                // title
                TextRenderer.DrawText(g, title,
                    new Font("Segoe UI", 7f, FontStyle.Bold), new Rectangle(12, 8, rc.Width - 14, 16),
                    C_MUTED, TextFormatFlags.Left);
                // value  – pull the current text from Tag
                var val = lbl.Tag?.ToString() ?? value;
                TextRenderer.DrawText(g, val,
                    new Font("Segoe UI", 18f, FontStyle.Bold), new Rectangle(12, 22, rc.Width - 14, 28),
                    accent, TextFormatFlags.Left);
            };
            lbl.Tag = value;
            return lbl;
        }

        private Button BuildSidebarButton(string text, int index)
        {
            var btn = new Button
            {
                Text      = text,
                FlatStyle = FlatStyle.Flat,
                Font      = new Font("Segoe UI", 9.5f),
                ForeColor = C_MUTED,
                BackColor = Color.Transparent,
                Cursor    = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(14, 0, 0, 0),
                Bounds    = new Rectangle(0, index * 48 + 16, 220, 40),
                FlatAppearance = { BorderSize = 0, MouseOverBackColor = Color.FromArgb(30, 0, 210, 255) }
            };
            btn.MouseEnter += (s, _) => { btn.ForeColor = C_ACCENT; };
            btn.MouseLeave += (s, _) => { btn.ForeColor = C_MUTED;  };
            return btn;
        }

        private void PaintSidebarAccent(object sender, PaintEventArgs e)
        {
            var g  = e.Graphics;
            var rc = panelSidebar.ClientRectangle;
            // right border glow
            using var pen = new Pen(C_BORDER, 1);
            g.DrawLine(pen, rc.Right - 1, 0, rc.Right - 1, rc.Height);
            // top accent line
            using var grad = new LinearGradientBrush(
                new Point(0, 0), new Point(rc.Width, 0),
                C_ACCENT, Color.Transparent);
            g.FillRectangle(grad, 0, 0, rc.Width, 2);
        }

        // ── Main area ─────────────────────────────────────────────
        private void BuildMain()
        {
            panelMain = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = C_BG,
                Padding   = new Padding(24, 20, 24, 20)
            };

            BuildHeader();
            BuildInputCard();
            BuildProgressBar();
            BuildLogCard();
            BuildListCard();

            // Layout via TableLayoutPanel
            var layout = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 2,
                RowCount    = 4,
                BackColor   = Color.Transparent,
                Padding     = new Padding(24, 16, 24, 16),
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42f));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 65));   // header
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 90));   // input
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));   // progress
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // cards ← gets all remaining height

            layout.Controls.Add(panelHeader,   0, 0);
            layout.SetColumnSpan(panelHeader, 2);

            layout.Controls.Add(panelInputCard, 0, 1);
            layout.SetColumnSpan(panelInputCard, 2);

            layout.Controls.Add(panelProgress,  0, 2);
            layout.SetColumnSpan(panelProgress, 2);

            layout.Controls.Add(panelLogCard,   0, 3);
            layout.Controls.Add(panelListCard,  1, 3);

            panelMain.Controls.Add(layout);
        }

        private void BuildHeader()
        {
            panelHeader = new Panel { BackColor = Color.Transparent, Margin = new Padding(0, 0, 0, 8) };

            var lblTitle = new Label
            {
                Text      = "Barcode Scanner Dashboard",
                Font      = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = C_TEXT,
                AutoSize  = true,
                Location  = new Point(0, 8)
            };
            var lblSub = new Label
            {
                Text      = "Scan 50 barcodes · Auto-generate ECH codes · Export results",
                Font      = new Font("Segoe UI", 9f),
                ForeColor = C_MUTED,
                AutoSize  = true,
                Location  = new Point(2, 44)
            };

            panelHeader.Controls.Add(lblTitle);
            panelHeader.Controls.Add(lblSub);
        }

        private void BuildInputCard()
        {
            panelInputCard = new Panel
            {
                BackColor = C_CARD,
                Margin    = new Padding(0, 4, 8, 8),
                Padding   = new Padding(20, 16, 20, 16),
                Width = 350
                
            };
            panelInputCard.Paint += (s, e) => PaintCard(e, panelInputCard, C_ACCENT);

            lblBarcodeID = new Label
            {
                Text      = "BARCODE ID",
                Font      = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = C_ACCENT,
                AutoSize  = true,
                Location  = new Point(20, 16)
            };

            txtBarcode = new TextBox
            {
                Font            = new Font("Consolas", 14f),
                BackColor       = Color.FromArgb(12, 18, 38),
                ForeColor       = C_TEXT,
                BorderStyle     = BorderStyle.None,
                Location        = new Point(20, 38),
                Height          = 36,
                CharacterCasing = CharacterCasing.Upper
            };
            txtBarcode.Width = panelInputCard.Width - 40;
            txtBarcode.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;

            // underline
            var underline = new Panel
            {
                Height    = 2,
                BackColor = C_ACCENT,
                Location  = new Point(20, 76),
                Anchor    = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };
            underline.Width = panelInputCard.Width - 40;

            var lblHint = new Label
            {
                Text      = "Focus here and scan your barcode — press Enter or wait for auto-detect",
                Font      = new Font("Segoe UI", 8f),
                ForeColor = C_MUTED,
                AutoSize  = true,
                Location  = new Point(20, 82)
            };

            panelInputCard.Controls.Add(lblHint);
            panelInputCard.Controls.Add(underline);
            panelInputCard.Controls.Add(txtBarcode);
            panelInputCard.Controls.Add(lblBarcodeID);
        }

        private void BuildProgressBar()
        {
            panelProgress = new Panel
            {
                BackColor = Color.Transparent,
                Margin    = new Padding(0, 0, 8, 8),
                Width = 350
            };

            lblProgressInfo = new Label
            {
                Text      = "Progress: 0 / 50",
                Font      = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = C_MUTED,
                AutoSize  = true,
                Location  = new Point(0, 2)
            };

            pbScan = new ThemedProgressBar
            {
                Minimum   = 0,
                Maximum   = SCAN_TARGET,
                Value     = 0,
                Height    = 14,
                Anchor    = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
                Location  = new Point(0, 20)
            };

            panelProgress.Controls.Add(lblProgressInfo);
            panelProgress.Controls.Add(pbScan);

            panelProgress.Resize += (s, _) => pbScan.Width = panelProgress.Width;
        }

        private void BuildLogCard()
        {
            panelLogCard = new Panel
            {
                BackColor = C_CARD,
                Margin    = new Padding(0, 4, 8, 0),
                Padding   = new Padding(30),
                Height = 600,
                Width = 350
            };
            panelLogCard.Paint += (s, e) => PaintCard(e, panelLogCard, C_ACCENT);

            var lblLog = new Label
            {
                Text      = "SCAN LOG",
                Font      = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = C_ACCENT,
                AutoSize  = true,
                Location  = new Point(16, 14)
            };

            lblScanCount = new Label
            {
                Text      = "0 scans",
                Font      = new Font("Segoe UI", 7.5f),
                ForeColor = C_MUTED,
                AutoSize  = true,
                Location  = new Point(16, 30)
            };

            rtbLog = new RichTextBox
            {
                BackColor   = Color.FromArgb(12, 18, 38),
                ForeColor   = C_TEXT,
                BorderStyle = BorderStyle.None,
                Font        = new Font("Consolas", 9.5f),
                ReadOnly    = true,
                ScrollBars  = RichTextBoxScrollBars.Vertical,
                Anchor      = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Location    = new Point(16, 50),
                Size        = new Size(200, 300)   // initial placeholder; Resize event corrects it
            };

            panelLogCard.Controls.Add(rtbLog);
            panelLogCard.Controls.Add(lblScanCount);
            panelLogCard.Controls.Add(lblLog);

            // Keeps rtbLog filling the card whenever it resizes
            panelLogCard.Resize += (s, _) =>
            {
                rtbLog.Size = new Size(
                    panelLogCard.Width  - 32,
                    panelLogCard.Height - 66);
            };

            // One immediate sizing pass once the panel has a real handle/size
            panelLogCard.HandleCreated += (s, _) =>
            {
                rtbLog.Size = new Size(
                    panelLogCard.Width  - 32,
                    panelLogCard.Height - 66);
            };
        }

        private void BuildListCard()
        {
            panelListCard = new Panel
            {
                BackColor = C_CARD2,
                Margin    = new Padding(8, 4, 0, 0),
                Padding   = new Padding(16),
                Height = 600,
                Width = 350
            };
            panelListCard.Paint += (s, e) => PaintCard(e, panelListCard, C_SUCCESS);

            var lblListTitle = new Label
            {
                Text      = "GENERATED BARCODES",
                Font      = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = C_SUCCESS,
                AutoSize  = true,
                Location  = new Point(16, 14)
            };

            lvGenerated = new ListView
            {
                View            = View.Details,
                FullRowSelect   = true,
                GridLines       = false,
                BackColor       = Color.FromArgb(12, 18, 38),
                ForeColor       = C_TEXT,
                BorderStyle     = BorderStyle.None,
                Font            = new Font("Consolas", 9.5f),
                HeaderStyle     = ColumnHeaderStyle.Nonclickable,
                Anchor          = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Location        = new Point(16, 46),
                Size            = new Size(200, 300)   // initial placeholder; Resize corrects it
            };
            lvGenerated.Columns.Add("#",        38);
            lvGenerated.Columns.Add("Barcode",  220);
            lvGenerated.Columns.Add("Time",     100);

            // Style header
            lvGenerated.OwnerDraw = true;
            lvGenerated.DrawColumnHeader += (s, e) =>
            {
                e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(20, 30, 58)), e.Bounds);
                TextRenderer.DrawText(e.Graphics, e.Header.Text,
                    new Font("Segoe UI", 8f, FontStyle.Bold),
                    new Rectangle(e.Bounds.X + 4, e.Bounds.Y, e.Bounds.Width - 4, e.Bounds.Height),
                    C_SUCCESS, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
            };
            lvGenerated.DrawItem += (s, e) => e.DrawDefault = true;
            lvGenerated.DrawSubItem += (s, e) => e.DrawDefault = true;

            panelListCard.Controls.Add(lvGenerated);
            panelListCard.Controls.Add(lblListTitle);

            panelListCard.Resize += (s, _) =>
            {
                lvGenerated.Size = new Size(
                    panelListCard.Width  - 32,
                    panelListCard.Height - 62);
                if (lvGenerated.Columns.Count > 1)
                    lvGenerated.Columns[1].Width = lvGenerated.Width - 38 - 100 - 4;
            };

            panelListCard.HandleCreated += (s, _) =>
            {
                lvGenerated.Size = new Size(
                    panelListCard.Width  - 32,
                    panelListCard.Height - 62);
                if (lvGenerated.Columns.Count > 1)
                    lvGenerated.Columns[1].Width = lvGenerated.Width - 38 - 100 - 4;
            };
        }

        // ── Helpers ───────────────────────────────────────────────
        private static void PaintCard(PaintEventArgs e, Panel panel, Color accent)
        {
            var g  = e.Graphics;
            var rc = panel.ClientRectangle;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var path = RoundedRect(rc, 12);
            using var br   = new SolidBrush(panel.BackColor);
            g.FillPath(br, path);
            using var pen  = new Pen(Color.FromArgb(40, accent.R, accent.G, accent.B), 1);
            g.DrawPath(pen, path);
            // top glow bar
            using var grad = new LinearGradientBrush(
                new Point(0, 0), new Point(80, 0), accent, Color.Transparent);
            g.FillRectangle(grad, rc.X + 12, rc.Y, 80, 2);
        }

        private static GraphicsPath RoundedRect(Rectangle rc, int r)
        {
            var path = new GraphicsPath();
            path.AddArc(rc.X,                 rc.Y,                 r * 2, r * 2, 180, 90);
            path.AddArc(rc.Right - r * 2,     rc.Y,                 r * 2, r * 2, 270, 90);
            path.AddArc(rc.Right - r * 2,     rc.Bottom - r * 2,    r * 2, r * 2,   0, 90);
            path.AddArc(rc.X,                 rc.Bottom - r * 2,    r * 2, r * 2,  90, 90);
            path.CloseFigure();
            return path;
        }

        private static void SetProgressBarColor(ProgressBar pb) { /* styling handled by ThemedProgressBar */ }

        // ═════════════════════════════════════════════════════════
        //  EVENTS & LOGIC
        // ═════════════════════════════════════════════════════════
        private void WireEvents()
        {
            txtBarcode.KeyDown     += TxtBarcode_KeyDown;
            txtBarcode.TextChanged += TxtBarcode_TextChanged;
            btnClear.Click         += (s, _) => ClearAll();
            btnGenerateNow.Click   += (s, _) => GenerateBarcodes();
            btnExport.Click        += (s, _) => ExportList();
            this.Load              += (s, _) => txtBarcode.Focus();
        }

        private System.Windows.Forms.Timer _autoTimerFinal;
        private void TxtBarcode_TextChanged(object sender, EventArgs e)
        {
            // Auto-accept after 300 ms of no typing (typical scanner behaviour)
            if (_autoTimerFinal == null)
            {
                _autoTimerFinal = new System.Windows.Forms.Timer { Interval = 300 };
                _autoTimerFinal.Tick    += (s, _) => { _autoTimerFinal.Stop(); AcceptBarcode(); };
            }
            _autoTimerFinal.Stop();
            if (txtBarcode.Text.Length >= 4)
                _autoTimerFinal.Start();
        }

        private void TxtBarcode_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                _autoTimerFinal?.Stop();
                AcceptBarcode();
            }
        }

        private void AcceptBarcode()
        {
            var code = txtBarcode.Text.Trim();
            if (string.IsNullOrEmpty(code)) return;

            _scannedBarcodes.Add(code);

            // Append to RichTextBox with colour coding
            AppendLog(code);

            // Update stats
            UpdateStats();

            txtBarcode.Clear();
            txtBarcode.Focus();

            // Auto-generate when target hit
            if (_scannedBarcodes.Count == SCAN_TARGET)
                GenerateBarcodes();
        }

        private void AppendLog(string code)
        {
            int idx = _scannedBarcodes.Count;
            rtbLog.SelectionStart = rtbLog.TextLength;
            rtbLog.SelectionColor = C_MUTED;
            rtbLog.AppendText($"[{idx:D3}] ");
            rtbLog.SelectionColor = C_ACCENT;
            rtbLog.AppendText(code);
            rtbLog.SelectionColor = C_MUTED;
            rtbLog.AppendText($"   {DateTime.Now:HH:mm:ss}\n");
            rtbLog.ScrollToCaret();

            lblScanCount.Text = $"{idx} scan{(idx == 1 ? "" : "s")}";
        }

        private void UpdateStats()
        {
            int cnt = _scannedBarcodes.Count;
            int cap = Math.Min(cnt, SCAN_TARGET);

            pbScan.Value          = cap;
            lblProgressInfo.Text  = $"Progress: {cnt} / {SCAN_TARGET}";

            lblScanned.Tag        = $"{cnt} / {SCAN_TARGET}";
            lblScanned.Invalidate();
        }

        private void GenerateBarcodes()
        {
            // Generate one new barcode per 50 scans (or fraction)
            int batches = Math.Max(1, _scannedBarcodes.Count / SCAN_TARGET);
            for (int i = 0; i < batches; i++)
            {
                string julian  = DateTime.Now.DayOfYear.ToString("D3");
                string rand5   = _rng.Next(10000, 99999).ToString();
                string newCode = $"ECH53567{DateTime.Now:yy}{julian}{rand5}";
                _generatedBarcodes.Add(newCode);
                AddToListView(newCode);
            }

            lblGenerated.Tag = _generatedBarcodes.Count.ToString();
            lblGenerated.Invalidate();

            // Highlight last row
            if (lvGenerated.Items.Count > 0)
            {
                var last = lvGenerated.Items[lvGenerated.Items.Count - 1];
                last.BackColor = Color.FromArgb(0, 60, 40);
                last.ForeColor = C_SUCCESS;
                last.EnsureVisible();
            }

            MessageBox.Show(
                $"✓  {batches} barcode{(batches == 1 ? "" : "s")} generated successfully!\n\n" +
                $"Latest: {_generatedBarcodes[_generatedBarcodes.Count - 1]}",
                "Generation Complete",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void AddToListView(string code)
        {
            var item = new ListViewItem(lvGenerated.Items.Count + 1 + "");
            item.SubItems.Add(code);
            item.SubItems.Add(DateTime.Now.ToString("HH:mm:ss"));
            lvGenerated.Items.Add(item);
        }

        private void ClearAll()
        {
            if (MessageBox.Show("Clear all scanned barcodes and the log?",
                "Confirm Clear", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            _scannedBarcodes.Clear();
            rtbLog.Clear();
            pbScan.Value         = 0;
            lblProgressInfo.Text = "Progress: 0 / 50";
            lblScanCount.Text    = "0 scans";
            lblScanned.Tag       = "0 / 50";
            lblScanned.Invalidate();
            txtBarcode.Focus();
        }

        private void ExportList()
        {
            if (_generatedBarcodes.Count == 0)
            {
                MessageBox.Show("No generated barcodes to export yet.", "Export",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            using var dlg = new SaveFileDialog
            {
                Filter   = "Text File (*.txt)|*.txt|CSV (*.csv)|*.csv",
                FileName = $"ECH_Generated_{DateTime.Now:yyyyMMdd_HHmmss}"
            };
            if (dlg.ShowDialog() != DialogResult.OK) return;

            string sep = dlg.FilterIndex == 2 ? "," : "\t";
            var lines  = new System.Text.StringBuilder();
            lines.AppendLine($"#\tBarcode\tGenerated");
            for (int i = 0; i < _generatedBarcodes.Count; i++)
                lines.AppendLine($"{i + 1}{sep}{_generatedBarcodes[i]}{sep}{DateTime.Now:yyyy-MM-dd}");

            System.IO.File.WriteAllText(dlg.FileName, lines.ToString());
            MessageBox.Show("Export complete!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    // ── Custom owner-drawn progress bar ───────────────────────────
    internal class ThemedProgressBar : ProgressBar
    {
        public ThemedProgressBar()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g  = e.Graphics;
            var rc = ClientRectangle;

            // Background
            using var bgBrush = new SolidBrush(Color.FromArgb(20, 30, 58));
            g.FillRectangle(bgBrush, rc);

            // Filled portion
            if (Maximum > 0 && Value > 0)
            {
                int fillW = (int)((double)Value / Maximum * rc.Width);
                if (fillW > 0)
                {
                    var fillRect = new Rectangle(0, 0, fillW, rc.Height);
                    using var fgBrush = new LinearGradientBrush(
                        new Point(0, 0), new Point(fillW, 0),
                        Color.FromArgb(0, 180, 255),
                        Color.FromArgb(0, 230, 200));
                    g.FillRectangle(fgBrush, fillRect);
                }
            }

            // Subtle border
            using var pen = new Pen(Color.FromArgb(35, 55, 100), 1);
            g.DrawRectangle(pen, 0, 0, rc.Width - 1, rc.Height - 1);
        }
    }

}
