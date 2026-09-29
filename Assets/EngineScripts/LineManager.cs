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

    private void GenerateLine()
    {
        GameObject line = new GameObject("Line " + lineIndex.ToString());

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


            if(i==0 && lineIndex == 0)
            {


            }
            else
            {
                measure.numC.text = "";
                measure.domC.text = "";
            }
        }

        line.transform.position = lineSpawnPos;
 
    }

}
