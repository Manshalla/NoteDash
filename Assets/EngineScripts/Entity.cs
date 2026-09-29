using UnityEngine;

public class Entity : MonoBehaviour
{
    private bool move = false;
    private Vector3 startPos,desiredPos;
    private float t;
    private float tmax = 1;
    public AnimationCurve movementCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        public Vector3 smoothPosition
    {
        set
        {
            move = true;
            startPos = this.transform.position;
            desiredPos = value;
        }
    }
    public void Update()
    {
        if (move)
        {
            Vector3 delta = desiredPos-startPos;
            this.gameObject.transform.position = startPos + delta * movementCurve.Evaluate((t/tmax));
            t+=Time.deltaTime;
            if (t >= tmax)
            {
                move = false;
                t=0;
            }
        }
    }
    public void Animate(Vector3 to, float time)
    {
        if(time < 0.1f)
        {
            this.gameObject.transform.position = to;
            return;
        }
        tmax = time;
        this.smoothPosition = to;
    }
    
}
