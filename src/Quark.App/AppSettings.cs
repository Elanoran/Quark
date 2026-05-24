using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Quark.App;

public sealed class AppSettings
{
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 1143;
    public bool UseSsl { get; set; }
    public bool UseStartTls { get; set; } = true;
    public string UserName { get; set; } = string.Empty;
    [JsonIgnore]
    public string Password { get; set; } = string.Empty;
    public string Mailbox { get; set; } = "INBOX";
    public int PollSeconds { get; set; } = 60;
    public bool ShowBalloonOnIncrease { get; set; } = true;
    public bool StartWithWindows { get; set; } = true;
    public string ProtonMailShortcutPath { get; set; } = string.Empty;
    public string ProtonMailUrl { get; set; } = "https://mail.proton.me/u/0/inbox";

    public bool IsReady => !string.IsNullOrWhiteSpace(Host)
        && Port > 0
        && !string.IsNullOrWhiteSpace(UserName)
        && !string.IsNullOrWhiteSpace(Password)
        && !string.IsNullOrWhiteSpace(Mailbox);

    public static string SettingsPath
    {
        get
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Quark");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "settings.json");
        }
    }

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
            {
                return new AppSettings();
            }

            string json = File.ReadAllText(SettingsPath);
            SettingsFile? file = JsonSerializer.Deserialize<SettingsFile>(json);
            return file?.ToAppSettings() ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(SettingsFile.FromAppSettings(this), options));
    }

    private sealed class SettingsFile
    {
        public string Host { get; set; } = "127.0.0.1";
        public int Port { get; set; } = 1143;
        public bool UseSsl { get; set; }
        public bool UseStartTls { get; set; } = true;
        public string UserName { get; set; } = string.Empty;
        public string ProtectedPassword { get; set; } = string.Empty;
        public string Mailbox { get; set; } = "INBOX";
        public int PollSeconds { get; set; } = 60;
        public bool ShowBalloonOnIncrease { get; set; } = true;
        public bool StartWithWindows { get; set; } = true;
        public string ProtonMailShortcutPath { get; set; } = string.Empty;
        public string ProtonMailUrl { get; set; } = "https://mail.proton.me/u/0/inbox";

        public static SettingsFile FromAppSettings(AppSettings settings)
        {
            return new SettingsFile
            {
                Host = settings.Host,
                Port = settings.Port,
                UseSsl = settings.UseSsl,
                UseStartTls = settings.UseStartTls,
                UserName = settings.UserName,
                ProtectedPassword = Protect(settings.Password),
                Mailbox = settings.Mailbox,
                PollSeconds = settings.PollSeconds,
                ShowBalloonOnIncrease = settings.ShowBalloonOnIncrease,
                StartWithWindows = settings.StartWithWindows,
                ProtonMailShortcutPath = settings.ProtonMailShortcutPath,
                ProtonMailUrl = settings.ProtonMailUrl,
            };
        }

        public AppSettings ToAppSettings()
        {
            return new AppSettings
            {
                Host = Host,
                Port = Port,
                UseSsl = UseSsl,
                UseStartTls = UseStartTls,
                UserName = UserName,
                Password = !string.IsNullOrWhiteSpace(ProtectedPassword) ? Unprotect(ProtectedPassword) : string.Empty,
                Mailbox = Mailbox,
                PollSeconds = PollSeconds,
                ShowBalloonOnIncrease = ShowBalloonOnIncrease,
                StartWithWindows = StartWithWindows,
                ProtonMailShortcutPath = ProtonMailShortcutPath,
                ProtonMailUrl = ProtonMailUrl,
            };
        }
    }

    private static string Protect(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        byte[] plain = Encoding.UTF8.GetBytes(value);
        byte[] protectedBytes = ProtectedData.Protect(plain, null, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(protectedBytes);
    }

    private static string Unprotect(string value)
    {
        try
        {
            byte[] protectedBytes = Convert.FromBase64String(value);
            byte[] plain = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(plain);
        }
        catch
        {
            return string.Empty;
        }
    }
}
