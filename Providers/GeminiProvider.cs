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
using System.Net.Http;
using System.Text;
using System.Text.Json;
using TheCouncil.Models;

namespace TheCouncil.Providers;

public class GeminiProvider : ILlmProvider
{
    private readonly HttpClient http;
    private readonly string apiKey;
    private readonly string model;

    public ProviderKind Kind => ProviderKind.Gemini;

    public GeminiProvider(Profile cfg, HttpClient httpClient)
    {
        http = httpClient;
        apiKey = cfg.ApiKey;
        model = string.IsNullOrWhiteSpace(cfg.Model) ? "gemini-1.5-pro" : cfg.Model;
    }

    public async Task<string> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new LlmException("Gemini: API key is not configured (open Settings).");

        var requestUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

        var payload = new
        {
            system_instruction = new { parts = new[] { new { text = systemPrompt } } },
            contents = new[]
            {
                new { role = "user", parts = new[] { new { text = userPrompt } } }
            },
            generationConfig = new { temperature = 0.7 }
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, requestUrl)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };

        using var resp = await http.SendAsync(req, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new LlmException($"Gemini HTTP {(int)resp.StatusCode}: {Truncate(body)}");

        using var doc = JsonDocument.Parse(body);
        var sb = new StringBuilder();
        var candidates = doc.RootElement.GetProperty("candidates");
        if (candidates.GetArrayLength() == 0) return "";
        foreach (var part in candidates[0].GetProperty("content").GetProperty("parts").EnumerateArray())
        {
            if (part.TryGetProperty("text", out var t))
                sb.Append(t.GetString());
        }
        return sb.ToString();
    }

    private static string Truncate(string s) => s.Length > 400 ? s[..400] + "…" : s;
}
