# Contributing to The Council

Thanks for your interest! This is a personal project, but PRs and issues are welcome.

## Ground rules

- **Open an issue first** for anything non-trivial so we can agree on the approach.
- **License:** by contributing you agree your changes are licensed under **GPLv3**.
- **Style:** C# with the repo's conventions — private members are `camelCase` with
  **no** leading underscore; public members are `PascalCase`. Keep WinForms code
  Designer-compatible (flat `InitializeComponent`, parameterless form constructors).
- **Comment the "why".** Non-obvious logic should explain intent, not restate code.

## Development setup

Requires the **.NET 10 SDK** (targets `net10.0-windows`; Windows only).

```powershell
git clone https://github.com/ilyafainberg/TheCouncil.git
cd TheCouncil
dotnet build -c Release
dotnet run --project TheCouncil.csproj
```

## Before you open a PR

- `dotnet build -c Release` is clean (0 warnings, 0 errors).
- The three forms still open in the Visual Studio Designer.
- You've described what you changed and why, and how you tested it.

## Project layout

- `Models/` — data types (`Participant`, `Profile`, `ChatMessage`, `CouncilSettings`).
- `Providers/` — one `ILlmProvider` per backend (+ an OpenAI-compatible client).
- `Core/` — `CouncilOrchestrator` (the consensus engine) and prompt templates.
- `UI/` — WinForms: `MainForm`, `SettingsForm`, `ParticipantEditForm`, `ChatBubble`.

See [ARCHITECTURE.md](ARCHITECTURE.md) for detail.
