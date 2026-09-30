using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Put this next to a UI Button inside the practice menu and choose what it does.
/// PracticeMenu finds every PracticeOptionButton in its children automatically,
/// so you can freely duplicate, move, restyle or delete buttons in the inspector.
/// </summary>
[RequireComponent(typeof(Button))]
public class PracticeOptionButton : MonoBehaviour
{
    public enum OptionAction
    {
        ToggleNoteValue,
        ToggleRests,
        ToggleLedgerLines,
        SetMajor,
        SetMinor,
        ChangeTempo,          // uses Amount (e.g. -10, -1, +1, +10)
        ChangeKey,            // uses Amount (-1 = one flat more, +1 = one sharp more)
        ChangeClef,           // uses Amount (-1 / +1)
        ChangeTimeSignature,  // uses Amount (-1 / +1)
        NewExercise,
        Resume
    }

    public OptionAction action;
    [Tooltip("Only used for ToggleNoteValue")]
    public NoteValue noteValue = NoteValue.Quarter;
    [Tooltip("Step size for ChangeTempo / ChangeKey / ChangeClef / ChangeTimeSignature")]
    public int amount = 1;

    [Header("Selection look (toggle buttons only)")]
    [Tooltip("Tinted with the menu's selected / unselected background colours")]
    public Graphic background;
    [Tooltip("Glyphs and captions tinted with the menu's ink colours")]
    public Graphic[] ink;

    /// <summary>True for buttons that show an on/off state.</summary>
    public bool IsToggle =>
        action == OptionAction.ToggleNoteValue || action == OptionAction.ToggleRests ||
        action == OptionAction.ToggleLedgerLines || action == OptionAction.SetMajor ||
        action == OptionAction.SetMinor;

    public void Bind(PracticeMenu menu)
    {
        var button = GetComponent<Button>();
        button.onClick.RemoveListener(OnClick);
        button.onClick.AddListener(OnClick);
        this.menu = menu;
    }

    PracticeMenu menu;
    void OnClick() { if (menu != null) menu.Perform(this); }

    public void SetSelected(bool selected, PracticeMenu theme)
    {
        if (background != null)
            background.color = selected ? theme.selectedBackground : theme.unselectedBackground;
        if (ink != null)
            foreach (var g in ink)
                if (g != null) g.color = selected ? theme.selectedInk : theme.unselectedInk;
    }
}
