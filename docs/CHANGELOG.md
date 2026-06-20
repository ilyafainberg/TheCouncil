# Changelog

All notable changes to The Council are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.0.0/) and this project adheres
to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.2] - 2026-06-20

### Changed
- **Blind opening round**: in round 1 every member now proposes independently from
  the problem statement alone — no member sees another's idea first — so the round
  produces one proposal per member before any debate or voting begins. This removes
  anchoring on whoever spoke first.

### Fixed
- Updated repository references after the repo was renamed to `TheCouncil` (the
  auto-updater now queries the correct Releases endpoint).

## [1.0.1] - 2026-06-17

### Added
- **Auto-update**: on startup the app checks GitHub Releases and offers to download
  and apply a newer version in place (portable or installer), then relaunches. Uses
  the GitHub Releases API as the update server — no backend. See
  [docs/AUTOUPDATE.md](AUTOUPDATE.md).

## [1.0.0] - 2026-06-17

### Added
- Multi-model debate across OpenAI, Anthropic Claude, Google Gemini, xAI Grok and
  Azure OpenAI, plus an optional human seat.
- **Profiles**: reusable model endpoints (name, provider, model, key; endpoint +
  deployment for Azure) assigned to participants.
- **Consensus engine**: rounds run until AI-unanimous agreement, a stall, or a
  configurable round cap, then a final solution is synthesized by the winning
  proposal's author.
- Human turn each round: propose (typed message), vote, or abstain. Human votes are
  counted in the final tally.
- Teams-style UI: rounded chat bubbles, avatars, role badges, per-message copy icon,
  copy-whole-transcript, resizable members panel.
- Persistent profiles and council roster in `%APPDATA%\TheCouncil\settings.json`.
- Custom application icon; Enter-to-send / Ctrl+Enter-for-newline input.

[1.0.2]: https://github.com/ilyafainberg/TheCouncil/releases/tag/v1.0.2
[1.0.1]: https://github.com/ilyafainberg/TheCouncil/releases/tag/v1.0.1
[1.0.0]: https://github.com/ilyafainberg/TheCouncil/releases/tag/v1.0.0
