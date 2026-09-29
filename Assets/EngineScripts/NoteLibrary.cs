using UnityEditor;
using UnityEngine;

public class NoteLibrary
{
    public static GameObject notePrefab = Resources.Load<GameObject>("Prefabs/Note");
    public static string getNote(Note note)
    {
        if(!note.pause && !note.point)
        {
            switch (note.nenner)
            {
                case 1:
                    return "\U0001D15D"; // ♩ Viertelnote (du kannst hier eigene für ganze Note setzen)
                case 2:
                    return "\U0001D15E"; // 𝅝 Halbe Note
                case 4:
                    return "\u2669"; // ♩ Viertelnote
                case 8:
                    return "\u266A"; // ♪ Achtelnote
                case 16:
                    return "\u266B"; // ♫ Sechzehntelnote
                case 32:
                    return "\u266C"; // ♬ Zweiunddreißigstelnoten
                default:
                    return "?"; // unbekannter Wert
            }
        }
        if(note.pause && !note.point) // pause aber nicht punktiert
        {
            switch (note.nenner)
            {
                case 1:
                    return "\uE4E3";

                case 2:
                    return "\uE4E4";

                case 4:
                    return "\uE4E5";

                case 8:
                    return "\uE4E6";

                case 16:
                    return "\uE4E7";

            }
        }
        return "KA";
    }
    public static string getNumber(int number)
    {
        switch (number)
        {
        case 0: return "\uE080";
        case 1: return "\uE081";
        case 2: return "\uE082";
        case 3: return "\uE083";
        case 4: return "\uE084";
        case 5: return "\uE085";
        case 6: return "\uE086";
        case 7: return "\uE087";
        case 8: return "\uE088";
        case 9: return "\uE089";
        default:
            return "-1";
        }
    }

}
