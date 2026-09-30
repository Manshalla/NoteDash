
using UnityEngine;
using UnityEngine.InputSystem;

public class InputHandler : MonoBehaviour
{
    public GameObject EscapePanel;
    private Entity camEntity;

    public Vector3 escapePosition;
    public Vector3 positionWhenEscapeWasPressed;


    public void Start()
    {
        if (EscapePanel != null && EscapePanel.GetComponent<PracticeMenu>() == null)
            Debug.LogWarning("EscapePanel has no PracticeMenu. Use the menu 'NoteDash > Build Practice Menu' in the editor.");

        camEntity = Camera.main.GetComponent<Entity>();
    }

    public void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (Clock.getState())
            {
                if(!camEntity.IsMoving && !camEntity.IsZooming)
                {
                    Clock.switchState();          // pause
                    EscapePanel.SetActive(true);

                    positionWhenEscapeWasPressed = camEntity.transform.position;
                    
                    camEntity.Animate(escapePosition,1);
                    camEntity.AnimateOrthographicSize(12,1);
                }
                
            }
            else
            {
                if(!camEntity.IsMoving && !camEntity.IsZooming)
                {
                    camEntity.Animate(positionWhenEscapeWasPressed,1);
                    camEntity.AnimateOrthographicSize(10,1);
                    // leaving the menu with Escape behaves like the "Resume" button
                    PracticeMenu menu = EscapePanel.GetComponent<PracticeMenu>();
                    if (menu != null) menu.Resume();
                    else
                    {
                        Clock.switchState();
                        //EscapePanel.SetActive(false);

                        
                    }
                }
                
            }
            
        }
    }
}
