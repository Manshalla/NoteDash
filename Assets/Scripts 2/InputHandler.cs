using UnityEngine;
using UnityEngine.InputSystem;

public class InputHandler : MonoBehaviour
{
    public GameObject EscapePanel;
    public void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Clock.switchState();
            EscapePanel.SetActive(!Clock.getState()); // Clock.getstate = true if clock counting
        }
    }
}
