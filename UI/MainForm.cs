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
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using TheCouncil.Core;
using TheCouncil.Models;
using TheCouncil.Updating;

namespace TheCouncil.UI;

public partial class MainForm : Form
{
    private sealed class ComboVote
    {
        public string Id = "";
        public string Text = "";
        public override string ToString() => Text;
    }

    private CouncilSettings settings = CouncilSettings.Load();
    private readonly List<Participant> council = new();
    private Participant human = null!;

    private CouncilOrchestrator? orchestrator;
    private CancellationTokenSource? cts;
    private bool running;
    private bool awaitingHuman;

    public MainForm()
    {
        InitializeComponent();
        TryLoadIcon();
        SetupTooltips();
        rosterFlow.Resize += RosterFlow_Resize;
        InitCouncil();
        RefreshRoster();
        ShowWelcome();
        Shown += MainForm_Shown;
    }

    // ---------------- Auto-update ----------------
    private bool updateChecked;

    private async void MainForm_Shown(object? sender, EventArgs e)
    {
        if (updateChecked) return;
        updateChecked = true;
        await CheckForUpdatesAsync(silentIfNone: true);
    }

    /// <summary>
    /// Checks GitHub Releases for a newer version. On success, offers to download and
    /// apply it (the app exits and the helper relaunches). Never throws — a flaky
    /// network or API hiccup just results in "no update".
    /// </summary>
    private async Task CheckForUpdatesAsync(bool silentIfNone)
    {
        try
        {
            var updater = new UpdateChecker();
            var result = await updater.CheckAsync();

            if (!result.UpdateAvailable)
            {
                if (!silentIfNone)
                    MessageBox.Show(this, $"You're up to date (v{result.CurrentVersion}).",
                        "The Council", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var notes = string.IsNullOrWhiteSpace(result.ReleaseNotes)
                ? "" : $"\n\nWhat's new:\n{Trim(result.ReleaseNotes, 600)}";
            var prompt = $"Version {result.LatestVersion} is available "
                       + $"(you have {result.CurrentVersion}).{notes}\n\nUpdate now? "
                       + "The app will close, update, and reopen.";

            if (MessageBox.Show(this, prompt, "The Council — update available",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Information) != DialogResult.Yes)
                return;

            statusLabel.Text = "Downloading update…";
            await updater.DownloadAndApplyAsync(result, requestShutdown: Application.Exit);
        }
        catch
        {
            if (!silentIfNone)
                MessageBox.Show(this, "Could not check for updates right now.",
                    "The Council", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private static string Trim(string s, int max) => s.Length <= max ? s : s[..max] + "…";

    private void TryLoadIcon()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "app.ico");
            if (File.Exists(path)) Icon = new Icon(path);
        }
        catch { /* icon optional */ }
    }

    private void SetupTooltips()
    {
        var tip = new ToolTip { AutoPopDelay = 8000, InitialDelay = 400 };
        tip.SetToolTip(abstainButton, "Pass this round — back no proposal.");
        tip.SetToolTip(submitVoteButton, "Vote for the selected proposal (or type a solution to propose instead).");
        tip.SetToolTip(copyTranscriptButton, "Copy the whole conversation to the clipboard.");
    }

    private void CopyTranscriptButton_Click(object? sender, EventArgs e) => CopyTranscript();

    private void CopyTranscript()
    {
        var sb = new System.Text.StringBuilder();
        foreach (Control c in chatFlow.Controls)
            if (c is ChatBubble b)
                sb.AppendLine(b.PlainText).AppendLine();
        var text = sb.ToString().TrimEnd();
        if (string.IsNullOrEmpty(text)) return;
        try { Clipboard.SetText(text); } catch { /* clipboard busy */ }
        statusLabel.Text = "Transcript copied to clipboard.";
    }

    private void RosterSplitter_Paint(object? sender, PaintEventArgs e)
    {
        // subtle vertical grip of three dots in the centre
        int cx = rosterSplitter.Width / 2;
        int cy = rosterSplitter.Height / 2;
        using var b = new SolidBrush(Color.FromArgb(160, 160, 168));
        foreach (var dy in new[] { -8, 0, 8 })
            e.Graphics.FillEllipse(b, cx - 1, cy + dy - 1, 2, 2);
    }

    // ---------------- Header ----------------
    private void HeaderPanel_Resize(object? sender, EventArgs e)
    {
        settingsButton.Location = new Point(headerPanel.Width - settingsButton.Width - 16, 35);
        copyTranscriptButton.Location = new Point(settingsButton.Left - copyTranscriptButton.Width - 10, 35);
    }

    private void SettingsButton_Click(object? sender, EventArgs e)
    {
        using var f = new SettingsForm(settings);
        if (f.ShowDialog(this) == DialogResult.OK)
        {
            settings = CouncilSettings.Load();
            // keep the in-memory roster, just persist it back and refresh profile-name labels
            PersistCouncil();
            RefreshRoster();
        }
    }

    // ---------------- Roster ----------------
    private void AddMemberButton_Click(object? sender, EventArgs e)
    {
        var fresh = new Participant();
        using var f = new ParticipantEditForm(fresh, settings);
        if (f.ShowDialog(this) == DialogResult.OK)
        {
            council.Add(f.Participant);
            PersistCouncil();
            RefreshRoster();
        }
    }

    private void RefreshRoster()
    {
        rosterFlow.SuspendLayout();
        rosterFlow.Controls.Clear();
        foreach (var p in council)
            rosterFlow.Controls.Add(MakeMemberCard(p));
        rosterFlow.ResumeLayout();
        ResizeMemberCards();
    }

    private int CardWidth() =>
        Math.Max(150, rosterFlow.ClientSize.Width - rosterFlow.Padding.Horizontal - 8);

    private void ResizeMemberCards()
    {
        int w = CardWidth();
        foreach (Control c in rosterFlow.Controls)
        {
            c.Width = w;
            if (c.Controls.Count > 0 && c.Controls[0] is Button rm)
                rm.Location = new Point(w - rm.Width - 12, 14);
        }
    }

    private void RosterFlow_Resize(object? sender, EventArgs e) => ResizeMemberCards();

    private Control MakeMemberCard(Participant p)
    {
        var card = new Panel
        {
            Width = CardWidth(),
            Height = 54,
            Margin = new Padding(4),
            BackColor = Color.FromArgb(249, 249, 251),
            Cursor = Cursors.Hand,
            Tag = p
        };
        card.Paint += MemberCard_Paint;

        var remove = new Button
        {
            Text = "✕",
            Width = 26,
            Height = 26,
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.FromArgb(150, 150, 156),
            BackColor = card.BackColor,
            Location = new Point(card.Width - 38, 14),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Cursor = Cursors.Hand,
            TabStop = false,
            Tag = p
        };
        remove.FlatAppearance.BorderSize = 0;
        remove.Click += RemoveMember_Click;

        card.DoubleClick += MemberCard_DoubleClick;
        card.Controls.Add(remove);
        return card;
    }

    private void MemberCard_Paint(object? sender, PaintEventArgs e)
    {
        if (sender is not Panel card || card.Tag is not Participant p) return;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var b = new SolidBrush(p.Color);
        e.Graphics.FillEllipse(b, new Rectangle(10, 9, 36, 36));
        TextRenderer.DrawText(e.Graphics, p.IsHuman ? "🙂" : p.Initials,
            new Font("Segoe UI Semibold", p.IsHuman ? 12f : 11f), new Rectangle(10, 9, 36, 36),
            Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        TextRenderer.DrawText(e.Graphics, p.DisplayName, new Font("Segoe UI Semibold", 9.75f),
            new Point(56, 8), Color.FromArgb(37, 36, 35), TextFormatFlags.NoPadding);
        string subtitle = p.IsHuman ? "Human" : (settings.Find(p.ProfileId)?.Name ?? "— no profile —");
        TextRenderer.DrawText(e.Graphics, subtitle,
            new Font("Segoe UI", 8.25f), new Point(56, 28), Color.FromArgb(120, 120, 128), TextFormatFlags.NoPadding);
    }

    private void RemoveMember_Click(object? sender, EventArgs e)
    {
        if (running) return;
        if (sender is Button b && b.Tag is Participant p)
        {
            council.Remove(p);
            if (p == human)
                human = council.FirstOrDefault(x => x.IsHuman)!;
            PersistCouncil();
            RefreshRoster();
        }
    }

    private void MemberCard_DoubleClick(object? sender, EventArgs e)
    {
        if (running) return;
        if (sender is Panel card && card.Tag is Participant p)
        {
            using var f = new ParticipantEditForm(p, settings);
            if (f.ShowDialog(this) == DialogResult.OK)
            {
                PersistCouncil();
                RefreshRoster();
            }
        }
    }

    // ---------------- Chat ----------------
    private int BubbleWidth() =>
        Math.Max(260, chatFlow.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 24);

    private void ChatFlow_Resize(object? sender, EventArgs e)
    {
        chatFlow.SuspendLayout();
        int w = BubbleWidth();
        foreach (Control c in chatFlow.Controls)
            if (c is ChatBubble b) b.SetWidth(w);
        chatFlow.ResumeLayout();
    }

    private void AddBubble(ChatMessage m)
    {
        var bubble = new ChatBubble(m);
        bubble.SetWidth(BubbleWidth());
        chatFlow.Controls.Add(bubble);
        chatFlow.ScrollControlIntoView(bubble);
    }

    // ---------------- Input ----------------
    private void InputTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Enter) return;

        // Ctrl+Enter inserts a newline; plain Enter sends.
        if (e.Control)
        {
            e.SuppressKeyPress = true;
            int pos = inputTextBox.SelectionStart;
            inputTextBox.Text = inputTextBox.Text.Insert(pos, Environment.NewLine);
            inputTextBox.SelectionStart = pos + Environment.NewLine.Length;
            return;
        }

        e.SuppressKeyPress = true;
        if (awaitingHuman)
        {
            // Typed text proposes; empty submits a vote (if one is selectable) or abstains.
            if (inputTextBox.Text.Trim().Length > 0) SubmitHuman(BuildProposal());
            else if (submitVoteButton.Enabled) SubmitHuman(BuildVote());
            else SubmitHuman(BuildAbstain());
        }
        else if (!running)
        {
            Convene();
        }
    }

    private void ConveneButton_Click(object? sender, EventArgs e) => Convene();

    private void StopButton_Click(object? sender, EventArgs e) => cts?.Cancel();

    // ---------------- Human gate ----------------
    private void OnHumanTurnRequested(int round, IReadOnlyList<ProposalInfo> proposalList)
    {
        awaitingHuman = true;
        voteCombo.Items.Clear();
        foreach (var p in proposalList)
            voteCombo.Items.Add(new ComboVote { Id = p.Id, Text = $"{p.Id} — {p.Title}" });

        bool hasProposals = voteCombo.Items.Count > 0;
        voteCombo.Enabled = hasProposals;
        if (hasProposals) voteCombo.SelectedIndex = 0;
        submitVoteButton.Enabled = hasProposals;

        humanPromptLabel.Text = hasProposals
            ? $"Round {round} — type a solution to propose, vote for one, or abstain:"
            : $"Round {round} — type a solution to propose, or abstain:";

        humanTurnPanel.Visible = true;
        inputTextBox.PlaceholderText = "Type a solution to propose it… (Enter to send, Ctrl+Enter for a new line)";
        inputTextBox.Focus();
    }

    private void OnHumanTurnEnded()
    {
        awaitingHuman = false;
        humanTurnPanel.Visible = false;
    }

    private void VoteCombo_SelectedIndexChanged(object? sender, EventArgs e)
    {
        submitVoteButton.Enabled = awaitingHuman && voteCombo.SelectedItem is ComboVote;
    }

    private void SubmitVoteButton_Click(object? sender, EventArgs e)
    {
        // If the human typed something, treat it as a proposal rather than a vote.
        if (inputTextBox.Text.Trim().Length > 0) SubmitHuman(BuildProposal());
        else SubmitHuman(BuildVote());
    }

    private void AbstainButton_Click(object? sender, EventArgs e) => SubmitHuman(BuildAbstain());

    private HumanRoundInput BuildProposal() => new()
    {
        ProposalText = inputTextBox.Text.Trim()
    };

    private HumanRoundInput BuildVote() => new()
    {
        VoteProposalId = (voteCombo.SelectedItem as ComboVote)?.Id
    };

    private HumanRoundInput BuildAbstain() => new()
    {
        Abstained = true
    };

    private void SubmitHuman(HumanRoundInput input)
    {
        if (!awaitingHuman || orchestrator == null) return;
        awaitingHuman = false;
        humanTurnPanel.Visible = false;
        inputTextBox.Clear();
        statusLabel.Text = "Recording your turn…";
        orchestrator.SubmitHumanRound(input);
    }

    // ---------------- Council control ----------------
    private static readonly Color[] SeedColors =
    {
        Color.FromArgb(16, 124, 16), Color.FromArgb(202, 80, 16), Color.FromArgb(140, 88, 184),
        Color.FromArgb(0, 153, 168), Color.FromArgb(196, 49, 75), Color.FromArgb(98, 100, 167),
    };

    private void InitCouncil()
    {
        council.Clear();
        if (settings.Council.Count > 0)
        {
            council.AddRange(settings.Council);
            human = council.FirstOrDefault(p => p.IsHuman)
                    ?? new Participant { DisplayName = "You", IsHuman = true, Color = Color.FromArgb(0, 120, 212) };
            if (!council.Contains(human)) council.Insert(0, human);
            HealColors();
            return;
        }

        human = new Participant { DisplayName = "You", IsHuman = true, Color = Color.FromArgb(0, 120, 212) };
        council.Add(human);
        int i = 0;
        foreach (var profile in settings.Profiles)
        {
            council.Add(new Participant
            {
                DisplayName = profile.Name,
                ProfileId = profile.Id,
                Color = SeedColors[i % SeedColors.Length]
            });
            i++;
        }
        PersistCouncil();
    }

    /// <summary>Repairs participants whose stored colour was lost (transparent) by an earlier serialization bug.</summary>
    private void HealColors()
    {
        bool changed = false;
        int i = 0;
        foreach (var p in council)
        {
            if (p.IsHuman)
            {
                if (Color.FromArgb(p.ColorArgb).A == 0) { p.Color = Color.FromArgb(0, 120, 212); changed = true; }
                continue;
            }
            if (Color.FromArgb(p.ColorArgb).A == 0)
            {
                p.Color = SeedColors[i % SeedColors.Length];
                changed = true;
            }
            i++;
        }
        if (changed) PersistCouncil();
    }

    private void PersistCouncil()
    {
        settings.Council = new List<Participant>(council);
        settings.Save();
    }

    private void ShowWelcome()
    {
        AddBubble(new ChatMessage
        {
            Action = TurnAction.System,
            Content = "Welcome to The Council. Add or remove members on the left, type a problem below, then click "
                    + "Convene. The council debates until the AI members unanimously agree (or it stalls / hits "
                    + $"{settings.MaxRounds} rounds). Each round you can add a message and must vote for a "
                    + "proposal or abstain. Set your API keys in Settings first."
        });
    }

    private async void Convene()
    {
        if (running) return;
        var problem = inputTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(problem))
        {
            MessageBox.Show(this, "Type a problem for the council to debate.", "The Council",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (council.Count(p => !p.IsHuman) == 0)
        {
            MessageBox.Show(this, "Add at least one AI member.", "The Council",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        human = council.FirstOrDefault(p => p.IsHuman)
                ?? new Participant { DisplayName = "You", IsHuman = true, Color = Color.FromArgb(0, 120, 212) };

        chatFlow.Controls.Clear();
        humanTurnPanel.Visible = false;

        running = true;
        SetRunningUi(true);
        inputTextBox.Clear();

        cts = new CancellationTokenSource();
        orchestrator = new CouncilOrchestrator(settings, council);
        orchestrator.MessageAdded += AddBubble;
        orchestrator.StatusChanged += s => statusLabel.Text = s;
        orchestrator.HumanTurnRequested += OnHumanTurnRequested;
        orchestrator.HumanTurnEnded += OnHumanTurnEnded;
        orchestrator.Finished += OnCouncilFinished;

        await orchestrator.RunAsync(human, problem, cts.Token);
    }

    private void OnCouncilFinished()
    {
        running = false;
        awaitingHuman = false;
        humanTurnPanel.Visible = false;
        SetRunningUi(false);
    }

    private void SetRunningUi(bool isRunning)
    {
        conveneButton.Enabled = !isRunning;
        stopButton.Enabled = isRunning;
        inputTextBox.PlaceholderText = isRunning
            ? "The council is in session… you'll be prompted to vote each round."
            : "Pose a problem for the council, then press Enter or Convene  (Ctrl+Enter for a new line)";
    }
}
