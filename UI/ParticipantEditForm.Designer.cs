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

partial class ParticipantEditForm
{
    private System.ComponentModel.IContainer components = null;
    private Label nameLabel;
    private TextBox nameTextBox;
    private Label providerLabel;
    private ComboBox providerComboBox;
    private Label personaLabel;
    private TextBox personaTextBox;
    private Label colorLabel;
    private FlowLayoutPanel colorPanel;
    private FlowLayoutPanel buttonPanel;
    private Button okButton;
    private Button cancelButton;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null) components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        nameLabel = new Label();
        nameTextBox = new TextBox();
        providerLabel = new Label();
        providerComboBox = new ComboBox();
        personaLabel = new Label();
        personaTextBox = new TextBox();
        colorLabel = new Label();
        colorPanel = new FlowLayoutPanel();
        buttonPanel = new FlowLayoutPanel();
        okButton = new Button();
        cancelButton = new Button();
        buttonPanel.SuspendLayout();
        SuspendLayout();
        // 
        // nameLabel
        // 
        nameLabel.AutoSize = true;
        nameLabel.Location = new Point(18, 23);
        nameLabel.Margin = new Padding(0, 8, 0, 0);
        nameLabel.Name = "nameLabel";
        nameLabel.Size = new Size(43, 17);
        nameLabel.TabIndex = 0;
        nameLabel.Text = "Name";
        // 
        // nameTextBox
        // 
        nameTextBox.Location = new Point(123, 20);
        nameTextBox.Name = "nameTextBox";
        nameTextBox.Size = new Size(370, 24);
        nameTextBox.TabIndex = 1;
        // 
        // providerLabel
        // 
        providerLabel.AutoSize = true;
        providerLabel.Location = new Point(18, 53);
        providerLabel.Margin = new Padding(0, 10, 0, 0);
        providerLabel.Name = "providerLabel";
        providerLabel.Size = new Size(57, 17);
        providerLabel.TabIndex = 2;
        providerLabel.Text = "Profile";
        // 
        // providerComboBox
        // 
        providerComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        providerComboBox.Location = new Point(123, 50);
        providerComboBox.Name = "providerComboBox";
        providerComboBox.Size = new Size(370, 25);
        providerComboBox.TabIndex = 3;
        // 
        // personaLabel
        // 
        personaLabel.AutoSize = true;
        personaLabel.Location = new Point(18, 84);
        personaLabel.Margin = new Padding(0, 10, 0, 0);
        personaLabel.Name = "personaLabel";
        personaLabel.Size = new Size(55, 17);
        personaLabel.TabIndex = 4;
        personaLabel.Text = "Persona";
        // 
        // personaTextBox
        // 
        personaTextBox.Location = new Point(123, 81);
        personaTextBox.Multiline = true;
        personaTextBox.Name = "personaTextBox";
        personaTextBox.PlaceholderText = "Optional: expertise / viewpoint (e.g. 'security-first architect')";
        personaTextBox.Size = new Size(370, 24);
        personaTextBox.TabIndex = 5;
        // 
        // colorLabel
        // 
        colorLabel.AutoSize = true;
        colorLabel.Location = new Point(18, 112);
        colorLabel.Margin = new Padding(0, 10, 0, 0);
        colorLabel.Name = "colorLabel";
        colorLabel.Size = new Size(40, 17);
        colorLabel.TabIndex = 6;
        colorLabel.Text = "Color";
        // 
        // colorPanel
        // 
        colorPanel.Location = new Point(123, 112);
        colorPanel.Name = "colorPanel";
        colorPanel.Size = new Size(370, 40);
        colorPanel.TabIndex = 7;
        // 
        // buttonPanel
        // 
        buttonPanel.BackColor = Color.FromArgb(247, 247, 250);
        buttonPanel.Controls.Add(okButton);
        buttonPanel.Controls.Add(cancelButton);
        buttonPanel.Dock = DockStyle.Bottom;
        buttonPanel.FlowDirection = FlowDirection.RightToLeft;
        buttonPanel.Location = new Point(0, 211);
        buttonPanel.Name = "buttonPanel";
        buttonPanel.Padding = new Padding(12, 8, 12, 8);
        buttonPanel.Size = new Size(521, 50);
        buttonPanel.TabIndex = 1;
        // 
        // okButton
        // 
        okButton.BackColor = Color.FromArgb(98, 100, 167);
        okButton.DialogResult = DialogResult.OK;
        okButton.FlatAppearance.BorderSize = 0;
        okButton.FlatStyle = FlatStyle.Flat;
        okButton.ForeColor = Color.White;
        okButton.Location = new Point(394, 11);
        okButton.Name = "okButton";
        okButton.Size = new Size(100, 32);
        okButton.TabIndex = 0;
        okButton.Text = "Save";
        okButton.UseVisualStyleBackColor = false;
        okButton.Click += OkButton_Click;
        // 
        // cancelButton
        // 
        cancelButton.DialogResult = DialogResult.Cancel;
        cancelButton.FlatStyle = FlatStyle.Flat;
        cancelButton.Location = new Point(298, 11);
        cancelButton.Name = "cancelButton";
        cancelButton.Size = new Size(90, 32);
        cancelButton.TabIndex = 1;
        cancelButton.Text = "Cancel";
        // 
        // ParticipantEditForm
        // 
        AcceptButton = okButton;
        AutoScaleDimensions = new SizeF(7F, 17F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.White;
        CancelButton = cancelButton;
        ClientSize = new Size(521, 261);
        Controls.Add(nameLabel);
        Controls.Add(providerLabel);
        Controls.Add(nameTextBox);
        Controls.Add(personaLabel);
        Controls.Add(buttonPanel);
        Controls.Add(colorLabel);
        Controls.Add(providerComboBox);
        Controls.Add(colorPanel);
        Controls.Add(personaTextBox);
        Font = new Font("Segoe UI", 9.5F);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "ParticipantEditForm";
        StartPosition = FormStartPosition.CenterParent;
        Text = "Participant";
        buttonPanel.ResumeLayout(false);
        ResumeLayout(false);
        PerformLayout();
    }
}
