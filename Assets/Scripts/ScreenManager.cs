using UnityEngine;

public class ScreenManager : MonoBehaviour
{
    public static Vector3 bounds;
    public static Vector3 usableBounds;
    public void Awake()
    {
        Vector3 screenSize = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width,Screen.height,0));
        
        bounds = screenSize;
        bounds = new Vector3(3,0,0);
        usableBounds = bounds * 0.8f;
    }
}
