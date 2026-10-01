using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InputHandler : MonoBehaviour
{
    public GameObject EscapePanel;
    private Entity camEntity;

    public Vector3 escapePosition;
    public float escapeOrthographicSize = 12;
    public float animationTime = 1;

    public float orthographicSiceWhenEscapeWasPressed;
    public Vector3 positionWhenEscapeWasPressed;


    public void Start()
    {
        if (EscapePanel != null)
        {
            EscapePanel.SetActive(true);   // the panel is always visible

            // the full-screen panel background must not swallow mouse input,
            // otherwise the camera controls think the mouse is always over UI
            Graphic background = EscapePanel.GetComponent<Graphic>();
            if (background != null) background.raycastTarget = false;
            if (EscapePanel.GetComponent<PracticeMenu>() == null)
                Debug.LogWarning("EscapePanel has no PracticeMenu. Use the menu 'NoteDash > Build Practice Menu' in the editor.");
        }

        camEntity = Camera.main.GetComponent<Entity>();
    }

    public void Update()
    {
        /*
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (Clock.getState()) PauseGame();
            else ResumeGame();
        }
        */
    }

    bool CameraBusy => camEntity != null && (camEntity.IsMoving || camEntity.IsZooming);

    /// <summary>Pauses and moves the camera to the escape view.</summary>
    public void PauseGame()
    {
        if (!Clock.getState() || CameraBusy) return;

        Clock.switchState();          // pause
        positionWhenEscapeWasPressed = camEntity.transform.position;
        orthographicSiceWhenEscapeWasPressed = camEntity.GetComponent<Camera>().orthographicSize;

        camEntity.Animate(escapePosition, animationTime);
        camEntity.AnimateOrthographicSize(escapeOrthographicSize, animationTime);
    }

    /// <summary>Unpauses and moves the camera back (used by Escape and by the menu's Resume button).</summary>
    public void ResumeGame()
    {
        if (Clock.getState() || CameraBusy) return;

        PracticeSettings.Save();
        camEntity.Animate(positionWhenEscapeWasPressed, animationTime);
        camEntity.AnimateOrthographicSize(orthographicSiceWhenEscapeWasPressed, animationTime);
        Clock.switchState();          // resume
    }
}
