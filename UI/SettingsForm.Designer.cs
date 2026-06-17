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
using System.Drawing;
using System.Windows.Forms;

namespace TheCouncil.UI;

partial class SettingsForm
{
    private System.ComponentModel.IContainer components = null;

    private Panel leftPanel;
    private Label listHeaderLabel;
    private ListBox profilesListBox;
    private Panel listButtons;
    private Button addProfileButton;
    private Button removeProfileButton;

    private TableLayoutPanel detailLayout;
    private Label detailHeaderLabel;
    private Label nameLabel;
    private TextBox nameTextBox;
    private Label providerLabel;
    private ComboBox providerComboBox;
    private Label modelLabel;
    private TextBox modelTextBox;
    private Label keyLabel;
    private TextBox keyTextBox;
    private Label endpointLabel;
    private TextBox endpointTextBox;
    private Label deploymentLabel;
    private TextBox deploymentTextBox;
    private Label azureHintLabel;
    private Label roundLimitLabel;
    private NumericUpDown roundLimitUpDown;

    private Panel buttonPanel;
    private Button saveButton;
    private Button cancelButton;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null) components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        leftPanel = new Panel();
        listHeaderLabel = new Label();
        profilesListBox = new ListBox();
        listButtons = new Panel();
        addProfileButton = new Button();
        removeProfileButton = new Button();
        detailLayout = new TableLayoutPanel();
        detailHeaderLabel = new Label();
        nameLabel = new Label();
        nameTextBox = new TextBox();
        providerLabel = new Label();
        providerComboBox = new ComboBox();
        modelLabel = new Label();
        modelTextBox = new TextBox();
        keyLabel = new Label();
        keyTextBox = new TextBox();
        endpointLabel = new Label();
        endpointTextBox = new TextBox();
        deploymentLabel = new Label();
        deploymentTextBox = new TextBox();
        azureHintLabel = new Label();
        roundLimitLabel = new Label();
        roundLimitUpDown = new NumericUpDown();
        buttonPanel = new Panel();
        saveButton = new Button();
        cancelButton = new Button();
        leftPanel.SuspendLayout();
        listButtons.SuspendLayout();
        detailLayout.SuspendLayout();
        buttonPanel.SuspendLayout();
        SuspendLayout();
        //
        // leftPanel
        //
        leftPanel.BackColor = Color.FromArgb(249, 249, 251);
        leftPanel.Controls.Add(profilesListBox);
        leftPanel.Controls.Add(listButtons);
        leftPanel.Controls.Add(listHeaderLabel);
        leftPanel.Dock = DockStyle.Left;
        leftPanel.Padding = new Padding(12, 16, 12, 12);
        leftPanel.Width = 220;
        leftPanel.Name = "leftPanel";
        //
        // listHeaderLabel
        //
        listHeaderLabel.Dock = DockStyle.Top;
        listHeaderLabel.Font = new Font("Segoe UI Semibold", 9F);
        listHeaderLabel.ForeColor = Color.FromArgb(96, 96, 104);
        listHeaderLabel.Height = 24;
        listHeaderLabel.Name = "listHeaderLabel";
        listHeaderLabel.Text = "PROFILES";
        //
        // profilesListBox
        //
        profilesListBox.BorderStyle = BorderStyle.FixedSingle;
        profilesListBox.Dock = DockStyle.Fill;
        profilesListBox.Font = new Font("Segoe UI", 10F);
        profilesListBox.IntegralHeight = false;
        profilesListBox.ItemHeight = 24;
        profilesListBox.Name = "profilesListBox";
        profilesListBox.SelectedIndexChanged += ProfilesListBox_SelectedIndexChanged;
        //
        // listButtons
        //
        listButtons.Controls.Add(addProfileButton);
        listButtons.Controls.Add(removeProfileButton);
        listButtons.Dock = DockStyle.Bottom;
        listButtons.Height = 44;
        listButtons.Padding = new Padding(0, 8, 0, 0);
        listButtons.Name = "listButtons";
        //
        // addProfileButton
        //
        addProfileButton.BackColor = Color.FromArgb(98, 100, 167);
        addProfileButton.FlatStyle = FlatStyle.Flat;
        addProfileButton.ForeColor = Color.White;
        addProfileButton.Location = new Point(0, 8);
        addProfileButton.Size = new Size(91, 32);
        addProfileButton.Name = "addProfileButton";
        addProfileButton.Text = "＋ Add";
        addProfileButton.Click += AddProfileButton_Click;
        //
        // removeProfileButton
        //
        removeProfileButton.BackColor = Color.FromArgb(237, 237, 245);
        removeProfileButton.FlatStyle = FlatStyle.Flat;
        removeProfileButton.ForeColor = Color.FromArgb(60, 60, 70);
        removeProfileButton.Location = new Point(97, 8);
        removeProfileButton.Size = new Size(99, 32);
        removeProfileButton.Name = "removeProfileButton";
        removeProfileButton.Text = "✕ Remove";
        removeProfileButton.Click += RemoveProfileButton_Click;
        //
        // detailLayout
        //
        detailLayout.ColumnCount = 2;
        detailLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));
        detailLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        detailLayout.RowCount = 8;
        detailLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        detailLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        detailLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        detailLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        detailLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        detailLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        detailLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        detailLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        detailLayout.Controls.Add(detailHeaderLabel, 0, 0);
        detailLayout.Controls.Add(nameLabel, 0, 1);
        detailLayout.Controls.Add(nameTextBox, 1, 1);
        detailLayout.Controls.Add(providerLabel, 0, 2);
        detailLayout.Controls.Add(providerComboBox, 1, 2);
        detailLayout.Controls.Add(modelLabel, 0, 3);
        detailLayout.Controls.Add(modelTextBox, 1, 3);
        detailLayout.Controls.Add(keyLabel, 0, 4);
        detailLayout.Controls.Add(keyTextBox, 1, 4);
        detailLayout.Controls.Add(endpointLabel, 0, 5);
        detailLayout.Controls.Add(endpointTextBox, 1, 5);
        detailLayout.Controls.Add(deploymentLabel, 0, 6);
        detailLayout.Controls.Add(deploymentTextBox, 1, 6);
        detailLayout.SetColumnSpan(detailHeaderLabel, 2);
        detailLayout.Dock = DockStyle.Fill;
        detailLayout.Padding = new Padding(24, 16, 24, 12);
        detailLayout.Name = "detailLayout";
        //
        // detailHeaderLabel
        //
        detailHeaderLabel.AutoSize = true;
        detailHeaderLabel.Font = new Font("Segoe UI Semibold", 11F);
        detailHeaderLabel.ForeColor = Color.FromArgb(98, 100, 167);
        detailHeaderLabel.Margin = new Padding(0, 0, 0, 10);
        detailHeaderLabel.Name = "detailHeaderLabel";
        detailHeaderLabel.Text = "Profile details";
        //
        // nameLabel
        //
        nameLabel.Anchor = AnchorStyles.Left;
        nameLabel.AutoSize = true;
        nameLabel.Margin = new Padding(0, 9, 8, 4);
        nameLabel.Name = "nameLabel";
        nameLabel.Text = "Profile name";
        //
        // nameTextBox
        //
        nameTextBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        nameTextBox.Margin = new Padding(0, 6, 0, 4);
        nameTextBox.Name = "nameTextBox";
        nameTextBox.TextChanged += NameTextBox_TextChanged;
        //
        // providerLabel
        //
        providerLabel.Anchor = AnchorStyles.Left;
        providerLabel.AutoSize = true;
        providerLabel.Margin = new Padding(0, 11, 8, 4);
        providerLabel.Name = "providerLabel";
        providerLabel.Text = "Provider";
        //
        // providerComboBox
        //
        providerComboBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        providerComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        providerComboBox.Margin = new Padding(0, 6, 0, 4);
        providerComboBox.Name = "providerComboBox";
        providerComboBox.SelectedIndexChanged += ProviderComboBox_SelectedIndexChanged;
        //
        // modelLabel
        //
        modelLabel.Anchor = AnchorStyles.Left;
        modelLabel.AutoSize = true;
        modelLabel.Margin = new Padding(0, 9, 8, 4);
        modelLabel.Name = "modelLabel";
        modelLabel.Text = "Model";
        //
        // modelTextBox
        //
        modelTextBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        modelTextBox.Margin = new Padding(0, 6, 0, 4);
        modelTextBox.Name = "modelTextBox";
        //
        // keyLabel
        //
        keyLabel.Anchor = AnchorStyles.Left;
        keyLabel.AutoSize = true;
        keyLabel.Margin = new Padding(0, 9, 8, 4);
        keyLabel.Name = "keyLabel";
        keyLabel.Text = "API key";
        //
        // keyTextBox
        //
        keyTextBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        keyTextBox.Margin = new Padding(0, 6, 0, 4);
        keyTextBox.UseSystemPasswordChar = true;
        keyTextBox.Name = "keyTextBox";
        //
        // endpointLabel
        //
        endpointLabel.Anchor = AnchorStyles.Left;
        endpointLabel.AutoSize = true;
        endpointLabel.Margin = new Padding(0, 9, 8, 4);
        endpointLabel.Name = "endpointLabel";
        endpointLabel.Text = "Endpoint";
        //
        // endpointTextBox
        //
        endpointTextBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        endpointTextBox.Margin = new Padding(0, 6, 0, 4);
        endpointTextBox.Name = "endpointTextBox";
        //
        // deploymentLabel
        //
        deploymentLabel.Anchor = AnchorStyles.Left;
        deploymentLabel.AutoSize = true;
        deploymentLabel.Margin = new Padding(0, 9, 8, 4);
        deploymentLabel.Name = "deploymentLabel";
        deploymentLabel.Text = "Deployment";
        //
        // deploymentTextBox
        //
        deploymentTextBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        deploymentTextBox.Margin = new Padding(0, 6, 0, 4);
        deploymentTextBox.Name = "deploymentTextBox";
        //
        // roundLimitLabel
        //
        roundLimitLabel.AutoSize = true;
        roundLimitLabel.Location = new Point(14, 18);
        roundLimitLabel.Name = "roundLimitLabel";
        roundLimitLabel.Text = "Max debate rounds:";
        //
        // roundLimitUpDown
        //
        roundLimitUpDown.Location = new Point(140, 16);
        roundLimitUpDown.Minimum = 1;
        roundLimitUpDown.Maximum = 50;
        roundLimitUpDown.Value = 10;
        roundLimitUpDown.Width = 54;
        roundLimitUpDown.Name = "roundLimitUpDown";
        //
        // buttonPanel
        //
        buttonPanel.BackColor = Color.FromArgb(247, 247, 250);
        buttonPanel.Controls.Add(roundLimitLabel);
        buttonPanel.Controls.Add(roundLimitUpDown);
        buttonPanel.Controls.Add(saveButton);
        buttonPanel.Controls.Add(cancelButton);
        buttonPanel.Dock = DockStyle.Bottom;
        buttonPanel.Height = 56;
        buttonPanel.Name = "buttonPanel";
        buttonPanel.Resize += ButtonPanel_Resize;
        //
        // saveButton
        //
        saveButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        saveButton.BackColor = Color.FromArgb(98, 100, 167);
        saveButton.DialogResult = DialogResult.OK;
        saveButton.FlatStyle = FlatStyle.Flat;
        saveButton.ForeColor = Color.White;
        saveButton.Location = new Point(598, 11);
        saveButton.Size = new Size(110, 34);
        saveButton.Name = "saveButton";
        saveButton.Text = "Save";
        saveButton.Click += SaveButton_Click;
        //
        // cancelButton
        //
        cancelButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        cancelButton.DialogResult = DialogResult.Cancel;
        cancelButton.FlatStyle = FlatStyle.Flat;
        cancelButton.Location = new Point(488, 11);
        cancelButton.Size = new Size(100, 34);
        cancelButton.Name = "cancelButton";
        cancelButton.Text = "Cancel";
        //
        // SettingsForm
        //
        AcceptButton = saveButton;
        CancelButton = cancelButton;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.White;
        ClientSize = new Size(720, 470);
        Controls.Add(detailLayout);
        Controls.Add(leftPanel);
        Controls.Add(buttonPanel);
        Font = new Font("Segoe UI", 9.5F);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "SettingsForm";
        StartPosition = FormStartPosition.CenterParent;
        Text = "The Council — Profiles";
        saveButton.FlatAppearance.BorderSize = 0;
        addProfileButton.FlatAppearance.BorderSize = 0;
        removeProfileButton.FlatAppearance.BorderSize = 0;
        leftPanel.ResumeLayout(false);
        listButtons.ResumeLayout(false);
        detailLayout.ResumeLayout(false);
        detailLayout.PerformLayout();
        buttonPanel.ResumeLayout(false);
        ResumeLayout(false);
    }
}
