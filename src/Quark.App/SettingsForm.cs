using System.Drawing.Drawing2D;

namespace Quark.App;

public sealed class SettingsForm : Form
{
    private const int WindowWidth = 392;
    private const int ContentLeft = 24;
    private const int ContentRight = 368;
    private const int LabelLeft = 36;
    private const int ControlLeft = 168;
    private const int ControlWidth = 200;
    private const int DividerRight = ContentRight;

    private static readonly Color WindowBack = Color.FromArgb(25, 23, 34);
    private static readonly Color PanelBack = Color.FromArgb(25, 23, 34);
    private static readonly Color BorderColor = Color.FromArgb(54, 49, 69);
    private static readonly Color DividerColor = Color.FromArgb(48, 44, 62);
    private static readonly Color Accent = Color.FromArgb(109, 74, 255);
    private static readonly Color AccentLight = Color.FromArgb(214, 204, 255);
    private static readonly Color Muted = Color.FromArgb(184, 178, 207);
    private static readonly Color Dim = Color.FromArgb(123, 112, 156);
    private static readonly Color InputBack = Color.FromArgb(31, 29, 40);
    private static readonly Color InputBorder = Color.FromArgb(77, 70, 94);
    private static readonly Font UiFont = new("Segoe UI", 9F);
    private static readonly Font MonoFont = new("Consolas", 10F, FontStyle.Bold);

    private readonly RoundedTextBox _host = new();
    private readonly RoundedNumberBox _port = new(1, 65535, 1, "tcp");
    private readonly ChoiceButton _startTls = new("STARTTLS");
    private readonly ChoiceButton _ssl = new("SSL");
    private readonly RoundedTextBox _userName = new();
    private readonly RoundedTextBox _password = new() { IsPassword = true };
    private readonly RoundedTextBox _mailbox = new();
    private readonly RoundedNumberBox _pollSeconds = new(15, 3600, 15, "sec");
    private readonly ToggleSwitch _showBalloon = new();
    private readonly ToggleSwitch _startWithWindows = new();
    private readonly StatusPill _status = new();

    private bool _dragging;
    private Point _dragStart;

    public AppSettings Settings { get; }

    public SettingsForm(AppSettings settings)
    {
        Settings = settings;

        Text = "Quark Settings";
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(WindowWidth, 838);
        BackColor = WindowBack;
        Font = UiFont;
        DoubleBuffered = true;
        Icon = AppIcons.Main;

        BuildLayout();
        LoadValues();
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        Region = null;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.Clear(PanelBack);
        using var fill = new SolidBrush(PanelBack);
        using var border = new Pen(BorderColor, 1.4f);
        e.Graphics.FillRectangle(fill, ClientRectangle);
        e.Graphics.DrawRectangle(border, 0, 0, Width - 1, Height - 1);

        using var headerLine = new Pen(DividerColor);
        e.Graphics.DrawLine(headerLine, 0, 79, Width - 1, 79);
        e.Graphics.DrawLine(headerLine, ContentLeft, 528, DividerRight, 528);
        e.Graphics.DrawLine(headerLine, 0, 772, Width - 1, 772);
        base.OnPaint(e);
    }

    private void BuildLayout()
    {
        var logo = new LogoBox { Location = new Point(ContentLeft, 24) };
        logo.MouseDown += StartDrag;
        logo.MouseMove += DragWindow;
        logo.MouseUp += StopDrag;
        Controls.Add(logo);
        var headerText = new HeaderText { Location = new Point(68, 24) };
        headerText.MouseDown += StartDrag;
        headerText.MouseMove += DragWindow;
        headerText.MouseUp += StopDrag;
        Controls.Add(headerText);
        var close = new IconButton("X") { Location = new Point(340, 25) };
        close.Click += (_, _) => CloseAs(DialogResult.Cancel);
        Controls.Add(close);

        Section("CONNECTION", ContentLeft + 1, 112);
        RowLabel("Host", LabelLeft, 160);
        _host.Bounds = new Rectangle(ControlLeft, 146, ControlWidth, 37);
        Controls.Add(_host);

        RowLabel("Port", LabelLeft, 221);
        _port.Bounds = new Rectangle(ControlLeft, 207, ControlWidth, 37);
        Controls.Add(_port);

        RowLabel("Security", LabelLeft, 280);
        _startTls.Bounds = new Rectangle(ControlLeft, 266, 98, 28);
        _ssl.Bounds = new Rectangle(ControlLeft + 112, 266, 62, 28);
        _startTls.Click += (_, _) => SetSecurity(true, false);
        _ssl.Click += (_, _) => SetSecurity(false, true);
        Controls.Add(_startTls);
        Controls.Add(_ssl);

        Section("CREDENTIALS", ContentLeft + 1, 322);
        RowLabel("Username", LabelLeft, 369);
        _userName.Bounds = new Rectangle(ControlLeft, 354, ControlWidth, 37);
        Controls.Add(_userName);

        RowLabel("Password", LabelLeft, 430);
        _password.Bounds = new Rectangle(ControlLeft, 414, 164, 37);
        Controls.Add(_password);
        var showPassword = new EyeButton { Location = new Point(ContentRight - 30, 414), Size = new Size(30, 37) };
        showPassword.Click += (_, _) => _password.IsPassword = !_password.IsPassword;
        Controls.Add(showPassword);

        RowLabel("Mailbox", LabelLeft, 492);
        _mailbox.Bounds = new Rectangle(ControlLeft, 473, ControlWidth, 37);
        Controls.Add(_mailbox);

        Section("BEHAVIOR", ContentLeft + 1, 551);
        RowLabel("Poll every", LabelLeft, 599);
        _pollSeconds.Bounds = new Rectangle(ControlLeft, 583, ControlWidth, 37);
        Controls.Add(_pollSeconds);

        Controls.Add(Label("Unread notifications", LabelLeft, 646, 220, 20, AccentLight, UiFont));
        Controls.Add(Label("Show system alert on new mail", LabelLeft, 665, 230, 16, Dim, new Font("Segoe UI", 7.8F)));
        _showBalloon.Location = new Point(ContentRight - _showBalloon.Width, 648);
        Controls.Add(_showBalloon);

        Controls.Add(Label("Launch on startup", LabelLeft, 700, 220, 20, AccentLight, UiFont));
        Controls.Add(Label("Start with Windows automatically", LabelLeft, 719, 230, 16, Dim, new Font("Segoe UI", 7.8F)));
        _startWithWindows.Location = new Point(ContentRight - _startWithWindows.Width, 702);
        Controls.Add(_startWithWindows);

        _status.Bounds = new Rectangle(ContentLeft, 739, ContentRight - ContentLeft, 25);
        _status.ShowIdle();
        Controls.Add(_status);

        var test = new FooterButton("Test", false) { Bounds = new Rectangle(ContentLeft, 789, 124, 34) };
        test.Click += async (_, _) => await TestAsync();
        Controls.Add(test);

        var save = new FooterButton("Save settings", true) { Bounds = new Rectangle(ContentRight - 124, 789, 124, 34) };
        save.Click += (_, _) =>
        {
            Apply();
            CloseAs(DialogResult.OK);
        };
        Controls.Add(save);
    }

    private void LoadValues()
    {
        _host.Value = Settings.Host;
        _port.Value = Settings.Port;
        _userName.Value = Settings.UserName;
        _password.Value = Settings.Password;
        _mailbox.Value = Settings.Mailbox;
        _pollSeconds.Value = Settings.PollSeconds;
        _showBalloon.Checked = Settings.ShowBalloonOnIncrease;
        _startWithWindows.Checked = Settings.StartWithWindows || StartupManager.IsEnabled();
        SetSecurity(Settings.UseStartTls, Settings.UseSsl);
    }

    private void Section(string text, int x, int y)
    {
        Controls.Add(Label(text, x, y, 250, 18, Accent, new Font("Segoe UI", 7.5F, FontStyle.Bold)));
    }

    private void RowLabel(string text, int x, int y)
    {
        Controls.Add(Label(text, x, y, 110, 20, AccentLight, UiFont));
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
        _startTls.Active = useStartTls;
        _ssl.Active = useSsl;
    }

    private async Task TestAsync()
    {
        Apply();
        _status.ShowMessage("Testing connection...", false);
        try
        {
            _ = await new ImapUnreadClient().GetUnreadCountAsync(Settings, CancellationToken.None);
            string security = Settings.UseStartTls ? "STARTTLS" : Settings.UseSsl ? "SSL" : "plain IMAP";
            _status.ShowMessage($"✓  Connected to {Settings.Host}:{Settings.Port} via {security}", false, success: true);
        }
        catch (Exception ex)
        {
            _status.ShowMessage(ex.Message, true);
        }
    }

    private void Apply()
    {
        Settings.Host = _host.Value.Trim();
        Settings.Port = _port.Value;
        Settings.UseStartTls = _startTls.Active;
        Settings.UseSsl = _ssl.Active;
        Settings.UserName = _userName.Value.Trim();
        Settings.Password = _password.Value;
        Settings.Mailbox = string.IsNullOrWhiteSpace(_mailbox.Value) ? "INBOX" : _mailbox.Value.Trim();
        Settings.PollSeconds = _pollSeconds.Value;
        Settings.ShowBalloonOnIncrease = _showBalloon.Checked;
        Settings.StartWithWindows = _startWithWindows.Checked;
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
        if (e.Y <= 80)
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
            _textBox.Location = new Point(13, 10);
            _textBox.Width = 170;
            Controls.Add(_textBox);
        }

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
            _textBox.Location = new Point(13, 10);
            _textBox.Width = 116;
            Controls.Add(_textBox);

            var up = new MiniButton(true) { Location = new Point(181, 0) };
            var down = new MiniButton(false) { Location = new Point(181, 19) };
            up.Click += (_, _) => Value += _increment;
            down.Click += (_, _) => Value -= _increment;
            Controls.Add(up);
            Controls.Add(down);
        }

        public int Value
        {
            get => int.TryParse(_textBox.Text, out int value) ? Math.Clamp(value, _min, _max) : _min;
            set => _textBox.Text = Math.Clamp(value, _min, _max).ToString();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var fill = new SolidBrush(InputBack);
            using var border = new Pen(InputBorder);
            using GraphicsPath path = RoundedRect(new Rectangle(0, 0, Width - 37, Height - 1), 4);
            e.Graphics.FillPath(fill, path);
            e.Graphics.DrawPath(border, path);
            TextRenderer.DrawText(
                e.Graphics,
                _unit,
                new Font("Consolas", 8F),
                new Rectangle(124, 11, 32, 15),
                Dim,
                TextFormatFlags.Right);
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

            using var arrow = new SolidBrush(AccentLight);
            pevent.Graphics.FillPolygon(arrow, points);
        }
    }

    private sealed class ChoiceButton : Button
    {
        private bool _active;

        public ChoiceButton(string text)
        {
            Text = text;
            FlatStyle = FlatStyle.Flat;
            Font = UiFont;
        }

        public bool Active
        {
            get => _active;
            set
            {
                _active = value;
                BackColor = value ? Color.FromArgb(54, 42, 109) : Color.FromArgb(31, 29, 40);
                ForeColor = value ? AccentLight : Color.FromArgb(139, 129, 166);
                FlatAppearance.BorderColor = value ? Accent : BorderColor;
                FlatAppearance.MouseOverBackColor = value ? Color.FromArgb(67, 52, 132) : Color.FromArgb(39, 36, 50);
                Text = value ? "• " + Text.TrimStart('•', ' ') : Text.TrimStart('•', ' ');
                Invalidate();
            }
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
                _checked = value;
                Invalidate();
            }
        }

        protected override void OnClick(EventArgs e)
        {
            Checked = !Checked;
            base.OnClick(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var fill = new SolidBrush(Checked ? Accent : Color.FromArgb(75, 70, 91));
            using var glow = new Pen(Checked ? Color.FromArgb(137, 107, 255) : Color.FromArgb(92, 86, 110));
            using GraphicsPath track = RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), 8);
            e.Graphics.FillPath(fill, track);
            e.Graphics.DrawPath(glow, track);
            int knobX = Checked ? Width - 18 : 3;
            using var knob = new SolidBrush(Checked ? Color.FromArgb(229, 222, 255) : Color.FromArgb(151, 145, 168));
            e.Graphics.FillEllipse(knob, knobX, 3, 14, 14);
        }
    }

    private sealed class FooterButton : Button
    {
        private readonly bool _primary;

        public FooterButton(string text, bool primary)
        {
            _primary = primary;
            Text = text;
            FlatStyle = FlatStyle.Flat;
            Font = UiFont;
            ForeColor = Color.White;
            BackColor = primary ? Color.FromArgb(37, 31, 61) : Color.FromArgb(25, 23, 34);
            FlatAppearance.BorderColor = primary ? Accent : BorderColor;
            FlatAppearance.MouseOverBackColor = primary ? Color.FromArgb(54, 42, 109) : Color.FromArgb(35, 32, 45);
            FlatAppearance.MouseDownBackColor = primary ? Color.FromArgb(70, 52, 150) : Color.FromArgb(31, 29, 40);
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            base.OnPaint(pevent);
            if (_primary)
            {
                using var pen = new Pen(Accent);
                pevent.Graphics.DrawLine(pen, 14, Height - 2, Width - 14, Height - 2);
            }
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
            FlatAppearance.BorderColor = BorderColor;
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
            using var eyePen = new Pen(AccentLight, 1.4f);
            using var pupil = new SolidBrush(AccentLight);

            using var path = new GraphicsPath();
            path.AddBezier(eyeRect.Left, eyeRect.Top + eyeRect.Height / 2, eyeRect.Left + 4, eyeRect.Top, eyeRect.Right - 4, eyeRect.Top, eyeRect.Right, eyeRect.Top + eyeRect.Height / 2);
            path.AddBezier(eyeRect.Right, eyeRect.Top + eyeRect.Height / 2, eyeRect.Right - 4, eyeRect.Bottom, eyeRect.Left + 4, eyeRect.Bottom, eyeRect.Left, eyeRect.Top + eyeRect.Height / 2);
            path.CloseFigure();
            e.Graphics.DrawPath(eyePen, path);
            e.Graphics.FillEllipse(pupil, left + 6, 15, 5, 5);
        }
    }

    private sealed class LogoBox : Control
    {
        public LogoBox()
        {
            Size = new Size(31, 31);
            DoubleBuffered = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            QuarkIconPainter.Paint(e.Graphics, ClientRectangle, hasError: false);
        }
    }

    private sealed class HeaderText : Control
    {
        public HeaderText()
        {
            Size = new Size(260, 31);
            DoubleBuffered = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(PanelBack);
            TextRenderer.DrawText(
                e.Graphics,
                "Quark",
                new Font("Segoe UI", 10F, FontStyle.Bold),
                new Rectangle(0, -1, Width, 17),
                Color.White,
                TextFormatFlags.Left | TextFormatFlags.NoPadding);
            TextRenderer.DrawText(
                e.Graphics,
                "Fundamental particle. unread mail counter.",
                new Font("Segoe UI", 6.8F),
                new Rectangle(0, 18, Width, 13),
                Muted,
                TextFormatFlags.Left | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
        }
    }

    private sealed class StatusPill : Control
    {
        private string _message = string.Empty;
        private bool _error;
        private bool _success;

        public StatusPill()
        {
            DoubleBuffered = true;
        }

        public void ShowIdle()
        {
            _message = string.Empty;
            _error = false;
            _success = false;
            Visible = true;
            Invalidate();
        }

        public void ShowMessage(string message, bool error, bool success = false)
        {
            _message = message;
            _error = error;
            _success = success;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Color back = _error ? Color.FromArgb(61, 33, 43) : _success ? Color.FromArgb(25, 54, 48) : Color.FromArgb(31, 29, 40);
            Color fore = _error ? Color.FromArgb(255, 135, 135) : _success ? Color.FromArgb(74, 214, 173) : Muted;
            using var fill = new SolidBrush(back);
            using GraphicsPath path = RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), 4);
            e.Graphics.FillPath(fill, path);
            if (string.IsNullOrEmpty(_message))
            {
                return;
            }

            TextRenderer.DrawText(e.Graphics, _message, new Font("Segoe UI", 7.8F), new Rectangle(10, 4, Width - 20, Height - 7), fore, TextFormatFlags.EndEllipsis);
        }
    }
}
