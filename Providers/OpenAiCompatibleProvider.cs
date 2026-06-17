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
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using TheCouncil.Models;

namespace TheCouncil.Providers;

/// <summary>
/// Works for any OpenAI-compatible /chat/completions endpoint: OpenAI, xAI Grok, and Azure OpenAI.
/// </summary>
public class OpenAiCompatibleProvider : ILlmProvider
{
    private readonly HttpClient http;
    private readonly string url;
    private readonly string model;
    private readonly string apiKey;
    private readonly bool azureStyleAuth;

    public ProviderKind Kind { get; }

    public OpenAiCompatibleProvider(ProviderKind kind, Profile cfg, HttpClient httpClient)
    {
        Kind = kind;
        http = httpClient;
        apiKey = cfg.ApiKey;

        switch (kind)
        {
            case ProviderKind.OpenAI:
                url = "https://api.openai.com/v1/chat/completions";
                model = string.IsNullOrWhiteSpace(cfg.Model) ? "gpt-4o" : cfg.Model;
                azureStyleAuth = false;
                break;
            case ProviderKind.Grok:
                url = "https://api.x.ai/v1/chat/completions";
                model = string.IsNullOrWhiteSpace(cfg.Model) ? "grok-2-latest" : cfg.Model;
                azureStyleAuth = false;
                break;
            case ProviderKind.AzureAI:
                const string apiVersion = "2024-10-21";
                var baseRoot = NormalizeAzureRoot(cfg.Endpoint);
                var deployment = string.IsNullOrWhiteSpace(cfg.Deployment) ? cfg.Model : cfg.Deployment;
                url = $"{baseRoot}/openai/deployments/{deployment}/chat/completions?api-version={apiVersion}";
                model = deployment;
                azureStyleAuth = true;
                break;
            default:
                throw new ArgumentException($"{kind} is not OpenAI-compatible");
        }
    }

    public async Task<string> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new LlmException($"{Kind}: API key is not configured (open Settings).");

        var (ok, body) = await PostAsync(systemPrompt, userPrompt, includeTemperature: true, ct);

        // Some newer models (e.g. GPT-5 / o-series reasoning models) reject a custom temperature.
        // Transparently retry once without it instead of failing the turn.
        if (!ok && body.Contains("temperature", StringComparison.OrdinalIgnoreCase))
            (ok, body) = await PostAsync(systemPrompt, userPrompt, includeTemperature: false, ct);

        if (!ok)
            throw new LlmException($"{Kind}: {Truncate(body)}");

        using var doc = JsonDocument.Parse(body);
        return doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "";
    }

    private async Task<(bool ok, string body)> PostAsync(string systemPrompt, string userPrompt, bool includeTemperature, CancellationToken ct)
    {
        object payload = includeTemperature
            ? new
            {
                model,
                temperature = 0.7,
                messages = new object[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                }
            }
            : new
            {
                model,
                messages = new object[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                }
            };

        using var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };

        if (azureStyleAuth)
            req.Headers.TryAddWithoutValidation("api-key", apiKey);
        else
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        using var resp = await http.SendAsync(req, ct);
        var body = await resp.Content.ReadAsStringAsync(ct);
        return (resp.IsSuccessStatusCode, resp.IsSuccessStatusCode ? body : $"HTTP {(int)resp.StatusCode}: {body}");
    }

    private static string Truncate(string s) => s.Length > 400 ? s[..400] + "…" : s;

    /// <summary>
    /// Reduces an Azure endpoint to its scheme+host root so the OpenAI deployment path can be
    /// appended cleanly. This strips any extra path such as an AI Foundry project endpoint
    /// (e.g. ".../api/projects/MyProject") that would otherwise corrupt the request URL.
    /// </summary>
    private static string NormalizeAzureRoot(string endpoint)
    {
        var raw = (endpoint ?? "").Trim();
        if (raw.Length == 0) return raw;
        if (!raw.Contains("://")) raw = "https://" + raw;
        return Uri.TryCreate(raw, UriKind.Absolute, out var uri)
            ? $"{uri.Scheme}://{uri.Host}"
            : raw.TrimEnd('/');
    }
}
