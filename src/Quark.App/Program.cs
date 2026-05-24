namespace Quark.App;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        if (args.Length == 2 && args[0].Equals("--screenshot", StringComparison.OrdinalIgnoreCase))
        {
            ApplicationConfiguration.Initialize();
            SaveSettingsScreenshot(args[1]);
            return;
        }

        using var mutex = new Mutex(initiallyOwned: true, "Quark.TrayApp.SingleInstance", out bool createdNew);
        if (!createdNew)
        {
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new TrayApplicationContext());
    }    

    private static void SaveSettingsScreenshot(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);

        var settings = new AppSettings
        {
            Host = "127.0.0.1",
            Port = 1143,
            UseStartTls = true,
            UseSsl = false,
            UserName = string.Empty,
            Password = string.Empty,
            Mailbox = "INBOX",
            PollSeconds = 60,
            ShowBalloonOnIncrease = true,
            StartWithWindows = true,
        };

        using var form = new SettingsForm(settings);
        form.StartPosition = FormStartPosition.Manual;
        form.Location = new Point(-2000, -2000);
        form.Show();
        Application.DoEvents();

        using var bitmap = new Bitmap(form.Width, form.Height);
        form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, form.Size));
        bitmap.Save(path, System.Drawing.Imaging.ImageFormat.Png);
        form.Close();
    }
}
