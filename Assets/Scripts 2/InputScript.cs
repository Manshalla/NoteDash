using UnityEngine;
using UnityEngine.InputSystem;

public class InputScript: MonoBehaviour
{
    public static Vector2 scroll;
    public InputAction scrollAction;

    void OnEnable()
    {
        scrollAction.Enable();
    }

    void OnDisable()
    {
        scrollAction.Disable();
    }

    void Update()
    {
        scroll = scrollAction.ReadValue<Vector2>();

        if (scroll.y > 0)
        {
            Debug.Log("Nach oben gescrollt");
        }
        else if (scroll.y < 0)
        {
            Debug.Log("Nach unten gescrollt");
        }
    }
}
