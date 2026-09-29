using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.InputSystem;

public class CamMover : MonoBehaviour
{
    public Camera cam;
    public Entity entity;

public static Vector3 topRight,bottomLeft;
public void Start()
    {
        entity = this.GetComponent<Entity>();
    }
    public void Update()
    {
        float height = 2f * cam.orthographicSize;
        float width = height * cam.aspect;

        Vector3 tPositionWithoutZ = new Vector3(this.transform.position.x,this.transform.position.y,0);
        bottomLeft = tPositionWithoutZ + new Vector3(-width / 2, -height / 2, 0);
        topRight   = tPositionWithoutZ + new Vector3( width / 2,  height / 2, 0);
    
    }
}
