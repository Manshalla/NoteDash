using UnityEngine;

public class Follower : MonoBehaviour
{
    public Vector3 offset;
    float relativeCount =0 ;

    // Update is called once per frame
    void Update()
    {
        relativeCount = Clock.count2;
        float position = relativeCount;
        float spacePerNote = ScreenManager.usableBounds.x/(Clock.numerator-1);
        Vector3 pos =  offset + new Vector3(spacePerNote * (position-1) - ScreenManager.usableBounds.x/2 -0.5f,0,0);

        this.transform.position = pos;
    }
}
