using UnityEngine;
using TMPro;
using UnityEditor.Rendering;
using System.Collections.Generic;

public class NoteSpawner : MonoBehaviour
{
    /*
    public float xMultiplier = 13;
    

    private List<Note> spawnedNotes;

    public void spawnNote(Note note)
    {   
        Vector3 spawnPos = new Vector3(note.count,0,0);
        note.noteObject = Instantiate(NoteLibrary.notePrefab,spawnPos,this.transform.rotation,this.transform);
        note.noteObject.GetComponent<TMP_Text>().text = NoteLibrary.getNote(note);
        note.noteObject.name = note.value.ToString() + ", " + note.globalCount; 
    }
    public List<Note> notes
    {
        get => spawnedNotes;
        set
        {
            if(spawnedNotes != null)
            {
                foreach (Note note in spawnedNotes) // clear old spawned notes
                {
                    Destroy(note.noteObject);
                }
            }
            

            spawnedNotes = value;

            foreach (Note note in spawnedNotes)
            {
                spawnNote(note);
            }
        }
    }
    */
}
