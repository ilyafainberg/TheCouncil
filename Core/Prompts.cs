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
using System.Text;
using TheCouncil.Models;

namespace TheCouncil.Core;

public static class Prompts
{
    public static string SystemFor(Participant p, IEnumerable<Participant> council, string phase)
    {
        var others = string.Join(", ", council.Where(x => x.Id != p.Id).Select(x => x.DisplayName));
        var persona = string.IsNullOrWhiteSpace(p.Persona) ? "" : $" Your perspective/expertise: {p.Persona}.";

        return
$@"You are {p.DisplayName}, a member of ""The Council"" — a panel that debates a problem and must converge on ONE best solution.{persona}
The other members are: {others}.
You receive the FULL running transcript every turn. Read it, build on good ideas, and disagree when justified.

You must reply with a SINGLE action as STRICT JSON only — no markdown, no code fences, no prose outside the JSON.
Schema:
{{
  ""action"": ""propose"" | ""clarify"" | ""vote"",
  ""title"": ""<=6 word name of your proposal (only when action=propose)"",
  ""content"": ""your message to the council (1-4 short sentences)"",
  ""voteFor"": ""the PROPOSAL id you back, e.g. P2 (only when action=vote)"",
  ""reasoning"": ""why you propose/clarify/vote this (1-2 sentences)""
}}

Guidance for this phase: {phase}
Keep it tight and substantive. Never restate the whole transcript. Output JSON only.";
    }

    public const string PhaseDebate =
        "It is an open debate round. Choose the single most useful action: propose a NEW or REFINED solution, " +
        "ask a clarifying question if something is genuinely blocking, or vote for an existing proposal you find strongest. " +
        "Prefer proposing/refining early; vote once a strong option exists.";

    /// <summary>
    /// Builds round-aware guidance that pushes the council toward UNANIMOUS agreement: convergence
    /// pressure grows with the round number and once proposals (and a leader) exist.
    /// </summary>
    public static string DebatePhase(int round, int maxRounds, bool hasProposals, string? leaderId)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append($"This is round {round} of at most {maxRounds}. The council keeps debating until EVERY AI member's ");
        sb.Append("latest action is a vote for the SAME proposal — that unanimous agreement is the only way to finish. ");

        if (!hasProposals)
        {
            sb.Append("No proposals exist yet — propose a concrete solution or ask a genuinely blocking question.");
            return sb.ToString();
        }

        sb.Append("Proposals already exist, so STRONGLY prefer voting for the strongest option, or refining the leading ");
        sb.Append("proposal, over spawning a new near-duplicate. Only create a new proposal if every existing one is ");
        sb.Append("genuinely inadequate. ");
        if (!string.IsNullOrEmpty(leaderId))
            sb.Append($"The current front-runner is {leaderId}; back it unless you have a specific, substantive objection. ");
        if (round >= maxRounds - 2)
            sb.Append("Time is nearly up — converge now: vote, do not open new threads.");
        return sb.ToString();
    }

    public static string SynthesisPrompt(string problem, string winnerTitle, string winnerBody, string transcript)
    {
        return
$@"You are the Council's scribe. The council debated this problem:
---
{problem}
---
The winning proposal was ""{winnerTitle}"":
{winnerBody}

Full transcript:
{transcript}

Write the FINAL AGREED SOLUTION the whole council endorses. Use this plain-text structure (no JSON):
1. One-sentence summary of the agreed solution.
2. ""How it works"" — 3-6 concise bullet points.
3. ""Why the council agreed this is best"" — 2-4 bullets referencing the debate (trade-offs beaten, concerns addressed).
Be decisive and concrete.";
    }
}
