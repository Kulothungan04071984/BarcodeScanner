using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Xml.Linq;
using System.Configuration;
using BarTender;

namespace BarcodeScannerNew
{
    public class MainFormnew : Form
    {
        // ── Controls ──────────────────────────────────────────────
        private Panel panelSidebar;
        private Panel panelMain;
        private Panel panelHeader;
        private Panel panelDropdownCard;
        private Panel panelTrayCard;
        private Panel panelInputCard;
        private Panel panelLogCard;
        private Panel panelListCard;
        private Panel panelProgress;

        private Label lblAppTitle;
        private Label lblAppSubtitle;
        private Label lblBarcodeID;
        private Label lblScanned;
        private Label lblGenerated;
        private Label lblScanCount;
        private Label lblProgressInfo;
        private Label lblTrayID;
        private Label lblTrayValue;

        private ComboBox cmbCustomer;
        private ComboBox cmbProduct;
        private ComboBox cmbProductName;
        private ComboBox cmbFGNumber;
        private ComboBox cmbWorkOrder;

        private TextBox txtBarcode;
        private RichTextBox rtbLog;
        private ListView lvGenerated;

        private Button btnClear;
        private Button btnCreateTray;
        private Button btnExport;

        private ThemedProgressBar pbScan;

        // ── State ─────────────────────────────────────────────────
        private readonly List<string> _scannedBarcodes = new List<string>();
        private string _currentTrayID = null;
        private int _traySerial = 0;
        private bool _trayCreated = false;
        private int SCAN_TARGET = 0;
        private const int OBA_TARGET = 2;
        public string[] nextidinfo = { "", "" };
        public string[] nextstages = { "", "" };
        private CheckBox chkFail;
        private ComboBox cmbFailReason;
        // ── DB ────────────────────────────────────────────────────
        private const string CONN_STR =
            "Server=192.168.1.146;Database=Barcode;User Id=sa;Password=syrma@123;" +
            "Connect Timeout=10;";

        private const string CONN_STREss =
            "Server=192.168.1.146;Database=Essencore;User Id=sa;Password=syrma@123;" +
            "Connect Timeout=10;";

        private const string CONN_STR_stage =
           "Server=192.168.1.181;Database=SFCS;User Id=sa;Password=Syrma@2022;" +
           "Connect Timeout=10;";

        // ── Palette ───────────────────────────────────────────────
        private static readonly Color C_BG = Color.FromArgb(10, 14, 26);
        private static readonly Color C_SIDEBAR = Color.FromArgb(15, 20, 38);
        private static readonly Color C_CARD = Color.FromArgb(20, 28, 52);
        private static readonly Color C_CARD2 = Color.FromArgb(24, 34, 60);
        private static readonly Color C_ACCENT = Color.FromArgb(0, 210, 255);
        private static readonly Color C_ACCENT2 = Color.FromArgb(120, 80, 255);
        private static readonly Color C_SUCCESS = Color.FromArgb(0, 230, 150);
        private static readonly Color C_WARNING = Color.FromArgb(255, 180, 0);
        private static readonly Color C_TEXT = Color.FromArgb(220, 235, 255);
        private static readonly Color C_MUTED = Color.FromArgb(100, 130, 180);
        private static readonly Color C_BORDER = Color.FromArgb(35, 55, 100);
        private static readonly Color C_INPUT = Color.FromArgb(30, 36, 54);

        // ── Constructor ───────────────────────────────────────────
        public MainFormnew()
        {
            InitializeComponent();
            WireEvents();
            LoadCustomers();

        }

        // ═════════════════════════════════════════════════════════
        //  UI BUILD
        // ═════════════════════════════════════════════════════════
        private void InitializeComponent()
        {
            this.Text = "ECH Barcode Scanner Pro";
            this.Size = new Size(1300, 920);
            this.MinimumSize = new Size(1100, 780);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = C_BG;
            this.ForeColor = C_TEXT;
            this.Font = new Font("Segoe UI", 9.5f);
            this.DoubleBuffered = true;

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
                Dock = DockStyle.Left,
                Width = 220,
                BackColor = C_SIDEBAR,
            };
            panelSidebar.Paint += PaintSidebarAccent;

            var pnlLogo = new Panel
            {
                Dock = DockStyle.Top,
                Height = 90,
                BackColor = Color.Transparent
            };

            lblAppTitle = new Label
            {
                Text = "ECH",
                Font = new Font("Segoe UI Black", 28f, FontStyle.Bold),
                ForeColor = C_ACCENT,
                AutoSize = false,
                Bounds = new Rectangle(22, 20, 90, 42),
                BackColor = Color.Transparent
            };

            lblAppSubtitle = new Label
            {
                Text = "BARCODE PRO",
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = C_MUTED,
                AutoSize = false,
                Bounds = new Rectangle(22, 60, 150, 18),
                BackColor = Color.Transparent
            };

            pnlLogo.Controls.Add(lblAppTitle);
            pnlLogo.Controls.Add(lblAppSubtitle);

            var divider = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = C_BORDER };

            var pnlStats = new Panel
            {
                Dock = DockStyle.Top,
                Height = 160,
                BackColor = Color.Transparent,
                Padding = new Padding(16, 20, 16, 0)
            };

            lblScanned = BuildStatLabel("SCANNED", "0", Color.FromArgb(30, 40, 70), C_ACCENT);
            lblScanned.Bounds = new Rectangle(16, 20, 188, 55);

            lblGenerated = BuildStatLabel("TRAY BOARDS", "0", Color.FromArgb(30, 40, 70), C_SUCCESS);
            lblGenerated.Bounds = new Rectangle(16, 88, 188, 55);

            pnlStats.Controls.Add(lblScanned);
            pnlStats.Controls.Add(lblGenerated);

            btnClear = BuildSidebarButton("⟳  Clear All", 0);
            btnCreateTray = BuildSidebarButton("⚡  Create Tray ID", 1);
            btnExport = BuildSidebarButton("↓  Export List", 2);

            var pnlButtons = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 160,
                BackColor = Color.Transparent
            };
            pnlButtons.Controls.Add(btnExport);
            pnlButtons.Controls.Add(btnCreateTray);
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
                AutoSize = false,
                Cursor = Cursors.Default
            };
            lbl.Paint += (s, e) =>
            {
                var g = e.Graphics;
                var rc = lbl.ClientRectangle;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = RoundedRect(rc, 10);
                using var br = new SolidBrush(bg);
                g.FillPath(br, path);
                using var barBr = new SolidBrush(accent);
                g.FillRectangle(barBr, 0, 0, 4, rc.Height);
                TextRenderer.DrawText(g, title,
                    new Font("Segoe UI", 7f, FontStyle.Bold),
                    new Rectangle(12, 8, rc.Width - 14, 16), C_MUTED, TextFormatFlags.Left);
                var val = lbl.Tag?.ToString() ?? value;
                TextRenderer.DrawText(g, val,
                    new Font("Segoe UI", 18f, FontStyle.Bold),
                    new Rectangle(12, 22, rc.Width - 14, 28), accent, TextFormatFlags.Left);
            };
            lbl.Tag = value;
            return lbl;
        }

        private Button BuildSidebarButton(string text, int index)
        {
            var btn = new Button
            {
                Text = text,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = C_MUTED,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(14, 0, 0, 0),
                Bounds = new Rectangle(0, index * 48 + 16, 220, 40),
                FlatAppearance = { BorderSize = 0, MouseOverBackColor = Color.FromArgb(30, 0, 210, 255) }
            };
            btn.MouseEnter += (s, _) => btn.ForeColor = C_ACCENT;
            btn.MouseLeave += (s, _) => btn.ForeColor = C_MUTED;
            return btn;
        }

        private void PaintSidebarAccent(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            var rc = panelSidebar.ClientRectangle;
            using var pen = new Pen(C_BORDER, 1);
            g.DrawLine(pen, rc.Right - 1, 0, rc.Right - 1, rc.Height);
            using var grad = new LinearGradientBrush(
                new Point(0, 0), new Point(rc.Width, 0), C_ACCENT, Color.Transparent);
            g.FillRectangle(grad, 0, 0, rc.Width, 2);
        }

        // ── Main area ─────────────────────────────────────────────
        private void BuildMain()
        {
            panelMain = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = C_BG,
            };

            BuildHeader();
            BuildDropdownCard();
            BuildTrayCard();
            BuildInputCard();
            BuildProgressBar();
            BuildLogCard();
            BuildListCard();

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 6,
                BackColor = Color.Transparent,
                Padding = new Padding(24, 16, 24, 16),
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42f));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 65));   // row 0: header
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));  // row 1: dropdowns
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));   // row 2: tray id display
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 90));   // row 3: input
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));   // row 4: progress
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // row 5: log + list

            layout.Controls.Add(panelHeader, 0, 0); layout.SetColumnSpan(panelHeader, 2);
            layout.Controls.Add(panelDropdownCard, 0, 1); layout.SetColumnSpan(panelDropdownCard, 2);
            layout.Controls.Add(panelTrayCard, 0, 2); layout.SetColumnSpan(panelTrayCard, 2);
            layout.Controls.Add(panelInputCard, 0, 3); layout.SetColumnSpan(panelInputCard, 2);
            layout.Controls.Add(panelProgress, 0, 4); layout.SetColumnSpan(panelProgress, 2);
            layout.Controls.Add(panelLogCard, 0, 5);
            layout.Controls.Add(panelListCard, 1, 5);

            panelMain.Controls.Add(layout);
        }

        private void BuildHeader()
        {
            panelHeader = new Panel { BackColor = Color.Transparent, Margin = new Padding(0, 0, 0, 8) };

            var lblTitle = new Label
            {
                Text = "Barcode Scanner Dashboard",
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = C_TEXT,
                AutoSize = true,
                Location = new Point(0, 8)
            };
            var lblSub = new Label
            {
                Text = "Select Customer → FG Number → Work Order · Create Tray · Scan boards",
                Font = new Font("Segoe UI", 9f),
                ForeColor = C_MUTED,
                AutoSize = true,
                Location = new Point(2, 44)
            };

            panelHeader.Controls.Add(lblTitle);
            panelHeader.Controls.Add(lblSub);
        }

        // ── Dropdown Card ─────────────────────────────────────────
        private void BuildDropdownCard()
        {
            panelDropdownCard = new Panel
            {
                BackColor = C_CARD,
                Margin = new Padding(0, 4, 0, 8),
                Width = 1000
            };
            panelDropdownCard.Paint += (s, e) => PaintCard(e, panelDropdownCard, C_ACCENT2);

            // Helper to build a labelled combo
            ComboBox MakeCombo(string labelText, int x, out Label lbl)
            {
                lbl = new Label
                {
                    Text = labelText,
                    Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                    ForeColor = C_ACCENT2,
                    AutoSize = true,
                    Location = new Point(x, 14)
                };

                var cmb = new ComboBox
                {
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Font = new Font("Segoe UI", 10f),
                    BackColor = Color.FromArgb(12, 18, 38),
                    ForeColor = C_TEXT,
                    FlatStyle = FlatStyle.Flat,
                    Location = new Point(x, 34),
                    Width = 180,
                    Height = 50,
                };
                // Dark dropdown colours via owner-draw workaround
                cmb.DrawMode = DrawMode.OwnerDrawFixed;
                cmb.DrawItem += CmbDrawItem;

                return cmb;
            }

            Label lbl1, lbl2, lbl3, lbl4, lbl5;
            cmbCustomer = MakeCombo("CUSTOMER", 10, out lbl1);
            cmbProduct = MakeCombo("PRODUCT", 210, out lbl2);
            cmbProductName = MakeCombo("PRODUCTNAME", 410, out lbl3);
            cmbFGNumber = MakeCombo("FG NUMBER", 610, out lbl4);
            cmbWorkOrder = MakeCombo("WORK ORDER", 810, out lbl5);

            //panelDropdownCard.Controls.AddRange(new Control[]
            //    { lbl1, cmbCustomer, lbl2, cmbFGNumber, lbl3, cmbWorkOrder });
            panelDropdownCard.Controls.AddRange(new Control[]
            { lbl1,cmbCustomer, lbl2, cmbProduct,lbl3, cmbProductName, lbl4,cmbFGNumber,lbl5,cmbWorkOrder});
        }

        private void CmbDrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            e.Graphics.FillRectangle(
                new SolidBrush((e.State & DrawItemState.Selected) != 0
                    ? Color.FromArgb(30, 60, 100)
                    : Color.FromArgb(12, 18, 38)),
                e.Bounds);
            var cmb = (ComboBox)sender;
            //TextRenderer.DrawText(e.Graphics, cmb.Items[e.Index].ToString(),
            TextRenderer.DrawText(e.Graphics,
    cmb.GetItemText(cmb.Items[e.Index]),
                e.Font, e.Bounds, C_TEXT,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
        }

        // ── Tray ID Display Card ──────────────────────────────────
        private void BuildTrayCard()
        {
            panelTrayCard = new Panel
            {
                BackColor = Color.FromArgb(18, 28, 50),
                Margin = new Padding(0, 0, 0, 8),
                Width = 1000
            };
            panelTrayCard.Paint += (s, e) => PaintCard(e, panelTrayCard, C_WARNING);

            var lbl = new Label
            {
                Text = "CURRENT TRAY ID",
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = C_WARNING,
                AutoSize = true,
                Location = new Point(20, 12)
            };

            lblTrayValue = new Label
            {
                Text = "— No tray created yet. Select dropdowns and click ⚡ Create Tray ID —",
                Font = new Font("Consolas", 13f, FontStyle.Bold),
                ForeColor = C_WARNING,
                AutoSize = true,
                Location = new Point(20, 32)
            };

            panelTrayCard.Controls.Add(lbl);
            panelTrayCard.Controls.Add(lblTrayValue);
        }

        // ── Input Card ────────────────────────────────────────────
        //private void BuildInputCard()
        //{
        //    panelInputCard = new Panel
        //    {
        //        BackColor = C_CARD,
        //        Margin = new Padding(0, 4, 0, 8),
        //        Padding = new Padding(20, 16, 20, 16),
        //        Width = 400
        //    };
        //    panelInputCard.Paint += (s, e) => PaintCard(e, panelInputCard, C_ACCENT);

        //    lblBarcodeID = new Label
        //    {
        //        Text = "BOARD BARCODE",
        //        Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
        //        ForeColor = C_ACCENT,
        //        AutoSize = true,
        //        Location = new Point(20, 16)
        //    };

        //    txtBarcode = new TextBox
        //    {
        //        Font = new Font("Consolas", 14f),
        //        BackColor = Color.FromArgb(12, 18, 38),
        //        ForeColor = C_TEXT,
        //        BorderStyle = BorderStyle.None,
        //        Location = new Point(20, 38),
        //        Height = 36,
        //        CharacterCasing = CharacterCasing.Upper,
        //        Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
        //        Enabled = false   // disabled until tray is created
        //    };

        //    var underline = new Panel
        //    {
        //        Height = 2,
        //        BackColor = C_ACCENT,
        //        Location = new Point(20, 76),
        //        Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
        //    };

        //    var lblHint = new Label
        //    {
        //        Text = "Create a Tray ID first, then scan boards — each scan inserts into the database",
        //        Font = new Font("Segoe UI", 8f),
        //        ForeColor = C_MUTED,
        //        AutoSize = true,
        //        Location = new Point(20, 82)
        //    };

        //    panelInputCard.Controls.Add(lblHint);
        //    panelInputCard.Controls.Add(underline);
        //    panelInputCard.Controls.Add(txtBarcode);
        //    panelInputCard.Controls.Add(lblBarcodeID);

        //    panelInputCard.Resize += (s, _) =>
        //    {
        //        txtBarcode.Width = panelInputCard.Width - 40;
        //        underline.Width = panelInputCard.Width - 40;
        //    };
        //}

        private void CardBorder_Paint(object sender, PaintEventArgs e)
        {
            var panel = sender as Panel;
            if (panel == null) return;

            using (var pen = new Pen(C_ACCENT, 1.5f))
            {
                // Draw border inset by 1px so it's not clipped
                var rect = new Rectangle(0, 0, panel.Width - 1, panel.Height - 1);
                e.Graphics.DrawRectangle(pen, rect);
            }
        }
        private void BuildInputCard()
        {
            panelInputCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 6, 0, 6),
            };
            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = C_CARD,
                Padding = new Padding(16, 10, 16, 10),
            };
            card.Paint += CardBorder_Paint;

            var lblTitle = new Label
            {
                Text = "BOARD BARCODE",
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = C_ACCENT,
                AutoSize = true,
                Location = new Point(16, 10),
            };
            txtBarcode = new TextBox
            {
                Location = new Point(16, 30),
                Width = 280,
                Height = 32,
                BackColor = C_INPUT,
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Consolas", 11f),
            };
            txtBarcode.KeyDown += TxtBarcode_KeyDown;

            // ── Fail CheckBox ─────────────────────────────────────────
            chkFail = new CheckBox
            {
                Text = "Fail",
                Location = new Point(316, 33),   // right of barcode box
                AutoSize = true,
                ForeColor = Color.OrangeRed,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Checked = false,
            };
            chkFail.CheckedChanged += ChkFail_CheckedChanged;
            cmbFailReason = new ComboBox
            {
                Location = new Point(375, 30),
                Width = 210,
                Height = 32,
                BackColor = C_INPUT,
                ForeColor = Color.OrangeRed,
                Font = new Font("Segoe UI", 9f),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Enabled = false,   // only enabled when chkFail is checked
                FlatStyle = FlatStyle.Flat,
            };
            cmbFailReason.Items.AddRange(new string[]
   {
        "-- Select Reason --",
        "Short Circuit",
        "Open Circuit",
        "Missing Component",
        "Wrong Component",
        "Solder Bridge",
        "Damaged PCB",
        "Component Shifted",
        "Cold Solder Joint",
        "Other",
   });
            cmbFailReason.SelectedIndex = 0;

            card.Controls.Add(lblTitle);
            card.Controls.Add(txtBarcode);
            card.Controls.Add(chkFail);
            card.Controls.Add(cmbFailReason);

            panelInputCard.Controls.Add(card);
        }


        // ── Toggle combobox when checkbox changes ─────────────────────
        private void ChkFail_CheckedChanged(object sender, EventArgs e)
        {
            bool failing = chkFail.Checked;
            cmbFailReason.Enabled = failing;

            if (!failing)
                cmbFailReason.SelectedIndex = 0;

            // Red tint on barcode box while fail mode is active
            txtBarcode.BackColor = failing ? Color.FromArgb(60, 20, 20) : C_INPUT;
        }
        private void BuildProgressBar()
        {
            panelProgress = new Panel
            {
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 8),
                Width = 400
            };

            lblProgressInfo = new Label
            {
                Text = "Boards scanned in current tray: 0",
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = C_MUTED,
                AutoSize = true,
                Location = new Point(0, 2)
            };

            pbScan = new ThemedProgressBar
            {
                Minimum = 0,
                Maximum = SCAN_TARGET,
                Value = 0,
                Height = 14,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
                Location = new Point(0, 20)
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
                Margin = new Padding(0, 4, 8, 0),
                Width = 400,
                Height = 800
            };
            panelLogCard.Paint += (s, e) => PaintCard(e, panelLogCard, C_ACCENT);

            var lblLog = new Label
            {
                Text = "SCAN LOG",
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = C_ACCENT,
                AutoSize = true,
                Location = new Point(16, 14)
            };

            lblScanCount = new Label
            {
                Text = "0 scans",
                Font = new Font("Segoe UI", 7.5f),
                ForeColor = C_MUTED,
                AutoSize = true,
                Location = new Point(16, 30)
            };

            rtbLog = new RichTextBox
            {
                BackColor = Color.FromArgb(12, 18, 38),
                ForeColor = C_TEXT,
                BorderStyle = BorderStyle.None,
                Font = new Font("Consolas", 9.5f),
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Location = new Point(16, 50),
                Size = new Size(200, 300)
            };

            panelLogCard.Controls.Add(rtbLog);
            panelLogCard.Controls.Add(lblScanCount);
            panelLogCard.Controls.Add(lblLog);

            panelLogCard.Resize += (s, _) =>
                rtbLog.Size = new Size(panelLogCard.Width - 32, panelLogCard.Height - 66);
            panelLogCard.HandleCreated += (s, _) =>
                rtbLog.Size = new Size(panelLogCard.Width - 32, panelLogCard.Height - 66);
        }

        private void BuildListCard()
        {
            panelListCard = new Panel
            {
                BackColor = C_CARD2,
                Margin = new Padding(8, 4, 0, 0),
                Width = 400,
                Height = 800
            };
            panelListCard.Paint += (s, e) => PaintCard(e, panelListCard, C_SUCCESS);

            var lblListTitle = new Label
            {
                Text = "SCANNED BOARDS — CURRENT TRAY",
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = C_SUCCESS,
                AutoSize = true,
                Location = new Point(16, 14)
            };

            lvGenerated = new ListView
            {
                View = View.Details,
                FullRowSelect = true,
                GridLines = false,
                BackColor = Color.FromArgb(12, 18, 38),
                ForeColor = C_TEXT,
                BorderStyle = BorderStyle.None,
                Font = new Font("Consolas", 9.5f),
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Location = new Point(16, 46),
                Size = new Size(200, 300)
            };
            lvGenerated.Columns.Add("#", 38);
            lvGenerated.Columns.Add("Board Barcode", 220);
            lvGenerated.Columns.Add("Time", 100);

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
                lvGenerated.Size = new Size(panelListCard.Width - 32, panelListCard.Height - 62);
                if (lvGenerated.Columns.Count > 1)
                    lvGenerated.Columns[1].Width = lvGenerated.Width - 38 - 100 - 4;
            };
            panelListCard.HandleCreated += (s, _) =>
            {
                lvGenerated.Size = new Size(panelListCard.Width - 32, panelListCard.Height - 62);
                if (lvGenerated.Columns.Count > 1)
                    lvGenerated.Columns[1].Width = lvGenerated.Width - 38 - 100 - 4;
            };
        }

        // ── Shared paint helpers ───────────────────────────────────
        private static void PaintCard(PaintEventArgs e, Panel panel, Color accent)
        {
            var g = e.Graphics;
            var rc = panel.ClientRectangle;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var path = RoundedRect(rc, 12);
            using var br = new SolidBrush(panel.BackColor);
            g.FillPath(br, path);
            using var pen = new Pen(Color.FromArgb(40, accent.R, accent.G, accent.B), 1);
            g.DrawPath(pen, path);
            using var grad = new LinearGradientBrush(
                new Point(0, 0), new Point(80, 0), accent, Color.Transparent);
            g.FillRectangle(grad, rc.X + 12, rc.Y, 80, 2);
        }

        private static GraphicsPath RoundedRect(Rectangle rc, int r)
        {
            var path = new GraphicsPath();
            path.AddArc(rc.X, rc.Y, r * 2, r * 2, 180, 90);
            path.AddArc(rc.Right - r * 2, rc.Y, r * 2, r * 2, 270, 90);
            path.AddArc(rc.Right - r * 2, rc.Bottom - r * 2, r * 2, r * 2, 0, 90);
            path.AddArc(rc.X, rc.Bottom - r * 2, r * 2, r * 2, 90, 90);
            path.CloseFigure();
            return path;
        }

        // ═════════════════════════════════════════════════════════
        //  DATABASE HELPERS
        // ═════════════════════════════════════════════════════════

        private SqlConnection OpenConnection()
        {
            var conn = new SqlConnection(CONN_STR);
            conn.Open();
            return conn;
        }

        private SqlConnection OpenEssConnection()
        {
            var conEss=new SqlConnection(CONN_STREss);
            conEss.Open();
            return conEss;
        }

        private SqlConnection OpenSatgeConnection()
        {
            var conSatge = new SqlConnection(CONN_STR_stage);
            conSatge.Open();
            return conSatge;
        }
        /// <summary>Load distinct customers from the DB.</summary
        /// 
        public class CustomerItem
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        private void LoadProduct(string customerid)
        {
            try
            {
                if (customerid == "1" || customerid == "2")
                {
                    cmbProduct.Items.Clear();
                    using var conn = OpenConnection();
                    SqlCommand cmd = new SqlCommand("pro_getProductName", conn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@cusid", customerid);
                    SqlDataAdapter da = new SqlDataAdapter(cmd);

                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    if (dt != null)
                    {
                        if (dt.Rows.Count > 0)
                        {
                            cmbProduct.DataSource = null;
                            cmbProduct.DataSource = dt;
                            cmbProduct.DisplayMember = "productname";
                            cmbProduct.ValueMember = "prodcutid";
                            conn.Close();
                            cmbProduct.SelectedValue = 0;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AppendLog($"[DB ERROR] Loading Prodcut: {ex.Message}", C_WARNING);
            }
        }
        private void LoadCustomers()
        {
            cmbCustomer.Items.Clear();
            cmbFGNumber.Items.Clear();
            cmbWorkOrder.Items.Clear();
            cmbFGNumber.Enabled = false;
            cmbWorkOrder.Enabled = false;

            try
            {
                cmbCustomer.Items.Clear();
                using var conn = OpenConnection();
                SqlCommand cmd = new SqlCommand("pro_getcustomer", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                SqlDataAdapter da = new SqlDataAdapter(cmd);

                DataTable dt = new DataTable();
                da.Fill(dt);
                if (dt != null)
                {
                    if (dt.Rows.Count > 0)
                    {
                        cmbCustomer.DataSource = null;
                        cmbCustomer.DataSource = dt;
                        cmbCustomer.DisplayMember = "Name";
                        cmbCustomer.ValueMember = "id";
                        conn.Close();
                        cmbCustomer.SelectedValue = 0;
                    }
                   
                }
                //                cmbCustomer.Items.AddRange(new string[]
                //{
                //    "Essencore",
                //    });
            }
            catch (Exception ex)
            {
                AppendLog($"[DB ERROR] Loading customers: {ex.Message}", C_WARNING);
            }
        }

        private void LoadFGNumbers(string productnameid, string productname)
        {
            cmbFGNumber.Items.Clear();
            cmbWorkOrder.Items.Clear();
            cmbFGNumber.Enabled = false;
            cmbWorkOrder.Enabled = false;

            try
            {
                if (productnameid == "1" || productnameid == "2")
                {
                    using var conn = OpenEssConnection();
                    // ── Adjust to your schema ──
                    const string sql = "pro_getDDRFgnumber";
                    using var cmd = new SqlCommand(sql, conn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@productname", productname);
                    using var rdr = cmd.ExecuteReader();
                    while (rdr.Read())
                        cmbFGNumber.Items.Add(rdr.GetString(0));

                    cmbFGNumber.Enabled = cmbFGNumber.Items.Count > 0;
                    conn.Close();
                    cmbFGNumber.Text = "-- Select FG Number --";
                }
                else if (productnameid == "3" || productnameid == "4")
                {
                    using var connEss = OpenEssConnection();
                    // ── Adjust to your schema ──
                    const string sql = "pro_getM2FGnumber";
                    using var cmd = new SqlCommand(sql, connEss);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@productid", productnameid == "3" ? "1" : "2");
                    using var rdr = cmd.ExecuteReader();
                    while (rdr.Read())
                        cmbFGNumber.Items.Add(rdr.GetString(0));

                    cmbFGNumber.Enabled = cmbFGNumber.Items.Count > 0;
                    connEss.Close();
                    cmbFGNumber.Text = "-- Select FG Number --";
                }
            }
            catch (Exception ex)
            {
                AppendLog($"[DB ERROR] Loading FG numbers: {ex.Message}", C_WARNING);
            }
        }

        private void LoadWorkOrders(string fgNumber)
        {
            cmbWorkOrder.Items.Clear();
            cmbWorkOrder.Enabled = false;
            string product = cmbProduct.Text.ToString();
            string productname=cmbProductName.Text.ToString();

            try
            {
                if (product == "DRAM")
                {
                    using var conn = OpenConnection();
                    // ── Adjust to your schema ──
                    const string sql = "pro_getWorkOrder_Dram";
                    using var cmd = new SqlCommand(sql, conn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@fgnumber", fgNumber);
                    using var rdr = cmd.ExecuteReader();
                    while (rdr.Read())
                        cmbWorkOrder.Items.Add(rdr.GetString(0));

                    cmbWorkOrder.Enabled = cmbWorkOrder.Items.Count > 0;
                    conn.Close();
                    cmbWorkOrder.Text= "--Select Work Order--";
                }
                else
                {
                    if (product == "SSD" && productname == "M.2")
                    {
                        using var conn = OpenConnection();
                        // ── Adjust to your schema ──
                        const string sql = "pro_getWorkOrderM2";
                        using var cmd = new SqlCommand(sql, conn);
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@fgnumber", fgNumber);
                        using var rdr = cmd.ExecuteReader();
                        while (rdr.Read())
                            cmbWorkOrder.Items.Add(rdr.GetString(0));

                        cmbWorkOrder.Enabled = cmbWorkOrder.Items.Count > 0;
                        conn.Close();
                        cmbWorkOrder.Text = "--Select Work Order--";
                    }
                    else if(product == "SSD" && productname == "SATA")
                    {
                        using var conn = OpenConnection();
                        // ── Adjust to your schema ──
                        const string sql = "pro_getWorkOrderSATA";
                        using var cmd = new SqlCommand(sql, conn);
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@fgnumber", fgNumber);
                        using var rdr = cmd.ExecuteReader();
                        while (rdr.Read())
                            cmbWorkOrder.Items.Add(rdr.GetString(0));

                        cmbWorkOrder.Enabled = cmbWorkOrder.Items.Count > 0;
                        conn.Close();
                        cmbWorkOrder.Text = "--Select Work Order--";
                    }
                }
                txtBarcode.Enabled = true; 
                txtBarcode.Focus();
            }
            catch (Exception ex)
            {
                AppendLog($"[DB ERROR] Loading work orders: {ex.Message}", C_WARNING);
            }
        }

        /// <summary>
        /// Get the next running serial for today's Julian date + workorder combination.
        /// Returns the next integer (1-based).
        /// </summary>
        private int GetNextSerial(string workOrder, string julianDate)
        {
            int result = 0;
            try
            {
                using var conn = OpenConnection();
                // ── Adjust table / column names to your schema ──
                const string sql = "pro_getSerialNo";
                using var cmd = new SqlCommand(sql, conn);
                cmd.CommandType=CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@workOrder", workOrder);
                cmd.Parameters.AddWithValue("@julianDate", julianDate);
                result = Convert.ToInt32(cmd.ExecuteScalar());
            }
            catch(Exception ex) 
            {
                AppendLog($"[DB ERROR] Loading work orders: {ex.Message}", C_WARNING);
                return result;
            }
            return result;
        }

        /// <summary>Insert tray master record.</summary>
        private void InsertTrayMaster(string trayID, string customer, string fgNumber,
                                      string workOrder, string julianDate, int serial)
        {
            using var conn = OpenConnection();
            // ── Adjust table / column names to your schema ──
            const string sql = "pro_insertTrayMaster";
            using var cmd = new SqlCommand(sql, conn);
            cmd.CommandType=CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@TrayBarcode", trayID);
            cmd.Parameters.AddWithValue("@CustomerName", customer);
            cmd.Parameters.AddWithValue("@FGNumber", fgNumber);
            cmd.Parameters.AddWithValue("@WorkOrderNo ", workOrder);
            cmd.Parameters.AddWithValue("@JulianDate", julianDate);
            cmd.Parameters.AddWithValue("@SerialNo", serial);
            cmd.ExecuteNonQuery();
        }

        /// <summary>Insert a scanned board barcode linked to the current tray.</summary>
        private void InsertBoardScan(string trayID, string customerserialno, string pcbaserialnmumber)
        {
            try
            {
                using var conn = OpenConnection();
                // ── Adjust table / column names to your schema ──
                const string sql = "pro_insertTrayDetails";
                using var cmd = new SqlCommand(sql, conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@TrayBarcodeid", trayID);
                cmd.Parameters.AddWithValue("@CustomerSerialNo", customerserialno);
                cmd.Parameters.AddWithValue("@PCBASerialNo", pcbaserialnmumber);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                AppendLog($"[DB ERROR] InsertBoardScan issue: {ex.Message}", C_WARNING);
                return;
            }
        }

        // ═════════════════════════════════════════════════════════
        //  TRAY ID LOGIC
        // ═════════════════════════════════════════════════════════

        /// <summary>
        /// Tray ID format:  WorkOrderNo + FGNumber + JulianDate(3-digit) + "-" + Serial(5-digit)
        /// Example:         7896436ECHN038001987626135-00001
        /// </summary>
        private string BuildTrayID(string workOrder, string fgNumber, string julianDate, int serial)
        {
            return $"{workOrder}{fgNumber}{julianDate}{serial:D5}";
        }

        // ═════════════════════════════════════════════════════════
        //  EVENTS & LOGIC
        // ═════════════════════════════════════════════════════════
        private void WireEvents()
        {
            cmbCustomer.SelectedIndexChanged += CmbCustomer_Changed;
            cmbProduct.SelectedIndexChanged += CmbProduct_Changed;
            cmbProductName.SelectedIndexChanged += CmbProductName_Changed;
            cmbFGNumber.SelectedIndexChanged += CmbFGNumber_Changed;
            txtBarcode.KeyDown += TxtBarcode_KeyDown;
            txtBarcode.TextChanged += TxtBarcode_TextChanged;
            btnClear.Click += (s, _) => ClearAll();
            btnCreateTray.Click += (s, _) => CreateTrayID();
            btnExport.Click += (s, _) => ExportList();
            this.Load += (s, _) => cmbCustomer.Focus();
        }
        private void CmbProduct_Changed(object sender, EventArgs e)
        {
            if (cmbProduct.SelectedItem == null) return;
            ResetTray();
            LoadProductName(cmbProduct.SelectedValue.ToString());
        }

        private void CmbProductName_Changed(object sender, EventArgs e)
        {
            if (cmbProductName.SelectedItem == null) return;
            ResetTray();
            LoadFGNumbers(cmbProductName.SelectedValue.ToString(),cmbProductName.Text.ToString());
        }
        private void LoadProductName(string productid)
        {
            try
            {
                if (productid == "1" || productid == "2")
                {
                    //cmbProductName.Items.Clear();
                    using var conn = OpenConnection();
                    SqlCommand cmd = new SqlCommand("pro_getproductitem", conn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@productid", Convert.ToInt32(productid));
                    SqlDataAdapter da = new SqlDataAdapter(cmd);

                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    if (dt != null)
                    {
                        if (dt.Rows.Count > 0)
                        {
                            cmbProductName.DataSource = null;
                            cmbProductName.DataSource = dt;
                            cmbProductName.DisplayMember = "ProductName";
                            cmbProductName.ValueMember = "ProductNameid";
                            conn.Close();
                            cmbProductName.SelectedValue = 0;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AppendLog($"[DB ERROR] Loading customers: {ex.Message}", C_WARNING);

            }
}
        private void CmbCustomer_Changed(object sender, EventArgs e)
        {
            if (cmbCustomer.SelectedItem == null) return;
            ResetTray();
            LoadProduct(cmbCustomer.SelectedValue.ToString());
            //LoadFGNumbers(cmbCustomer.SelectedItem.ToString());
        }

        private void CmbFGNumber_Changed(object sender, EventArgs e)
        {
            if (cmbFGNumber.SelectedItem == null) return;
            ResetTray();
            LoadWorkOrders(cmbFGNumber.Text.ToString());
        }

        private void CreateTrayID()
        {
            // Validate selections
            if (cmbCustomer.Text == null || cmbCustomer.Text == "--Select Customer--")
            { MessageBox.Show("Please select a Customer.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if(cmbProduct.Text == null || cmbProduct.Text == "--Select Product --")
            { MessageBox.Show("Please select a Product.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (cmbProductName.Text == null || cmbProductName.Text == "--Select Product Name --")
            { MessageBox.Show("Please select a Product Details.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (cmbFGNumber.Text == null || cmbFGNumber.Text == "-- Select FG Number --")
            { MessageBox.Show("Please select an FG Number.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (cmbWorkOrder.Text == null || cmbWorkOrder.Text == "--Select Work Order--")
            { MessageBox.Show("Please select a Work Order.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            //string customer  = cmbCustomer.SelectedItem.ToString();
            string customer = cmbCustomer.Text;
            string fgNumber  = cmbFGNumber.SelectedItem.ToString();
            string workOrder = cmbWorkOrder.SelectedItem.ToString();
            string product = cmbProduct.Text.ToString();
            string productname = cmbProductName.Text.ToString();

            if (product == "DRAM")
                SCAN_TARGET = 48;
            else if (productname == "SATA")
                SCAN_TARGET = 38;
            else if (productname == "M.2")
                SCAN_TARGET = 23;


            //string julian    = DateTime.Now.DayOfYear.ToString("D3");
            string julian = DateTime.Now.ToString("yy") + DateTime.Now.DayOfYear.ToString("000");

            int serial;
            try
            {
                serial = GetNextSerial(workOrder, julian);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not fetch serial from DB:\n{ex.Message}",
                    "DB Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _currentTrayID = BuildTrayID(workOrder, fgNumber, julian, serial);
            _traySerial    = serial;

            // Persist to TrayMaster
            try
            {
                InsertTrayMaster(_currentTrayID, customer, fgNumber, workOrder, julian, serial);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Tray ID created locally but DB insert failed:\n{ex.Message}",
                    "DB Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            // Update UI
            lblTrayValue.Text    = _currentTrayID;
            lblTrayValue.ForeColor = C_SUCCESS;
            _trayCreated         = true;
            txtBarcode.Enabled   = true;
            txtBarcode.Focus();

            // Lock dropdowns so they can't be changed mid-tray
            cmbCustomer.Enabled  = false;
            cmbFGNumber.Enabled  = false;
            cmbWorkOrder.Enabled = false;

            AppendLog($"[TRAY] Created: {_currentTrayID}", C_SUCCESS);
        }

        // ── Scan logic ────────────────────────────────────────────
        private System.Windows.Forms.Timer _autoTimerFinal;
        private void TxtBarcode_TextChanged(object sender, EventArgs e)
       {
            if (_autoTimerFinal == null)
            {
                _autoTimerFinal = new System.Windows.Forms.Timer { Interval = 300 };
                _autoTimerFinal.Tick += (s, _) => { _autoTimerFinal.Stop(); AcceptBarcode(); };
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
            if (!_trayCreated)
            {
                MessageBox.Show("Please create a Tray ID before scanning boards.",
                    "No Tray Active", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (chkFail.Checked && cmbFailReason.SelectedIndex == 0)
            {
                MessageBox.Show("Please select a failure reason.", "Fail Reason Required",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                //txtBarcode.Clear();
                //txtBarcode.Focus();
                return;

            }

            if (chkFail.Checked && cmbFailReason.SelectedIndex != 0)
            {
                DialogResult dr = MessageBox.Show($"Are you sure you want to mark this board as FAIL for reason: {cmbFailReason.SelectedItem}?",
                    "Confirm Fail", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (dr == DialogResult.No)
                {
                    txtBarcode.Clear();
                    txtBarcode.Focus();
                    return;
                }
                else if (dr == DialogResult.Yes)
                {
                    AppendLog($"{txtBarcode.Text.Trim()} [-BOARD MARKED AS FAIL] Reason: {cmbFailReason.SelectedItem}", C_WARNING);
                    FailBoardUpdated();
                    txtBarcode.Clear();
                    txtBarcode.Focus();
                    chkFail.Checked = false;
                    cmbFailReason.SelectedIndex = 0;
                    return;
                }
            }
                bool isFail = chkFail.Checked;
               string failReason = isFail ? cmbFailReason.SelectedItem.ToString() : null;

            var code = txtBarcode.Text.Trim();
            if (string.IsNullOrEmpty(code)) return;

            // Check for duplicate in current tray
            if (_scannedBarcodes.Contains(code))
            {
                AppendLog($"[DUPLICATE] {code} — already in this tray!", C_WARNING);
                txtBarcode.Clear();
                txtBarcode.Focus();
                SystemSounds_Beep();
                return;
            }

           
            bool dbOk = true;
            bool checkPCBA = false;
            bool packing = true;
            try
            {
                //string pcbaNo = getPCBANumnber(code);
                //if (string.IsNullOrEmpty(pcbaNo))
                //{
                //    AppendLog($"PCBA Serail Number Not Fount - {code}: ", C_WARNING);
                //    return;
                //}
                //checkPCBA = checkPCBAStage(pcbaNo);

                checkPCBA = checkPCBAStage(code);
                if (checkPCBA)
                    InsertBoardScan(_currentTrayID, code, code);
                else
                {
                    AppendLog($"[MisMatch] {code} — Stage MisMatch!", C_WARNING);
                    return;
                }


                _scannedBarcodes.Add(code);
                AppendLog(code, dbOk ? C_ACCENT : C_WARNING);
                AddToListView(code);
                UpdateStats();
                 nextstages = Nextstartchecksfcs();

                if (_scannedBarcodes.Count <= SCAN_TARGET)
                {
                   
                    nextstages[0] = "21";
                    nextstages[1] = "Packing";
                    packing = true;
                }
                else if (_scannedBarcodes.Count == SCAN_TARGET + 1 || _scannedBarcodes.Count == SCAN_TARGET + 2)
                {
                    nextstages[0] = "240";
                    nextstages[1] = "Pass Mark Test";
                    packing = false;
                }
               int resultNextstage = UpdateNextStage(code, nextstages[0], nextstages[1]);
                if (resultNextstage == 0)
                {
                    AppendLog($"[DB ERROR] Could not update next stage for {_currentTrayID}", C_WARNING);
                    return;
                }
            
                if (cmbProduct.Text == "DRAM")
                {
                    if (_scannedBarcodes.Count == 50)
                        GenerateBarcode();
                }
                else if (cmbProductName.Text == "SATA")
                {
                    if (_scannedBarcodes.Count == 40)
                        GenerateBarcode();
                }
                else if (cmbProductName.Text == "M.2")
                {
                    if (_scannedBarcodes.Count == 25)
                        GenerateBarcode();
                }
                  
               


                txtBarcode.Clear();
                txtBarcode.Focus();
            }
            catch (Exception ex)
            {
                dbOk = false;
                AppendLog($"[DB ERROR] Could not save {code}: {ex.Message}", C_WARNING);
            }
        }

        public void FailBoardUpdated()
        {
            string workorder = cmbWorkOrder.Text.ToString();
            string pcbaid=txtBarcode.Text.Trim();
            string customer=cmbCustomer.Text.ToString();
            string machineName = Environment.MachineName;
            try
            {
                using var conn = OpenSatgeConnection();
                const string sql = "pro_insertFileEssencoreDetails";
                using var cmd = new SqlCommand(sql, conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@lapp", nextstages[0]);
                cmd.Parameters.AddWithValue("@app_name", nextstages[1]);
                cmd.Parameters.AddWithValue("@workorderno", workorder);
                cmd.Parameters.AddWithValue("@pcbaid", pcbaid);
                cmd.Parameters.AddWithValue("@customerid", customer);
                cmd.Parameters.AddWithValue("@result", "Fail");
                cmd.Parameters.AddWithValue("@boardFailDesc", cmbFailReason.SelectedItem.ToString());
                cmd.Parameters.AddWithValue("@reworkCount", 1);
                cmd.ExecuteNonQuery();
                conn.Close();

                using var connstage = OpenSatgeConnection();
                const string sqlstage = "pro_updateNextStage";
                using var cmdstage = new SqlCommand(sqlstage, connstage);
                cmdstage.CommandType = CommandType.StoredProcedure;
                cmdstage.Parameters.AddWithValue("@nextStageid", "24");
                cmdstage.Parameters.AddWithValue("@nextStageName", "Rework");
                cmdstage.Parameters.AddWithValue("@previousstagename", "FVI - Sample Decider");
                cmdstage.Parameters.AddWithValue("@pcbano", pcbaid);
                cmdstage.Parameters.AddWithValue("@Machineid", machineName);
                cmdstage.ExecuteNonQuery();
                connstage.Close();

                using var connFCT = OpenSatgeConnection();
                const string sqlFCT = "pro_insertFCT";
                using var cmdFCT = new SqlCommand(sqlFCT, connFCT);
                cmdFCT.CommandType = CommandType.StoredProcedure;
                cmdFCT.Parameters.AddWithValue("@stagename", nextstages[1]);
                cmdFCT.Parameters.AddWithValue("@workordernumber", workorder);
                cmdFCT.Parameters.AddWithValue("@updateMachineid", machineName);
                cmdFCT.Parameters.AddWithValue("@Pcbaid", pcbaid);
                cmdFCT.Parameters.AddWithValue("@updateEmpID", "0000");
                cmdFCT.Parameters.AddWithValue("@remarks", cmbFailReason.SelectedItem.ToString());
                cmdFCT.ExecuteNonQuery();
                connFCT.Close();
            }
            catch (Exception ex)
            {
                AppendLog($"[DB ERROR] Could not update fail board: {ex.Message}", C_WARNING);
                return;
            }
        }
        public string[] Nextstartchecksfcs()
        {
           string FgNumber = cmbFGNumber.Text.ToString();
            try
            {
                var SFCS_db = OpenSatgeConnection();
                if (SFCS_db.State == ConnectionState.Open)
                    SFCS_db.Close();

                SqlCommand cmd = new SqlCommand(
                    "pro_getNextstage",
                    SFCS_db);
                cmd.CommandType=CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@FgNumber", FgNumber);
                if (SFCS_db.State == ConnectionState.Closed)
                    SFCS_db.Open();

                SqlDataReader sdr = cmd.ExecuteReader();

                if (sdr.Read())
                {
                    try
                    {
                        string[] stages = sdr["Stages"].ToString().Split(',');
                        int index = Array.IndexOf(stages, "21");

                        if (index >= 0 && index < stages.Length - 1)
                        {
                            nextidinfo[0] = stages[index + 1];
                        }

                    }
                    catch (Exception ex)
                    {
                        AppendLog($"[DB ERROR] stages : {ex.Message}", C_WARNING);
                    }
                }

                sdr.Close();
                SFCS_db.Close();

                SqlCommand cmd1 =new SqlCommand("pro_getNextstageName", SFCS_db);
                cmd1.CommandType=CommandType.StoredProcedure;
                cmd1.Parameters.AddWithValue("@stageid", nextidinfo[0]);
                SqlDataAdapter da1 = new SqlDataAdapter(cmd1);
                DataSet ds1 = new DataSet();
                da1.Fill(ds1, "app_name");

                if (ds1.Tables[0].Rows.Count > 0)
                {
                    nextidinfo[1] = ds1.Tables[0].Rows[0][1].ToString();
                }

                SFCS_db.Close();


            }
            catch (Exception ex)
            {
                AppendLog($" Check PCBA Stage Issue: {ex.Message}", C_WARNING);
                return nextidinfo;
            }
            return nextidinfo;
        }
       
        public void GenerateBarcode()
        {
            try
            {

                //string labelFormatPath = @"D:\QR_CODE.btw";
                string labelFormatPath = ConfigurationManager.AppSettings["barcodepath"].ToString();

                //var product_no = string.IsNullOrEmpty(productno) ? string.Empty : productno;
                //var cus_no = string.IsNullOrEmpty(cus_serialno) ? string.Empty : cus_serialno;
                var trayid = string.IsNullOrEmpty(_currentTrayID) ? string.Empty : _currentTrayID;
                if (trayid != string.Empty)
                {

                    var externalValues = new Dictionary<string, string>
        {
            { "SerialNumber", trayid },
           
            //{ "QR_value1", productno.Trim().Substring(1,4) },
            //{ "QR_value2", productno.Trim().Substring(5,10) },

        };

                    PrintLabel(labelFormatPath, externalValues);
                    // txtPCBSerialNo.Text = string.Empty;
                    //txtPCBSerialNo.Focus();
                }
                else
                {

                    MessageBox.Show("Database not connected. Please check with admin");
                }
            }
            catch (Exception ex)
            {
                AppendLog($"[DB ERROR] Could not Print: {ex.Message}", C_WARNING);
                MessageBox.Show($"Error printing barcode: {ex.Message}");
            }

        }

        public void PrintLabel(string labelFormatPath, Dictionary<string, string> values)
        {

            BarTender.Application btApp = null;
            BarTender.Format btFormat = null;


            try
            {

                btApp = new BarTender.Application();
                btFormat = btApp.Formats.Open(labelFormatPath, false, "");

                foreach (var param in values)
                {
                    btFormat.SetNamedSubStringValue(param.Key, param.Value);
                }

                btFormat.PrintOut(false, false);
                btFormat.Close(BtSaveOptions.btDoNotSaveChanges);

                AppendLog($"[PRINT] Print Successfully Completed: {_currentTrayID}", C_SUCCESS);
                //rtbInstruction.Text = "Print Successfully Completed";
                //rtbInstruction.Font = new Font("Showcard Gothic", 12f);
                //rtbInstruction.BackColor = Color.Gray;

            }
            catch (COMException comEx)
            {
                MessageBox.Show("COM Error: " + comEx.Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
            finally
            {
                // Quit the BarTender application
                if (btApp != null)
                {
                    btApp.Quit(BtSaveOptions.btDoNotSaveChanges);
                    Marshal.ReleaseComObject(btApp);
                }

                if (btFormat != null)
                {
                    Marshal.ReleaseComObject(btFormat);
                }
            }


        }

        public int UpdateNextStage(string pcbaid, string nextstageid, string nextstagename) 
        {
            int result = 0;
            try
            {
                using var conn = OpenSatgeConnection();
                // ── Adjust table / column names to your schema ──
                const string sql = "updateNextStage";
                using var cmd = new SqlCommand(sql, conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@pcbaid", pcbaid);
                cmd.Parameters.AddWithValue("@packingnextstageid", nextstageid);
                cmd.Parameters.AddWithValue("@packingnextstagename", nextstagename);
                result = cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                AppendLog($"[DB ERROR] Update Next Stage: {ex.Message}", C_WARNING);
                return result;
            }
            return result;
        }

        public bool checkPCBAStage(string PCBANo)
        {
            bool check = false;
            string nextstageid=string.Empty;
            string nextsatgename=string.Empty;
            try
            {
                using var conn = OpenSatgeConnection();
                string query = "pro_checkPCBAstage";
                using var cmd = new SqlCommand(query, conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@PCBAid", PCBANo);
                using SqlDataAdapter da=new SqlDataAdapter (cmd);
                using DataTable dataTable = new DataTable ();   
                da.Fill (dataTable);
                if(dataTable != null )
                {
                    if(dataTable.Rows.Count > 0 )
                    {
                        nextstageid = dataTable.Rows[0]["Next_Stage_Id"].ToString();
                        nextsatgename = dataTable.Rows[0]["Next_Stage_Name"].ToString();
                        if(nextstageid.Trim() == "243")
                        {
                            check = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AppendLog($" Check PCBA Stage Issue: {ex.Message}", C_WARNING);
                return false;
            }
            return check;
        }
        public string getPCBANumnber(string code)
        {
            string result=string.Empty;
            string procedure = string.Empty;
            if (cmbProductName.Text == "M.2")
                procedure = "pro_getPCBAM2";
            else if (cmbProductName.Text == "SATA")
                procedure = "pro_getPCBASATA";
            else if (cmbProduct.Text == "DRAM")
                procedure = "pro_getPCBADRAM";

            try
            {
                using var conn = OpenConnection();
                // ── Adjust table / column names to your schema ──
                string sql = procedure;
                using var cmd = new SqlCommand(sql, conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@customerserialno", code);

                result = Convert.ToString(cmd.ExecuteScalar());
            }
            catch (Exception ex)
            {
                AppendLog($" Get PCBA Number Issue: {ex.Message}", C_WARNING);
                return ex.Message.ToString();
            }
            return result;
        }
        private static void SystemSounds_Beep() { System.Media.SystemSounds.Beep.Play(); }

        private void AppendLog(string message, Color? colour = null)
        {
            int idx = _scannedBarcodes.Count;
            rtbLog.SelectionStart = rtbLog.TextLength;
            rtbLog.SelectionColor = C_MUTED;
            rtbLog.AppendText($"[{DateTime.Now:HH:mm:ss}] ");
            rtbLog.SelectionColor = colour ?? C_ACCENT;
            rtbLog.AppendText(message + "\n");
            rtbLog.ScrollToCaret();
            lblScanCount.Text = $"{idx} scan{(idx == 1 ? "" : "s")}";
        }

        private void AddToListView(string code)
        {
            var item = new ListViewItem(lvGenerated.Items.Count + 1 + "");
            item.SubItems.Add(code);
            item.SubItems.Add(DateTime.Now.ToString("HH:mm:ss"));
            item.BackColor = Color.FromArgb(0, 40, 30);
            item.ForeColor = C_SUCCESS;
            lvGenerated.Items.Add(item);
            item.EnsureVisible();
        }

        private void UpdateStats()
        {
            int cnt = _scannedBarcodes.Count;
            pbScan.Value             = Math.Min(cnt, SCAN_TARGET);
            lblProgressInfo.Text     = $"Boards scanned in current tray: {cnt}";
            lblScanned.Tag           = cnt.ToString();
            lblScanned.Invalidate();
            lblGenerated.Tag         = cnt.ToString();
            lblGenerated.Invalidate();

        }

        private void ResetTray()
        {
            _trayCreated   = false;
            _currentTrayID = null;
            txtBarcode.Enabled = false;
            lblTrayValue.Text  = "— No tray created yet. Select dropdowns and click ⚡ Create Tray ID —";
            lblTrayValue.ForeColor = C_WARNING;
        }

        private void ClearAll()
        {
            if (MessageBox.Show(
                "Clear all scanned boards and reset the tray?\n(DB records are kept.)",
                "Confirm Clear", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            _scannedBarcodes.Clear();
            rtbLog.Clear();
            lvGenerated.Items.Clear();
            pbScan.Value         = 0;
            lblProgressInfo.Text = "Boards scanned in current tray: 0";
            lblScanCount.Text    = "0 scans";
            lblScanned.Tag       = "0"; lblScanned.Invalidate();
            lblGenerated.Tag     = "0"; lblGenerated.Invalidate();

            ResetTray();

            // Re-enable dropdowns
            cmbCustomer.Enabled  = true;
            cmbFGNumber.Enabled  = true;
            cmbWorkOrder.Enabled = true;
            cmbCustomer.Focus();
        }

        private void ExportList()
        {
            if (_scannedBarcodes.Count == 0)
            {
                MessageBox.Show("No scanned boards to export yet.", "Export",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            using var dlg = new SaveFileDialog
            {
                Filter   = "Text File (*.txt)|*.txt|CSV (*.csv)|*.csv",
                FileName = $"ECH_Tray_{_currentTrayID ?? "export"}_{DateTime.Now:yyyyMMdd_HHmmss}"
            };
            if (dlg.ShowDialog() != DialogResult.OK) return;

            string sep  = dlg.FilterIndex == 2 ? "," : "\t";
            var    sb   = new System.Text.StringBuilder();
            sb.AppendLine($"TrayID{sep}{_currentTrayID}");
            sb.AppendLine($"#\tBoard Barcode\tScanned At");
            for (int i = 0; i < _scannedBarcodes.Count; i++)
                sb.AppendLine($"{i + 1}{sep}{_scannedBarcodes[i]}{sep}{DateTime.Now:yyyy-MM-dd}");

            System.IO.File.WriteAllText(dlg.FileName, sb.ToString());
            MessageBox.Show("Export complete!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    // ── Custom themed progress bar ────────────────────────────────
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
            using var bgBr = new SolidBrush(Color.FromArgb(20, 30, 58));
            g.FillRectangle(bgBr, rc);
            if (Maximum > 0 && Value > 0)
            {
                int fillW = (int)((double)Value / Maximum * rc.Width);
                if (fillW > 0)
                {
                    var fillRect = new Rectangle(0, 0, fillW, rc.Height);
                    using var fgBr = new LinearGradientBrush(
                        new Point(0, 0), new Point(fillW, 0),
                        Color.FromArgb(0, 180, 255),
                        Color.FromArgb(0, 230, 200));
                    g.FillRectangle(fgBr, fillRect);
                }
            }
            using var pen = new Pen(Color.FromArgb(35, 55, 100), 1);
            g.DrawRectangle(pen, 0, 0, rc.Width - 1, rc.Height - 1);
        }
    }
}
