using System.Collections.Generic;

using UnityEngine;

public class LineData
{
    public List<Note> notes = new List<Note>();
    public int line;
    public TimeSignature timeSignature;
    public GameObject lineObject;
    private static GameObject lineAsset = Resources.Load<GameObject>("Prefabs/lineObject");
    private float xMin= -1;
    private float xMax = 5;
    public bool animated;

    public LineData(int num,int dom, int line,Vector3 spawnPos,params Note[] notesToAdd) // params like *args in python
    {
        
        this.timeSignature = new TimeSignature(num,dom);
        this.line = line;

        spawnLine(spawnPos);

        foreach(Note note in notesToAdd)
        {
            addNote(note);
        }
        
    }
    public void spawnLine(Vector3 spawnPos)
    {
        lineObject = Object.Instantiate(lineAsset,spawnPos,Quaternion.identity);
        lineObject.GetComponent<LineManager>().timeSignature = timeSignature;
        lineObject.name = "Line: " + line; 
    }
    public void addNote(Note note,float time = 0)
    {
        notes.Add(note);
        Vector3 posInCleff = new Vector3(countToPosition(note),heightToPosition(note),0);
    
        note.noteObject.GetComponent<Entity>().Animate(posInCleff,time);
        if(note.lData != null)
        {
            note.lData.removeNote(note);
        }
        note.lData = this;
    }
    public void removeNote(Note note)
    {
        notes.Remove(note);
    }
    private float countToPosition(Note note)
    {
        float f =  xMin + (xMax-xMin)/timeSignature.numerator* (note.count-1);
        return lineObject.transform.position.x + f;
    }
    private float heightToPosition(Note note)
    {
        return lineObject.transform.position.y + 0;
    }
    
}
