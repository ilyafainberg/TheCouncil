# Architecture

The Council is a single-process .NET 10 WinForms app. There is no backend — it calls
LLM provider REST APIs directly and stores everything locally.

## Layers

```
Models/      plain data         Participant, Profile, ChatMessage, CouncilSettings, Enums
Providers/   network I/O        ILlmProvider + OpenAiCompatible, Claude, Gemini, Factory
Core/        debate logic       CouncilOrchestrator, Prompts
UI/          WinForms           MainForm, SettingsForm, ParticipantEditForm, ChatBubble
```

### Models

- **`Profile`** — a reusable model endpoint: provider, model, API key, and (for Azure)
  endpoint + deployment. Participants reference a profile by id.
- **`Participant`** — a council member: a display name, color, persona, and either
  `IsHuman` or a `ProfileId`. Only `ColorArgb` is serialized (the `Color` wrapper is
  `[JsonIgnore]`d to avoid a transparent round-trip).
- **`CouncilSettings`** — the persisted document (`%APPDATA%\TheCouncil\settings.json`):
  the list of profiles, the council roster, and the max-rounds cap. Migrates a legacy
  per-provider settings file into profiles on first load.
- **`ChatMessage`** — one entry in the transcript, tagged with a `TurnAction`
  (Problem, Propose, Clarify, Vote, Consensus, HumanChat, Final, System).

### Providers

All providers implement `ILlmProvider.CompleteAsync(system, user, ct)`.

- **`OpenAiCompatibleProvider`** serves OpenAI, xAI Grok and Azure OpenAI (Azure uses
  `api-key` auth and a deployment URL; the endpoint is normalized to its host root,
  and a `temperature`-rejected request transparently retries without it).
- **`ClaudeProvider`** and **`GeminiProvider`** wrap their respective REST shapes.
- **`ProviderFactory.Create(profile)`** picks the implementation from the profile's
  provider kind.

### Core — the consensus engine

`CouncilOrchestrator.RunAsync` drives the debate:

1. Records the human-posed **problem**.
2. Loops rounds (capped by `settings.MaxRounds`). Each round, every AI member takes a
   turn: the orchestrator builds a prompt containing the full transcript, the current
   proposals, and the live vote standing, then parses the model's JSON reply into a
   **propose / clarify / vote** action.
3. If a human is present, the round pauses (`HumanTurnRequested`) for the UI to submit
   a `HumanRoundInput` — a typed message becomes a **proposal** authored by the human;
   otherwise a vote or abstention.
4. After each round it checks **AI-scoped unanimity** (every AI's latest action is a
   vote for the same proposal) and a **stall** signature (no change for two rounds).
5. On conclusion it tallies **all** participants' latest votes (human included), then
   asks the winning proposal's author to synthesize the final agreed solution.

The orchestrator raises events (`MessageAdded`, `StatusChanged`, `HumanTurnRequested`,
`HumanTurnEnded`, `Finished`) on the captured `SynchronizationContext` so the UI
updates live without cross-thread calls.

### UI

- **`MainForm`** — header (status, copy-transcript, settings), a resizable members
  roster (a `Splitter`), the chat flow, the per-round human action panel, and the
  problem input. Owns the orchestrator and marshals its events onto bubbles.
- **`ChatBubble`** — a custom-painted Teams-style bubble (avatar, name, role badge,
  body, timestamp, copy icon) with a `PlainText` accessor for clipboard export.
- **`SettingsForm`** — a profile-list manager plus the max-rounds control.
- **`ParticipantEditForm`** — pick a profile (or the human seat), name, persona, color.

All three forms follow the standard partial-class + `.Designer.cs` pattern so they
open in the Visual Studio Designer.
