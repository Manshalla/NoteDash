using UnityEngine;

public enum ClefType
{
    Treble,
    Bass,
    Alto,
    Tenor
}
public class KeySignature
{
    public int Fifths { get; set; } // sharps positive, flats negative

    public KeySignature(int fifths)
    {
        Fifths = fifths;
    }
}
public class TimeSignature
{
    public int numerator { get; set; }
    public int denominator { get; set; }

    public TimeSignature(int numerator, int denominator)
    {
        this.numerator = numerator;
        this.denominator = denominator;
    }
}
