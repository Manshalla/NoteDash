using UnityEngine;

public class NoteCollector
{
    public static Note[] noteList = new Note[1000];
    public static int i = 0;

    public static void addNote(Note note)
    {
        if(i < noteList.Length)
        {
            noteList[i] = note;
            i++;
            
        }
        else
        {
            i = 0;
            noteList[i] = note;
            i++;
        }
    }
    public static Note getNextNote(float gloablCount)
    {
        float min  = float.MaxValue;
        Note temp = null;

        foreach(Note note in noteList)
        {
            if(note == null)
            {
                continue;
            }
            float delta = note.globalCount-gloablCount;
            delta = Mathf.Abs(delta);
            
            if (min > delta && min>=0)
            {
                min = delta;
                temp = note; 
            }
             
        }
        return temp;

    }
    public static Note getPrevNote(float gloablCount)
    {
        float max  = float.MinValue;
        Note temp = null;

        foreach(Note note in noteList)
        {
            if(note == null)
            {
                continue;
            }
            float delta = note.globalCount-gloablCount;
            delta = Mathf.Abs(delta);
            
            if (max < delta && max<0)
            {
                max = delta;
                temp = note;
                
            }
             
        }
        return temp;

    }
    public static Note getClosestNote(float gloablCount)
    {
        
        Note nextNote = getNextNote(gloablCount);
        Note prevNote = getPrevNote(gloablCount);
        if(nextNote == null|| prevNote == null)
        {
            return null;
        }

        float delta1 = gloablCount - nextNote.globalCount;
        float delta2 = gloablCount - prevNote.globalCount;
        delta1 = Mathf.Abs(delta1);
        delta2 = Mathf.Abs(delta2);

        
        if (delta1 < delta2)
        {
            return nextNote;
        }
        else
        {
            return prevNote;
        }

    }
}
