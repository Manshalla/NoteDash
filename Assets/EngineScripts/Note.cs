using System.Threading;
using UnityEngine;
using TMPro;

public class Note 
{
    public float count;
    public float globalCount = 0; //actually not needed
    public int nenner;
    public int value;
    public bool pause;
    public bool point;
    public GameObject noteObject;
    private LineData lineData;

    private GameObject notePrefab = Resources.Load<GameObject>("Prefabs/Note");
    
    public Note(float count,int nenner,int height,bool pause)
    {

        this.nenner = nenner;
        this.pause = pause;
        this.count = count;
        this.point = false;
        spawnVisual();
    }
    public LineData lData
    {
        set
        {
            lineData = value;
            noteObject.transform.SetParent(lineData.lineObject.transform);
            //change position of note for example
        }
        get
        {
            return lineData;
        }
    }
    private void spawnVisual()
    {
        Transform parent = null;
        if(lineData != null)
        {
            parent = lineData.lineObject.transform;
        }
        Vector3 spawnPos = new Vector3(count,0,0);
        noteObject = Object.Instantiate(notePrefab,spawnPos,Quaternion.identity,parent);
        noteObject.GetComponent<TMP_Text>().text = NoteLibrary.getNote(this);
        noteObject.name = value.ToString() + ", " + count;
    }
    public float duration()
    {
        if (point)
        {
            return (lineData.timeSignature.denominator*1/(float)nenner) *1.5f;
        }
        return lineData.timeSignature.denominator*1/(float)nenner;
    }
}
