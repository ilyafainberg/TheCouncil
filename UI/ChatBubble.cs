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
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using TheCouncil.Models;

namespace TheCouncil.UI;

/// <summary>A Teams-style chat bubble: avatar + rounded card with name, role badge and body.</summary>
public class ChatBubble : Control
{
    private readonly ChatMessage msg;
    private const int Avatar = 38;
    private const int Gap = 10;
    private const int Pad = 12;

    // palette
    private static readonly Color Ink = Color.FromArgb(37, 36, 35);
    private static readonly Color SubInk = Color.FromArgb(120, 120, 120);
    private static readonly Color OtherCard = Color.White;
    private static readonly Color HumanCard = Color.FromArgb(233, 236, 250);   // light Teams purple
    private static readonly Color FinalCard = Color.FromArgb(223, 246, 221);   // light green
    private static readonly Color SystemPill = Color.FromArgb(230, 230, 235);
    private static readonly Color Accent = Color.FromArgb(98, 100, 167);

    private readonly Font nameFont = new("Segoe UI Semibold", 9.5f);
    private readonly Font bodyFont = new("Segoe UI", 9.75f);
    private readonly Font titleFont = new("Segoe UI Semibold", 10.5f);
    private readonly Font badgeFont = new("Segoe UI Semibold", 7.5f);
    private readonly Font timeFont = new("Segoe UI", 7.5f);
    private readonly Font avatarFont = new("Segoe UI Semibold", 11f);

    private bool IsHuman => msg.Action is TurnAction.HumanChat or TurnAction.Problem;
    private bool IsFinal => msg.Action == TurnAction.Final;
    private bool IsSystem => msg.Action == TurnAction.System;

    private Rectangle copyIconRect = Rectangle.Empty;
    private bool copyHover;
    private DateTime copiedAt = DateTime.MinValue;
    private bool CopiedRecently => (DateTime.Now - copiedAt).TotalMilliseconds < 1200;

    public ChatBubble(ChatMessage msg)
    {
        this.msg = msg;
        DoubleBuffered = true;
        Margin = new Padding(8, 4, 8, 4);
        SetStyle(ControlStyles.ResizeRedraw, true);
    }

    /// <summary>Plain-text form of this bubble's content, suitable for copying to the clipboard.</summary>
    public string PlainText
    {
        get
        {
            string who = msg.Action == TurnAction.Final ? "Agreed Solution" : msg.AuthorName;
            string head = msg.Action switch
            {
                TurnAction.Problem => $"{who} (problem)",
                TurnAction.Propose => $"{who} — Proposal {msg.ProposalId}: {msg.Title}",
                TurnAction.Clarify => $"{who} (question)",
                TurnAction.Vote => msg.VoteForProposalId == null ? $"{who} (abstains)" : $"{who} (votes {msg.VoteForProposalId})",
                TurnAction.Consensus => $"{who} (final vote {msg.VoteForProposalId})",
                TurnAction.Final => "✅ Agreed Solution" + (string.IsNullOrWhiteSpace(msg.Title) ? "" : $" — {msg.Title}"),
                _ => who
            };
            return $"{head}\r\n{BodyText()}".Trim();
        }
    }

    public void SetWidth(int width)
    {
        Width = Math.Max(220, width);
        Height = Measure();
        Invalidate();
    }

    private int TextWidth()
    {
        // card spans ~78% of the row, minus avatar gutter and padding
        int cardW = (int)(Width * (IsFinal ? 0.96 : 0.80));
        return Math.Max(120, cardW - Avatar - Gap - Pad * 2);
    }

    private int Measure()
    {
        if (IsSystem)
        {
            var s = TextRenderer.MeasureText(msg.Content, bodyFont,
                new Size(Width - 80, int.MaxValue), TextFormatFlags.WordBreak);
            return s.Height + 18;
        }

        int tw = TextWidth();
        int h = Pad;
        h += 20; // name row
        if (msg.Action == TurnAction.Propose && !string.IsNullOrWhiteSpace(msg.Title))
            h += TextRenderer.MeasureText(msg.Title, titleFont, new Size(tw, int.MaxValue), TextFormatFlags.WordBreak).Height + 2;
        var body = BodyText();
        h += TextRenderer.MeasureText(body, bodyFont, new Size(tw, int.MaxValue), TextFormatFlags.WordBreak).Height;
        h += Pad;
        return Math.Max(h, Avatar + 8);
    }

    private string BodyText()
    {
        if (msg.Action is TurnAction.Vote or TurnAction.Consensus)
            return msg.Reasoning ?? msg.Content;
        return msg.Content;
    }

    private (string text, Color color)? Badge()
    {
        return msg.Action switch
        {
            TurnAction.Problem => ("PROBLEM", Color.FromArgb(196, 49, 75)),
            TurnAction.Propose => ($"PROPOSAL {msg.ProposalId}", Accent),
            TurnAction.Clarify => ("QUESTION", Color.FromArgb(193, 120, 23)),
            TurnAction.Vote => msg.VoteForProposalId == null
                ? ("ABSTAIN", Color.FromArgb(120, 120, 128))
                : ($"VOTE ▶ {msg.VoteForProposalId}", Color.FromArgb(45, 125, 70)),
            TurnAction.Consensus => ($"FINAL VOTE ▶ {msg.VoteForProposalId}", Color.FromArgb(45, 125, 70)),
            _ => null
        };
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        if (IsSystem) { PaintSystem(g); return; }

        bool right = IsHuman && !IsFinal;
        int cardW = (int)(Width * (IsFinal ? 0.96 : 0.80));
        int textW = TextWidth();

        int avatarX = right ? Width - Avatar - 4 : 4;
        int cardX = right ? Width - cardW - 4 : Avatar + Gap;
        if (IsFinal) { cardX = 4; cardW = Width - 8; textW = cardW - Pad * 2; }

        var cardColor = IsFinal ? FinalCard : (IsHuman ? HumanCard : OtherCard);
        var cardRect = new Rectangle(cardX, 0, cardW, Height - 4);

        // avatar (not for final summary card)
        if (!IsFinal)
        {
            var ac = msg.Author?.Color ?? Accent;
            using var ab = new SolidBrush(ac);
            g.FillEllipse(ab, new Rectangle(avatarX, 0, Avatar, Avatar));
            var initials = msg.Author?.Initials ?? "C";
            TextRenderer.DrawText(g, initials, avatarFont, new Rectangle(avatarX, 0, Avatar, Avatar),
                Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        // card
        using (var path = Rounded(cardRect, 12))
        {
            using var cb = new SolidBrush(cardColor);
            g.FillPath(cb, path);
            using var bp = new Pen(Color.FromArgb(225, 225, 230));
            g.DrawPath(bp, path);
        }

        int x = cardRect.X + Pad;
        int y = cardRect.Y + Pad / 2 + 2;

        // name + badge + time row
        var nameColor = msg.Author?.Color ?? Accent;
        if (IsFinal) { nameColor = Color.FromArgb(45, 125, 70); }
        string name = IsFinal ? "✅ Agreed Solution" + (string.IsNullOrWhiteSpace(msg.Title) ? "" : $" — {msg.Title}") : msg.AuthorName;
        var nameSize = TextRenderer.MeasureText(g, name, nameFont);
        TextRenderer.DrawText(g, name, nameFont, new Point(x, y), nameColor, TextFormatFlags.NoPadding);

        int bx = x + nameSize.Width + 8;
        var badge = IsFinal ? null : Badge();
        if (badge is { } b)
        {
            var bs = TextRenderer.MeasureText(g, b.text, badgeFont);
            var pill = new Rectangle(bx, y + 1, bs.Width + 12, 15);
            using (var pp = Rounded(pill, 7))
            using (var pb = new SolidBrush(Color.FromArgb(30, b.color)))
                g.FillPath(pb, pp);
            TextRenderer.DrawText(g, b.text, badgeFont, pill, b.color,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        var time = msg.Timestamp.ToString("HH:mm");
        var ts = TextRenderer.MeasureText(g, time, timeFont);
        int timeX = cardRect.Right - Pad - ts.Width;
        TextRenderer.DrawText(g, time, timeFont,
            new Point(timeX, y + 2), SubInk, TextFormatFlags.NoPadding);

        // copy icon (left of the timestamp)
        copyIconRect = new Rectangle(timeX - 22, y, 16, 16);
        DrawCopyIcon(g, copyIconRect);

        y += 20;

        // proposal title
        if (msg.Action == TurnAction.Propose && !string.IsNullOrWhiteSpace(msg.Title))
        {
            var tRect = new Rectangle(x, y, textW, int.MaxValue);
            var th = TextRenderer.MeasureText(g, msg.Title, titleFont, new Size(textW, int.MaxValue), TextFormatFlags.WordBreak).Height;
            TextRenderer.DrawText(g, msg.Title, titleFont, new Rectangle(x, y, textW, th), Ink, TextFormatFlags.WordBreak);
            y += th + 2;
        }

        // body
        var body = BodyText();
        TextRenderer.DrawText(g, body, bodyFont, new Rectangle(x, y, textW, Height - y - Pad / 2),
            Ink, TextFormatFlags.WordBreak);
    }

    private void PaintSystem(Graphics g)
    {
        copyIconRect = Rectangle.Empty;
        var size = TextRenderer.MeasureText(msg.Content, bodyFont, new Size(Width - 80, int.MaxValue), TextFormatFlags.WordBreak);
        int w = Math.Min(Width - 40, size.Width + 28);
        int x = (Width - w) / 2;
        var rect = new Rectangle(x, 4, w, Height - 10);
        using (var p = Rounded(rect, 10))
        using (var b = new SolidBrush(SystemPill))
            g.FillPath(b, p);
        TextRenderer.DrawText(g, msg.Content, bodyFont, rect, Color.FromArgb(90, 90, 95),
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak);
    }

    /// <summary>Draws a small "copy" glyph (two offset rounded rectangles), or a check when just copied.</summary>
    private void DrawCopyIcon(Graphics g, Rectangle r)
    {
        if (CopiedRecently)
        {
            using var okPen = new Pen(Color.FromArgb(45, 125, 70), 1.6f);
            g.DrawLines(okPen, new[]
            {
                new Point(r.X + 3, r.Y + 8),
                new Point(r.X + 6, r.Y + 11),
                new Point(r.X + 12, r.Y + 4)
            });
            return;
        }

        var col = copyHover ? Accent : Color.FromArgb(150, 150, 158);
        using var pen = new Pen(col, 1.3f);
        // back sheet
        var back = new Rectangle(r.X + 4, r.Y + 1, 9, 11);
        // front sheet
        var front = new Rectangle(r.X + 1, r.Y + 4, 9, 11);
        using (var p1 = Rounded(back, 2)) g.DrawPath(pen, p1);
        using (var bg = new SolidBrush(copyHover ? Color.FromArgb(235, 236, 248) : Color.White))
        using (var p2 = Rounded(front, 2)) { g.FillPath(bg, p2); g.DrawPath(pen, p2); }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        bool over = copyIconRect != Rectangle.Empty && copyIconRect.Contains(e.Location);
        if (over != copyHover)
        {
            copyHover = over;
            Cursor = over ? Cursors.Hand : Cursors.Default;
            Invalidate();
        }
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        if (copyHover) { copyHover = false; Cursor = Cursors.Default; Invalidate(); }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button == MouseButtons.Left && copyIconRect != Rectangle.Empty && copyIconRect.Contains(e.Location))
        {
            try { Clipboard.SetText(PlainText); copiedAt = DateTime.Now; }
            catch { /* clipboard busy */ }
            Invalidate();
            var t = new System.Windows.Forms.Timer { Interval = 1300 };
            t.Tick += (_, _) => { t.Stop(); t.Dispose(); Invalidate(); };
            t.Start();
        }
    }

    private static GraphicsPath Rounded(Rectangle r, int radius)
    {
        int d = radius * 2;
        var p = new GraphicsPath();
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }
}
