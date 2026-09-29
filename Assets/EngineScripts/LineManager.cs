using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.VisualScripting;

public class LineManager : MonoBehaviour
{
    public TMP_Text numC,domC;
    private TimeSignature curSignature;

    public int beatsThisLine = 4;

    public TimeSignature timeSignature
    {
        get=>curSignature;
        set
        {
            curSignature = value;
            domC.text = NoteLibrary.getNumber(curSignature.denominator);
            numC.text = NoteLibrary.getNumber(curSignature.numerator);
        }
    }
}


    