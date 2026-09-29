using UnityEngine;
using UnityEngine.InputSystem.Controls;
using TMPro;

public class Clock : MonoBehaviour
{
    public static int numerator = 4;
    public static int dominator = 4; // n/d
    public TMP_Text counter;

    public int bpm;
    private float startTime,startTime2 ;
    public static float count,count2;
    
    public bool reset;
    
    public static float allowedOffset = 0.1f; //1 equels 1 quarter note 
    // Update is called once per frame
    void Start()
    {
        startCounting();
        startTime2 = -0.5f;
    }
    void Update()
    {
        reset = false;
        float delta = Time.time-startTime;
        float delta2 = Time.time-startTime2;

        count = 1+ (delta / 60)*bpm;

        count2 = 1 + (delta2 / 60)*bpm;;
        if(count2 > numerator + 1)
        {
            startTime2 = Time.time;
        }
        
        if(count > numerator+1)
        {
            startCounting();
        }

        counter.text = Mathf.Floor(count).ToString();
    }
    public void startCounting()
    {
        startTime = Time.time;
        reset = true;

    }
    
    public static bool isInTime(float inputTime,float noteTime)
    {
        
        if(noteTime - allowedOffset < inputTime && noteTime + allowedOffset > inputTime)
        {
            return true;
        }
        if(noteTime*numerator - allowedOffset < inputTime && noteTime*numerator + allowedOffset > inputTime)
        {
            return true;
        }

        return false;
        
    }
}
