# The Council

> A Teams-style desktop chat where multiple LLMs — **Gemini, Grok, OpenAI, Azure AI, Claude** — and *you* debate a problem across rounds and converge on one agreed solution.

[![Release](https://img.shields.io/github/v/release/ilyafainberg/TheCouncil)](https://github.com/ilyafainberg/TheCouncil/releases)
[![Build](https://github.com/ilyafainberg/TheCouncil/actions/workflows/build.yml/badge.svg)](https://github.com/ilyafainberg/TheCouncil/actions)
[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](LICENSE)

<img width="1024" height="768" alt="TheCouncil" src="https://github.com/user-attachments/assets/8d244603-92a0-4f56-9427-c4245a8b98ac" />

The Council convenes a panel of AI models (and an optional human seat). You pose a
problem; each round every participant may **propose** a solution, **vote** for an
existing proposal, or **abstain**. The debate continues until the AI members reach
**unanimous agreement**, the conversation **stalls**, or it hits the **round cap** —
then the winning proposal's author synthesizes the final agreed solution.

## Features

- **Multi-model debate** — OpenAI, Anthropic Claude, Google Gemini, xAI Grok and
  Azure OpenAI, all in one conversation, each with its own persona.
- **Profiles** — define reusable model endpoints (name, provider, model, key; or
  endpoint + deployment for Azure) and assign them to participants.
- **Human in the loop** — add yourself as a participant; each round pauses for you
  to propose, vote, or abstain.
- **Blind opening round** — in round 1 every member proposes from the problem alone,
  before seeing anyone else's idea, so the debate starts from independent solutions
  (no anchoring on whoever spoke first).
- **Consensus engine** — debates run until unanimous (AI-scoped), stalled, or the
  configurable round cap; votes are tallied and a final solution is synthesized.
- **Teams-style UI** — rounded chat bubbles, avatars, role badges, per-message copy,
  copy-whole-transcript, and a resizable members panel.
- **Local & private** — your API keys live only in `%APPDATA%\TheCouncil\settings.json`
  on your machine. No backend, no telemetry.
- **Auto-update** — checks GitHub Releases on startup and updates itself in place
  (see [docs/AUTOUPDATE.md](docs/AUTOUPDATE.md)).

## Install

### Option A — Installer (recommended)

1. Go to the [latest release](https://github.com/ilyafainberg/TheCouncil/releases/latest).
2. Download **`TheCouncil-<version>-setup.zip`**.
3. Unzip it and run **`TheCouncil-<version>-setup.exe`**.
4. Follow the wizard. The app installs to *Program Files* with Start Menu shortcuts.

> **SmartScreen note:** this build is not code-signed, so Windows may show
> "Windows protected your PC". Click **More info → Run anyway**.

### Option B — Portable (no install, no admin)

1. From the [latest release](https://github.com/ilyafainberg/TheCouncil/releases/latest),
   download **`TheCouncil-<version>-portable-win-x64.zip`**.
2. Unzip it anywhere (a USB stick is fine).
3. Run **`TheCouncil.exe`**. The build is self-contained — no .NET install required.

### Verify your download (optional)

Each release includes `SHA256SUMS.txt`:

```powershell
Get-FileHash .\TheCouncil-<version>-setup.zip -Algorithm SHA256
```

## Usage

1. **Add your API keys.** Open **⚙ Settings** and create one **Profile** per model you
   want on the council:
   - *OpenAI / Claude / Gemini / Grok*: set **API key** and **Model**.
   - *Azure AI*: set **API key**, **Endpoint** and **Deployment** (Model is ignored).
   - Optionally set **Max debate rounds** (1–50, default 10).
2. **Build the council.** On the left, add members and pick a **Profile** for each.
   Add the **🙂 Human (you)** option if you want a seat at the table.
3. **Pose a problem** in the box at the bottom and press **Enter** (or **Convene**).
   Use **Ctrl+Enter** for a newline.
4. **Watch them debate.** Each round the AIs propose / vote / ask questions. If you're
   a member, the panel pauses each round for you to **propose** (type a message),
   **vote** for a proposal, or **abstain**.
5. **Get the verdict.** When the council concludes, it tallies the votes and the
   winning proposal's author writes up the **✅ Agreed Solution** (summary, how it
   works, and why the council agreed).

Use the **copy icon** on any message, or **⧉ Copy transcript** in the header, to
grab the conversation.

## Command line

The Council also runs headlessly — handy for scripts and pipelines. Configure your
profiles in the GUI first, then:

```powershell
# Use the saved roster, print the live debate + final decision:
TheCouncil "Which database for a small read-heavy app?"

# Pick members explicitly as [profile, name, persona] triples, decision only:
TheCouncil --problem "Cache strategy?" `
  --members ["Claude Opus 4.8","Skeptic","contrarian"],["GPT 5.4","Optimist",""] `
  --quiet
```

- `profile` matches a saved profile by name (or id); `name` and `persona` are optional.
- Omit `--members` to reuse the last saved roster.
- The **final decision** prints to **stdout**; the live debate/log goes to **stderr**
  (so `--quiet` + stdout redirection gives you just the answer).
- **CLI mode is AI-only** — any human members are dropped automatically with a warning.
- Run `TheCouncil --help` for the full option list.

## Configuration

| Setting | Where | Default | Notes |
| --- | --- | --- | --- |
| Profiles (keys, models, endpoints) | `%APPDATA%\TheCouncil\settings.json` | seeded defaults | One per provider; reusable across members. |
| Council roster | `%APPDATA%\TheCouncil\settings.json` | You + one per profile | Persisted across restarts. |
| Max debate rounds | Settings dialog | 10 | Hard cap (1–50) that forces a conclusion. |

> API keys are stored **in plain text** in `settings.json` on your machine. Keep that
> file private; don't commit or share it.

## Build from source

Requires the **.NET 10 SDK** (the project targets `net10.0-windows`).

```powershell
git clone https://github.com/ilyafainberg/TheCouncil.git
cd TheCouncil
dotnet build -c Release
dotnet run --project TheCouncil.csproj
```

To reproduce a release build locally:

```powershell
dotnet publish TheCouncil.csproj -c Release -r win-x64 --self-contained true -o publish
```

## Troubleshooting / FAQ

- **A member posts "API key is not configured"** — open Settings and add the key to
  that member's profile.
- **Azure member fails with "API version not supported"** — paste your resource
  endpoint; the app normalizes it to the host root automatically. Set the
  **Deployment** to your deployed model name.
- **App won't start / missing DLL** — use the self-contained Portable zip from
  Releases; it bundles the runtime.
- **SmartScreen / antivirus warning** — the build is unsigned; verify the SHA256
  against the release and choose "Run anyway".

## Contributing

PRs welcome — see [CONTRIBUTING.md](docs/CONTRIBUTING.md). Please open an issue first
for anything non-trivial.

## Architecture

See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for a tour of the providers,
orchestrator (the consensus engine) and the WinForms UI.

## License

The Council is licensed under the **GNU General Public License v3.0**. See [LICENSE](LICENSE).
