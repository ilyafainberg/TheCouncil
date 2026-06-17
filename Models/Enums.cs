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

public enum ProviderKind
{
    OpenAI,
    Claude,
    Gemini,
    Grok,
    AzureAI,
    Human
}

public enum TurnAction
{
    Problem,    // the human-posed problem that starts the council
    Propose,    // a participant proposes a solution
    Clarify,    // a participant asks a clarifying question
    Vote,       // a participant votes for an existing proposal
    Consensus,  // a participant's final binding vote
    HumanChat,  // free-form human message mid-debate
    Final,      // the synthesized agreed solution
    System      // orchestrator status / narration
}
