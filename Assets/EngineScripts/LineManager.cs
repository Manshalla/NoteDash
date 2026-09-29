using System.Collections.Generic;
using UnityEngine;

public class LineManager : MonoBehaviour
{
    public GameObject MeasurePrefab;
    public Transform linesParent;

    public Transform Cam;

    Vector3 lineSpawnPos;

    public static List<Transform> gespLines = new List<Transform>();

    public int measuresPerLine = 3;

    static float ySpacing = 2;
    float delta = 10;


    int lineIndex  = 0;

    private GameObject PagePrefab;

    MusicGenerator generator;

    public void Awake()
    {
        PracticeSettings.Load();
        PracticeSettings.ApplyToClock();
        generator = new MusicGenerator();
    }

    public void Update()
    {

        if(Cam.position.y - lineSpawnPos.y < delta)
        {
            //gespLines.Add(Instantiate(linePrefab,spawnPos,Quaternion.identity,lineParent).transform);
            GenerateLine();

            lineSpawnPos -= ySpacing*Vector3.up;
            lineIndex++;
        }

    }

    /// <summary>
    /// Throws away all lines and starts over with the current PracticeSettings
    /// (called from the pause menu). New lines are spawned again by Update().
    /// </summary>
    public void Regenerate()
    {
        foreach (Transform line in gespLines)
            if (line != null) Destroy(line.gameObject);
        gespLines.Clear();

        lineSpawnPos = Vector3.zero;
        lineIndex = 0;
        PracticeSettings.ApplyToClock();
        generator = new MusicGenerator();
    }

    private void GenerateLine()
    {
        GameObject line = new GameObject("Line " + lineIndex.ToString());
        if (linesParent != null) line.transform.SetParent(linesParent, true);
        gespLines.Add(line.transform);

        for(int i = 0; i< measuresPerLine; i++)
        {

            float xSize = 5f;
            Vector3 pos = new Vector3(i*xSize ,0,0);
            Measure measure = Instantiate( MeasurePrefab,pos,Quaternion.identity,line.transform).GetComponent<Measure>();

            if(i == measuresPerLine - 1)
            {
                measure.RightLine.SetActive(true);
            }
            else
            {
                measure.RightLine.SetActive(false);
            }

            bool showTimeSig = i == 0 && lineIndex == 0;
            if(showTimeSig)
            {
                measure.timeSignature = new TimeSignature(PracticeSettings.TimeNum, PracticeSettings.TimeDen);
            }
            else
            {
                measure.numC.text = "";
                measure.domC.text = "";
            }

            // fill the measure with generated notes (clef + key signature at the start of every line)
            MeasureRenderer.Render(measure, generator.NextMeasure(), i == 0, showTimeSig);
        }

        line.transform.position = lineSpawnPos;

    }

}
