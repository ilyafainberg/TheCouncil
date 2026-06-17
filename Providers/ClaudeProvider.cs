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

public class ClaudeProvider : ILlmProvider
{
    private readonly HttpClient http;
    private readonly string apiKey;
    private readonly string model;

    public ProviderKind Kind => ProviderKind.Claude;

    public ClaudeProvider(Profile cfg, HttpClient httpClient)
    {
        http = httpClient;
        apiKey = cfg.ApiKey;
        model = string.IsNullOrWhiteSpace(cfg.Model) ? "claude-3-5-sonnet-latest" : cfg.Model;
    }

    public async Task<string> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new LlmException("Claude: API key is not configured (open Settings).");

        var payload = new
        {
            model,
            max_tokens = 1024,
            system = systemPrompt,
            messages = new object[]
            {
                new { role = "user", content = userPrompt }
            }
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        req.Headers.TryAddWithoutValidation("x-api-key", apiKey);
        req.Headers.TryAddWithoutValidation("anthropic-version", "2023-06-01");

        using var resp = await http.SendAsync(req, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new LlmException($"Claude HTTP {(int)resp.StatusCode}: {Truncate(body)}");

        using var doc = JsonDocument.Parse(body);
        var sb = new StringBuilder();
        foreach (var block in doc.RootElement.GetProperty("content").EnumerateArray())
        {
            if (block.TryGetProperty("text", out var t))
                sb.Append(t.GetString());
        }
        return sb.ToString();
    }

    private static string Truncate(string s) => s.Length > 400 ? s[..400] + "…" : s;
}
