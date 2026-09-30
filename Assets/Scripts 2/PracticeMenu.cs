using TMPro;
using UnityEngine;

/// <summary>
/// Sight-reading options in the pause (Escape) panel.
///
/// Nothing is created at runtime: build the panel once with the menu
/// "NoteDash > Build Practice Menu" (or make your own), then restyle it freely in the inspector.
/// Buttons are found automatically via their PracticeOptionButton component; this script only
/// changes colours, texts and glyph positions.
///
/// Every change is applied immediately and the music behind the menu is regenerated.
/// </summary>
public class PracticeMenu : MonoBehaviour
{
    [Header("Theme (used for toggle buttons)")]
    public Color selectedBackground = new Color32(0xD6, 0xB2, 0x6A, 0xFF);
    public Color unselectedBackground = new Color32(0xEA, 0xDD, 0xC0, 0xFF);
    public Color selectedInk = new Color32(0x2A, 0x1B, 0x10, 0xFF);
    public Color unselectedInk = new Color32(0x2A, 0x1B, 0x10, 0x55);

    [Header("Labels")]
    [Tooltip("Bravura metronome note (quarter / half / eighth depending on the metre)")]
    public TMP_Text tempoGlyph;
    [Tooltip("Shows \"= 80\"")]
    public TMP_Text tempoLabel;
    [Tooltip("Shows \"G major\"")]
    public TMP_Text keyLabel;
    [Tooltip("Optional: shows \"Treble\" etc.")]
    public TMP_Text clefLabel;
    [Tooltip("Optional: shows \"6/8\" etc.")]
    public TMP_Text timeLabel;

    [Header("Staff preview (Bravura glyphs)")]
    public RectTransform[] staffLines;
    public TMP_Text clefGlyph;
    public TMP_Text[] accidentals = new TMP_Text[7];
    public TMP_Text timeSigNumerator;
    public TMP_Text timeSigDenominator;
    [Tooltip("Distance between two staff lines in pixels. Glyph size follows (font size = 4 spaces).")]
    public float staffSpace = 16f;
    public float clefX = 14f;
    public float keySignatureX = 72f;
    public float sharpSpacing = 18f;
    public float flatSpacing = 16f;
    public float timeSignatureGap = 16f;

    PracticeOptionButton[] buttons;
    LineManager lineManager;

    void Awake()
    {
        buttons = GetComponentsInChildren<PracticeOptionButton>(true);
        foreach (var b in buttons) b.Bind(this);
    }

    void OnEnable() => Refresh();

    // ------------------------------------------------------------------ actions

    public void Perform(PracticeOptionButton b)
    {
        switch (b.action)
        {
            case PracticeOptionButton.OptionAction.ToggleNoteValue:
                if (PracticeSettings.Allowed.Contains(b.noteValue))
                {
                    if (PracticeSettings.Allowed.Count == 1) return;   // keep at least one value
                    PracticeSettings.Allowed.Remove(b.noteValue);
                }
                else PracticeSettings.Allowed.Add(b.noteValue);
                break;
            case PracticeOptionButton.OptionAction.ToggleRests:
                PracticeSettings.IncludeRests = !PracticeSettings.IncludeRests; break;
            case PracticeOptionButton.OptionAction.ToggleLedgerLines:
                PracticeSettings.LedgerLines = !PracticeSettings.LedgerLines; break;
            case PracticeOptionButton.OptionAction.SetMajor:
                PracticeSettings.Minor = false; break;
            case PracticeOptionButton.OptionAction.SetMinor:
                PracticeSettings.Minor = true; break;
            case PracticeOptionButton.OptionAction.ChangeTempo:
                PracticeSettings.Bpm = Mathf.Clamp(PracticeSettings.Bpm + b.amount, PracticeSettings.MinBpm, PracticeSettings.MaxBpm);
                PracticeSettings.ApplyToClock();
                PracticeSettings.Save();
                Refresh();
                return;   // tempo doesn't change the notes
            case PracticeOptionButton.OptionAction.ChangeKey:
                PracticeSettings.KeyFifths = Mathf.Clamp(PracticeSettings.KeyFifths + b.amount, -7, 7); break;
            case PracticeOptionButton.OptionAction.ChangeClef:
                PracticeSettings.ClefIndex = Wrap(PracticeSettings.ClefIndex + b.amount, PracticeSettings.Clefs.Length); break;
            case PracticeOptionButton.OptionAction.ChangeTimeSignature:
                PracticeSettings.TimeSigIndex = Wrap(PracticeSettings.TimeSigIndex + b.amount, PracticeSettings.TimeSignatures.Length); break;
            case PracticeOptionButton.OptionAction.NewExercise:
                Regenerate();
                return;
            case PracticeOptionButton.OptionAction.Resume:
                Resume();
                return;
        }

        PracticeSettings.Save();
        Regenerate();
        Refresh();
    }

    /// <summary>Resume button: lets InputHandler unpause and move the camera back. The panel stays visible.</summary>
    public void Resume()
    {
        PracticeSettings.Save();
        InputHandler handler = FindFirstObjectByType<InputHandler>();
        if (handler != null) handler.ResumeGame();
        else if (!Clock.getState()) Clock.switchState();
    }

    public void Regenerate()
    {
        if (lineManager == null) lineManager = FindFirstObjectByType<LineManager>();
        if (lineManager != null) lineManager.Regenerate();
        else PracticeSettings.ApplyToClock();
    }

    static int Wrap(int value, int count) => ((value % count) + count) % count;

    // ------------------------------------------------------------------ display

    public void Refresh()
    {
        if (buttons == null) buttons = GetComponentsInChildren<PracticeOptionButton>(true);

        foreach (var b in buttons)
        {
            if (!b.IsToggle) continue;
            bool on = b.action switch
            {
                PracticeOptionButton.OptionAction.ToggleNoteValue => PracticeSettings.Allowed.Contains(b.noteValue),
                PracticeOptionButton.OptionAction.ToggleRests => PracticeSettings.IncludeRests,
                PracticeOptionButton.OptionAction.ToggleLedgerLines => PracticeSettings.LedgerLines,
                PracticeOptionButton.OptionAction.SetMajor => !PracticeSettings.Minor,
                PracticeOptionButton.OptionAction.SetMinor => PracticeSettings.Minor,
                _ => false
            };
            b.SetSelected(on, this);
        }

        if (tempoGlyph != null) tempoGlyph.text = MetronomeGlyph(PracticeSettings.TimeDen);
        if (tempoLabel != null) tempoLabel.text = "= " + PracticeSettings.Bpm;
        if (keyLabel != null) keyLabel.text = PracticeSettings.KeyShortName;
        if (clefLabel != null) clefLabel.text = PracticeSettings.Clef.ToString();
        if (timeLabel != null) timeLabel.text = PracticeSettings.TimeNum + "/" + PracticeSettings.TimeDen;

        RefreshStaffPreview();
    }

    static string MetronomeGlyph(int denominator) => denominator switch
    {
        2 => "\uECA3",   // metNoteHalfUp
        8 => "\uECA7",   // metNote8thUp
        _ => "\uECA5"    // metNoteQuarterUp
    };

    void RefreshStaffPreview()
    {
        float step = staffSpace * 0.5f;
        float fontSize = staffSpace * 4f;

        if (staffLines != null)
            for (int i = 0; i < staffLines.Length; i++)
                if (staffLines[i] != null)
                    staffLines[i].anchoredPosition = new Vector2(staffLines[i].anchoredPosition.x, (i - 2) * staffSpace);

        ClefType clef = PracticeSettings.Clef;
        Place(clefGlyph, ClefRenderer.GetClefGlyph(clef), clefX, MeasureRenderer.ClefGlyphPosition(clef) * step, fontSize);

        int fifths = PracticeSettings.KeyFifths;
        bool sharps = fifths > 0;
        float x = keySignatureX;
        for (int i = 0; i < accidentals.Length; i++)
        {
            TMP_Text acc = accidentals[i];
            if (acc == null) continue;
            bool visible = i < Mathf.Abs(fifths) && i < 7;
            acc.gameObject.SetActive(visible);
            if (!visible) continue;
            Place(acc, sharps ? ClefRenderer.GetSharpGlyph() : ClefRenderer.GetFlatGlyph(),
                  x, MeasureRenderer.KeySignaturePosition(i, sharps, clef) * step, fontSize);
            x += sharps ? sharpSpacing : flatSpacing;
        }

        // Time-signature texts are centre-aligned, so x is the middle of the digits.
        // SMuFL digits are centred on the baseline: numerator on the 4th line, denominator on the 2nd.
        x += timeSignatureGap + staffSpace;
        Place(timeSigNumerator, NoteLibrary.getNumberString(PracticeSettings.TimeNum), x, 2 * step, fontSize);
        Place(timeSigDenominator, NoteLibrary.getNumberString(PracticeSettings.TimeDen), x, -2 * step, fontSize);
    }

    static void Place(TMP_Text t, string glyph, float x, float y, float fontSize)
    {
        if (t == null) return;
        t.text = glyph;
        t.fontSize = fontSize;
        t.rectTransform.anchoredPosition = new Vector2(x, y);
    }
}
