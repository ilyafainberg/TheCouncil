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
using System.Linq;
using System.Windows.Forms;
using TheCouncil.Models;

namespace TheCouncil.UI;

public partial class SettingsForm : Form
{
    private readonly CouncilSettings settings;
    private readonly System.Collections.Generic.List<Profile> working = new();
    private Profile? current;
    private bool loadingEditor;

    /// <summary>Parameterless constructor required by the Visual Studio Designer.</summary>
    public SettingsForm()
    {
        InitializeComponent();
        foreach (ProviderKind k in Enum.GetValues<ProviderKind>())
            if (k != ProviderKind.Human)
                providerComboBox.Items.Add(k);
        settings = new CouncilSettings();
    }

    public SettingsForm(CouncilSettings councilSettings) : this()
    {
        settings = councilSettings;
        foreach (var p in settings.Profiles)
            working.Add(Clone(p));
        RefreshList();
        roundLimitUpDown.Value = Math.Clamp(settings.MaxRounds <= 0 ? 10 : settings.MaxRounds, 1, 50);
        if (working.Count > 0) profilesListBox.SelectedIndex = 0;
        else SetEditorEnabled(false);
    }

    private static Profile Clone(Profile p) => new()
    {
        Id = p.Id, Name = p.Name, Provider = p.Provider, Model = p.Model,
        ApiKey = p.ApiKey, Endpoint = p.Endpoint, Deployment = p.Deployment
    };

    private void RefreshList()
    {
        profilesListBox.BeginUpdate();
        profilesListBox.Items.Clear();
        foreach (var p in working) profilesListBox.Items.Add(p);
        profilesListBox.EndUpdate();
    }

    private void ProfilesListBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        FlushEditor();
        current = profilesListBox.SelectedItem as Profile;
        LoadEditor();
    }

    private void LoadEditor()
    {
        if (current == null)
        {
            SetEditorEnabled(false);
            return;
        }
        loadingEditor = true;
        SetEditorEnabled(true);
        nameTextBox.Text = current.Name;
        providerComboBox.SelectedItem = current.Provider;
        modelTextBox.Text = current.Model;
        keyTextBox.Text = current.ApiKey;
        endpointTextBox.Text = current.Endpoint;
        deploymentTextBox.Text = current.Deployment;
        loadingEditor = false;
        UpdateAzureFields();
    }

    private void FlushEditor()
    {
        if (current == null) return;
        current.Name = string.IsNullOrWhiteSpace(nameTextBox.Text) ? "Unnamed" : nameTextBox.Text.Trim();
        current.Provider = providerComboBox.SelectedItem is ProviderKind k ? k : current.Provider;
        current.Model = modelTextBox.Text.Trim();
        current.ApiKey = keyTextBox.Text.Trim();
        current.Endpoint = endpointTextBox.Text.Trim();
        current.Deployment = deploymentTextBox.Text.Trim();
    }

    private void SetEditorEnabled(bool on)
    {
        nameTextBox.Enabled = providerComboBox.Enabled = modelTextBox.Enabled =
            keyTextBox.Enabled = on;
        endpointTextBox.Enabled = deploymentTextBox.Enabled = on;
        removeProfileButton.Enabled = on;
    }

    private void UpdateAzureFields()
    {
        bool azure = providerComboBox.SelectedItem is ProviderKind.AzureAI;
        endpointTextBox.Enabled = azure;
        deploymentTextBox.Enabled = azure;
        endpointLabel.Enabled = azure;
        deploymentLabel.Enabled = azure;

        // Azure addresses the model via its Deployment, so Model is not used there.
        modelTextBox.Enabled = !azure;
        modelLabel.Enabled = !azure;
        modelTextBox.PlaceholderText = azure ? "not used for Azure (set Deployment)" : "";
    }

    private void NameTextBox_TextChanged(object? sender, EventArgs e)
    {
        if (loadingEditor || current == null) return;
        current.Name = nameTextBox.Text;
        // Repaint the list so the item label updates, WITHOUT reassigning the item
        // (reassigning Items[idx] steals focus back to the ListBox on every keystroke).
        profilesListBox.Invalidate();
    }

    private void ProviderComboBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (!loadingEditor) UpdateAzureFields();
    }

    private void AddProfileButton_Click(object? sender, EventArgs e)
    {
        FlushEditor();
        var p = new Profile { Name = "New profile", Provider = ProviderKind.OpenAI, Model = "" };
        working.Add(p);
        RefreshList();
        profilesListBox.SelectedItem = p;
        nameTextBox.Focus();
        nameTextBox.SelectAll();
    }

    private void RemoveProfileButton_Click(object? sender, EventArgs e)
    {
        if (current == null) return;
        working.Remove(current);
        current = null;
        RefreshList();
        if (working.Count > 0) profilesListBox.SelectedIndex = 0;
        else { ClearEditor(); SetEditorEnabled(false); }
    }

    private void ClearEditor()
    {
        loadingEditor = true;
        nameTextBox.Clear();
        providerComboBox.SelectedIndex = -1;
        modelTextBox.Clear();
        keyTextBox.Clear();
        endpointTextBox.Clear();
        deploymentTextBox.Clear();
        loadingEditor = false;
    }

    private void SaveButton_Click(object? sender, EventArgs e)
    {
        FlushEditor();
        settings.Profiles.Clear();
        settings.Profiles.AddRange(working);
        settings.MaxRounds = (int)roundLimitUpDown.Value;
        settings.Save();
    }

    private void ButtonPanel_Resize(object? sender, EventArgs e)
    {
        saveButton.Location = new Point(buttonPanel.Width - saveButton.Width - 12, 11);
        cancelButton.Location = new Point(saveButton.Left - cancelButton.Width - 8, 11);
    }
}
