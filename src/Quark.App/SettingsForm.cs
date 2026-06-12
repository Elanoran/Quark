using System.Drawing.Drawing2D;

namespace Quark.App;

public sealed class SettingsForm : Form
{
    private const int WindowWidth = 400;
    private const int ContentLeft = 22;
    private const int ContentRight = 378;
    private const int LabelLeft = 31;
    private const int ControlLeft = 145;
    private const int ControlWidth = 224;
    private const int DividerRight = ContentRight;

    private static readonly Color WindowBack = Color.FromArgb(25, 23, 34);
    private static readonly Color PanelBack = Color.FromArgb(25, 23, 34);
    private static readonly Color Surface = Color.FromArgb(34, 32, 45);
    private static readonly Color SurfaceHover = Color.FromArgb(43, 40, 57);
    private static readonly Color BorderColor = Color.FromArgb(55, 51, 70);
    private static readonly Color DividerColor = Color.FromArgb(50, 46, 63);
    private static readonly Color Accent = Color.FromArgb(111, 83, 226);
    private static readonly Color AccentLight = Color.FromArgb(174, 154, 255);
    private static readonly Color PlusBlue = Color.FromArgb(116, 151, 255);
    private static readonly Color Muted = Color.FromArgb(178, 172, 197);
    private static readonly Color Dim = Color.FromArgb(132, 124, 154);
    private static readonly Color InputBack = Color.FromArgb(34, 32, 45);
    private static readonly Color InputBorder = Color.FromArgb(61, 56, 76);
    private static readonly Color Success = Color.FromArgb(84, 211, 159);
    private static readonly Font UiFont = new("Segoe UI", 9F);
    private static readonly Font MonoFont = new("Consolas", 10F, FontStyle.Bold);

    private readonly DashboardHero _hero = new();
    private readonly RoundedTextBox _host = new();
    private readonly RoundedNumberBox _port = new(1, 65535, 1, "tcp");
    private readonly SecuritySwitch _security = new();
    private readonly RoundedTextBox _userName = new();
    private readonly RoundedTextBox _password = new() { IsPassword = true };
    private readonly RoundedTextBox _mailbox = new();
    private readonly RoundedNumberBox _pollSeconds = new(15, 3600, 15, "sec");
    private readonly ToggleSwitch _showBalloon = new();
    private readonly ToggleSwitch _startWithWindows = new();
    private readonly StatusPill _status = new();
    private readonly System.Windows.Forms.Timer _statusRestoreTimer = new() { Interval = 6000 };

    private FooterButton? _saveButton;
    private SettingsSnapshot _loadedSnapshot;
    private bool _loading;
    private bool _dragging;
    private Point _dragStart;
    private int? _unreadCount;
    private DateTimeOffset? _lastCheckedAt;

    public AppSettings Settings { get; }

    public SettingsForm(AppSettings settings, int? unreadCount = null, DateTimeOffset? lastCheckedAt = null)
    {
        Settings = settings;
        _unreadCount = unreadCount;
        _lastCheckedAt = lastCheckedAt;

        Text = "Quark Settings";
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(WindowWidth, 1003);
        BackColor = WindowBack;
        Font = UiFont;
        DoubleBuffered = true;
        Icon = AppIcons.Main;
        _statusRestoreTimer.Tick += (_, _) =>
        {
            _statusRestoreTimer.Stop();
            ShowVersionStatus();
        };

        BuildLayout();
        LoadValues();
    }

    public void SetUnreadState(int? unreadCount, DateTimeOffset? lastCheckedAt)
    {
        _unreadCount = unreadCount;
        _lastCheckedAt = lastCheckedAt;
        _hero.SetUnreadState(unreadCount, lastCheckedAt);
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        Region = null;
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        using var fill = new SolidBrush(PanelBack);
        e.Graphics.FillRectangle(fill, ClientRectangle);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var fill = new SolidBrush(PanelBack);
        e.Graphics.FillRectangle(fill, ClientRectangle);

        using (var footerFill = new LinearGradientBrush(
            new Rectangle(0, 947, Width, Math.Max(1, Height - 947)),
            Color.FromArgb(24, 27, 41),
            Color.FromArgb(18, 17, 26),
            LinearGradientMode.Vertical))
        {
            e.Graphics.FillRectangle(footerFill, 0, 947, Width, Height - 947);
        }

        using var headerLine = new Pen(DividerColor);
        e.Graphics.DrawLine(headerLine, 0, 70, Width - 1, 70);
        e.Graphics.DrawLine(headerLine, 0, 312, Width - 1, 312);
        e.Graphics.DrawLine(headerLine, ContentLeft, 691, ContentRight, 691);
        e.Graphics.DrawLine(headerLine, ContentLeft, 867, ContentRight, 867);
        e.Graphics.DrawLine(headerLine, 0, 947, Width - 1, 947);
        QuarkIconPainter.Paint(e.Graphics, new Rectangle(ContentLeft, 23, 30, 30), hasError: false);
        PaintHeaderText(e.Graphics);
    }

    private void BuildLayout()
    {
        var close = new IconButton("X") { Location = new Point(350, 21) };
        close.Click += (_, _) => CloseAs(DialogResult.Cancel);
        Controls.Add(close);

        _hero.Bounds = new Rectangle(0, 71, WindowWidth, 241);
        var checkNow = new HeroCheckButton { Bounds = new Rectangle(ContentLeft, 178, ContentRight - ContentLeft, 36) };
        checkNow.Click += async (_, _) => await TestAsync();
        _hero.Controls.Add(checkNow);
        Controls.Add(_hero);

        Section("CONNECTION", ContentLeft, 321);
        RowLabel("Host", LabelLeft, 364);
        _host.Bounds = new Rectangle(ControlLeft, 349, ControlWidth, 37);
        _host.ValueChanged += (_, _) => UpdateSaveState();
        Controls.Add(_host);

        RowLabel("Port", LabelLeft, 416);
        _port.Bounds = new Rectangle(ControlLeft, 401, ControlWidth, 37);
        _port.ValueChanged += (_, _) => UpdateSaveState();
        Controls.Add(_port);

        RowLabel("Security", LabelLeft, 468);
        _security.Bounds = new Rectangle(ControlLeft, 450, ControlWidth, 38);
        _security.ValueChanged += (_, _) => UpdateSaveState();
        Controls.Add(_security);

        Section("CREDENTIALS", ContentLeft, 510);
        RowLabel("Username", LabelLeft, 553);
        _userName.Bounds = new Rectangle(ControlLeft, 538, ControlWidth, 37);
        _userName.ValueChanged += (_, _) =>
        {
            RefreshHeroPreview();
            UpdateSaveState();
        };
        Controls.Add(_userName);

        RowLabel("Password", LabelLeft, 605);
        _password.Bounds = new Rectangle(ControlLeft, 590, 186, 37);
        _password.ValueChanged += (_, _) => UpdateSaveState();
        Controls.Add(_password);
        var showPassword = new EyeButton { Location = new Point(337, 590), Size = new Size(30, 37) };
        showPassword.Click += (_, _) => _password.IsPassword = !_password.IsPassword;
        Controls.Add(showPassword);

        RowLabel("Mailbox", LabelLeft, 657);
        _mailbox.Bounds = new Rectangle(ControlLeft, 642, ControlWidth, 37);
        _mailbox.ValueChanged += (_, _) =>
        {
            RefreshHeroPreview();
            UpdateSaveState();
        };
        Controls.Add(_mailbox);

        Section("BEHAVIOR", ContentLeft, 705);
        RowLabel("Poll every", LabelLeft, 745);
        _pollSeconds.Bounds = new Rectangle(ControlLeft, 730, ControlWidth, 37);
        _pollSeconds.ValueChanged += (_, _) =>
        {
            RefreshHeroPreview();
            UpdateSaveState();
        };
        Controls.Add(_pollSeconds);

        Controls.Add(Label("Unread notifications", LabelLeft, 785, 220, 20, Color.White, UiFont));
        Controls.Add(Label("Show system alert on new mail", LabelLeft, 804, 230, 16, Dim, new Font("Segoe UI", 7.8F)));
        _showBalloon.Location = new Point(ControlLeft + ControlWidth - _showBalloon.Width, 787);
        _showBalloon.CheckedChanged += (_, _) => UpdateSaveState();
        Controls.Add(_showBalloon);

        Controls.Add(Label("Launch on startup", LabelLeft, 823, 220, 20, Color.White, UiFont));
        Controls.Add(Label("Start with Windows automatically", LabelLeft, 842, 230, 16, Dim, new Font("Segoe UI", 7.8F)));
        _startWithWindows.Location = new Point(ControlLeft + ControlWidth - _startWithWindows.Width, 825);
        _startWithWindows.CheckedChanged += (_, _) => UpdateSaveState();
        Controls.Add(_startWithWindows);

        Section("STATUS", ContentLeft, 881);
        _status.Bounds = new Rectangle(LabelLeft, 907, ContentRight - LabelLeft, 22);
        ShowVersionStatus();
        Controls.Add(_status);

        _saveButton = new FooterButton("Save", false) { Bounds = new Rectangle(ContentRight - 170, 961, 170, 34) };
        _saveButton.Click += (_, _) =>
        {
            Apply();
            CloseAs(DialogResult.OK);
        };
        Controls.Add(_saveButton);
    }

    private void LoadValues()
    {
        _loading = true;
        _host.Value = Settings.Host;
        _port.Value = Settings.Port;
        _userName.Value = Settings.UserName;
        _password.Value = Settings.Password;
        _mailbox.Value = Settings.Mailbox;
        _pollSeconds.Value = Settings.PollSeconds;
        _showBalloon.Checked = Settings.ShowBalloonOnIncrease;
        _startWithWindows.Checked = Settings.StartWithWindows || StartupManager.IsEnabled();
        _hero.SetValues(Settings.UserName, Settings.Mailbox, Settings.PollSeconds, _unreadCount, _lastCheckedAt);
        SetSecurity(Settings.UseStartTls, Settings.UseSsl);
        _loadedSnapshot = CaptureSnapshot();
        _loading = false;
        UpdateSaveState();
    }

    private void RefreshHeroPreview()
    {
        _hero.SetValues(_userName.Value, _mailbox.Value, _pollSeconds.Value, _unreadCount, _lastCheckedAt);
    }

    private void UpdateSaveState()
    {
        if (_loading || _saveButton is null)
        {
            return;
        }

        _saveButton.Primary = CaptureSnapshot() != _loadedSnapshot;
    }

    private SettingsSnapshot CaptureSnapshot()
    {
        return new SettingsSnapshot(
            _host.Value.Trim(),
            _port.Value,
            _security.UseStartTls,
            _security.UseSsl,
            _userName.Value.Trim(),
            _password.Value,
            string.IsNullOrWhiteSpace(_mailbox.Value) ? "INBOX" : _mailbox.Value.Trim(),
            _pollSeconds.Value,
            _showBalloon.Checked,
            _startWithWindows.Checked);
    }

    private void Section(string text, int x, int y)
    {
        Controls.Add(Label(text, x, y, 250, 18, PlusBlue, new Font("Segoe UI", 7.5F, FontStyle.Bold)));
    }

    private void RowLabel(string text, int x, int y)
    {
        Controls.Add(Label(text, x, y, 110, 20, Color.White, UiFont));
    }

    private static Label Label(string text, int x, int y, int width, int height, Color color, Font font)
    {
        return new Label
        {
            Text = text,
            ForeColor = color,
            BackColor = Color.Transparent,
            Font = font,
            Location = new Point(x, y),
            Size = new Size(width, height),
        };
    }

    private void SetSecurity(bool useStartTls, bool useSsl)
    {
        _security.SetValue(useStartTls, useSsl);
        UpdateSaveState();
    }

    private async Task TestAsync()
    {
        Apply();
        _statusRestoreTimer.Stop();
        _status.ShowMessage("Testing connection...", false);
        try
        {
            int count = await new ImapUnreadClient().GetUnreadCountAsync(Settings, CancellationToken.None);
            SetUnreadState(count, DateTimeOffset.Now);
            string security = Settings.UseStartTls ? "STARTTLS" : Settings.UseSsl ? "SSL" : "plain IMAP";
            _status.ShowMessage($"✓  Connected to {Settings.Host}:{Settings.Port} via {security}", false, success: true);
            _statusRestoreTimer.Start();
        }
        catch (Exception ex)
        {
            _status.ShowMessage(ex.Message, true);
            _statusRestoreTimer.Start();
        }
    }

    private void ShowVersionStatus()
    {
        string version = typeof(SettingsForm).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";
        _status.ShowVersion($"Quark v{version}");
    }

    private void Apply()
    {
        Settings.Host = _host.Value.Trim();
        Settings.Port = _port.Value;
        Settings.UseStartTls = _security.UseStartTls;
        Settings.UseSsl = _security.UseSsl;
        Settings.UserName = _userName.Value.Trim();
        Settings.Password = _password.Value;
        Settings.Mailbox = string.IsNullOrWhiteSpace(_mailbox.Value) ? "INBOX" : _mailbox.Value.Trim();
        Settings.PollSeconds = _pollSeconds.Value;
        Settings.ShowBalloonOnIncrease = _showBalloon.Checked;
        Settings.StartWithWindows = _startWithWindows.Checked;
        _hero.SetValues(Settings.UserName, Settings.Mailbox, Settings.PollSeconds, _unreadCount, _lastCheckedAt);
    }

    private void CloseAs(DialogResult result)
    {
        DialogResult = result;
        Close();
    }

    private void StartDrag(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        _dragging = true;
        _dragStart = e.Location;
    }

    private void DragWindow(object? sender, MouseEventArgs e)
    {
        if (_dragging)
        {
            Location = new Point(Location.X + e.X - _dragStart.X, Location.Y + e.Y - _dragStart.Y);
        }
    }

    private void StopDrag(object? sender, MouseEventArgs e)
    {
        _dragging = false;
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Y <= 70)
        {
            StartDrag(this, e);
        }

        base.OnMouseDown(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        DragWindow(this, e);
        base.OnMouseMove(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        StopDrag(this, e);
        base.OnMouseUp(e);
    }

    private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
    {
        int d = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
        path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    private readonly record struct SettingsSnapshot(
        string Host,
        int Port,
        bool UseStartTls,
        bool UseSsl,
        string UserName,
        string Password,
        string Mailbox,
        int PollSeconds,
        bool ShowBalloon,
        bool StartWithWindows);

    private static void PaintHeaderText(Graphics graphics)
    {
        TextRenderer.DrawText(
            graphics,
            "Quark",
            new Font("Segoe UI", 10F, FontStyle.Bold),
            new Rectangle(72, 22, 260, 17),
            Color.White,
            TextFormatFlags.Left | TextFormatFlags.NoPadding);
        TextRenderer.DrawText(
            graphics,
            "Fundamental particle. unread mail counter.",
            new Font("Segoe UI", 6.8F),
            new Rectangle(72, 40, 260, 13),
            Muted,
            TextFormatFlags.Left | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
    }

    private sealed class DashboardHero : Control
    {
        private readonly System.Windows.Forms.Timer _timer = new() { Interval = 24 };
        private string _userName = string.Empty;
        private string _mailbox = "INBOX";
        private int _pollSeconds;
        private int? _unreadCount;
        private DateTimeOffset? _lastCheckedAt;
        private float _angle;

        public DashboardHero()
        {
            DoubleBuffered = true;
            BackColor = PanelBack;
            _timer.Tick += (_, _) =>
            {
                _angle = (_angle + 2.3f) % 360f;
                Invalidate(new Rectangle(307, 24, 60, 60));
            };
            _timer.Start();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timer.Dispose();
            }

            base.Dispose(disposing);
        }

        public void SetValues(string userName, string mailbox, int pollSeconds, int? unreadCount, DateTimeOffset? lastCheckedAt)
        {
            _userName = string.IsNullOrWhiteSpace(userName) ? "not configured" : userName.Trim();
            _mailbox = string.IsNullOrWhiteSpace(mailbox) ? "INBOX" : mailbox.Trim();
            _pollSeconds = pollSeconds;
            _unreadCount = unreadCount;
            _lastCheckedAt = lastCheckedAt;
            Invalidate();
        }

        public void SetUnreadState(int? unreadCount, DateTimeOffset? lastCheckedAt)
        {
            _unreadCount = unreadCount;
            _lastCheckedAt = lastCheckedAt;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var fill = new LinearGradientBrush(
                ClientRectangle,
                Color.FromArgb(25, 29, 44),
                Color.FromArgb(19, 18, 28),
                LinearGradientMode.Vertical);
            e.Graphics.FillRectangle(fill, ClientRectangle);

            PaintOrbitIcon(e.Graphics, new Rectangle(308, 24, 58, 58), _angle);

            string countText = _unreadCount.HasValue ? _unreadCount.Value.ToString() : "0";
            TextRenderer.DrawText(e.Graphics, "+", new Font("Consolas", 18F, FontStyle.Bold), new Rectangle(ContentLeft + 3, 31, 23, 24), PlusBlue, TextFormatFlags.Left | TextFormatFlags.NoPadding);
            TextRenderer.DrawText(e.Graphics, countText, new Font("Consolas", 36F, FontStyle.Bold), new Rectangle(ContentLeft + 25, 25, 62, 55), Color.White, TextFormatFlags.Left | TextFormatFlags.NoPadding);
            TextRenderer.DrawText(e.Graphics, $"unread in {_mailbox}", new Font("Segoe UI", 9.2F, FontStyle.Bold), new Rectangle(100, 34, 180, 19), Color.White, TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
            TextRenderer.DrawText(e.Graphics, _userName, new Font("Segoe UI", 7.8F), new Rectangle(100, 55, 200, 18), Dim, TextFormatFlags.Left | TextFormatFlags.EndEllipsis);

            PaintStat(e.Graphics, 22, 110, "BRIDGE", _unreadCount.HasValue ? "Online" : "Ready", Success);
            PaintStat(e.Graphics, 144, 110, "LAST CHECK", FormatLastChecked(_lastCheckedAt), Color.White);
            PaintStat(e.Graphics, 266, 110, "POLL", $"{Math.Max(1, _pollSeconds)}s", Color.White);
        }

        private static string FormatLastChecked(DateTimeOffset? lastCheckedAt)
        {
            if (!lastCheckedAt.HasValue)
            {
                return "Never";
            }

            DateTimeOffset local = lastCheckedAt.Value.ToLocalTime();
            return local.ToString("HH:mm");
        }

        private static void PaintOrbitIcon(Graphics graphics, Rectangle bounds, float angle)
        {
            GraphicsState iconState = graphics.Save();
            float iconSize = Math.Min(bounds.Width, bounds.Height);
            float scale = iconSize / 128f;
            graphics.TranslateTransform(bounds.X + (bounds.Width - iconSize) / 2f, bounds.Y + (bounds.Height - iconSize) / 2f);
            graphics.ScaleTransform(scale, scale);

            DrawOrbit(graphics, Color.FromArgb(150, 138, 100, 255), 1.5f, -30, 42, 16);
            DrawOrbit(graphics, Color.FromArgb(95, 180, 140, 255), 1.2f, 30, 42, 16);
            DrawOrbit(graphics, Color.FromArgb(90, 108, 78, 255), 1.2f, 90, 42, 14);

            using var halo = new SolidBrush(Color.FromArgb(45, 109, 74, 255));
            graphics.FillEllipse(halo, 46, 46, 36, 36);

            using var core = new LinearGradientBrush(
                new Rectangle(53, 53, 22, 22),
                Color.FromArgb(200, 168, 255),
                Color.FromArgb(74, 45, 181),
                LinearGradientMode.ForwardDiagonal);
            graphics.FillEllipse(core, 53, 53, 22, 22);

            using var shine = new SolidBrush(Color.FromArgb(100, 255, 255, 255));
            graphics.FillEllipse(shine, 56, 57, 8, 6);

            DrawMovingParticle(graphics, angle, 42, 16, -30, Color.FromArgb(220, 200, 168, 255), 3.8f);
            DrawMovingParticle(graphics, angle + 125, 42, 16, 30, Color.FromArgb(205, 138, 100, 255), 3.3f);
            DrawMovingParticle(graphics, angle + 250, 42, 14, 90, Color.FromArgb(190, 170, 136, 255), 2.8f);

            graphics.Restore(iconState);
        }

        private static void DrawOrbit(Graphics graphics, Color color, float width, float angle, float rx, float ry)
        {
            GraphicsState state = graphics.Save();
            graphics.TranslateTransform(64, 64);
            graphics.RotateTransform(angle);
            using var pen = new Pen(color, width);
            graphics.DrawEllipse(pen, -rx, -ry, rx * 2, ry * 2);
            graphics.Restore(state);
        }

        private static void DrawMovingParticle(Graphics graphics, float phase, float rx, float ry, float orbitAngle, Color color, float radius)
        {
            float radians = phase * MathF.PI / 180f;
            float x = MathF.Cos(radians) * rx;
            float y = MathF.Sin(radians) * ry;
            float angle = orbitAngle * MathF.PI / 180f;
            float rotatedX = x * MathF.Cos(angle) - y * MathF.Sin(angle);
            float rotatedY = x * MathF.Sin(angle) + y * MathF.Cos(angle);
            using var brush = new SolidBrush(color);
            graphics.FillEllipse(brush, 64 + rotatedX - radius, 64 + rotatedY - radius, radius * 2, radius * 2);
            using var shine = new SolidBrush(Color.FromArgb(120, 255, 255, 255));
            graphics.FillEllipse(shine, 64 + rotatedX - radius / 2, 64 + rotatedY - radius / 2, radius * 0.65f, radius * 0.65f);
        }

        private static void PaintStat(Graphics g, int x, int y, string label, string value, Color valueColor)
        {
            var rect = new Rectangle(x, y, 104, 52);
            using GraphicsPath path = RoundedRect(rect, 6);
            using var fill = new SolidBrush(Color.FromArgb(37, 34, 48));
            using var border = new Pen(BorderColor);
            g.FillPath(fill, path);
            g.DrawPath(border, path);
            TextRenderer.DrawText(g, label, new Font("Segoe UI", 6.8F, FontStyle.Bold), new Rectangle(x + 11, y + 9, 82, 12), Dim, TextFormatFlags.Left | TextFormatFlags.NoPadding);
            TextRenderer.DrawText(g, value, new Font("Consolas", 9F, FontStyle.Bold), new Rectangle(x + 11, y + 26, 82, 16), valueColor, TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
        }
    }

    private sealed class HeroCheckButton : Control
    {
        private bool _hovered;
        private bool _pressed;

        public HeroCheckButton()
        {
            Cursor = Cursors.Hand;
            DoubleBuffered = true;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _hovered = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hovered = false;
            _pressed = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _pressed = true;
                Invalidate();
            }

            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            _pressed = false;
            Invalidate();
            base.OnMouseUp(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Color back = _pressed
                ? Color.FromArgb(34, 37, 58)
                : _hovered ? Color.FromArgb(31, 34, 53) : Color.FromArgb(27, 30, 48);
            using GraphicsPath path = RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), 6);
            using var fill = new SolidBrush(back);
            using var border = new Pen(PlusBlue, 1.2f);
            e.Graphics.FillPath(fill, path);
            e.Graphics.DrawPath(border, path);

            int centerX = Width / 2 - 36;
            int centerY = Height / 2;
            DrawRefreshIcon(e.Graphics, centerX, centerY);
            TextRenderer.DrawText(e.Graphics, "Check now", new Font("Segoe UI", 8.8F, FontStyle.Bold), new Rectangle(centerX + 20, 0, 100, Height), PlusBlue, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
        }

        private static void DrawRefreshIcon(Graphics graphics, int centerX, int centerY)
        {
            using var pen = new Pen(PlusBlue, 1.7f);
            graphics.DrawArc(pen, centerX - 6, centerY - 6, 12, 12, 45, 285);
            Point[] arrow =
            [
                new Point(centerX + 6, centerY - 7),
                new Point(centerX + 8, centerY - 2),
                new Point(centerX + 3, centerY - 4),
            ];
            using var brush = new SolidBrush(PlusBlue);
            graphics.FillPolygon(brush, arrow);
        }
    }

    private sealed class RoundedTextBox : UserControl
    {
        private readonly TextBox _textBox = new();
        private bool _isPassword;

        public RoundedTextBox()
        {
            BackColor = Color.Transparent;
            DoubleBuffered = true;
            _textBox.BorderStyle = BorderStyle.None;
            _textBox.BackColor = InputBack;
            _textBox.ForeColor = Color.White;
            _textBox.Font = MonoFont;
            _textBox.Location = new Point(13, 0);
            _textBox.Width = 170;
            _textBox.TextChanged += (_, _) => ValueChanged?.Invoke(this, EventArgs.Empty);
            Controls.Add(_textBox);
        }

        public event EventHandler? ValueChanged;

        public string Value
        {
            get => _textBox.Text;
            set => _textBox.Text = value;
        }

        public bool IsPassword
        {
            get => _isPassword;
            set
            {
                _isPassword = value;
                _textBox.UseSystemPasswordChar = value;
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            _textBox.Location = new Point(13, Math.Max(0, (Height - _textBox.Height) / 2));
            _textBox.Width = Math.Max(20, Width - 26);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var fill = new SolidBrush(InputBack);
            using var border = new Pen(InputBorder);
            using GraphicsPath path = RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), 4);
            e.Graphics.FillPath(fill, path);
            e.Graphics.DrawPath(border, path);
        }
    }

    private sealed class RoundedNumberBox : UserControl
    {
        private readonly TextBox _textBox = new();
        private readonly MiniButton _up = new(true);
        private readonly MiniButton _down = new(false);
        private readonly int _min;
        private readonly int _max;
        private readonly int _increment;
        private readonly string _unit;

        public RoundedNumberBox(int min, int max, int increment, string unit)
        {
            _min = min;
            _max = max;
            _increment = increment;
            _unit = unit;
            BackColor = Color.Transparent;
            DoubleBuffered = true;
            _textBox.BorderStyle = BorderStyle.None;
            _textBox.BackColor = InputBack;
            _textBox.ForeColor = Color.White;
            _textBox.Font = MonoFont;
            _textBox.Location = new Point(13, 0);
            _textBox.Width = 120;
            _textBox.TextChanged += (_, _) => ValueChanged?.Invoke(this, EventArgs.Empty);
            Controls.Add(_textBox);

            _up.Click += (_, _) => Value += _increment;
            _down.Click += (_, _) => Value -= _increment;
            Controls.Add(_up);
            Controls.Add(_down);
        }

        public event EventHandler? ValueChanged;

        public int Value
        {
            get => int.TryParse(_textBox.Text, out int value) ? Math.Clamp(value, _min, _max) : _min;
            set => _textBox.Text = Math.Clamp(value, _min, _max).ToString();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            int spinnerX = Math.Max(0, Width - 19);
            _up.Location = new Point(spinnerX, 0);
            _down.Location = new Point(spinnerX, 19);
            _textBox.Location = new Point(13, Math.Max(0, (Height - _textBox.Height) / 2));
            _textBox.Width = Math.Max(20, Width - 95);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var fill = new SolidBrush(InputBack);
            using var border = new Pen(InputBorder);
            using GraphicsPath path = RoundedRect(new Rectangle(0, 0, Width - 24, Height - 1), 4);
            e.Graphics.FillPath(fill, path);
            e.Graphics.DrawPath(border, path);
            TextRenderer.DrawText(
                e.Graphics,
                _unit,
                new Font("Consolas", 8F),
                new Rectangle(Width - 81, 6, 56, 20),
                Dim,
                TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
        }
    }

    private sealed class MiniButton : Button
    {
        private readonly bool _up;

        public MiniButton(bool up)
        {
            _up = up;
            Size = new Size(19, 18);
            FlatStyle = FlatStyle.Flat;
            BackColor = Color.FromArgb(42, 37, 59);
            ForeColor = Muted;
            FlatAppearance.BorderColor = BorderColor;
            FlatAppearance.MouseOverBackColor = Color.FromArgb(53, 45, 83);
            FlatAppearance.MouseDownBackColor = Color.FromArgb(65, 52, 108);
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            pevent.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var fill = new SolidBrush(BackColor);
            using var border = new Pen(BorderColor);
            pevent.Graphics.FillRectangle(fill, new Rectangle(0, 0, Width - 1, Height - 1));
            pevent.Graphics.DrawRectangle(border, 0, 0, Width - 1, Height - 1);

            Point[] points = _up
                ? [new Point(Width / 2, 5), new Point(Width / 2 - 4, 11), new Point(Width / 2 + 4, 11)]
                : [new Point(Width / 2, 12), new Point(Width / 2 - 4, 6), new Point(Width / 2 + 4, 6)];

            using var arrow = new SolidBrush(Color.White);
            pevent.Graphics.FillPolygon(arrow, points);
        }
    }

    private sealed class SecuritySwitch : Control
    {
        private bool _useStartTls = true;
        private bool _hoverStartTls;
        private bool _hoverSsl;

        public SecuritySwitch()
        {
            Cursor = Cursors.Hand;
            DoubleBuffered = true;
        }

        public bool UseStartTls => _useStartTls;
        public bool UseSsl => !_useStartTls;

        public event EventHandler? ValueChanged;

        public void SetValue(bool useStartTls, bool useSsl)
        {
            bool next = useStartTls || !useSsl;
            if (_useStartTls == next)
            {
                Invalidate();
                return;
            }

            _useStartTls = next;
            Invalidate();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            bool hoverStartTls = e.X < Width / 2;
            bool hoverSsl = !hoverStartTls;
            if (_hoverStartTls != hoverStartTls || _hoverSsl != hoverSsl)
            {
                _hoverStartTls = hoverStartTls;
                _hoverSsl = hoverSsl;
                Invalidate();
            }

            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hoverStartTls = false;
            _hoverSsl = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                bool next = e.X < Width / 2;
                if (_useStartTls != next)
                {
                    _useStartTls = next;
                    Invalidate();
                    ValueChanged?.Invoke(this, EventArgs.Empty);
                }
            }

            base.OnMouseDown(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle outer = new(0, 0, Width - 1, Height - 1);
            using GraphicsPath outerPath = RoundedRect(outer, 6);
            using var back = new LinearGradientBrush(outer, Color.FromArgb(29, 29, 40), Color.FromArgb(22, 22, 32), LinearGradientMode.Horizontal);
            using var border = new Pen(BorderColor);
            e.Graphics.FillPath(back, outerPath);
            e.Graphics.DrawPath(border, outerPath);

            int segmentWidth = Width / 2;
            Rectangle activeRect = _useStartTls
                ? new Rectangle(4, 4, segmentWidth - 7, Height - 8)
                : new Rectangle(segmentWidth + 3, 4, Width - segmentWidth - 7, Height - 8);
            using GraphicsPath activePath = RoundedRect(activeRect, 4);
            using var activeFill = new SolidBrush(PlusBlue);
            e.Graphics.FillPath(activeFill, activePath);

            if ((_hoverStartTls && !_useStartTls) || (_hoverSsl && _useStartTls))
            {
                Rectangle hoverRect = _hoverStartTls
                    ? new Rectangle(4, 4, segmentWidth - 7, Height - 8)
                    : new Rectangle(segmentWidth + 3, 4, Width - segmentWidth - 7, Height - 8);
                using GraphicsPath hoverPath = RoundedRect(hoverRect, 4);
                using var hoverFill = new SolidBrush(Color.FromArgb(28, 255, 255, 255));
                e.Graphics.FillPath(hoverFill, hoverPath);
            }

            DrawSegmentText(e.Graphics, new Rectangle(0, 0, segmentWidth, Height), "• STARTTLS", _useStartTls);
            DrawSegmentText(e.Graphics, new Rectangle(segmentWidth, 0, Width - segmentWidth, Height), "SSL/TLS", !_useStartTls);
        }

        private static void DrawSegmentText(Graphics graphics, Rectangle bounds, string text, bool active)
        {
            TextRenderer.DrawText(
                graphics,
                text,
                new Font("Segoe UI", 8.8F, FontStyle.Bold),
                bounds,
                active ? Color.White : Muted,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }
    }

    private sealed class ToggleSwitch : Control
    {
        private bool _checked;

        public ToggleSwitch()
        {
            Size = new Size(37, 20);
            Cursor = Cursors.Hand;
            DoubleBuffered = true;
        }

        public bool Checked
        {
            get => _checked;
            set
            {
                if (_checked == value)
                {
                    return;
                }

                _checked = value;
                Invalidate();
                CheckedChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler? CheckedChanged;

        protected override void OnClick(EventArgs e)
        {
            Checked = !Checked;
            base.OnClick(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var fill = new SolidBrush(Checked ? Color.FromArgb(27, 30, 48) : Color.FromArgb(75, 70, 91));
            using var glow = new Pen(Checked ? PlusBlue : Color.FromArgb(92, 86, 110));
            using GraphicsPath track = RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), 8);
            e.Graphics.FillPath(fill, track);
            e.Graphics.DrawPath(glow, track);
            int knobX = Checked ? Width - 18 : 3;
            using var knob = new SolidBrush(Checked ? PlusBlue : Color.FromArgb(151, 145, 168));
            e.Graphics.FillEllipse(knob, knobX, 3, 14, 14);
        }
    }

    private sealed class FooterButton : Button
    {
        private bool _primary;
        private bool _hovered;
        private bool _pressed;

        public FooterButton(string text, bool primary)
        {
            Text = text;
            FlatStyle = FlatStyle.Flat;
            Font = UiFont;
            ForeColor = Color.White;
            FlatAppearance.BorderSize = 0;
            Primary = primary;
        }

        public bool Primary
        {
            get => _primary;
            set
            {
                _primary = value;
                Invalidate();
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _hovered = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hovered = false;
            _pressed = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _pressed = true;
                Invalidate();
            }

            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            _pressed = false;
            Invalidate();
            base.OnMouseUp(e);
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            pevent.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            pevent.Graphics.Clear(Parent?.BackColor ?? PanelBack);

            Color top;
            Color bottom;
            Color border;
            Color text;
            if (_primary)
            {
                top = _pressed ? Color.FromArgb(78, 128, 229) : _hovered ? Color.FromArgb(105, 154, 255) : Color.FromArgb(96, 145, 246);
                bottom = _pressed ? Color.FromArgb(70, 116, 207) : _hovered ? Color.FromArgb(91, 137, 237) : Color.FromArgb(83, 128, 226);
                border = top;
                text = Color.White;
            }
            else
            {
                top = _hovered ? Color.FromArgb(30, 32, 45) : Color.FromArgb(25, 23, 34);
                bottom = _hovered ? Color.FromArgb(26, 27, 39) : Color.FromArgb(22, 21, 31);
                border = BorderColor;
                text = Color.FromArgb(170, 164, 186);
            }

            Rectangle rect = new(0, 0, Width - 1, Height - 1);
            using GraphicsPath path = RoundedRect(rect, 6);
            using var fill = new LinearGradientBrush(rect, top, bottom, LinearGradientMode.Vertical);
            using var pen = new Pen(border);
            pevent.Graphics.FillPath(fill, path);
            pevent.Graphics.DrawPath(pen, path);

            string label = _primary ? "✓  Save" : "Save";
            TextRenderer.DrawText(
                pevent.Graphics,
                label,
                new Font("Segoe UI", 9F, FontStyle.Bold),
                rect,
                text,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }
    }

    private sealed class IconButton : Button
    {
        public IconButton(string text)
        {
            Text = text;
            Size = new Size(28, 28);
            FlatStyle = FlatStyle.Flat;
            BackColor = Color.FromArgb(31, 29, 40);
            ForeColor = Muted;
            Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            FlatAppearance.BorderSize = 0;
            FlatAppearance.BorderColor = Color.FromArgb(0, 0, 0, 0);
            FlatAppearance.MouseOverBackColor = Color.FromArgb(39, 36, 50);
            FlatAppearance.MouseDownBackColor = Color.FromArgb(45, 40, 61);
        }
    }

    private sealed class EyeButton : Control
    {
        public EyeButton()
        {
            BackColor = PanelBack;
            Cursor = Cursors.Hand;
            DoubleBuffered = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var clear = new SolidBrush(PanelBack);
            e.Graphics.FillRectangle(clear, ClientRectangle);

            float left = (Width - 17) / 2f;
            var eyeRect = new RectangleF(left, 13, 17, 10);
            using var eyePen = new Pen(Color.White, 1.4f);
            using var pupil = new SolidBrush(Color.White);

            using var path = new GraphicsPath();
            path.AddBezier(eyeRect.Left, eyeRect.Top + eyeRect.Height / 2, eyeRect.Left + 4, eyeRect.Top, eyeRect.Right - 4, eyeRect.Top, eyeRect.Right, eyeRect.Top + eyeRect.Height / 2);
            path.AddBezier(eyeRect.Right, eyeRect.Top + eyeRect.Height / 2, eyeRect.Right - 4, eyeRect.Bottom, eyeRect.Left + 4, eyeRect.Bottom, eyeRect.Left, eyeRect.Top + eyeRect.Height / 2);
            path.CloseFigure();
            e.Graphics.DrawPath(eyePen, path);
            e.Graphics.FillEllipse(pupil, left + 6, 15, 5, 5);
        }
    }

    private sealed class StatusPill : Control
    {
        private string _message = string.Empty;
        private bool _error;
        private bool _success;
        private bool _version;

        public StatusPill()
        {
            DoubleBuffered = true;
        }

        public void ShowVersion(string message)
        {
            _message = message;
            _error = false;
            _success = false;
            _version = true;
            Visible = true;
            Invalidate();
        }

        public void ShowMessage(string message, bool error, bool success = false)
        {
            _message = message;
            _error = error;
            _success = success;
            _version = false;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            if (string.IsNullOrEmpty(_message))
            {
                return;
            }

            Color fore = _error ? Color.FromArgb(255, 135, 135) : _success ? Color.FromArgb(74, 214, 173) : Muted;
            if (!_version)
            {
                Color back = _error ? Color.FromArgb(61, 33, 43) : _success ? Color.FromArgb(25, 54, 48) : Color.FromArgb(31, 29, 40);
                using var fill = new SolidBrush(back);
                using GraphicsPath path = RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), 4);
                e.Graphics.FillPath(fill, path);
            }

            TextRenderer.DrawText(e.Graphics, _message, new Font("Segoe UI", 7.8F), new Rectangle(10, 4, Width - 20, Height - 7), fore, TextFormatFlags.EndEllipsis);
        }
    }
}
