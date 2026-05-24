namespace Quark.App;

public static class AppIcons
{
    private static readonly Lazy<Icon> MainIcon = new(LoadMainIcon);

    public static Icon Main => MainIcon.Value;

    private static Icon LoadMainIcon()
    {
        string basePath = AppContext.BaseDirectory;
        string[] candidates =
        [
            Path.Combine(basePath, "assets", "quark.ico"),
            Path.GetFullPath(Path.Combine(basePath, "..", "..", "..", "..", "assets", "quark.ico")),
            Path.GetFullPath(Path.Combine(basePath, "..", "..", "..", "..", "..", "assets", "quark.ico")),
        ];

        string? path = candidates.FirstOrDefault(File.Exists);
        return path is not null ? new Icon(path) : SystemIcons.Application;
    }
}
