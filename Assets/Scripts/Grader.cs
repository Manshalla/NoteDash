using NUnit.Framework.Interfaces;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
public class Grader : MonoBehaviour
{
    public InputAction jump;
    public GameObject result;
    public Sprite right,wrong;
    public GameObject[] gespResults = new GameObject[30];
    public Clock clock;
    private int i = 0;
    public void Start()
    {

    }
    /*
    public void Update()
    {
    if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {

            if (checkNotes())
            {
                GameObject gespRes = Instantiate(result,this.transform.position,this.transform.rotation);
                gespRes.GetComponent<SpriteRenderer>().sprite = right;
                addResults(gespRes);
            }
            else
            {
                GameObject gespRes = Instantiate(result,this.transform.position,this.transform.rotation);
                gespRes.GetComponent<SpriteRenderer>().sprite = wrong;
                addResults(gespRes);
            }
        }
        if(clock.reset)//change this in future
        {
            foreach(GameObject entity in gespResults)
            {
                Destroy(entity);
                i = 0;
            }
        }
    }
    
    public bool checkNotes()
    {
    foreach (Note note in currentLine.notes)
        {
            if( note == null)
            {
                continue;
            }
            else
            {
                if (Clock.isInTime(Clock.count, note.position.x))
                {
                    return true;
                }
            }
            
        }
        return false;

    }
    */
    public void addResults(GameObject result)
    {
        gespResults[i] = result;
        i++;
    }
    
   
}
