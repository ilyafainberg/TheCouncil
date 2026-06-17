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
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using TheCouncil.Models;

namespace TheCouncil.UI;

public partial class ParticipantEditForm : Form
{
    private static readonly Color[] Palette =
    {
        Color.FromArgb(98, 100, 167),  // purple
        Color.FromArgb(16, 124, 16),   // green
        Color.FromArgb(196, 49, 75),   // red
        Color.FromArgb(0, 120, 212),   // blue
        Color.FromArgb(202, 80, 16),   // orange
        Color.FromArgb(140, 88, 184),  // violet
        Color.FromArgb(0, 153, 168),   // teal
    };

    private readonly List<Profile> profiles = new();
    private Color selectedColor;
    private bool populating;

    /// <summary>Sentinel item shown at the top of the dropdown to create the human seat.</summary>
    private static readonly string HumanOption = "🙂  Human (you)";

    public Participant Participant { get; }

    /// <summary>Parameterless constructor required by the Visual Studio Designer.</summary>
    public ParticipantEditForm()
    {
        InitializeComponent();
        Participant = new Participant();
        selectedColor = Participant.Color;
        providerLabel.Text = "Profile";
        providerComboBox.SelectedIndexChanged += ProfileComboBox_SelectedIndexChanged;
        BuildSwatches();
        Text = "Add participant";
    }

    public ParticipantEditForm(Participant existing, CouncilSettings settings) : this()
    {
        Participant = existing;
        profiles.AddRange(settings.Profiles);
        PopulateProfiles();

        Text = string.IsNullOrEmpty(existing.DisplayName) ? "Add participant" : "Edit participant";
        nameTextBox.Text = Participant.DisplayName;
        personaTextBox.Text = Participant.Persona;
        selectedColor = Participant.Color;
        HighlightSelectedSwatch();
    }

    private void PopulateProfiles()
    {
        populating = true;
        providerComboBox.Items.Clear();
        providerComboBox.Items.Add(HumanOption);
        foreach (var p in profiles)
            providerComboBox.Items.Add(p);

        if (Participant.IsHuman)
            providerComboBox.SelectedItem = HumanOption;
        else
        {
            var match = profiles.FirstOrDefault(p => p.Id == Participant.ProfileId);
            if (match != null) providerComboBox.SelectedItem = match;
            else providerComboBox.SelectedIndex = profiles.Count > 0 ? 1 : 0;
        }
        populating = false;
        UpdateForSelection();
    }

    private void ProfileComboBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (populating) return;
        if (providerComboBox.SelectedItem is Profile p && string.IsNullOrWhiteSpace(nameTextBox.Text))
            nameTextBox.Text = p.Name;
        UpdateForSelection();
    }

    /// <summary>Humans have no model/persona — grey those out and offer a sensible default name.</summary>
    private void UpdateForSelection()
    {
        bool human = ReferenceEquals(providerComboBox.SelectedItem, HumanOption);
        personaTextBox.Enabled = !human;
        if (human && string.IsNullOrWhiteSpace(nameTextBox.Text))
            nameTextBox.Text = "You";
    }

    private void BuildSwatches()
    {
        colorPanel.Controls.Clear();
        foreach (var c in Palette)
        {
            var swatch = new Panel
            {
                Width = 28,
                Height = 28,
                BackColor = c,
                Margin = new Padding(3),
                Cursor = Cursors.Hand,
                Tag = c
            };
            swatch.Click += Swatch_Click;
            colorPanel.Controls.Add(swatch);
        }
        HighlightSelectedSwatch();
    }

    private void Swatch_Click(object? sender, EventArgs e)
    {
        if (sender is Panel p && p.Tag is Color c)
        {
            selectedColor = c;
            HighlightSelectedSwatch();
        }
    }

    private void HighlightSelectedSwatch()
    {
        foreach (Control ctl in colorPanel.Controls)
        {
            if (ctl is Panel p && p.Tag is Color c)
                p.BorderStyle = c.ToArgb() == selectedColor.ToArgb()
                    ? BorderStyle.Fixed3D : BorderStyle.None;
        }
    }

    private void OkButton_Click(object? sender, EventArgs e)
    {
        if (ReferenceEquals(providerComboBox.SelectedItem, HumanOption))
        {
            Participant.IsHuman = true;
            Participant.ProfileId = null;
            Participant.Persona = "";
            if (string.IsNullOrWhiteSpace(nameTextBox.Text)) nameTextBox.Text = "You";
        }
        else
        {
            Participant.IsHuman = false;
            if (providerComboBox.SelectedItem is Profile p)
            {
                Participant.ProfileId = p.Id;
                if (string.IsNullOrWhiteSpace(nameTextBox.Text)) nameTextBox.Text = p.Name;
            }
            Participant.Persona = personaTextBox.Text.Trim();
        }
        Participant.DisplayName = string.IsNullOrWhiteSpace(nameTextBox.Text) ? "Unnamed" : nameTextBox.Text.Trim();
        Participant.Color = selectedColor;
    }
}
