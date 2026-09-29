using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.InputSystem;

public class CamMover : MonoBehaviour
{
    Entity entity;
    int lineStartCount = 0;

    public int index = 0;
    public void Start()
    {
        entity = this.GetComponent<Entity>();
    }
    public void Update()
    {
        if(LineSpawner.gespLines.Count > index && lineStartCount + LineSpawner.gespLines[index].GetComponent<LineManager>().beatsThisLine - 1 
                    < Clock.globalCount)
        {
            lineStartCount += LineSpawner.gespLines[index].GetComponent<LineManager>().beatsThisLine;
            index++;
            Vector3 linePos = LineSpawner.gespLines[index].position;
            entity.Animate(new Vector3(linePos.x,linePos.y,this.transform.position.z),1);
        }
    }
}
