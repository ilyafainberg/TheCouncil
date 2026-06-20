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
//
// =============================================================================
//  CliRunner.cs — headless command-line mode for The Council.
//
//  Runs the same CouncilOrchestrator as the GUI, but with no window: streams the
//  debate to the console (on stderr) and prints ONLY the final agreed decision to
//  stdout, so it composes in pipelines.
//
//  Usage:
//    TheCouncil --problem "Which database for a small read-heavy app?"
//    TheCouncil "Which database..." --members ["Claude","Skeptic","contrarian"],["OpenAI","Optimist",""]
//
//  --members takes one or more [profile, name, persona] triples. "profile" matches
//  a saved profile by name (or id); name/persona are optional. With no --members we
//  reuse the last saved council roster. Human members are NOT allowed in CLI mode —
//  any are dropped with a warning.
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TheCouncil.Core;
using TheCouncil.Models;

namespace TheCouncil.Cli;

public static class CliRunner
{
    /// <summary>True when the args request CLI mode (a problem and/or members, or --help).</summary>
    public static bool IsCliInvocation(string[] args) =>
        args.Any(a => a is "--problem" or "-p" or "--members" or "-m" or "--help" or "-h" or "--cli")
        || (args.Length > 0 && !args[0].StartsWith('-'));

    public static async Task<int> RunAsync(string[] args)
    {
        ParsedArgs parsed;
        try
        {
            parsed = ParseArgs(args);
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            PrintUsage();
            return 2;
        }

        if (parsed.ShowHelp)
        {
            PrintUsage();
            return 0;
        }

        if (string.IsNullOrWhiteSpace(parsed.Problem))
        {
            Console.Error.WriteLine("Error: no problem provided. Pass it positionally or with --problem.");
            PrintUsage();
            return 2;
        }

        var settings = CouncilSettings.Load();

        // Build the council: explicit --members, else the saved roster.
        List<Participant> council;
        if (parsed.Members.Count > 0)
            council = BuildFromSpecs(parsed.Members, settings);
        else
            council = LoadSavedRoster(settings);

        // CLI mode forbids human members — drop them with a warning.
        int humans = council.RemoveAll(p => p.IsHuman);
        if (humans > 0)
            Console.Error.WriteLine($"Warning: dropped {humans} human member(s) — CLI mode runs AI-only.");

        var unresolved = council.Where(p => settings.Find(p.ProfileId) == null).Select(p => p.DisplayName).ToList();
        if (unresolved.Count > 0)
        {
            Console.Error.WriteLine($"Error: no matching profile for: {string.Join(", ", unresolved)}.");
            Console.Error.WriteLine("Configure profiles in the GUI (Settings), or check the names in --members.");
            return 2;
        }

        if (council.Count == 0)
        {
            Console.Error.WriteLine("Error: no AI members to convene. Use --members or save a roster in the GUI.");
            return 2;
        }

        return await Convene(settings, council, parsed.Problem!, parsed.Quiet);
    }

    private static async Task<int> Convene(CouncilSettings settings, List<Participant> council, string problem, bool quiet)
    {
        var orchestrator = new CouncilOrchestrator(settings, council);
        string? finalDecision = null;

        orchestrator.StatusChanged += s => { if (!quiet) Console.Error.WriteLine($"… {s}"); };
        orchestrator.MessageAdded += m =>
        {
            if (m.Action == TurnAction.Final)
                finalDecision = m.Content;
            else if (!quiet)
                Console.Error.WriteLine(m.ToTranscriptLine());
        };

        Console.Error.WriteLine($"Convening {council.Count} member(s): {string.Join(", ", council.Select(c => c.DisplayName))}");

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

        // A throwaway human is required by the signature but is not in the council,
        // so no human gate ever fires.
        var dummyHuman = new Participant { DisplayName = "cli", IsHuman = true };
        await orchestrator.RunAsync(dummyHuman, problem, cts.Token);

        if (string.IsNullOrWhiteSpace(finalDecision))
        {
            Console.Error.WriteLine("The council did not reach a decision.");
            return 1;
        }

        // The final decision is the ONLY thing on stdout.
        Console.Out.WriteLine(finalDecision.Trim());
        return 0;
    }

    // ---- roster building ----

    private static List<Participant> LoadSavedRoster(CouncilSettings settings)
    {
        if (settings.Council.Count > 0)
            return settings.Council.Select(Clone).ToList();

        // No saved roster — seed one AI per profile.
        return settings.Profiles.Select(p => new Participant { DisplayName = p.Name, ProfileId = p.Id }).ToList();
    }

    private static List<Participant> BuildFromSpecs(List<MemberSpec> specs, CouncilSettings settings)
    {
        var result = new List<Participant>();
        foreach (var spec in specs)
        {
            var profile = ResolveProfile(spec.Profile, settings);
            result.Add(new Participant
            {
                DisplayName = string.IsNullOrWhiteSpace(spec.Name) ? (profile?.Name ?? spec.Profile) : spec.Name,
                ProfileId = profile?.Id,           // null -> reported as unresolved later
                Persona = spec.Persona ?? "",
            });
        }
        return result;
    }

    private static Profile? ResolveProfile(string token, CouncilSettings settings)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        return settings.Profiles.FirstOrDefault(p => p.Id.Equals(token, StringComparison.OrdinalIgnoreCase))
            ?? settings.Profiles.FirstOrDefault(p => p.Name.Equals(token, StringComparison.OrdinalIgnoreCase))
            ?? settings.Profiles.FirstOrDefault(p => p.Name.Contains(token, StringComparison.OrdinalIgnoreCase));
    }

    private static Participant Clone(Participant p) => new()
    {
        DisplayName = p.DisplayName,
        IsHuman = p.IsHuman,
        ProfileId = p.ProfileId,
        Persona = p.Persona,
        ColorArgb = p.ColorArgb
    };

    // ---- argument parsing ----

    private sealed record MemberSpec(string Profile, string? Name, string? Persona);

    private sealed class ParsedArgs
    {
        public string? Problem;
        public List<MemberSpec> Members = new();
        public bool ShowHelp;
        public bool Quiet;
    }

    private static ParsedArgs ParseArgs(string[] args)
    {
        var p = new ParsedArgs();
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--help" or "-h":
                    p.ShowHelp = true;
                    break;
                case "--quiet" or "-q":
                    p.Quiet = true;
                    break;
                case "--cli":
                    break; // explicit mode flag; no-op (mode already chosen)
                case "--problem" or "-p":
                    p.Problem = NextValue(args, ref i, "--problem");
                    break;
                case "--members" or "-m":
                    p.Members.AddRange(ParseMembers(NextValue(args, ref i, "--members")));
                    break;
                default:
                    if (args[i].StartsWith('-'))
                        throw new ArgumentException($"unknown option '{args[i]}'.");
                    // First bare token is the problem (if not already set).
                    p.Problem ??= args[i];
                    break;
            }
        }
        return p;
    }

    private static string NextValue(string[] args, ref int i, string opt)
    {
        if (i + 1 >= args.Length) throw new ArgumentException($"{opt} requires a value.");
        return args[++i];
    }

    /// <summary>
    /// Parses a --members value of the form: ["profile","name","persona"],["profile","name",""].
    /// Each bracket group yields a [profile, name, persona] triple; name/persona optional.
    /// </summary>
    private static IEnumerable<MemberSpec> ParseMembers(string raw)
    {
        var groups = Regex.Matches(raw, @"\[(.*?)\]");
        if (groups.Count == 0)
            throw new ArgumentException("--members must contain one or more [profile, name, persona] groups.");

        foreach (Match g in groups)
        {
            var fields = SplitFields(g.Groups[1].Value);
            if (fields.Count == 0 || string.IsNullOrWhiteSpace(fields[0]))
                throw new ArgumentException("each --members group needs at least a profile name.");
            yield return new MemberSpec(
                fields[0],
                fields.Count > 1 ? fields[1] : null,
                fields.Count > 2 ? fields[2] : null);
        }
    }

    /// <summary>Splits a group's comma-separated fields, honoring optional double quotes.</summary>
    private static List<string> SplitFields(string inner)
    {
        var fields = new List<string>();
        var sb = new StringBuilder();
        bool inQuotes = false;
        foreach (var ch in inner)
        {
            if (ch == '"') { inQuotes = !inQuotes; continue; }
            if (ch == ',' && !inQuotes) { fields.Add(sb.ToString().Trim()); sb.Clear(); continue; }
            sb.Append(ch);
        }
        fields.Add(sb.ToString().Trim());
        return fields;
    }

    private static void PrintUsage()
    {
        Console.Error.WriteLine(
@"The Council - command line mode

USAGE
  TheCouncil [--problem] ""<problem>"" [--members <specs>] [--quiet]

OPTIONS
  -p, --problem ""text""   The problem to debate. May also be the first bare argument.
  -m, --members <specs>  AI members as [profile,name,persona] groups, comma-separated:
                           --members [""Claude"",""Skeptic"",""contrarian""],[""OpenAI"",""Optimist"",""""]
                         'profile' matches a saved profile by name (or id); name and
                         persona are optional. Omit --members to reuse the saved roster.
  -q, --quiet            Suppress the live debate; print only the final decision.
  -h, --help             Show this help.

NOTES
  - CLI mode is AI-only. Human members are dropped automatically with a warning.
  - The final agreed decision is printed to stdout; debate/log goes to stderr.
  - Configure profiles (API keys, models) in the GUI first (Settings).

EXAMPLES
  TheCouncil ""Pick a logging format for a microservice.""
  TheCouncil --problem ""Cache strategy?"" --members [""Claude"",""A"",""""],[""OpenAI"",""B"",""""] --quiet");
    }
}
