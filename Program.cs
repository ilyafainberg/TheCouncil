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

using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using TheCouncil.Cli;
using TheCouncil.UI;

namespace TheCouncil;

internal static class Program
{
    // The app is built as a WinExe (no console). When launched from a terminal with
    // CLI args we attach to the PARENT console so stdout/stderr land in the user's
    // shell instead of a popup window.
    private const int AttachParentProcess = -1;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AttachConsole(int dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool FreeConsole();

    [STAThread]
    static int Main(string[] args)
    {
        if (CliRunner.IsCliInvocation(args))
            return RunCli(args);

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
        return 0;
    }

    private static int RunCli(string[] args)
    {
        bool attached = AttachConsole(AttachParentProcess);
        RebindConsoleStreams();
        try
        {
            // STAThread can't be async; bridge into the async CLI runner here.
            return Task.Run(() => CliRunner.RunAsync(args)).GetAwaiter().GetResult();
        }
        finally
        {
            if (attached) FreeConsole();
        }
    }

    // After AttachConsole, the existing Console.Out/Error (bound before attach) don't
    // point at the new console buffer. Rebind them to the real console handles so our
    // writes appear in the parent shell.
    private static void RebindConsoleStreams()
    {
        try
        {
            var stdout = new System.IO.StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };
            var stderr = new System.IO.StreamWriter(Console.OpenStandardError()) { AutoFlush = true };
            Console.SetOut(stdout);
            Console.SetError(stderr);
        }
        catch
        {
            // No console (e.g. launched detached) — writes simply go nowhere; harmless.
        }
    }
}
