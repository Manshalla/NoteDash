using UnityEngine;

public static class ClefRenderer
{
    public static string GetClefGlyph(ClefType clefType)
    {
        return clefType switch
        {
            ClefType.Treble => "\uE050",
            ClefType.Bass   => "\uE062",
            ClefType.Alto   => "\uE05C",
            ClefType.Tenor  => "\uE05D",
            _ => ""
        };
    }

    public static string GetSharpGlyph() => "\uE262";
    public static string GetFlatGlyph() => "\uE260";
}
