using UnityEngine;
using UnityEngine.InputSystem;

public class InputHandler : MonoBehaviour
{
    public GameObject EscapePanel;

    public void Start()
    {
        if (EscapePanel != null && EscapePanel.GetComponent<PracticeMenu>() == null)
            Debug.LogWarning("EscapePanel has no PracticeMenu. Use the menu 'NoteDash > Build Practice Menu' in the editor.");
    }

    public void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (Clock.getState())
            {
                Clock.switchState();          // pause
                EscapePanel.SetActive(true);
            }
            else
            {
                // leaving the menu with Escape behaves like the "Resume" button
                PracticeMenu menu = EscapePanel.GetComponent<PracticeMenu>();
                if (menu != null) menu.Resume();
                else
                {
                    Clock.switchState();
                    EscapePanel.SetActive(false);
                }
            }
        }
    }
}
