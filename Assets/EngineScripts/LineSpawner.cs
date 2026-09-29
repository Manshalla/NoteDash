using System.Collections.Generic;
using UnityEngine;

public class LineSpawner : MonoBehaviour
{
    public GameObject linePrefab;
    public Transform lineParent;

    public Transform Cam;

    Vector3 spawnPos;

    public static List<Transform> gespLines = new List<Transform>();

    static float spacing = 2;
    float delta = 10;


    public void Update()
    {
        if(Cam.position.y - spawnPos.y < delta)
        {
            gespLines.Add(Instantiate(linePrefab,spawnPos,Quaternion.identity,lineParent).transform);

            spawnPos -= spacing*Vector3.up;
        }

    }



}
