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

partial class UpdateProgressForm
{
    private System.ComponentModel.IContainer components = null;

    private Label titleLabel;
    private Label phaseLabel;
    private ProgressBar progressBar;
    private Label detailLabel;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null) components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        titleLabel = new Label();
        phaseLabel = new Label();
        progressBar = new ProgressBar();
        detailLabel = new Label();
        SuspendLayout();
        //
        // titleLabel
        //
        titleLabel.AutoSize = true;
        titleLabel.Font = new Font("Segoe UI Semibold", 11F);
        titleLabel.ForeColor = Color.FromArgb(98, 100, 167);
        titleLabel.Location = new Point(18, 16);
        titleLabel.Name = "titleLabel";
        titleLabel.Text = "Updating The Council";
        //
        // phaseLabel
        //
        phaseLabel.AutoSize = true;
        phaseLabel.Location = new Point(20, 46);
        phaseLabel.Name = "phaseLabel";
        phaseLabel.Text = "Starting…";
        //
        // progressBar
        //
        progressBar.Location = new Point(20, 70);
        progressBar.Name = "progressBar";
        progressBar.Size = new Size(380, 18);
        progressBar.Style = ProgressBarStyle.Marquee;
        progressBar.MarqueeAnimationSpeed = 30;
        //
        // detailLabel
        //
        detailLabel.AutoSize = true;
        detailLabel.ForeColor = Color.FromArgb(120, 120, 128);
        detailLabel.Location = new Point(20, 94);
        detailLabel.Name = "detailLabel";
        detailLabel.Text = "";
        //
        // UpdateProgressForm
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.White;
        ClientSize = new Size(420, 128);
        ControlBox = false;
        Controls.Add(titleLabel);
        Controls.Add(phaseLabel);
        Controls.Add(progressBar);
        Controls.Add(detailLabel);
        Font = new Font("Segoe UI", 9.5F);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        Name = "UpdateProgressForm";
        StartPosition = FormStartPosition.CenterParent;
        Text = "Updating";
        ResumeLayout(false);
        PerformLayout();
    }
}
