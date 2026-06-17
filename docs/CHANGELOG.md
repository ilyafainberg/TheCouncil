# Changelog

All notable changes to The Council are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.0.0/) and this project adheres
to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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

[1.0.0]: https://github.com/ilyafainberg/the-council/releases/tag/v1.0.0
