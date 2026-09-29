using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
public class Tester : MonoBehaviour
{
    private LineData L1;
    public GameObject lowerLeft,upperRight;
    Note note;
    int y = 0;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        L1 = new LineData(3,4,1,new Vector3(-5,0,0));
        LineData L2 = new LineData(3,4,1,new Vector3(1,-4,0));

        //L1.addNote(new Note(1,4,1,false));
        
        note = new Note(1,4,0,true);
        Note note2 = new Note(2,4,0,false);
        
        
        L2.addNote(note,1);
        L2.addNote(new Note(3,4,0,false),0);
        L2.addNote(new Note(4,4,0,false),0);
        L2.addNote(note2,0);

    }
    void Update()
    {
        lowerLeft.transform.position = CamMover.bottomLeft;
        upperRight.transform.position = CamMover.topRight;  
        
         if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
           L1.addNote(note,0.5f);
        }
    }
}
