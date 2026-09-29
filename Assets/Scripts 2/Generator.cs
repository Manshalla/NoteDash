
using System.Globalization;
using TMPro.EditorUtilities;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

public class Generator
{
    /*
    private static float globalPosition = 1;
    private static Note generateNote(float globalPosition,int num,int dom,int line)
    {
        //bool pause = false;
        if (Random.Range(0, 1) == 1)
        {
           pause = true; 
        }
        if(line == 0)
        {
            //return new Note(globalPosition,dom,1,true,num,dom);
        }
        //return new Note(globalPosition,4,1,pause,num,dom);
    }
    private static Note[] arrayAddNote(Note[] array, Note note)
    {  
        Note[] temp =  new Note[array.Length+1];
        
        for(int i = 0; i < array.Length; i++)
        {
            temp[i] = array[i];
        }
        temp[temp.Length-1] = note;
        return temp;
    }
    private static void printNotes(Note[] notes)
    {
    foreach(Note note in notes)
        {
            if(note == null)
            {
                Debug.Log("Null");
                return;
            }
            Debug.Log(note.count);
        }
    }
    public static Note[] generateNotes(int line,int num,int dom)
    {
        Note[] temp = new Note[0];
        float localCount =1;

        while (localCount < num+1)
        {
            Note newNote = generateNote(globalPosition,num,dom,line);
            localCount += newNote.duration;
            globalPosition += newNote.duration;
            if(localCount > num+1)
            {
                break;
            }
            //newNote.line = line;
            temp = arrayAddNote(temp,newNote);
        }
        return temp;
    }
    */
}
