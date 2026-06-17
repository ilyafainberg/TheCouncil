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
using TheCouncil.Models;

namespace TheCouncil.Providers;

public static class ProviderFactory
{
    private static readonly HttpClient http = new() { Timeout = TimeSpan.FromSeconds(120) };

    public static ILlmProvider Create(Profile profile)
    {
        return profile.Provider switch
        {
            ProviderKind.OpenAI => new OpenAiCompatibleProvider(ProviderKind.OpenAI, profile, http),
            ProviderKind.Grok => new OpenAiCompatibleProvider(ProviderKind.Grok, profile, http),
            ProviderKind.AzureAI => new OpenAiCompatibleProvider(ProviderKind.AzureAI, profile, http),
            ProviderKind.Claude => new ClaudeProvider(profile, http),
            ProviderKind.Gemini => new GeminiProvider(profile, http),
            _ => throw new ArgumentException($"No provider for {profile.Provider}")
        };
    }
}
