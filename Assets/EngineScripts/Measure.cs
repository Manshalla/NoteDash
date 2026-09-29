using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.VisualScripting;

public class Measure : MonoBehaviour
{
    public TMP_Text numC,domC;
    int numerator,denominator;
    private TimeSignature curSignature;
    public GameObject leftLine,RightLine;




    public TimeSignature timeSignature
    {
        get=>curSignature;
        set
        {
            curSignature = value;
            domC.text = NoteLibrary.getNumberString(curSignature.denominator);
            numC.text = NoteLibrary.getNumberString(curSignature.numerator);
        }
    }
}



