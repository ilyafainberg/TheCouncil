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

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;

    private Panel headerPanel;
    private Label titleLabel;
    private Label subtitleLabel;
    private Label statusLabel;
    private Button settingsButton;
    private Button copyTranscriptButton;

    private Splitter rosterSplitter;
    private Panel rosterPanel;
    private Label rosterHeaderLabel;
    private FlowLayoutPanel rosterFlow;
    private Button addMemberButton;

    private Panel chatHost;
    private FlowLayoutPanel chatFlow;
    private FlowLayoutPanel humanTurnPanel;
    private Label humanPromptLabel;
    private ComboBox voteCombo;
    private Button submitVoteButton;
    private Button abstainButton;

    private Panel inputSeparator;
    private Panel inputBar;
    private TextBox inputTextBox;
    private FlowLayoutPanel inputRight;
    private Button conveneButton;
    private Button stopButton;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null) components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        headerPanel = new Panel();
        titleLabel = new Label();
        subtitleLabel = new Label();
        statusLabel = new Label();
        settingsButton = new Button();
        copyTranscriptButton = new Button();
        rosterSplitter = new Splitter();
        rosterPanel = new Panel();
        rosterHeaderLabel = new Label();
        rosterFlow = new FlowLayoutPanel();
        addMemberButton = new Button();
        chatHost = new Panel();
        chatFlow = new FlowLayoutPanel();
        humanTurnPanel = new FlowLayoutPanel();
        humanPromptLabel = new Label();
        voteCombo = new ComboBox();
        submitVoteButton = new Button();
        abstainButton = new Button();
        inputSeparator = new Panel();
        inputBar = new Panel();
        inputTextBox = new TextBox();
        inputRight = new FlowLayoutPanel();
        conveneButton = new Button();
        stopButton = new Button();

        headerPanel.SuspendLayout();
        rosterPanel.SuspendLayout();
        chatHost.SuspendLayout();
        humanTurnPanel.SuspendLayout();
        inputBar.SuspendLayout();
        inputRight.SuspendLayout();
        SuspendLayout();
        //
        // headerPanel
        //
        headerPanel.BackColor = Color.FromArgb(98, 100, 167);
        headerPanel.Controls.Add(titleLabel);
        headerPanel.Controls.Add(subtitleLabel);
        headerPanel.Controls.Add(statusLabel);
        headerPanel.Controls.Add(copyTranscriptButton);
        headerPanel.Controls.Add(settingsButton);
        headerPanel.Dock = DockStyle.Top;
        headerPanel.Height = 104;
        headerPanel.Name = "headerPanel";
        headerPanel.Resize += HeaderPanel_Resize;
        //
        // titleLabel
        //
        titleLabel.AutoSize = true;
        titleLabel.Font = new Font("Segoe UI Semibold", 16F);
        titleLabel.ForeColor = Color.White;
        titleLabel.Location = new Point(20, 14);
        titleLabel.Name = "titleLabel";
        titleLabel.Text = "🏛  The Council";
        //
        // subtitleLabel
        //
        subtitleLabel.AutoSize = true;
        subtitleLabel.Font = new Font("Segoe UI", 8.75F);
        subtitleLabel.ForeColor = Color.FromArgb(225, 225, 240);
        subtitleLabel.Location = new Point(22, 48);
        subtitleLabel.Name = "subtitleLabel";
        subtitleLabel.Text = "The council debates until unanimous — or until it stalls or hits the round cap";
        //
        // statusLabel
        //
        statusLabel.AutoSize = true;
        statusLabel.Font = new Font("Segoe UI Semibold", 9F);
        statusLabel.ForeColor = Color.FromArgb(232, 233, 246);
        statusLabel.Location = new Point(22, 76);
        statusLabel.Name = "statusLabel";
        statusLabel.Text = "Idle";
        //
        // copyTranscriptButton
        //
        copyTranscriptButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        copyTranscriptButton.BackColor = Color.FromArgb(118, 120, 185);
        copyTranscriptButton.FlatStyle = FlatStyle.Flat;
        copyTranscriptButton.Font = new Font("Segoe UI", 9F);
        copyTranscriptButton.ForeColor = Color.White;
        copyTranscriptButton.Height = 34;
        copyTranscriptButton.Width = 150;
        copyTranscriptButton.Location = new Point(884, 35);
        copyTranscriptButton.Name = "copyTranscriptButton";
        copyTranscriptButton.Text = "⧉  Copy transcript";
        copyTranscriptButton.Click += CopyTranscriptButton_Click;
        //
        // settingsButton
        //
        settingsButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        settingsButton.BackColor = Color.FromArgb(118, 120, 185);
        settingsButton.FlatStyle = FlatStyle.Flat;
        settingsButton.Font = new Font("Segoe UI", 9F);
        settingsButton.ForeColor = Color.White;
        settingsButton.Height = 34;
        settingsButton.Width = 110;
        settingsButton.Location = new Point(1044, 35);
        settingsButton.Name = "settingsButton";
        settingsButton.Text = "⚙  Settings";
        settingsButton.Click += SettingsButton_Click;
        //
        // rosterSplitter
        //
        rosterSplitter.BackColor = Color.FromArgb(225, 225, 230);
        rosterSplitter.Dock = DockStyle.Left;
        rosterSplitter.Width = 6;
        rosterSplitter.MinExtra = 360;
        rosterSplitter.MinSize = 170;
        rosterSplitter.Cursor = Cursors.SizeWE;
        rosterSplitter.Name = "rosterSplitter";
        rosterSplitter.Paint += RosterSplitter_Paint;
        //
        // rosterPanel
        //
        rosterPanel.BackColor = Color.White;
        rosterPanel.Controls.Add(rosterFlow);
        rosterPanel.Controls.Add(addMemberButton);
        rosterPanel.Controls.Add(rosterHeaderLabel);
        rosterPanel.Dock = DockStyle.Left;
        rosterPanel.Width = 240;
        rosterPanel.Name = "rosterPanel";
        //
        // rosterHeaderLabel
        //
        rosterHeaderLabel.Dock = DockStyle.Top;
        rosterHeaderLabel.Font = new Font("Segoe UI Semibold", 9F);
        rosterHeaderLabel.ForeColor = Color.FromArgb(96, 96, 104);
        rosterHeaderLabel.Height = 38;
        rosterHeaderLabel.Padding = new Padding(16, 0, 0, 0);
        rosterHeaderLabel.TextAlign = ContentAlignment.MiddleLeft;
        rosterHeaderLabel.Name = "rosterHeaderLabel";
        rosterHeaderLabel.Text = "COUNCIL MEMBERS";
        //
        // rosterFlow
        //
        rosterFlow.AutoScroll = true;
        rosterFlow.BackColor = Color.White;
        rosterFlow.Dock = DockStyle.Fill;
        rosterFlow.FlowDirection = FlowDirection.TopDown;
        rosterFlow.Padding = new Padding(8, 4, 8, 4);
        rosterFlow.WrapContents = false;
        rosterFlow.Name = "rosterFlow";
        //
        // addMemberButton
        //
        addMemberButton.BackColor = Color.FromArgb(237, 237, 245);
        addMemberButton.Dock = DockStyle.Bottom;
        addMemberButton.FlatStyle = FlatStyle.Flat;
        addMemberButton.Font = new Font("Segoe UI Semibold", 9.5F);
        addMemberButton.ForeColor = Color.FromArgb(98, 100, 167);
        addMemberButton.Height = 42;
        addMemberButton.Name = "addMemberButton";
        addMemberButton.Text = "＋  Add member";
        addMemberButton.Click += AddMemberButton_Click;
        //
        // chatHost
        //
        chatHost.BackColor = Color.FromArgb(243, 242, 245);
        chatHost.Controls.Add(chatFlow);
        chatHost.Controls.Add(humanTurnPanel);
        chatHost.Dock = DockStyle.Fill;
        chatHost.Name = "chatHost";
        //
        // chatFlow
        //
        chatFlow.AutoScroll = true;
        chatFlow.BackColor = Color.FromArgb(243, 242, 245);
        chatFlow.Dock = DockStyle.Fill;
        chatFlow.FlowDirection = FlowDirection.TopDown;
        chatFlow.Padding = new Padding(16, 12, 16, 12);
        chatFlow.WrapContents = false;
        chatFlow.Name = "chatFlow";
        chatFlow.Resize += ChatFlow_Resize;
        //
        // humanTurnPanel
        //
        humanTurnPanel.BackColor = Color.FromArgb(233, 236, 250);
        humanTurnPanel.Controls.Add(humanPromptLabel);
        humanTurnPanel.Controls.Add(voteCombo);
        humanTurnPanel.Controls.Add(submitVoteButton);
        humanTurnPanel.Controls.Add(abstainButton);
        humanTurnPanel.Dock = DockStyle.Bottom;
        humanTurnPanel.Height = 52;
        humanTurnPanel.Padding = new Padding(14, 9, 14, 9);
        humanTurnPanel.WrapContents = false;
        humanTurnPanel.Visible = false;
        humanTurnPanel.Name = "humanTurnPanel";
        //
        // humanPromptLabel
        //
        humanPromptLabel.AutoSize = true;
        humanPromptLabel.Font = new Font("Segoe UI Semibold", 9.5F);
        humanPromptLabel.ForeColor = Color.FromArgb(60, 60, 90);
        humanPromptLabel.Margin = new Padding(0, 8, 12, 0);
        humanPromptLabel.Name = "humanPromptLabel";
        humanPromptLabel.Text = "Your turn:";
        //
        // voteCombo
        //
        voteCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        voteCombo.Width = 240;
        voteCombo.Margin = new Padding(0, 5, 8, 0);
        voteCombo.Name = "voteCombo";
        voteCombo.SelectedIndexChanged += VoteCombo_SelectedIndexChanged;
        //
        // submitVoteButton
        //
        submitVoteButton.BackColor = Color.FromArgb(98, 100, 167);
        submitVoteButton.Enabled = false;
        submitVoteButton.FlatStyle = FlatStyle.Flat;
        submitVoteButton.ForeColor = Color.White;
        submitVoteButton.Height = 30;
        submitVoteButton.Width = 110;
        submitVoteButton.Margin = new Padding(0, 3, 6, 0);
        submitVoteButton.Name = "submitVoteButton";
        submitVoteButton.Text = "Vote";
        submitVoteButton.Click += SubmitVoteButton_Click;
        //
        // abstainButton
        //
        abstainButton.BackColor = Color.FromArgb(237, 237, 245);
        abstainButton.FlatStyle = FlatStyle.Flat;
        abstainButton.ForeColor = Color.FromArgb(60, 60, 70);
        abstainButton.Height = 30;
        abstainButton.Width = 90;
        abstainButton.Margin = new Padding(0, 3, 0, 0);
        abstainButton.Name = "abstainButton";
        abstainButton.Text = "Abstain";
        abstainButton.Click += AbstainButton_Click;
        //
        // inputSeparator
        //
        inputSeparator.BackColor = Color.FromArgb(225, 225, 230);
        inputSeparator.Dock = DockStyle.Bottom;
        inputSeparator.Height = 1;
        inputSeparator.Name = "inputSeparator";
        //
        // inputBar
        //
        inputBar.BackColor = Color.White;
        inputBar.Controls.Add(inputTextBox);
        inputBar.Controls.Add(inputRight);
        inputBar.Dock = DockStyle.Bottom;
        inputBar.Height = 100;
        inputBar.Padding = new Padding(16, 14, 16, 14);
        inputBar.Name = "inputBar";
        //
        // inputTextBox
        //
        inputTextBox.BorderStyle = BorderStyle.FixedSingle;
        inputTextBox.Dock = DockStyle.Fill;
        inputTextBox.Font = new Font("Segoe UI", 10F);
        inputTextBox.Multiline = true;
        inputTextBox.Name = "inputTextBox";
        inputTextBox.PlaceholderText = "Pose a problem for the council, then press Enter or Convene  (Ctrl+Enter for a new line)";
        inputTextBox.KeyDown += InputTextBox_KeyDown;
        //
        // inputRight
        //
        inputRight.Controls.Add(conveneButton);
        inputRight.Controls.Add(stopButton);
        inputRight.Dock = DockStyle.Right;
        inputRight.FlowDirection = FlowDirection.TopDown;
        inputRight.Width = 130;
        inputRight.WrapContents = false;
        inputRight.Name = "inputRight";
        //
        // conveneButton
        //
        conveneButton.BackColor = Color.FromArgb(98, 100, 167);
        conveneButton.FlatStyle = FlatStyle.Flat;
        conveneButton.Font = new Font("Segoe UI Semibold", 9.5F);
        conveneButton.ForeColor = Color.White;
        conveneButton.Height = 34;
        conveneButton.Width = 124;
        conveneButton.Margin = new Padding(0, 0, 0, 0);
        conveneButton.Name = "conveneButton";
        conveneButton.Text = "Convene  ▶";
        conveneButton.Click += ConveneButton_Click;
        //
        // stopButton
        //
        stopButton.BackColor = Color.FromArgb(237, 237, 245);
        stopButton.Enabled = false;
        stopButton.FlatStyle = FlatStyle.Flat;
        stopButton.Font = new Font("Segoe UI", 9F);
        stopButton.ForeColor = Color.FromArgb(60, 60, 70);
        stopButton.Height = 30;
        stopButton.Width = 124;
        stopButton.Margin = new Padding(0, 6, 0, 0);
        stopButton.Name = "stopButton";
        stopButton.Text = "Stop";
        stopButton.Click += StopButton_Click;
        //
        // MainForm
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.FromArgb(243, 242, 245);
        ClientSize = new Size(1180, 800);
        Controls.Add(chatHost);
        Controls.Add(inputSeparator);
        Controls.Add(inputBar);
        Controls.Add(rosterSplitter);
        Controls.Add(rosterPanel);
        Controls.Add(headerPanel);
        Font = new Font("Segoe UI", 9.5F);
        MinimumSize = new Size(980, 640);
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "The Council";

        settingsButton.FlatAppearance.BorderSize = 0;
        copyTranscriptButton.FlatAppearance.BorderSize = 0;
        addMemberButton.FlatAppearance.BorderSize = 0;
        conveneButton.FlatAppearance.BorderSize = 0;
        stopButton.FlatAppearance.BorderSize = 0;
        submitVoteButton.FlatAppearance.BorderSize = 0;
        abstainButton.FlatAppearance.BorderSize = 0;

        headerPanel.ResumeLayout(false);
        headerPanel.PerformLayout();
        rosterPanel.ResumeLayout(false);
        chatHost.ResumeLayout(false);
        humanTurnPanel.ResumeLayout(false);
        humanTurnPanel.PerformLayout();
        inputBar.ResumeLayout(false);
        inputRight.ResumeLayout(false);
        ResumeLayout(false);
    }
}
