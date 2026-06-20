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
using System.Windows.Forms;
using TheCouncil.Updating;

namespace TheCouncil.UI;

/// <summary>
/// A small modeless dialog that shows live update progress so the user always knows
/// what the updater is doing (downloading %, preparing, launching) and when it's done.
/// </summary>
public partial class UpdateProgressForm : Form, IProgress<UpdateChecker.UpdateProgress>
{
    public UpdateProgressForm(Version targetVersion)
    {
        InitializeComponent();
        titleLabel.Text = $"Updating to v{targetVersion}";
    }

    /// <summary>Receives progress from the updater (marshaled onto the UI thread).</summary>
    public void Report(UpdateChecker.UpdateProgress value)
    {
        if (IsDisposed) return;
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => Report(value)));
            return;
        }

        switch (value.Phase)
        {
            case UpdateChecker.UpdatePhase.Downloading:
                if (value.Fraction >= 0)
                {
                    progressBar.Style = ProgressBarStyle.Continuous;
                    progressBar.Value = (int)Math.Round(Math.Clamp(value.Fraction, 0, 1) * 100);
                    phaseLabel.Text = $"Downloading update… {progressBar.Value}%";
                    detailLabel.Text = $"{Mb(value.BytesReceived)} of {Mb(value.TotalBytes)} MB";
                }
                else
                {
                    progressBar.Style = ProgressBarStyle.Marquee;
                    phaseLabel.Text = "Downloading update…";
                    detailLabel.Text = $"{Mb(value.BytesReceived)} MB received";
                }
                break;

            case UpdateChecker.UpdatePhase.Preparing:
                progressBar.Style = ProgressBarStyle.Continuous;
                progressBar.Value = 100;
                phaseLabel.Text = "Download complete — preparing to install…";
                detailLabel.Text = "";
                break;

            case UpdateChecker.UpdatePhase.Launching:
                progressBar.Style = ProgressBarStyle.Continuous;
                progressBar.Value = 100;
                phaseLabel.Text = "Installing update — the app will close and reopen…";
                detailLabel.Text = "";
                break;
        }
    }

    private static string Mb(long bytes) => bytes <= 0 ? "0.0" : (bytes / 1048576.0).ToString("0.0");
}
