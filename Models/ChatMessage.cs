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

public class ChatMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public Participant? Author { get; set; }
    public string AuthorName => Author?.DisplayName ?? "The Council";
    public TurnAction Action { get; set; } = TurnAction.System;

    /// <summary>The visible body of the message.</summary>
    public string Content { get; set; } = "";

    /// <summary>Short title for a proposal (e.g. "Cache-first sync").</summary>
    public string? Title { get; set; }

    /// <summary>Stable proposal id like P1, P2 (set for Propose messages).</summary>
    public string? ProposalId { get; set; }

    /// <summary>For votes: the proposal id the author is backing.</summary>
    public string? VoteForProposalId { get; set; }

    /// <summary>For votes/proposals: the author's reasoning.</summary>
    public string? Reasoning { get; set; }

    public int Round { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;

    /// <summary>How this single turn is rendered in the running transcript handed to every model.</summary>
    public string ToTranscriptLine()
    {
        return Action switch
        {
            TurnAction.Problem => $"[PROBLEM posed by {AuthorName}]: {Content}",
            TurnAction.Propose => $"[{AuthorName} — PROPOSAL {ProposalId}: \"{Title}\"]: {Content}"
                                  + (string.IsNullOrWhiteSpace(Reasoning) ? "" : $" (Rationale: {Reasoning})"),
            TurnAction.Clarify => $"[{AuthorName} — CLARIFYING QUESTION]: {Content}",
            TurnAction.Vote => VoteForProposalId == null
                ? $"[{AuthorName} — ABSTAINS]: {Reasoning ?? Content}"
                : $"[{AuthorName} — VOTES for {VoteForProposalId}]: {Reasoning ?? Content}",
            TurnAction.Consensus => $"[{AuthorName} — FINAL VOTE for {VoteForProposalId}]: {Reasoning ?? Content}",
            TurnAction.HumanChat => $"[{AuthorName} (human) says]: {Content}",
            TurnAction.Final => $"[AGREED SOLUTION]: {Content}",
            _ => $"[{AuthorName}]: {Content}"
        };
    }
}
