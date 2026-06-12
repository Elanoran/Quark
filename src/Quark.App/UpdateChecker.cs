using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;

namespace Quark.App;

public sealed class UpdateChecker
{
    private static readonly Uri LatestReleaseUri = new("https://api.github.com/repos/Elanoran/Quark/releases/latest");

    public async Task<UpdateCheckResult> CheckLatestAsync(CancellationToken cancellationToken)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Quark", CurrentVersion.ToString(3)));
        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

        using HttpResponseMessage response = await http.GetAsync(LatestReleaseUri, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using JsonDocument document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        string tagName = document.RootElement.TryGetProperty("tag_name", out JsonElement tag)
            ? tag.GetString() ?? string.Empty
            : string.Empty;
        string releaseUrl = document.RootElement.TryGetProperty("html_url", out JsonElement url)
            ? url.GetString() ?? "https://github.com/Elanoran/Quark/releases"
            : "https://github.com/Elanoran/Quark/releases";

        Version latestVersion = ParseVersion(tagName);
        return new UpdateCheckResult(CurrentVersion, latestVersion, tagName, releaseUrl, latestVersion > CurrentVersion);
    }

    private static Version CurrentVersion =>
        typeof(UpdateChecker).Assembly.GetName().Version ?? new Version(0, 0, 0);

    private static Version ParseVersion(string tagName)
    {
        string clean = tagName.Trim().TrimStart('v', 'V');
        return Version.TryParse(clean, out Version? version) ? version : new Version(0, 0, 0);
    }
}

public sealed record UpdateCheckResult(
    Version CurrentVersion,
    Version LatestVersion,
    string LatestTag,
    string ReleaseUrl,
    bool IsNewer);
