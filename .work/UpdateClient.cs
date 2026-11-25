using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

internal sealed class UpdateRelease
{
    public Version Version;
    public string Tag;
    public string Notes;
    public Uri PackageUri;
    public string Sha256;
}

internal static class UpdateClient
{
    private static readonly Uri LatestReleaseUri = new Uri("https://api.github.com/repos/weeshi/WinPilot-USB/releases/latest");
    internal const string PackageName = "WinPilot-USB-Update.zip";
    private const string HashName = "WinPilot-USB-Update.sha256";

    internal static Version CurrentVersion { get { return Assembly.GetExecutingAssembly().GetName().Version; } }
    internal static string CurrentVersionText { get { var v = CurrentVersion; return v.Major + "." + v.Minor + "." + v.Build + "." + v.Revision; } }

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("WinPilot-USB/" + CurrentVersionText);
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        return client;
    }

    private static string Value(Dictionary<string, object> map, string key)
    {
        object value; return map != null && map.TryGetValue(key, out value) && value != null ? Convert.ToString(value) : "";
    }

    private static Version ParseVersion(string tag)
    {
        Version version;
        var text = (tag ?? "").Trim().TrimStart('v', 'V');
        return Version.TryParse(text, out version) ? version : null;
    }

    private static bool IsTrustedDownload(Uri uri)
    {
        if (uri == null || uri.Scheme != Uri.UriSchemeHttps) return false;
        var host = uri.Host.ToLowerInvariant();
        return host == "github.com" || host.EndsWith(".github.com") || host.EndsWith(".githubusercontent.com");
    }

    internal static async Task<UpdateRelease> GetLatestAsync(CancellationToken cancellation)
    {
        using (var client = CreateClient()) {
            var json = await client.GetStringAsync(LatestReleaseUri).ConfigureAwait(false);
            cancellation.ThrowIfCancellationRequested();
            var release = new JavaScriptSerializer { MaxJsonLength = 2 * 1024 * 1024 }.DeserializeObject(json) as Dictionary<string, object>;
            var tag = Value(release, "tag_name");
            var version = ParseVersion(tag);
            if (version == null) throw new InvalidDataException("Latest release has an invalid version tag.");
            if (version <= CurrentVersion) return null;

            Dictionary<string, object> packageAsset = null;
            Dictionary<string, object> hashAsset = null;
            object rawAssets;
            var assets = release != null && release.TryGetValue("assets", out rawAssets) ? rawAssets as object[] : null;
            if (assets != null) foreach (var raw in assets) {
                var asset = raw as Dictionary<string, object>;
                var name = Value(asset, "name");
                if (name.Equals(PackageName, StringComparison.OrdinalIgnoreCase)) packageAsset = asset;
                if (name.Equals(HashName, StringComparison.OrdinalIgnoreCase)) hashAsset = asset;
            }
            if (packageAsset == null) throw new InvalidDataException("The latest release does not contain " + PackageName + ".");
            Uri packageUri;
            if (!Uri.TryCreate(Value(packageAsset, "browser_download_url"), UriKind.Absolute, out packageUri) || !IsTrustedDownload(packageUri))
                throw new InvalidDataException("The update package URL is not trusted.");

            var digest = Value(packageAsset, "digest");
            var hashMatch = Regex.Match(digest, @"(?i)sha256:([a-f0-9]{64})");
            string hash = hashMatch.Success ? hashMatch.Groups[1].Value.ToUpperInvariant() : null;
            if (hash == null && hashAsset != null) {
                Uri hashUri;
                if (!Uri.TryCreate(Value(hashAsset, "browser_download_url"), UriKind.Absolute, out hashUri) || !IsTrustedDownload(hashUri))
                    throw new InvalidDataException("The update hash URL is not trusted.");
                var hashText = await client.GetStringAsync(hashUri).ConfigureAwait(false);
                cancellation.ThrowIfCancellationRequested();
                hashMatch = Regex.Match(hashText, @"(?i)\b[a-f0-9]{64}\b");
                if (hashMatch.Success) hash = hashMatch.Value.ToUpperInvariant();
            }
            if (hash == null) throw new InvalidDataException("The latest release has no SHA-256 digest.");
            return new UpdateRelease { Version = version, Tag = tag, Notes = Value(release, "body"), PackageUri = packageUri, Sha256 = hash };
        }
    }

    internal static async Task<string> DownloadAsync(UpdateRelease release, IProgress<int> progress, CancellationToken cancellation)
    {
        var folder = Path.Combine(Path.GetTempPath(), "WinPilot-Update-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var package = Path.Combine(folder, PackageName);
        try {
            using (var client = CreateClient())
            using (var response = await client.GetAsync(release.PackageUri, HttpCompletionOption.ResponseHeadersRead, cancellation).ConfigureAwait(false)) {
                response.EnsureSuccessStatusCode();
                var total = response.Content.Headers.ContentLength;
                using (var input = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                using (var output = new FileStream(package, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true)) {
                    var buffer = new byte[81920]; long received = 0; int read;
                    while ((read = await input.ReadAsync(buffer, 0, buffer.Length, cancellation).ConfigureAwait(false)) > 0) {
                        await output.WriteAsync(buffer, 0, read, cancellation).ConfigureAwait(false);
                        received += read;
                        if (total.HasValue && total.Value > 0 && progress != null) progress.Report((int)Math.Min(99, received * 100 / total.Value));
                    }
                }
            }
            string actual;
            using (var sha = SHA256.Create()) using (var stream = File.OpenRead(package)) actual = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
            if (!actual.Equals(release.Sha256, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Update SHA-256 verification failed.");
            if (progress != null) progress.Report(100);
            return package;
        } catch { try { Directory.Delete(folder, true); } catch { } throw; }
    }

    internal static string Quote(string value) { return "\"" + value.Replace("\"", "\\\"") + "\""; }
}
