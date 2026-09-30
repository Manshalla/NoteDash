using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Draws clef, key signature and generated notes into an existing Measure prefab instance.
///
/// Geometry (taken from Measure.prefab): staff lines at y = -0.4 ... +0.4, so one staff space = 0.2
/// and one staff step (line -> space) = 0.1. The measure spans x = -2.5 ... +2.5.
/// Glyphs are Bravura (SMuFL) characters using the Note prefab (TextMeshPro, font size 8 = 4 staff spaces per em).
/// Stems, beams and ledger lines are drawn with the same sprite/material as the staff lines.
/// </summary>
public static class MeasureRenderer
{
    // --- geometry (world units)
    const float Step = 0.1f;          // one staff position
    const float Space = 0.2f;         // one staff space
    const float HeadW = 1.18f * Space;
    const float WholeHeadW = 1.688f * Space;
    const float StemW = 0.12f * Space;
    const float StemLen = 3.5f * Space;
    const float StemAttachY = 0.168f * Space;
    const float BeamT = 0.5f * Space;
    const float BeamGap = 0.75f * Space;  // distance between beams (centre to centre)
    const float LedgerT = 0.16f * Space;
    const float LineT = 0.1f * Space;
    const float MeasureLeft = -2.5f, MeasureRight = 2.5f;
    const float GlyphSize = 8f;

    // --- SMuFL code points
    const string NoteheadWhole = "\uE0A2";
    const string NoteheadHalf = "\uE0A3";
    const string NoteheadBlack = "\uE0A4";
    const string AugDot = "\uE1E7";
    const string Flag8Up = "\uE240", Flag8Down = "\uE241", Flag16Up = "\uE242", Flag16Down = "\uE243";
    const string RestWhole = "\uE4E3", RestHalf = "\uE4E4", RestQuarter = "\uE4E5", Rest8 = "\uE4E6", Rest16 = "\uE4E7";

    // key signature staff positions for treble clef (0 = middle line B4)
    static readonly int[] SharpPositions = { 4, 1, 5, 2, -1, 3, 0 };
    static readonly int[] FlatPositions = { 0, 3, -1, 2, -2, 1, -3 };

    static Sprite lineSprite;
    static Material lineMaterial;
    static Color lineColor = Color.black;

    /// <summary>Absolute diatonic step of the middle staff line for a clef (C4 = 28).</summary>
    public static int ClefMiddleStep(ClefType clef) => clef switch
    {
        ClefType.Bass => 22,   // D3
        ClefType.Alto => 28,   // C4
        ClefType.Tenor => 26,  // A3
        _ => 34                // B4 (treble)
    };

    /// <summary>Staff position (0 = middle line) where the clef glyph's origin sits.</summary>
    public static int ClefGlyphPosition(ClefType clef) => clef switch
    {
        ClefType.Bass => 2,    // F line
        ClefType.Alto => 0,    // C line
        ClefType.Tenor => 2,
        _ => -2                // G line
    };

    static int KeySigOffset(ClefType clef) => clef switch
    {
        ClefType.Bass => -2,
        ClefType.Alto => -1,
        ClefType.Tenor => 1,
        _ => 0
    };

    /// <summary>Staff position of the index-th sharp/flat of a key signature in the given clef.</summary>
    public static int KeySignaturePosition(int index, bool sharps, ClefType clef) =>
        (sharps ? SharpPositions[index] : FlatPositions[index]) + KeySigOffset(clef);

    // =====================================================================================

    public static void Clear(Measure measure)
    {
        Transform old = measure.transform.Find("Generated");
        if (old != null) Object.Destroy(old.gameObject);
    }

    public static void Render(Measure measure, List<Note> notes, bool lineStart, bool showTimeSig)
    {
        CacheLineSprite(measure);
        Clear(measure);

        var root = new GameObject("Generated").transform;
        root.SetParent(measure.transform, false);
        root.localPosition = new Vector3(0, 0, -0.01f);

        ClefType clef = PracticeSettings.Clef;
        float x = MeasureLeft + 0.1f;

        // ---------------- header: clef, key signature, time signature
        if (lineStart)
        {
            Glyph(root, ClefRenderer.GetClefGlyph(clef), x, ClefGlyphPosition(clef) * Step);
            x += 0.62f;

            int fifths = PracticeSettings.KeyFifths;
            int[] positions = fifths > 0 ? SharpPositions : FlatPositions;
            string acc = fifths > 0 ? ClefRenderer.GetSharpGlyph() : ClefRenderer.GetFlatGlyph();
            for (int i = 0; i < Mathf.Abs(fifths); i++)
            {
                Glyph(root, acc, x, (positions[i] + KeySigOffset(clef)) * Step);
                x += fifths > 0 ? 0.21f : 0.19f;
            }
            if (fifths != 0) x += 0.08f;
        }

        Transform timeSig = measure.numC != null ? measure.numC.transform.parent : null;
        if (showTimeSig && timeSig != null)
        {
            timeSig.localPosition = new Vector3(x + 0.12f, timeSig.localPosition.y, timeSig.localPosition.z);
            x += 0.38f;
        }

        float areaStart = (lineStart || showTimeSig) ? x + 0.15f : MeasureLeft + 0.25f;
        float areaEnd = MeasureRight - 0.3f;

        if (notes == null || notes.Count == 0) return;

        int middle = ClefMiddleStep(clef);
        int measureUnits = PracticeSettings.MeasureUnits;

        // ---------------- whole-measure rest
        if (notes.Count == 1 && notes[0].isRest)
        {
            float cx = (areaStart + areaEnd) * 0.5f;
            Glyph(root, RestWhole, cx - 0.113f, 2 * Step);
            return;
        }

        // ---------------- horizontal spacing (logarithmic, like engraved music)
        var xs = new float[notes.Count];
        float total = 0;
        foreach (var n in notes) total += SpacingWeight(n.duration);
        float cum = 0;
        for (int i = 0; i < notes.Count; i++)
        {
            xs[i] = areaStart + cum / total * (areaEnd - areaStart);
            cum += SpacingWeight(notes[i].duration);
        }

        // ---------------- beam groups (8ths/16ths inside one beat, broken by rests)
        int beat = PracticeSettings.BeatUnits;
        var groupOf = new int[notes.Count];
        var groups = new List<List<int>>();
        for (int i = 0; i < notes.Count; i++)
        {
            groupOf[i] = -1;
            Note n = notes[i];
            if (n.isRest || n.Beams == 0) continue;
            bool joinPrev = i > 0 && groupOf[i - 1] >= 0 && notes[i - 1].start / beat == n.start / beat;
            if (joinPrev) { groups[groupOf[i - 1]].Add(i); groupOf[i] = groupOf[i - 1]; }
            else { groups.Add(new List<int> { i }); groupOf[i] = groups.Count - 1; }
        }

        // ---------------- notes & rests
        for (int i = 0; i < notes.Count; i++)
        {
            Note n = notes[i];
            float left = xs[i];

            if (n.isRest)
            {
                DrawRest(root, n, left);
                continue;
            }

            int pos = n.step - middle;
            float y = pos * Step;
            bool whole = n.BaseDuration >= 16;
            float headW = whole ? WholeHeadW : HeadW;

            DrawLedgerLines(root, pos, left, headW);
            Glyph(root, whole ? NoteheadWhole : n.BaseDuration >= 8 ? NoteheadHalf : NoteheadBlack, left, y);
            if (n.IsDotted)
                Glyph(root, AugDot, left + headW + 0.05f, (pos % 2 == 0 ? pos + 1 : pos) * Step);

            if (whole) continue;

            // unbeamed notes (quarters, halves, lone 8ths/16ths)
            int g = groupOf[i];
            if (g < 0 || groups[g].Count == 1)
            {
                bool down = pos >= 0;
                float tip = down ? Mathf.Min(y - StemLen, 0f) : Mathf.Max(y + StemLen, 0f);
                float sx = StemX(left, down);
                Stem(root, sx, y, tip, down);
                if (n.Beams > 0)
                {
                    string flag = n.Beams == 1 ? (down ? Flag8Down : Flag8Up) : (down ? Flag16Down : Flag16Up);
                    Glyph(root, flag, sx - StemW * 0.5f, tip);
                }
            }
        }

        foreach (var group in groups)
            if (group.Count > 1) DrawBeamGroup(root, notes, xs, group, middle);
    }

    // =====================================================================================

    static float SpacingWeight(int duration) => 1f + 0.65f * Mathf.Log(duration, 2);

    static float StemX(float headLeft, bool down) =>
        down ? headLeft + StemW * 0.5f : headLeft + HeadW - StemW * 0.5f;

    static void Stem(Transform root, float sx, float y, float tip, bool down)
    {
        float from = down ? y - StemAttachY : y + StemAttachY;
        Rect(root, sx, (from + tip) * 0.5f, StemW, Mathf.Abs(tip - from));
    }

    static void DrawBeamGroup(Transform root, List<Note> notes, float[] xs, List<int> group, int middle)
    {
        int minPos = int.MaxValue, maxPos = int.MinValue;
        foreach (int i in group)
        {
            int p = notes[i].step - middle;
            minPos = Mathf.Min(minPos, p);
            maxPos = Mathf.Max(maxPos, p);
        }
        bool down = maxPos + minPos >= 0;

        // flat beam at a comfortable distance from the outermost note
        float tip = down ? Mathf.Min(minPos * Step - StemLen, 0f) : Mathf.Max(maxPos * Step + StemLen, 0f);
        float dir = down ? 1f : -1f; // direction from beam towards noteheads

        var stemXs = new float[group.Count];
        for (int k = 0; k < group.Count; k++)
        {
            int i = group[k];
            float y = (notes[i].step - middle) * Step;
            stemXs[k] = StemX(xs[i], down);
            Stem(root, stemXs[k], y, tip, down);
        }

        // primary beam
        float primaryY = tip + dir * BeamT * 0.5f;
        BeamRect(root, stemXs[0] - StemW * 0.5f, stemXs[group.Count - 1] + StemW * 0.5f, primaryY);

        // secondary beams (sixteenths)
        float secondaryY = primaryY + dir * BeamGap;
        int k2 = 0;
        while (k2 < group.Count)
        {
            if (notes[group[k2]].Beams < 2) { k2++; continue; }
            int runStart = k2;
            while (k2 + 1 < group.Count && notes[group[k2 + 1]].Beams >= 2) k2++;
            int runEnd = k2;

            if (runEnd > runStart)
            {
                BeamRect(root, stemXs[runStart] - StemW * 0.5f, stemXs[runEnd] + StemW * 0.5f, secondaryY);
            }
            else
            {
                // single sixteenth in the group: partial beam pointing towards its neighbour
                float stub = Space * 1.1f;
                float sx = stemXs[runStart];
                if (runStart == 0) BeamRect(root, sx - StemW * 0.5f, sx + stub, secondaryY);
                else BeamRect(root, sx - stub, sx + StemW * 0.5f, secondaryY);
            }
            k2++;
        }
    }

    static void BeamRect(Transform root, float x1, float x2, float cy) =>
        Rect(root, (x1 + x2) * 0.5f, cy, x2 - x1, BeamT);

    static void DrawLedgerLines(Transform root, int pos, float headLeft, float headW)
    {
        float w = headW + 0.1f;
        float cx = headLeft + headW * 0.5f;
        for (int p = 6; p <= pos; p += 2) Rect(root, cx, p * Step, w, LedgerT);
        for (int p = -6; p >= pos; p -= 2) Rect(root, cx, p * Step, w, LedgerT);
    }

    static void DrawRest(Transform root, Note n, float left)
    {
        int b = n.BaseDuration;
        string glyph = b >= 16 ? RestWhole : b >= 8 ? RestHalf : b >= 4 ? RestQuarter : b >= 2 ? Rest8 : Rest16;
        int pos = b >= 16 ? 2 : 0;
        Glyph(root, glyph, left, pos * Step);
        if (n.IsDotted)
        {
            float w = b >= 8 ? 0.23f : b >= 4 ? 0.14f : 0.2f;
            Glyph(root, AugDot, left + w + 0.05f, 1 * Step);
        }
    }

    // =====================================================================================
    // primitive helpers

    static void CacheLineSprite(Measure measure)
    {
        if (lineSprite != null) return;
        SpriteRenderer sr = measure.leftLine != null ? measure.leftLine.GetComponent<SpriteRenderer>() : null;
        if (sr == null) sr = measure.GetComponentInChildren<SpriteRenderer>(true);
        if (sr != null)
        {
            lineSprite = sr.sprite;
            lineMaterial = sr.sharedMaterial;
            lineColor = sr.color;
        }
        else
        {
            var tex = Texture2D.whiteTexture;
            lineSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width);
        }
    }

    static void Rect(Transform parent, float cx, float cy, float w, float h)
    {
        var go = new GameObject("rect");
        go.transform.SetParent(parent, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = lineSprite;
        if (lineMaterial != null) sr.sharedMaterial = lineMaterial;
        sr.color = lineColor;
        sr.sortingOrder = 1;

        Bounds b = lineSprite.bounds;   // size/centre at scale 1
        var scale = new Vector3(w / b.size.x, h / b.size.y, 1f);
        go.transform.localScale = scale;
        go.transform.localPosition = new Vector3(cx - b.center.x * scale.x, cy - b.center.y * scale.y, 0f);
    }

    /// <summary>Places a Bravura glyph with its SMuFL origin (left edge, baseline) at (x, y).</summary>
    static TMP_Text Glyph(Transform parent, string glyph, float x, float y)
    {
        GameObject go = Object.Instantiate(NoteLibrary.notePrefab, parent);
        go.name = "glyph";
        var rt = (RectTransform)go.transform;
        rt.pivot = new Vector2(0f, 0.5f);
        rt.sizeDelta = new Vector2(2f, 2f);
        rt.localRotation = Quaternion.identity;
        rt.localScale = Vector3.one;
        rt.localPosition = new Vector3(x, y, 0f);

        var t = go.GetComponent<TMP_Text>();
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.overflowMode = TextOverflowModes.Overflow;
        t.alignment = TextAlignmentOptions.BaselineLeft;
        t.fontSize = GlyphSize;
        t.text = glyph;
        if (t is TextMeshPro tmp) tmp.sortingOrder = 2;
        return t;
    }
}
