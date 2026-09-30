using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Note values in sixteenth-note units (sixteenth = 1, whole = 16).
/// </summary>
public enum NoteValue
{
    Whole = 16,
    DottedHalf = 12,
    Half = 8,
    DottedQuarter = 6,
    Quarter = 4,
    DottedEighth = 3,
    Eighth = 2,
    Sixteenth = 1
}

/// <summary>
/// Everything the player can choose in the pause menu. Static so the generator,
/// renderer, clock and menu all read the same values. Persisted with PlayerPrefs.
/// </summary>
public static class PracticeSettings
{
    public static readonly NoteValue[] AllValues =
    {
        NoteValue.Whole, NoteValue.DottedHalf, NoteValue.Half, NoteValue.DottedQuarter,
        NoteValue.Quarter, NoteValue.DottedEighth, NoteValue.Eighth, NoteValue.Sixteenth
    };

    public static readonly (int num, int den)[] TimeSignatures =
    {
        (2, 4), (3, 4), (4, 4), (2, 2), (3, 8), (6, 8), (9, 8), (12, 8)
    };

    public static readonly ClefType[] Clefs = { ClefType.Treble, ClefType.Bass, ClefType.Alto };

    static readonly string[] MajorNames = { "Cb", "Gb", "Db", "Ab", "Eb", "Bb", "F", "C", "G", "D", "A", "E", "B", "F#", "C#" };
    static readonly string[] MinorNames = { "Ab", "Eb", "Bb", "F", "C", "G", "D", "A", "E", "B", "F#", "C#", "G#", "D#", "A#" };

    // ---- current selection ----
    public static HashSet<NoteValue> Allowed = new HashSet<NoteValue> { NoteValue.Half, NoteValue.Quarter, NoteValue.Eighth };
    public static bool IncludeRests = true;
    public static bool LedgerLines = true;
    public static int Bpm = 80;
    public static int KeyFifths = 0;          // + = sharps, - = flats
    public static bool Minor = false;
    public static int ClefIndex = 0;          // into Clefs
    public static int TimeSigIndex = 2;       // into TimeSignatures (4/4)

    public const int MinBpm = 20, MaxBpm = 240;

    public static ClefType Clef => Clefs[ClefIndex];
    public static int TimeNum => TimeSignatures[TimeSigIndex].num;
    public static int TimeDen => TimeSignatures[TimeSigIndex].den;

    /// <summary>Compound meter (6/8, 9/8, 12/8, 3/8): beat = dotted quarter.</summary>
    public static bool Compound => TimeDen == 8 && TimeNum % 3 == 0;
    public static int MeasureUnits => TimeNum * 16 / TimeDen;
    public static int BeatUnits => Compound ? 6 : 16 / TimeDen;

    /// <summary>"G major" / "E minor" without the accidental count.</summary>
    public static string KeyShortName =>
        Minor ? MinorNames[KeyFifths + 7] + " minor" : MajorNames[KeyFifths + 7] + " major";

    public static string KeyName =>
        (Minor ? MinorNames[KeyFifths + 7] + " minor" : MajorNames[KeyFifths + 7] + " major") +
        (KeyFifths == 0 ? "" : KeyFifths > 0 ? $"  ({KeyFifths} sharp{(KeyFifths > 1 ? "s" : "")})"
                                              : $"  ({-KeyFifths} flat{(KeyFifths < -1 ? "s" : "")})");

    public static string ValueName(NoteValue v) => v switch
    {
        NoteValue.Whole => "Whole",
        NoteValue.DottedHalf => "Dotted half",
        NoteValue.Half => "Half",
        NoteValue.DottedQuarter => "Dotted quarter",
        NoteValue.Quarter => "Quarter",
        NoteValue.DottedEighth => "Dotted 8th",
        NoteValue.Eighth => "8th",
        NoteValue.Sixteenth => "16th",
        _ => v.ToString()
    };

    /// <summary>Pushes tempo / meter into the existing Clock.</summary>
    public static void ApplyToClock()
    {
        Clock.bpm = Bpm;
        Clock.num = TimeNum;
        Clock.dom = TimeDen;
    }

    // ---- persistence ----
    const string Prefix = "NoteDash.";

    public static void Save()
    {
        int mask = 0;
        foreach (var v in Allowed) mask |= 1 << (int)v;
        PlayerPrefs.SetInt(Prefix + "values", mask);
        PlayerPrefs.SetInt(Prefix + "rests", IncludeRests ? 1 : 0);
        PlayerPrefs.SetInt(Prefix + "ledger", LedgerLines ? 1 : 0);
        PlayerPrefs.SetInt(Prefix + "bpm", Bpm);
        PlayerPrefs.SetInt(Prefix + "key", KeyFifths);
        PlayerPrefs.SetInt(Prefix + "minor", Minor ? 1 : 0);
        PlayerPrefs.SetInt(Prefix + "clef", ClefIndex);
        PlayerPrefs.SetInt(Prefix + "time", TimeSigIndex);
        PlayerPrefs.Save();
    }

    public static void Load()
    {
        if (!PlayerPrefs.HasKey(Prefix + "values")) return;
        int mask = PlayerPrefs.GetInt(Prefix + "values");
        var set = new HashSet<NoteValue>();
        foreach (var v in AllValues) if ((mask & (1 << (int)v)) != 0) set.Add(v);
        if (set.Count > 0) Allowed = set;
        IncludeRests = PlayerPrefs.GetInt(Prefix + "rests", 1) == 1;
        LedgerLines = PlayerPrefs.GetInt(Prefix + "ledger", 1) == 1;
        Bpm = Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "bpm", Bpm), MinBpm, MaxBpm);
        KeyFifths = Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "key", 0), -7, 7);
        Minor = PlayerPrefs.GetInt(Prefix + "minor", 0) == 1;
        ClefIndex = Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "clef", 0), 0, Clefs.Length - 1);
        TimeSigIndex = Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "time", 2), 0, TimeSignatures.Length - 1);
    }
}
