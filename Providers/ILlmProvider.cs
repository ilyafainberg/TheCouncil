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
using TheCouncil.Models;

namespace TheCouncil.Providers;

public interface ILlmProvider
{
    ProviderKind Kind { get; }

    /// <summary>Single-shot completion. The full council transcript is embedded in userPrompt.</summary>
    Task<string> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken ct);
}

public class LlmException : Exception
{
    public LlmException(string message) : base(message) { }
}
