using UnityEngine;

/// <summary>
/// One rhythmic event inside a measure (a note or a rest).
/// Times are in sixteenth-note units, pitch is a diatonic step (C0 = 0, D0 = 1 ... C4 = 28).
/// The key signature supplies the sharps/flats, so no accidentals are stored.
/// </summary>
public class Note
{
    public int start;     // position in the measure (0 = beat 1)
    public int duration;  // 16 = whole, 8 = half, 4 = quarter, 2 = eighth, 1 = sixteenth, dotted = x1.5
    public bool isRest;
    public int step;      // absolute diatonic step

    public Note(int start, int duration, bool isRest)
    {
        this.start = start;
        this.duration = duration;
        this.isRest = isRest;
    }

    public bool IsDotted => duration == 3 || duration == 6 || duration == 12;

    /// <summary>Duration without the dot (dotted quarter -> quarter).</summary>
    public int BaseDuration => IsDotted ? duration * 2 / 3 : duration;

    /// <summary>Number of beams/flags (eighth = 1, sixteenth = 2).</summary>
    public int Beams => BaseDuration == 2 ? 1 : BaseDuration == 1 ? 2 : 0;

    public Note Clone() => new Note(start, duration, isRest) { step = step };
}
