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
namespace TheCouncil.Models;

/// <summary>
/// A reusable model endpoint configuration. Participants reference a profile by id, so several
/// participants can share one profile and the same provider can back many differently-named profiles.
/// </summary>
public class Profile
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string Name { get; set; } = "";
    public ProviderKind Provider { get; set; }
    public string Model { get; set; } = "";
    public string ApiKey { get; set; } = "";

    // Azure-only
    public string Endpoint { get; set; } = "";
    public string Deployment { get; set; } = "";

    public bool IsAzure => Provider == ProviderKind.AzureAI;

    public override string ToString() => string.IsNullOrWhiteSpace(Name) ? Provider.ToString() : Name;
}
