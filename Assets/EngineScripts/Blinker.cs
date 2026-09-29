using UnityEngine;

public class Blinker : MonoBehaviour
{
    int prevCount =0;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    // Update is called once per frame
    SpriteRenderer sRenderer;
    bool blink;
    float t = 0f;
    float blinkTime = 0.25f;
    private Color blinkCol;
    public AudioSource source;
    
    void Start()
    {
        sRenderer = this.GetComponent<SpriteRenderer>();
        blinkCol = sRenderer.color;
        //source = this.GetComponent<AudioSource>();
        
    }
    void Update()
    {
        if(StateManager.playing && prevCount< (int)Mathf.Floor(Clock.globalCount))
        {
            prevCount = (int)Mathf.Floor(Clock.globalCount);
            blinkVoid();
        }
        float b = Mathf.Cos(Clock.count*2*Mathf.PI);

        if (b < 0)
        {
            b=0;
        }
        sRenderer.color = blinkCol*new Color(1,1,1,b);

        
    }
    private void blinkVoid()
    {
        Debug.Log("Blink");
        source.Play();
    }
}
