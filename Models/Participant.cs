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
using System.Drawing;
using System.Text.Json.Serialization;

namespace TheCouncil.Models;

public class Participant
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string DisplayName { get; set; } = "";

    /// <summary>True for the human seat. AI participants reference a <see cref="Profile"/> via <see cref="ProfileId"/>.</summary>
    public bool IsHuman { get; set; }

    /// <summary>Id of the <see cref="Profile"/> backing this AI participant (null for the human).</summary>
    public string? ProfileId { get; set; }

    /// <summary>Optional persona / area of expertise that flavors the system prompt.</summary>
    public string Persona { get; set; } = "";

    /// <summary>Accent color used for the participant's chat bubble / avatar.</summary>
    public int ColorArgb { get; set; } = Color.FromArgb(98, 100, 167).ToArgb();

    /// <summary>
    /// Convenience wrapper over <see cref="ColorArgb"/>. Marked <see cref="JsonIgnoreAttribute"/> so the
    /// serializer never persists the System.Drawing.Color (which would round-trip to a transparent value
    /// and clobber <see cref="ColorArgb"/>). A fully-transparent stored value falls back to the default accent.
    /// </summary>
    [JsonIgnore]
    public Color Color
    {
        get
        {
            var c = Color.FromArgb(ColorArgb);
            return c.A == 0 ? Color.FromArgb(98, 100, 167) : c;
        }
        set => ColorArgb = value.ToArgb();
    }

    [JsonIgnore]
    public string Initials
    {
        get
        {
            var parts = DisplayName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return "?";
            if (parts.Length == 1) return parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant();
            return ($"{parts[0][0]}{parts[^1][0]}").ToUpperInvariant();
        }
    }

    public override string ToString() => DisplayName;
}
