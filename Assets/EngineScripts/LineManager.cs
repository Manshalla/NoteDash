using System.Collections.Generic;
using UnityEngine;

public class LineManager : MonoBehaviour
{
    public GameObject MeasurePrefab;
    public Transform curPage;

    public Transform Cam;

    Vector3 lineSpawnPos;
    Vector3 pageSpawnPos = new Vector3();

    public static List<Transform> gespLines = new List<Transform>();

    public int measuresPerLine = 3;
    public int linesPerPage = 7;

    public float lineYSpacing;
    float pageYSpacing = 25;
    float delta = 10;


    int lineIndex  = 0;
    int pageIndex = 0;

    public Vector3 pagePosition;

    public GameObject PagePrefab;

    MusicGenerator generator;

    // every measure that has been spawned, in reading order (needed to refill them on Regenerate)
    readonly List<(Measure measure, bool lineStart)> spawnedMeasures = new List<(Measure, bool)>();

    public void Awake()
    {
        PracticeSettings.Load();
        PracticeSettings.ApplyToClock();
        generator = new MusicGenerator();
    }
    public void Start()
    {
        GameObject page = generatePage();

        page. transform.position = pagePosition;
    }

    /// <summary>
    /// Refills every measure that already exists (on all pages) with new music from the current
    /// PracticeSettings. Pages, lines and the camera stay where they are; pages spawned later
    /// simply continue with the same generator. Called from the pause menu.
    /// </summary>
    /// 
    public GameObject generatePage()
    {
        curPage = Instantiate(PagePrefab,pageSpawnPos,Quaternion.identity,this.transform).transform;
            pageSpawnPos -= Vector3.up*pageYSpacing;


            for (int i = 0; i < linesPerPage; i++)
            {
                //gespLines.Add(Instantiate(linePrefab,spawnPos,Quaternion.identity,lineParent).transform);

                lineSpawnPos = curPage.position + new Vector3(-5,9 - lineYSpacing * i,3);
                GenerateLine();

                
                lineIndex++;
            }
        return curPage.gameObject;
    }
    public void Regenerate()
    {
        PracticeSettings.ApplyToClock();
        generator = new MusicGenerator();

        spawnedMeasures.RemoveAll(entry => entry.measure == null);
        for (int k = 0; k < spawnedMeasures.Count; k++)
        {
            Measure measure = spawnedMeasures[k].measure;
            bool showTimeSig = k == 0;
            if (showTimeSig)
                measure.timeSignature = new TimeSignature(PracticeSettings.TimeNum, PracticeSettings.TimeDen);

            MeasureRenderer.Render(measure, generator.NextMeasure(), spawnedMeasures[k].lineStart, showTimeSig);
        }
    }

    private void GenerateLine()
    {
        GameObject line = new GameObject("Line " + lineIndex.ToString());
        if (curPage != null) line.transform.SetParent(curPage, true);
        
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
            spawnedMeasures.Add((measure, i == 0));
        }

        line.transform.position = lineSpawnPos;

    }

}
