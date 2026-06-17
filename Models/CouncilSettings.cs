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
using System.Text.Json;

namespace TheCouncil.Models;

public class CouncilSettings
{
    public List<Profile> Profiles { get; set; } = new();

    /// <summary>Persisted council roster (human + AI participants). Empty on first run → seeded from profiles.</summary>
    public List<Participant> Council { get; set; } = new();

    /// <summary>Maximum debate rounds before the council is forced to conclude.</summary>
    public int MaxRounds { get; set; } = 10;

    public Profile? Find(string? id) =>
        string.IsNullOrEmpty(id) ? null : Profiles.FirstOrDefault(p => p.Id == id);

    public static string SettingsPath
    {
        get
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "TheCouncil");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "settings.json");
        }
    }

    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    public static CouncilSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                var s = JsonSerializer.Deserialize<CouncilSettings>(json) ?? new CouncilSettings();
                if (s.Profiles == null || s.Profiles.Count == 0)
                    s.Profiles = BuildInitialProfiles(json);
                return s;
            }
        }
        catch { /* fall through to defaults */ }
        return new CouncilSettings { Profiles = BuildInitialProfiles(null) };
    }

    public void Save() => File.WriteAllText(SettingsPath, JsonSerializer.Serialize(this, JsonOpts));

    /// <summary>Seeds the five default profiles, overlaying any API keys found in a legacy settings file.</summary>
    private static List<Profile> BuildInitialProfiles(string? legacyJson)
    {
        var profiles = new List<Profile>
        {
            new() { Name = "OpenAI",   Provider = ProviderKind.OpenAI,  Model = "gpt-4o" },
            new() { Name = "Claude",   Provider = ProviderKind.Claude,  Model = "claude-3-5-sonnet-latest" },
            new() { Name = "Gemini",   Provider = ProviderKind.Gemini,  Model = "gemini-1.5-pro" },
            new() { Name = "Grok",     Provider = ProviderKind.Grok,    Model = "grok-2-latest" },
            new() { Name = "Azure AI", Provider = ProviderKind.AzureAI, Model = "gpt-4o" },
        };

        if (string.IsNullOrWhiteSpace(legacyJson)) return profiles;

        try
        {
            using var doc = JsonDocument.Parse(legacyJson);
            var root = doc.RootElement;
            foreach (var p in profiles)
            {
                if (root.TryGetProperty(p.Provider.ToString(), out var cfg) && cfg.ValueKind == JsonValueKind.Object)
                {
                    p.ApiKey = Str(cfg, "ApiKey");
                    var model = Str(cfg, "Model");
                    if (!string.IsNullOrWhiteSpace(model)) p.Model = model;
                    p.Endpoint = Str(cfg, "Endpoint");
                    p.Deployment = Str(cfg, "Deployment");
                }
            }
        }
        catch { /* legacy parse failed — keep defaults */ }

        return profiles;
    }

    private static string Str(JsonElement el, string name) =>
        el.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? (v.GetString() ?? "") : "";
}
