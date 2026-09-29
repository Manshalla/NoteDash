using UnityEngine;
using TMPro;
public class TextUpdater : MonoBehaviour
{
    public TMP_Text counter;

    // Update is called once per frame
    void Update()
    {
        if(StateManager.gameFinished == false)
        {
            counter.text = Mathf.Floor(Clock2.count).ToString();
        }
        else
        {
            counter.text = "";
        }
            
        
    }
}
