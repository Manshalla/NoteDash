
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
public class GameManager : MonoBehaviour
{
    public GameObject gradeObject;
    public Sprite right,wrong;
    Note nextNote,prevNote = null;
    public int curLine = 0;
    public GameObject curLineObject;
    public static int nLines = 20;
    private Note closestNote;

    public AudioSource winSource;


    void Update()
    {
        if (StateManager.playing)
        {
           gameLoop();
        }
        if (Keyboard.current.spaceKey.wasPressedThisFrame && StateManager.playing != true)
        {
           startGame();
        }
        
    }
    private void gameLoop()
    {
        nextNote = NoteCollector.getNextNote(Clock2.globalCount);
        closestNote = NoteCollector.getClosestNote(Clock2.globalCount);

         if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
            grade(checkNote(nextNote),nextNote);
            }
        
        //if (closestNote != null && curLine < closestNote.line)
            {
                //Debug.Log(closestNote.noteObject);
             nextLine();
            }
        
        if(curLine>= nLines)
        {
            StateManager.playing = false;
            StateManager.gameFinished = true;
            Debug.Log("Game Finished");
        }
        
    }
    private void startGame()
    {

        StateManager.playing = true;
        Clock2.switchState();
    }
    private void nextLine()
    {
        //this.gameObject.GetComponent<CamMover>().nextLine();
        curLine++;


    }

    bool checkNote(Note note)
    {/*
        if (note == null||note.pause)
        {
            return false;
        }   
        return Mathf.Abs(note.globalCount - Clock2.globalCount) < 0.2f ;
        */
        return false;
    }
    private void grade(bool result,Note note)
    {
        if(note == null)
        {
            return;
        }
        int prefix = -1;
        /*if(note.globalCount - Clock2.globalCount < 0)
        {
            prefix = 1;
        }
        */
        Vector3 pos = new Vector3(0,1,0) + note.noteObject.transform.position + new Vector3(prefix*Clock2.globalToLocalCount(Mathf.Abs(note.globalCount - Clock2.globalCount)),0,0);
        Sprite temp = null;
        temp = wrong;
        if (result)
        {
            temp = right;
            winSource.Play();
        }
        Instantiate(gradeObject,pos,this.transform.rotation,note.noteObject.transform).GetComponent<SpriteRenderer>().sprite = temp;
    }
}
