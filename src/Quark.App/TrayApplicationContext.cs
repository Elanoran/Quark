using System.Diagnostics;

namespace Quark.App;

public sealed class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _notifyIcon;
    private readonly System.Windows.Forms.Timer _timer;
    private readonly System.Windows.Forms.Timer _singleClickTimer;
    private readonly ImapUnreadClient _client = new();
    private AppSettings _settings;
    private SettingsForm? _settingsForm;
    private Icon? _currentIcon;
    private int? _lastUnreadCount;
    private bool _polling;

    public TrayApplicationContext()
    {
        _settings = AppSettings.Load();

        var menu = new ContextMenuStrip();
        menu.Items.Add("Open Proton Mail", null, (_, _) => OpenProtonMail());
        menu.Items.Add("Refresh now", null, async (_, _) => await RefreshAsync());
        menu.Items.Add("Settings", null, (_, _) => ShowSettings());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => ExitThread());

        _notifyIcon = new NotifyIcon
        {
            ContextMenuStrip = menu,
            Icon = AppIcons.Main,
            Text = "Quark",
            Visible = true,
        };
        _notifyIcon.MouseClick += NotifyIcon_MouseClick;
        _notifyIcon.MouseDoubleClick += NotifyIcon_MouseDoubleClick;

        _timer = new System.Windows.Forms.Timer();
        _timer.Tick += async (_, _) => await RefreshAsync();

        _singleClickTimer = new System.Windows.Forms.Timer { Interval = SystemInformation.DoubleClickTime };
        _singleClickTimer.Tick += (_, _) =>
        {
            _singleClickTimer.Stop();
            OpenProtonMail();
        };

        StartupManager.Apply(_settings.StartWithWindows);
        ConfigureTimer();
        SetIcon(null, false, "Not checked yet");

        if (_settings.IsReady)
        {
            _ = RefreshAsync();
        }
        else
        {
            ShowSettings();
        }
    }

    protected override void ExitThreadCore()
    {
        _timer.Stop();
        _singleClickTimer.Stop();
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _singleClickTimer.Dispose();
        _currentIcon?.Dispose();
        base.ExitThreadCore();
    }

    private void NotifyIcon_MouseClick(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _singleClickTimer.Stop();
            _singleClickTimer.Start();
        }
    }

    private void NotifyIcon_MouseDoubleClick(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _singleClickTimer.Stop();
            ShowSettings();
        }
    }

    private void ShowSettings()
    {
        if (_settingsForm is not null && !_settingsForm.IsDisposed)
        {
            _settingsForm.Activate();
            _settingsForm.Focus();
            return;
        }

        _settingsForm = new SettingsForm(_settings);
        _settingsForm.FormClosed += (_, _) =>
        {
            if (_settingsForm?.DialogResult == DialogResult.OK)
            {
                _settings.Save();
                StartupManager.Apply(_settings.StartWithWindows);
                ConfigureTimer();
                _ = RefreshAsync();
            }

            _settingsForm = null;
        };
        _settingsForm.Show();
        _settingsForm.Activate();
    }

    private void ConfigureTimer()
    {
        _timer.Stop();
        _timer.Interval = Math.Max(15, _settings.PollSeconds) * 1000;
        _timer.Start();
    }

    private void OpenProtonMail()
    {
        try
        {
            string? shortcut = GetProtonMailShortcutPath();
            if (shortcut is not null)
            {
                Process.Start(new ProcessStartInfo(shortcut) { UseShellExecute = true });
                return;
            }

            string url = string.IsNullOrWhiteSpace(_settings.ProtonMailUrl)
                ? "https://mail.proton.me/u/0/inbox"
                : _settings.ProtonMailUrl;
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            SetIcon(_lastUnreadCount, true, ex.Message);
        }
    }

    private string? GetProtonMailShortcutPath()
    {
        if (!string.IsNullOrWhiteSpace(_settings.ProtonMailShortcutPath)
            && File.Exists(_settings.ProtonMailShortcutPath))
        {
            return _settings.ProtonMailShortcutPath;
        }

        string[] candidates =
        [
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Microsoft",
                "Windows",
                "Start Menu",
                "Programs",
                "Proton",
                "Proton Mail.lnk"),
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "Microsoft",
                "Windows",
                "Start Menu",
                "Programs",
                "Proton Mail.lnk"),
        ];

        return candidates.FirstOrDefault(File.Exists);
    }

    private async Task RefreshAsync()
    {
        if (_polling)
        {
            return;
        }

        if (!_settings.IsReady)
        {
            SetIcon(null, true, "Open settings to configure Proton Bridge IMAP.");
            return;
        }

        _polling = true;
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            int count = await _client.GetUnreadCountAsync(_settings, timeout.Token);
            bool increased = _lastUnreadCount.HasValue && count > _lastUnreadCount.Value;
            _lastUnreadCount = count;
            SetIcon(count, false, count == 1 ? "1 unread message" : $"{count} unread messages");

            if (increased && _settings.ShowBalloonOnIncrease)
            {
                _notifyIcon.ShowBalloonTip(3000, "Quark", $"{count} unread messages", ToolTipIcon.None);
            }
        }
        catch (Exception ex)
        {
            SetIcon(_lastUnreadCount, true, ex.Message);
        }
        finally
        {
            _polling = false;
        }
    }

    private void SetIcon(int? unreadCount, bool hasError, string tooltip)
    {
        Icon next = TrayIconRenderer.Create(unreadCount, hasError);
        Icon? previous = _currentIcon;
        _currentIcon = next;
        _notifyIcon.Icon = next;
        _notifyIcon.Text = "Quark";
        previous?.Dispose();
    }
}
