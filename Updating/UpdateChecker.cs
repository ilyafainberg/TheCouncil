// SPDX-License-Identifier: GPL-3.0-or-later
// Copyright (C) 2026 Ilya Fainberg
//
// This file is part of The Council.
// The Council is free software: you can redistribute it and/or modify it under
// the terms of the GNU General Public License as published by the Free Software
// Foundation, either version 3 of the License, or (at your option) any later
// version. See the LICENSE file for the full text. Distributed WITHOUT ANY
// WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
// FOR A PARTICULAR PURPOSE.
//
// =============================================================================
//  UpdateChecker.cs — self-rolled GitHub Releases auto-updater ("Option A").
//
//  Uses the public GitHub Releases API as the update server (no backend):
//    1. GET /repos/ilyafainberg/TheCouncil/releases/latest
//    2. Compare the release tag (e.g. "v1.3.0") against this app's own version.
//    3. If newer, pick the asset matching how the app was installed:
//         - PORTABLE  build -> the *-portable-win-x64.zip (extract over folder)
//         - INSTALLER build -> the *-setup.zip            (run setup silently)
//    4. Download it to %TEMP%, then hand off to apply-update.cmd, which waits for
//       this process to exit, applies the update, and relaunches the app.
//
//  Windows locks a running .exe, so a process cannot overwrite itself in place —
//  hence the separate helper script. See apply-update.cmd.
// =============================================================================

using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace TheCouncil.Updating;

/// <summary>How this copy of the app was installed. Drives which asset we fetch
/// and how we apply it.</summary>
public enum InstallKind
{
    /// <summary>Unzipped, runs in place. Updated by extracting the portable zip
    /// over the install directory.</summary>
    Portable,

    /// <summary>Installed via Inno Setup into Program Files. Updated by running
    /// the new setup.exe silently (UAC elevation).</summary>
    Installer
}

/// <summary>Outcome of an update check.</summary>
public sealed record UpdateCheckResult
{
    public bool UpdateAvailable { get; init; }
    public Version CurrentVersion { get; init; } = new(0, 0, 0);
    public Version LatestVersion { get; init; } = new(0, 0, 0);

    /// <summary>Direct download URL of the asset to fetch for this install kind.</summary>
    public string? AssetDownloadUrl { get; init; }

    /// <summary>The asset's file name (used for the temp download path).</summary>
    public string? AssetName { get; init; }

    /// <summary>Release notes / body for showing the user "what's new".</summary>
    public string ReleaseNotes { get; init; } = "";
}

/// <summary>
/// Checks GitHub Releases for a newer version and applies it. Stateless and safe
/// to construct per-check; reuses a single static HttpClient.
/// </summary>
public sealed class UpdateChecker
{
    private const string Owner = "ilyafainberg";
    private const string Repo = "TheCouncil";
    private const string AppExeName = "TheCouncil.exe";

    // One HttpClient for the whole app. GitHub's API REQUIRES a User-Agent header.
    private static readonly HttpClient Http = CreateHttpClient();

    private static HttpClient CreateHttpClient()
    {
        var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        http.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue("TheCouncil-Updater", CurrentVersion().ToString()));
        http.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        return http;
    }

    /// <summary>
    /// Queries the "latest release" endpoint and decides whether an update is
    /// available. Network/parse failures are swallowed into a "no update" result so
    /// a flaky connection never blocks startup.
    /// </summary>
    public async Task<UpdateCheckResult> CheckAsync(
        InstallKind? kindOverride = null,
        Action<Exception>? onError = null,
        CancellationToken ct = default)
    {
        var current = CurrentVersion();
        try
        {
            var url = $"https://api.github.com/repos/{Owner}/{Repo}/releases/latest";
            using var resp = await Http.GetAsync(url, ct).ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();

            await using var stream = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
            var root = doc.RootElement;

            // Skip drafts/prereleases — only ship stable to auto-update users.
            if (root.TryGetProperty("prerelease", out var pre) && pre.GetBoolean())
                return new UpdateCheckResult { CurrentVersion = current, LatestVersion = current };

            var tag = root.GetProperty("tag_name").GetString() ?? "";
            var notes = root.TryGetProperty("body", out var b) ? (b.GetString() ?? "") : "";
            var latest = ParseVersion(tag);

            if (latest <= current)
                return new UpdateCheckResult { CurrentVersion = current, LatestVersion = latest, ReleaseNotes = notes };

            var kind = kindOverride ?? DetectInstallKind();
            var (assetName, assetUrl) = PickAsset(root, kind);

            return new UpdateCheckResult
            {
                UpdateAvailable = assetUrl is not null,
                CurrentVersion = current,
                LatestVersion = latest,
                AssetDownloadUrl = assetUrl,
                AssetName = assetName,
                ReleaseNotes = notes
            };
        }
        catch (Exception ex)
        {
            onError?.Invoke(ex);
            return new UpdateCheckResult { CurrentVersion = current, LatestVersion = current };
        }
    }

    /// <summary>Progress phases reported during <see cref="DownloadAndApplyAsync"/>.</summary>
    public enum UpdatePhase { Downloading, Preparing, Launching }

    /// <summary>A progress update: the phase, and a 0..1 fraction (-1 if unknown).</summary>
    public readonly record struct UpdateProgress(UpdatePhase Phase, double Fraction, long BytesReceived, long TotalBytes);

    /// <summary>
    /// Downloads the chosen asset to %TEMP% and launches apply-update.cmd, then asks
    /// the app to exit so the helper can replace files (and relaunch). Reports
    /// download/launch progress via <paramref name="progress"/> if supplied.
    /// </summary>
    public async Task DownloadAndApplyAsync(
        UpdateCheckResult result,
        Action requestShutdown,
        IProgress<UpdateProgress>? progress = null,
        CancellationToken ct = default)
    {
        if (!result.UpdateAvailable || result.AssetDownloadUrl is null || result.AssetName is null)
            throw new InvalidOperationException("No downloadable update in this result.");

        var tempDir = Path.Combine(Path.GetTempPath(), $"TheCouncil-update-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var downloadPath = Path.Combine(tempDir, result.AssetName);

        using (var resp = await Http.GetAsync(result.AssetDownloadUrl, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false))
        {
            resp.EnsureSuccessStatusCode();
            long total = resp.Content.Headers.ContentLength ?? -1L;

            await using var src = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            await using var fs = File.Create(downloadPath);

            var buffer = new byte[81920];
            long received = 0;
            int read;
            progress?.Report(new UpdateProgress(UpdatePhase.Downloading, total > 0 ? 0 : -1, 0, total));
            while ((read = await src.ReadAsync(buffer, ct).ConfigureAwait(false)) > 0)
            {
                await fs.WriteAsync(buffer.AsMemory(0, read), ct).ConfigureAwait(false);
                received += read;
                double frac = total > 0 ? (double)received / total : -1;
                progress?.Report(new UpdateProgress(UpdatePhase.Downloading, frac, received, total));
            }
        }

        progress?.Report(new UpdateProgress(UpdatePhase.Preparing, 1, 0, 0));

        var installDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
        var helper = Path.Combine(installDir, "apply-update.cmd");
        if (!File.Exists(helper))
            throw new FileNotFoundException("apply-update.cmd not found next to the app.", helper);

        var kind = DetectInstallKind();

        // Launch the helper detached: %1=PID %2=kind %3=asset %4=dir %5=exe
        var psi = new ProcessStartInfo
        {
            FileName = helper,
            UseShellExecute = true,
            CreateNoWindow = false,
            WindowStyle = ProcessWindowStyle.Hidden,
            Arguments = string.Join(' ', new[]
            {
                Environment.ProcessId.ToString(),
                kind == InstallKind.Installer ? "installer" : "portable",
                Quote(downloadPath),
                Quote(installDir),
                Quote(AppExeName)
            })
        };

        progress?.Report(new UpdateProgress(UpdatePhase.Launching, 1, 0, 0));
        Process.Start(psi);

        requestShutdown();
    }

    // ---- helpers ------------------------------------------------------------

    /// <summary>This assembly's version (set by CI via -p:Version=). Falls back to
    /// 0.0.0 in dev builds.</summary>
    public static Version CurrentVersion()
    {
        var asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var info = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(info))
        {
            var clean = info.Split('+', '-')[0];
            if (Version.TryParse(clean, out var v)) return Normalize(v);
        }
        return Normalize(asm.GetName().Version ?? new Version(0, 0, 0));
    }

    private static Version ParseVersion(string tag)
    {
        var t = tag.TrimStart('v', 'V').Split('+', '-')[0];
        return Version.TryParse(t, out var v) ? Normalize(v) : new Version(0, 0, 0);
    }

    private static Version Normalize(Version v) =>
        new(v.Major, Math.Max(v.Minor, 0), Math.Max(v.Build, 0));

    /// <summary>
    /// Certainty over heuristic: the Inno installer drops an "installed.marker" file
    /// into the app folder, so its presence means we were installed. Otherwise we're
    /// portable. (Falls back to a Program Files check if the marker is absent.)
    /// </summary>
    private static InstallKind DetectInstallKind()
    {
        var baseDir = AppContext.BaseDirectory;
        if (File.Exists(Path.Combine(baseDir, "installed.marker")))
            return InstallKind.Installer;

        var pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var pfx = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        bool underProgramFiles =
            (!string.IsNullOrEmpty(pf) && baseDir.StartsWith(pf, StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrEmpty(pfx) && baseDir.StartsWith(pfx, StringComparison.OrdinalIgnoreCase));
        return underProgramFiles ? InstallKind.Installer : InstallKind.Portable;
    }

    /// <summary>Finds the right release asset for the install kind by name
    /// convention (matches what release.yml produces).</summary>
    private static (string? name, string? url) PickAsset(JsonElement release, InstallKind kind)
    {
        if (!release.TryGetProperty("assets", out var assets) || assets.ValueKind != JsonValueKind.Array)
            return (null, null);

        string needle = kind == InstallKind.Installer ? "-setup.zip" : "-portable-win-x64.zip";

        foreach (var a in assets.EnumerateArray())
        {
            var name = a.GetProperty("name").GetString() ?? "";
            if (name.EndsWith(needle, StringComparison.OrdinalIgnoreCase))
                return (name, a.GetProperty("browser_download_url").GetString());
        }
        return (null, null);
    }

    private static string Quote(string s) => "\"" + s.Replace("\"", "\\\"") + "\"";
}
