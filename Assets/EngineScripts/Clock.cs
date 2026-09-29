using System.Globalization;
using UnityEngine;

public class Clock : MonoBehaviour
{
    public static float count,globalCount;
    private float startCount,firstStart;
    public static int bpm = 60;
    public static int num = 6,dom = 8;
    public float pubCount;
    public float testGlobal,testLine;
    public float prevCount =1;
    private static bool counting = true;

   
    public void Update()
    {
        if (counting)
        {
            count = globalToLocalCount(globalCount);
            globalCount = 1+ (float)bpm/60 * Time.time -firstStart;
            testLine = globalToLocalCount(testGlobal);
            
        }
        else
        {
            count = 1;
            globalCount = 1;
            startCount = Time.time;
            firstStart = Time.time;
        }
        pubCount =globalCount;
       
    }
    public static float globalToLocalCount(float globCount)//shouldwork
    {  
        while( globCount >=num+1 )
        {
            globCount= globCount-num;
        }
        return globCount;
    }
    public static int curLineFromGlobal(float globalPosition)
    {
        int i = 0;
        while(globalPosition >= num+1)
        {
            globalPosition= globalPosition-num; // change this function later
            i++;
        }
        return i;
    }
    public static void switchState()
    {
        counting = !counting;
        
    }
    public static bool getState()
    {
        return counting;
    }

}
