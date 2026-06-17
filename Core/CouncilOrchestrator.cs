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
using System.Text.Json;
using System.Text.RegularExpressions;
using TheCouncil.Models;
using TheCouncil.Providers;

namespace TheCouncil.Core;

/// <summary>A snapshot of a proposal the human can vote for during their gated turn.</summary>
public record ProposalInfo(string Id, string Title);

/// <summary>
/// The human's single action for a round: either propose a solution (non-empty <see cref="ProposalText"/>),
/// vote for an existing proposal (<see cref="VoteProposalId"/>), or abstain.
/// </summary>
public class HumanRoundInput
{
    public string? ProposalText { get; set; }
    public string? VoteProposalId { get; set; }
    public bool Abstained { get; set; }
}

/// <summary>
/// Runs the council debate. AIs debate in rounds (capped at <see cref="maxRounds"/>) until either
/// the AI members are unanimous, the debate stalls, or the cap is hit. When a human is a member the
/// orchestrator pauses each round for the human to add a message and cast a vote (or abstain / skip).
/// Unanimity is scoped to AI members; the human votes but never blocks consensus.
/// </summary>
public class CouncilOrchestrator
{
    public const int DefaultMaxRounds = 10;

    private readonly int maxRounds;

    private readonly CouncilSettings settings;
    private readonly List<Participant> council;
    private readonly List<ChatMessage> transcript = new();
    private readonly Dictionary<string, ChatMessage> proposals = new(StringComparer.OrdinalIgnoreCase);
    private int proposalCounter;
    private readonly SynchronizationContext ui;

    private TaskCompletionSource<HumanRoundInput>? humanTurn;

    public event Action<ChatMessage>? MessageAdded;
    public event Action<string>? StatusChanged;
    public event Action<int, IReadOnlyList<ProposalInfo>>? HumanTurnRequested;
    public event Action? HumanTurnEnded;
    public event Action? Finished;

    public IReadOnlyList<ChatMessage> Transcript => transcript;

    public CouncilOrchestrator(CouncilSettings settings, IEnumerable<Participant> council)
    {
        this.settings = settings;
        this.council = council.ToList();
        maxRounds = Math.Clamp(settings.MaxRounds <= 0 ? DefaultMaxRounds : settings.MaxRounds, 1, 50);
        ui = SynchronizationContext.Current ?? new SynchronizationContext();
    }

    /// <summary>Called by the UI to complete the human's gated turn.</summary>
    public void SubmitHumanRound(HumanRoundInput input) => humanTurn?.TrySetResult(input);

    public async Task RunAsync(Participant human, string problem, CancellationToken ct)
    {
        try
        {
            Add(new ChatMessage { Author = human, Action = TurnAction.Problem, Content = problem, Round = 0 });

            var ais = council.Where(p => !p.IsHuman).ToList();
            if (ais.Count == 0)
            {
                Status("Add at least one AI participant to convene the council.");
                Finished?.Invoke();
                return;
            }
            bool humanPresent = council.Any(p => p.IsHuman);

            bool unanimous = false;
            string winnerId = "";
            string lastSignature = "";
            int stallRepeat = 0;
            int round = 0;

            while (round < maxRounds && !ct.IsCancellationRequested)
            {
                round++;
                Status($"Round {round} — open debate (max {maxRounds})");

                foreach (var p in ais)
                {
                    ct.ThrowIfCancellationRequested();
                    await TakeTurn(p, round, ais, ct);
                    await Task.Delay(300, ct);
                }

                if (humanPresent)
                    await HumanGate(human, round, ct);

                if (AisUnanimous(ais, out winnerId))
                {
                    unanimous = true;
                    break;
                }

                // Stall detection: stop if proposals and AI votes haven't changed for two consecutive rounds.
                var signature = RoundSignature(ais);
                stallRepeat = signature == lastSignature ? stallRepeat + 1 : 0;
                lastSignature = signature;
                if (stallRepeat >= 2)
                    break;
            }

            if (proposals.Count == 0)
            {
                Add(new ChatMessage { Action = TurnAction.System, Round = -1,
                    Content = "No proposals were made — nothing to ratify." });
                Status("Council adjourned without a proposal.");
                Finished?.Invoke();
                return;
            }

            string reason = unanimous ? "unanimous agreement"
                : round >= maxRounds ? $"reached the {maxRounds}-round limit"
                : "the debate stalled";
            await Conclude(ais, problem, unanimous, winnerId, reason, ct);
            Finished?.Invoke();
        }
        catch (OperationCanceledException)
        {
            Status("Council adjourned (stopped).");
            Finished?.Invoke();
        }
        catch (Exception ex)
        {
            Status($"Error: {ex.Message}");
            Finished?.Invoke();
        }
    }

    // ---- human gating ----

    private async Task HumanGate(Participant human, int round, CancellationToken ct)
    {
        humanTurn = new TaskCompletionSource<HumanRoundInput>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var reg = ct.Register(() => humanTurn.TrySetCanceled(ct));

        Status("Your turn — propose a solution, vote for a proposal, or abstain.");
        var snapshot = proposals.Select(kv => new ProposalInfo(kv.Key, kv.Value.Title ?? kv.Key)).ToList();
        ui.Post(_ => HumanTurnRequested?.Invoke(round, snapshot), null);

        HumanRoundInput input;
        try
        {
            input = await humanTurn.Task;
        }
        finally
        {
            ui.Post(_ => HumanTurnEnded?.Invoke(), null);
        }

        // A typed message is a PROPOSAL authored by the human.
        if (!string.IsNullOrWhiteSpace(input.ProposalText))
        {
            var text = input.ProposalText!.Trim();
            var prop = new ChatMessage
            {
                Author = human,
                Action = TurnAction.Propose,
                Round = round,
                ProposalId = $"P{++proposalCounter}",
                Title = DeriveTitle(text),
                Content = text
            };
            proposals[prop.ProposalId] = prop;
            Add(prop);
            return;
        }

        // Otherwise the human either votes for an existing proposal or abstains.
        var vote = new ChatMessage { Author = human, Action = TurnAction.Vote, Round = round };
        bool abstain = input.Abstained || string.IsNullOrWhiteSpace(input.VoteProposalId);
        if (abstain)
        {
            vote.VoteForProposalId = null;
            vote.Reasoning = "abstains this round";
        }
        else
        {
            vote.VoteForProposalId = NormalizeProposalRef(input.VoteProposalId!, "");
            vote.Reasoning = "casts a vote";
        }
        vote.Content = vote.Reasoning;
        Add(vote);
    }

    /// <summary>Derives a short proposal title (≤6 words) from the human's message.</summary>
    private static string DeriveTitle(string text)
    {
        var firstLine = text.Split('\n', '\r').FirstOrDefault(l => !string.IsNullOrWhiteSpace(l))?.Trim() ?? text;
        var words = firstLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var title = string.Join(' ', words.Take(6));
        if (words.Length > 6) title += "…";
        return string.IsNullOrWhiteSpace(title) ? "Human proposal" : title;
    }

    // ---- AI turns ----

    private async Task TakeTurn(Participant p, int round, List<Participant> ais, CancellationToken ct)
    {
        Status($"{p.DisplayName} is thinking… (round {round})");

        var profile = settings.Find(p.ProfileId);
        if (profile == null)
        {
            Add(new ChatMessage { Author = p, Action = TurnAction.System, Round = round,
                Content = $"⚠️ {p.DisplayName} has no profile assigned — pick one in the participant's settings." });
            return;
        }

        var provider = ProviderFactory.Create(profile);
        var phase = Prompts.DebatePhase(round, maxRounds, proposals.Count > 0, CurrentLeader(ais));
        var system = Prompts.SystemFor(p, council, phase);
        var user = BuildUserPrompt(ais);

        string raw;
        try
        {
            raw = await provider.CompleteAsync(system, user, ct);
        }
        catch (LlmException ex)
        {
            Add(new ChatMessage { Author = p, Action = TurnAction.System, Round = round,
                Content = $"⚠️ {p.DisplayName} could not respond: {ex.Message}" });
            return;
        }

        Add(ParseTurn(raw, p, round));
    }

    private string BuildUserPrompt(List<Participant> ais)
    {
        var sb = new StringBuilder();
        sb.AppendLine("COUNCIL TRANSCRIPT SO FAR:");
        foreach (var m in transcript)
            sb.AppendLine(m.ToTranscriptLine());

        sb.AppendLine();
        if (proposals.Count > 0)
        {
            sb.AppendLine("PROPOSALS ON THE TABLE (vote by id):");
            foreach (var kv in proposals)
                sb.AppendLine($"  {kv.Key}: \"{kv.Value.Title}\" — {kv.Value.Content}");

            sb.AppendLine();
            sb.AppendLine("CURRENT VOTE STANDING (each member's latest vote):");
            foreach (var line in VoteStanding(ais))
                sb.AppendLine("  " + line);
        }
        else
        {
            sb.AppendLine("No proposals yet — be the first to propose.");
        }
        sb.AppendLine();
        sb.AppendLine("Consensus is reached only when EVERY AI member's latest action is a vote for the SAME proposal id.");
        sb.AppendLine("Your turn. Respond with JSON only.");
        return sb.ToString();
    }

    private ChatMessage ParseTurn(string raw, Participant p, int round)
    {
        var msg = new ChatMessage { Author = p, Round = round };
        var json = ExtractJson(raw);

        string action = "propose", content = raw.Trim(), title = "", voteFor = "", reasoning = "";
        if (json != null)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                action = Get(root, "action").ToLowerInvariant().Trim();
                content = Get(root, "content");
                title = Get(root, "title");
                voteFor = Get(root, "voteFor");
                reasoning = Get(root, "reasoning");
            }
            catch { /* fall back to defaults */ }
        }

        if (string.IsNullOrWhiteSpace(content)) content = reasoning;

        switch (action)
        {
            case "vote":
                msg.Action = TurnAction.Vote;
                msg.VoteForProposalId = NormalizeProposalRef(voteFor, content + " " + reasoning);
                msg.Reasoning = string.IsNullOrWhiteSpace(reasoning) ? content : reasoning;
                msg.Content = msg.Reasoning;
                break;

            case "clarify":
                msg.Action = TurnAction.Clarify;
                msg.Content = string.IsNullOrWhiteSpace(content) ? "(asks for clarification)" : content;
                break;

            default: // propose
                msg.Action = TurnAction.Propose;
                msg.ProposalId = $"P{++proposalCounter}";
                msg.Title = string.IsNullOrWhiteSpace(title) ? $"Proposal {msg.ProposalId}" : title;
                msg.Content = content;
                msg.Reasoning = reasoning;
                proposals[msg.ProposalId] = msg;
                break;
        }
        return msg;
    }

    // ---- consensus detection ----

    /// <summary>True when every AI member's most recent action is a vote for the same proposal id.</summary>
    private bool AisUnanimous(List<Participant> ais, out string winnerId)
    {
        winnerId = "";
        if (proposals.Count == 0) return false;

        string? agreed = null;
        foreach (var ai in ais)
        {
            var last = LatestStance(ai);
            if (last == null || last.Action != TurnAction.Vote || last.VoteForProposalId == null)
                return false;
            agreed ??= last.VoteForProposalId;
            if (!string.Equals(agreed, last.VoteForProposalId, StringComparison.OrdinalIgnoreCase))
                return false;
        }
        winnerId = agreed ?? "";
        return winnerId.Length > 0;
    }

    /// <summary>A participant's most recent meaningful action (proposal, clarification or vote).</summary>
    private ChatMessage? LatestStance(Participant participant) =>
        transcript.LastOrDefault(m => m.Author?.Id == participant.Id &&
            m.Action is TurnAction.Vote or TurnAction.Propose or TurnAction.Clarify);

    private string? CurrentLeader(List<Participant> ais)
    {
        var tally = LatestVoteTally(ais);
        return tally.Count == 0 ? null : tally.OrderByDescending(kv => kv.Value).First().Key;
    }

    /// <summary>Counts the latest vote of every participant (human included) per proposal.</summary>
    private Dictionary<string, int> LatestVoteTally(IEnumerable<Participant> voters)
    {
        var tally = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in voters)
        {
            var last = LatestStance(p);
            if (last is { Action: TurnAction.Vote, VoteForProposalId: { } id })
                tally[id] = tally.GetValueOrDefault(id) + 1;
        }
        return tally;
    }

    private IEnumerable<string> VoteStanding(List<Participant> ais)
    {
        foreach (var ai in ais)
        {
            var last = LatestStance(ai);
            string state = last switch
            {
                { Action: TurnAction.Vote, VoteForProposalId: { } id } => $"votes {id}",
                { Action: TurnAction.Propose, ProposalId: { } pid } => $"proposing {pid}",
                { Action: TurnAction.Clarify } => "asking a question",
                _ => "not yet spoken"
            };
            yield return $"{ai.DisplayName}: {state}";
        }
    }

    private string RoundSignature(List<Participant> ais)
    {
        var votes = ais.Select(ai =>
        {
            var last = LatestStance(ai);
            return last is { Action: TurnAction.Vote, VoteForProposalId: { } id } ? id : "-";
        });
        return $"{proposals.Count}|{string.Join(",", votes)}";
    }

    // ---- conclusion ----

    private async Task Conclude(List<Participant> ais, string problem, bool unanimous, string winnerId, string reason, CancellationToken ct)
    {
        // Tally counts every participant's latest vote (human included); unanimity above stays AI-scoped.
        var tally = LatestVoteTally(council);

        if (string.IsNullOrEmpty(winnerId))
            winnerId = tally.Count > 0
                ? tally.OrderByDescending(kv => kv.Value).First().Key
                : proposals.Keys.First();

        if (!proposals.TryGetValue(winnerId, out var winner))
            winner = proposals.Values.First();

        var tallyText = string.Join("  ·  ",
            proposals.Keys.Select(id => $"{id} \"{proposals[id].Title}\": {(tally.TryGetValue(id, out var c) ? c : 0)} vote(s)"));
        Add(new ChatMessage { Action = TurnAction.System, Round = -1,
            Content = $"🗳️ Concluded by {reason}. Tally — {tallyText}. Winner: {winnerId} \"{winner.Title}\"." });

        Status("Synthesizing the agreed solution…");
        var scribe = (winner.Author is { IsHuman: false } author ? author : null) ?? ais[0];
        var scribeProfile = settings.Find(scribe.ProfileId) ?? settings.Profiles.FirstOrDefault();
        var provider = scribeProfile != null ? ProviderFactory.Create(scribeProfile) : null;
        var transcriptText = string.Join("\n", transcript.Select(m => m.ToTranscriptLine()));

        string final;
        try
        {
            if (provider == null) throw new LlmException("no profile available to synthesize");
            final = await provider.CompleteAsync(
                "You are a precise, decisive technical scribe.",
                Prompts.SynthesisPrompt(problem, winner.Title ?? winnerId, winner.Content, transcriptText),
                ct);
        }
        catch (LlmException ex)
        {
            final = $"(Could not synthesize via {scribe.DisplayName}: {ex.Message})\n\n" +
                    $"Agreed proposal {winnerId} — \"{winner.Title}\":\n{winner.Content}";
        }

        Add(new ChatMessage
        {
            Author = null,
            Action = TurnAction.Final,
            Round = -1,
            Title = winner.Title,
            Content = final.Trim()
        });
        Status(unanimous ? "Unanimous consensus reached. The Council rests."
                         : $"Concluded ({reason}). The Council rests.");
    }

    // ---- helpers ----

    private static string Get(JsonElement root, string name)
        => root.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? (v.GetString() ?? "") : "";

    private static string? ExtractJson(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        int start = s.IndexOf('{');
        int end = s.LastIndexOf('}');
        if (start >= 0 && end > start) return s.Substring(start, end - start + 1);
        return null;
    }

    private string NormalizeProposalRef(string voteFor, string fallback)
    {
        var m = Regex.Match(voteFor + " " + fallback, @"\bP(\d+)\b", RegexOptions.IgnoreCase);
        if (m.Success)
        {
            var id = "P" + m.Groups[1].Value;
            if (proposals.ContainsKey(id)) return id;
        }
        foreach (var kv in proposals)
        {
            if (!string.IsNullOrWhiteSpace(kv.Value.Title) &&
                (voteFor.Contains(kv.Value.Title!, StringComparison.OrdinalIgnoreCase) ||
                 fallback.Contains(kv.Value.Title!, StringComparison.OrdinalIgnoreCase)))
                return kv.Key;
        }
        foreach (var kv in proposals)
        {
            var an = kv.Value.Author?.DisplayName;
            if (!string.IsNullOrWhiteSpace(an) && voteFor.Contains(an!, StringComparison.OrdinalIgnoreCase))
                return kv.Key;
        }
        return proposals.Keys.LastOrDefault() ?? "P1";
    }

    private void Add(ChatMessage m)
    {
        transcript.Add(m);
        ui.Post(_ => MessageAdded?.Invoke(m), null);
    }

    private void Status(string s) => ui.Post(_ => StatusChanged?.Invoke(s), null);
}
