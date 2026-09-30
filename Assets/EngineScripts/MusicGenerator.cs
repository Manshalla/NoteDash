using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Generates measures of random-but-musical material from PracticeSettings.
///
/// "Musical sense" rules used:
///  - Music is built in 4-measure phrases over a simple chord progression that ends on I.
///  - Rhythms respect the beat: notes don't cross beats awkwardly (no half note on beat 2 in 4/4, etc.).
///  - The 2nd measure of a phrase often repeats the 1st measure's rhythm (motif).
///  - Melody moves mostly by step, sometimes by 3rd, occasionally leaps; after a leap it steps back.
///  - Strong beats favour tones of the current chord; each phrase starts on a tonic chord tone
///    and ends on the tonic with a longer note.
/// </summary>
public class MusicGenerator
{
    static readonly int[][] Progressions =
    {
        new[] { 0, 3, 4, 0 },   // I  IV V I
        new[] { 0, 5, 4, 0 },   // I  vi V I
        new[] { 0, 1, 4, 0 },   // I  ii V I
        new[] { 0, 4, 3, 0 },   // I  V  IV I
        new[] { 0, 3, 0, 0 },   // I  IV I  I
    };

    readonly System.Random rng;
    int measureCounter;
    int[] progression;
    List<Note> phraseMotif;
    int current = int.MinValue;   // last pitch (absolute diatonic step)
    int lastInterval;

    // cached from settings
    readonly int L, B;
    readonly bool compound;
    readonly int[] allowed;
    readonly int tonicLetter, low, high, center;

    public MusicGenerator(int? seed = null)
    {
        rng = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
        L = PracticeSettings.MeasureUnits;
        B = PracticeSettings.BeatUnits;
        compound = PracticeSettings.Compound;
        allowed = PracticeSettings.Allowed.Select(v => (int)v).OrderByDescending(d => d).ToArray();
        if (allowed.Length == 0) allowed = new[] { 4 };

        int majorTonic = ((PracticeSettings.KeyFifths * 4) % 7 + 7) % 7;   // C=0 D=1 ... B=6
        tonicLetter = PracticeSettings.Minor ? (majorTonic + 5) % 7 : majorTonic;

        center = MeasureRenderer.ClefMiddleStep(PracticeSettings.Clef);
        int range = PracticeSettings.LedgerLines ? 7 : 5;   // staff steps from the middle line
        low = center - range;
        high = center + range;
    }

    // ------------------------------------------------------------------ public

    public List<Note> NextMeasure()
    {
        int phrasePos = measureCounter % 4;
        if (phrasePos == 0) progression = Progressions[rng.Next(Progressions.Length)];

        bool phraseStart = phrasePos == 0;
        bool cadence = phrasePos == 3;

        // ---- rhythm
        List<Note> notes;
        if (phrasePos == 1 && phraseMotif != null && rng.NextDouble() < 0.5)
            notes = phraseMotif.Select(n => n.Clone()).ToList();
        else
            notes = GenerateRhythm(cadence);

        if (phraseStart) phraseMotif = notes.Select(n => n.Clone()).ToList();
        ApplyRests(notes, phraseStart, cadence);

        // ---- pitch
        int chordRoot = progression[phrasePos];
        int lastSounding = notes.FindLastIndex(n => !n.isRest);
        for (int i = 0; i < notes.Count; i++)
        {
            Note n = notes[i];
            if (n.isRest) continue;

            if (current == int.MinValue || (phraseStart && i == 0))
                n.step = NearestChordTone(current == int.MinValue ? center : (current + center) / 2, 0, true);
            else if (cadence && i == lastSounding)
                n.step = NearestChordTone(current, 0, false, rootOnly: true);
            else
                n.step = NextPitch(IsStrongBeat(n.start), n.duration <= 2, chordRoot);

            lastInterval = current == int.MinValue ? 0 : n.step - current;
            current = n.step;
        }

        measureCounter++;
        return notes;
    }

    // ------------------------------------------------------------------ rhythm

    List<Note> GenerateRhythm(bool cadence)
    {
        var result = new List<Note>();
        int end = L;
        Note final = null;

        // In the cadence measure, end the phrase on a longer note (at least a beat if allowed).
        if (cadence)
        {
            var longOnes = allowed.Where(d => d >= B && d <= L && IsValid(L - d, d, L)).ToList();
            if (longOnes.Count > 0)
            {
                int d = longOnes[rng.Next(longOnes.Count)];
                final = new Note(L - d, d, false);
                end = L - d;
            }
        }

        int p = 0, prev = 0;
        while (p < end)
        {
            var candidates = allowed.Where(d => p + d <= end && IsValid(p, d, L)).ToList();
            if (candidates.Count == 0)
            {
                // Selected values can't fill this spot musically -> use the largest value that fits.
                int fallback = PracticeSettings.AllValues.Select(v => (int)v)
                    .Where(d => p + d <= end && IsValid(p, d, L)).DefaultIfEmpty(1).Max();
                candidates.Add(fallback);
            }

            // weights: prefer repeating the previous value, and slightly prefer longer notes on beats
            float total = 0;
            var w = new float[candidates.Count];
            for (int i = 0; i < candidates.Count; i++)
            {
                float weight = 1f;
                if (candidates[i] == prev) weight *= 1.8f;
                if (candidates[i] == L) weight *= 0.35f;          // whole-measure notes: not too often
                if (p % B == 0 && candidates[i] >= B) weight *= 1.2f;
                w[i] = weight;
                total += weight;
            }
            float r = (float)rng.NextDouble() * total;
            int pick = candidates[candidates.Count - 1];
            for (int i = 0; i < candidates.Count; i++)
            {
                r -= w[i];
                if (r <= 0) { pick = candidates[i]; break; }
            }

            result.Add(new Note(p, pick, false));
            p += pick;
            prev = pick;
        }

        if (final != null) result.Add(final);
        return result;
    }

    /// <summary>Is a note of length d allowed to start at position p (sixteenth units)?</summary>
    bool IsValid(int p, int d, int measure)
    {
        if (p + d > measure) return false;
        if (d == measure) return p == 0;

        int beatStart = p / B * B;
        bool withinBeat = p + d <= beatStart + B;

        if (withinBeat)
        {
            switch (d)
            {
                case 1: return true;
                case 2: return p % 2 == 0;
                case 3: return compound ? p % B == 0 : p % 4 == 0;          // dotted 8th + 16th
                case 4: return p % 4 == 0 || (compound && p % 2 == 0);     // 6/8: quarter-eighth or eighth-quarter
                case 6: return p % B == 0 || (!compound && p % 8 == 0);
                case 8: return p % 8 == 0;
                case 12: return p % B == 0;
                default: return false;
            }
        }

        // crossing a beat line: only if it starts on a beat and spans whole beats, without syncopation
        if (p % B == 0 && d % B == 0)
            return p % d == 0 || p + d == measure;

        // dotted quarter + eighth on a strong beat in simple time (e.g. beats 1 or 3 of 4/4)
        if (!compound && B == 4 && d == 6 && p % 8 == 0) return true;

        return false;
    }

    void ApplyRests(List<Note> notes, bool phraseStart, bool cadence)
    {
        if (!PracticeSettings.IncludeRests) return;
        bool prevRest = false;
        for (int i = 0; i < notes.Count; i++)
        {
            bool forbidden = (phraseStart && i == 0) || (cadence && i == notes.Count - 1) || prevRest
                             || notes[i].duration < 2;   // no sixteenth rests
            notes[i].isRest = !forbidden && rng.NextDouble() < 0.13;
            prevRest = notes[i].isRest;
        }
        // never a measure made only of rests unless it's a single whole-measure value
        if (notes.All(n => n.isRest) && notes.Count > 1) notes[0].isRest = false;
    }

    // ------------------------------------------------------------------ pitch

    static readonly int[] IntervalSizes = { 0, 1, 2, 3, 4, 5, 7 };
    static readonly float[] IntervalWeights = { 7, 46, 24, 9, 7, 3, 4 };
    static readonly float[] ShortNoteWeights = { 5, 62, 25, 5, 3, 0, 0 };   // fast notes: mostly steps

    /// <summary>Downbeat, plus the middle of the measure in even meters (beat 3 in 4/4, beat 2 in 6/8).</summary>
    bool IsStrongBeat(int p) => p == 0 || (L % (2 * B) == 0 && p == L / 2);

    int NextPitch(bool strongBeat, bool shortNote, int chordRoot)
    {
        int size;
        bool up;

        if (System.Math.Abs(lastInterval) >= 3 && rng.NextDouble() < 0.8)
        {
            // after a leap: step back in the opposite direction
            size = rng.NextDouble() < 0.75 ? 1 : 2;
            up = lastInterval < 0;
        }
        else
        {
            size = IntervalSizes[WeightedIndex(shortNote ? ShortNoteWeights : IntervalWeights)];
            // pull gently towards the middle of the range
            double pUp = 0.5 - (current - center) / (2.0 * (high - center)) * 0.7;
            up = rng.NextDouble() < pUp;
        }

        int cand = current + (up ? size : -size);
        if (cand > high || cand < low) cand = current + (up ? -size : size);   // reflect
        cand = System.Math.Clamp(cand, low, high);

        if (strongBeat && rng.NextDouble() < 0.7)
        {
            int snapped = NearestChordTone(cand, chordRoot, false);
            // don't let the snap turn an intended move into a repeated note
            if (snapped == current && cand != current)
                snapped = NearestChordTone(cand + (cand > current ? 1 : -1), chordRoot, false);
            if (snapped != current || cand == current) cand = snapped;
        }

        return cand;
    }

    int NearestChordTone(int from, int chordRoot, bool randomTriadTone, bool rootOnly = false)
    {
        from = System.Math.Clamp(from, low, high);
        if (randomTriadTone)
        {
            var options = new List<int>();
            for (int s = from - 3; s <= from + 3; s++)
                if (s >= low && s <= high && IsChordTone(s, chordRoot, false)) options.Add(s);
            if (options.Count > 0) return options[rng.Next(options.Count)];
        }

        int[] order = rng.Next(2) == 0 ? new[] { 0, 1, -1, 2, -2, 3, -3, 4, -4, 5, -5, 6, -6, 7, -7 }
                                        : new[] { 0, -1, 1, -2, 2, -3, 3, -4, 4, -5, 5, -6, 6, -7, 7 };
        foreach (int o in order)
        {
            int s = from + o;
            if (s >= low && s <= high && IsChordTone(s, chordRoot, rootOnly)) return s;
        }
        return from;
    }

    bool IsChordTone(int step, int chordRoot, bool rootOnly)
    {
        int degree = ((step - tonicLetter) % 7 + 7) % 7;
        int rel = ((degree - chordRoot) % 7 + 7) % 7;
        return rootOnly ? rel == 0 : (rel == 0 || rel == 2 || rel == 4);
    }

    int WeightedIndex(float[] weights)
    {
        float total = weights.Sum();
        float r = (float)rng.NextDouble() * total;
        for (int i = 0; i < weights.Length; i++)
        {
            r -= weights[i];
            if (r <= 0) return i;
        }
        return weights.Length - 1;
    }
}
