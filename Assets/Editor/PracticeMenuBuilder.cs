using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using OptionAction = PracticeOptionButton.OptionAction;

/// <summary>
/// Editor-only: builds the classical-style practice menu into the EscapePanel of the open scene.
/// Run once via "NoteDash > Build Practice Menu". Everything it creates is a normal scene object
/// that you can move, restyle, duplicate or delete afterwards (Ctrl+Z undoes the whole build).
/// </summary>
public static class PracticeMenuBuilder
{
    // ---- palette
    static readonly Color Parchment = Hex("F3EAD3");
    static readonly Color ParchmentDark = Hex("E2D2AE");
    static readonly Color Ink = Hex("2A1B10");
    static readonly Color InkMuted = Hex("6B5440");
    static readonly Color Gold = Hex("A8823E");
    static readonly Color Burgundy = Hex("6E1E1E");
    static readonly Color Dimmer = new Color(0.08f, 0.05f, 0.03f, 0.45f);

    const string BravuraGuid = "d66b0d7c421c05348b248a2d9f4c0ed1";
    const string GaramondTtf = "Assets/Fonts/EBGaramond-Regular.ttf";
    const string GaramondItalicTtf = "Assets/Fonts/EBGaramond-Italic.ttf";

    static TMP_FontAsset bravura, serif, serifItalic;
    static Sprite buttonSprite, panelSprite;

    [MenuItem("NoteDash/Build Practice Menu")]
    public static void Build()
    {
        GameObject panel = FindEscapePanel();
        if (panel == null)
        {
            EditorUtility.DisplayDialog("Build Practice Menu",
                "Couldn't find the EscapePanel. Open the gameplay scene first (InputHandler.EscapePanel must be assigned).", "OK");
            return;
        }

        Transform existing = panel.transform.Find("PracticeMenu");
        if (existing != null)
        {
            if (!EditorUtility.DisplayDialog("Build Practice Menu",
                    "A PracticeMenu already exists under EscapePanel. Replace it? (your changes to it will be lost; Ctrl+Z restores it)",
                    "Replace", "Cancel")) return;
            Undo.DestroyObjectImmediate(existing.gameObject);
        }

        LoadAssets();
        Undo.SetCurrentGroupName("Build Practice Menu");
        int undoGroup = Undo.GetCurrentGroup();

        // dim the game a bit, sepia style
        var panelImage = panel.GetComponent<Image>();
        if (panelImage != null) { Undo.RecordObject(panelImage, "Build Practice Menu"); panelImage.color = Dimmer; }

        PracticeMenu menu = panel.GetComponent<PracticeMenu>();
        if (menu == null) menu = Undo.AddComponent<PracticeMenu>(panel);
        Undo.RecordObject(menu, "Build Practice Menu");

        // ------------------------------------------------------------ card
        GameObject card = UI("PracticeMenu", panel.transform);
        var cardRt = (RectTransform)card.transform;
        cardRt.anchorMin = new Vector2(0.04f, 0.40f);
        cardRt.anchorMax = new Vector2(0.96f, 0.98f);
        cardRt.offsetMin = cardRt.offsetMax = Vector2.zero;
        var cardImg = card.AddComponent<Image>();
        cardImg.sprite = panelSprite; cardImg.type = Image.Type.Sliced; cardImg.color = Parchment;
        var outline = card.AddComponent<Outline>();
        outline.effectColor = Gold; outline.effectDistance = new Vector2(3, -3);
        var v = card.AddComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(32, 32, 18, 18);
        v.spacing = 8;
        v.childAlignment = TextAnchor.UpperLeft;
        v.childControlWidth = v.childControlHeight = true;
        v.childForceExpandWidth = true; v.childForceExpandHeight = false;

        // ------------------------------------------------------------ title
        Transform header = Row(card.transform, "Header", 56);
        GlyphHolder(header, "Clef Ornament", "\uE050", 44, 40, 56, -0.22f * 44);
        var title = Text(header, "Title", "Sight-Reading Practice", serifItalic, 38, Ink, TextAlignmentOptions.MidlineLeft, -1, 56);
        title.characterSpacing = 2;
        Divider(card.transform);

        // ------------------------------------------------------------ note values
        Transform notes = Row(card.transform, "Row Note Values", 76);
        SectionLabel(notes, "Note values");
        NoteButton(notes, NoteValue.Whole, "\uE1D2", false);
        NoteButton(notes, NoteValue.DottedHalf, "\uE1D3<space=0.12em>\uE1E7", true);
        NoteButton(notes, NoteValue.Half, "\uE1D3", true);
        NoteButton(notes, NoteValue.DottedQuarter, "\uE1D5<space=0.12em>\uE1E7", true);
        NoteButton(notes, NoteValue.Quarter, "\uE1D5", true);
        NoteButton(notes, NoteValue.DottedEighth, "\uE1D7<space=0.05em>\uE1E7", true);
        NoteButton(notes, NoteValue.Eighth, "\uE1D7", true);
        NoteButton(notes, NoteValue.Sixteenth, "\uE1D9", true);

        // ------------------------------------------------------------ options
        Transform options = Row(card.transform, "Row Options", 50);
        SectionLabel(options, "Include");
        OptionButton(options, "Rests", OptionAction.ToggleRests, 150, 48, true, glyph: "\uE4E5", glyphSize: 34, caption: "Rests");
        OptionButton(options, "Ledger Lines", OptionAction.ToggleLedgerLines, 170, 48, true, caption: "Ledger lines");

        // ------------------------------------------------------------ tempo
        Transform tempo = Row(card.transform, "Row Tempo", 50);
        SectionLabel(tempo, "Tempo");
        OptionButton(tempo, "Tempo -10", OptionAction.ChangeTempo, 60, 44, false, caption: "\u221210", amount: -10);
        OptionButton(tempo, "Tempo -1", OptionAction.ChangeTempo, 48, 44, false, caption: "\u2212", amount: -1);
        GameObject tempoDisplay = UI("Tempo Display", tempo);
        Layout(tempoDisplay, 130, 50);
        var th = tempoDisplay.AddComponent<HorizontalLayoutGroup>();
        th.childAlignment = TextAnchor.MiddleCenter; th.spacing = 4;
        th.childControlWidth = th.childControlHeight = true; th.childForceExpandWidth = th.childForceExpandHeight = false;
        menu.tempoGlyph = GlyphHolder(tempoDisplay.transform, "Metronome Note", "\uECA5", 34, 26, 50, -0.3f * 34);
        menu.tempoLabel = Text(tempoDisplay.transform, "Tempo Label", "= 80", serif, 28, Ink, TextAlignmentOptions.MidlineLeft, 90, 50);
        OptionButton(tempo, "Tempo +1", OptionAction.ChangeTempo, 48, 44, false, caption: "+", amount: 1);
        OptionButton(tempo, "Tempo +10", OptionAction.ChangeTempo, 60, 44, false, caption: "+10", amount: 10);

        // ------------------------------------------------------------ notation: staff preview + controls
        Transform notation = Row(card.transform, "Row Notation", 138);
        BuildStaffPreview(notation, menu);

        GameObject controls = UI("Notation Controls", notation);
        Layout(controls, -1, 138, flexibleWidth: 1);
        var cv = controls.AddComponent<VerticalLayoutGroup>();
        cv.spacing = 4; cv.childAlignment = TextAnchor.MiddleLeft;
        cv.childControlWidth = cv.childControlHeight = true; cv.childForceExpandWidth = true; cv.childForceExpandHeight = false;

        Transform clefRow = Row(controls.transform, "Row Clef", 42);
        SectionLabel(clefRow, "Clef", 110);
        Arrow(clefRow, OptionAction.ChangeClef, -1);
        menu.clefLabel = Text(clefRow, "Clef Label", "Treble", serif, 26, Ink, TextAlignmentOptions.Center, 150, 42);
        Arrow(clefRow, OptionAction.ChangeClef, 1);

        Transform keyRow = Row(controls.transform, "Row Key", 42);
        SectionLabel(keyRow, "Key", 110);
        Arrow(keyRow, OptionAction.ChangeKey, -1);
        menu.keyLabel = Text(keyRow, "Key Label", "C major", serif, 26, Ink, TextAlignmentOptions.Center, 150, 42);
        Arrow(keyRow, OptionAction.ChangeKey, 1);
        Spacer(keyRow, 16);
        OptionButton(keyRow, "Major", OptionAction.SetMajor, 96, 40, true, caption: "Major");
        OptionButton(keyRow, "Minor", OptionAction.SetMinor, 96, 40, true, caption: "Minor");

        Transform timeRow = Row(controls.transform, "Row Time Signature", 42);
        SectionLabel(timeRow, "Metre", 110);
        Arrow(timeRow, OptionAction.ChangeTimeSignature, -1);
        menu.timeLabel = Text(timeRow, "Time Label", "4/4", serif, 26, Ink, TextAlignmentOptions.Center, 150, 42);
        Arrow(timeRow, OptionAction.ChangeTimeSignature, 1);

        // ------------------------------------------------------------ actions
        Divider(card.transform);
        Transform actions = Row(card.transform, "Row Actions", 52);
        Spacer(actions, 0, flexible: true);
        OptionButton(actions, "New Exercise", OptionAction.NewExercise, 220, 48, false, caption: "New exercise");
        var resume = OptionButton(actions, "Resume", OptionAction.Resume, 220, 48, false, caption: "Resume  (Esc)");
        resume.GetComponent<Image>().color = Burgundy;
        foreach (var g in resume.ink) g.color = Parchment;

        Undo.RegisterCreatedObjectUndo(card, "Build Practice Menu");
        Undo.CollapseUndoOperations(undoGroup);
        EditorUtility.SetDirty(menu);
        EditorSceneManager.MarkSceneDirty(panel.scene);
        Selection.activeGameObject = card;
        Debug.Log("Practice menu built under " + panel.name + ". Save the scene to keep it.");
    }

    // =====================================================================================

    static void BuildStaffPreview(Transform parent, PracticeMenu menu)
    {
        float space = menu.staffSpace;
        GameObject staff = UI("Staff Preview", parent);
        Layout(staff, 330, 138);

        var lines = new List<RectTransform>();
        for (int i = 0; i < 5; i++)
        {
            GameObject line = UI("Staff Line " + (i + 1), staff.transform);
            var img = line.AddComponent<Image>();
            img.color = Ink; img.raycastTarget = false;
            var rt = (RectTransform)line.transform;
            rt.anchorMin = new Vector2(0, 0.5f); rt.anchorMax = new Vector2(1, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(-8, 1.6f);
            rt.anchoredPosition = new Vector2(0, (i - 2) * space);
            lines.Add(rt);
        }
        menu.staffLines = lines.ToArray();

        // left barline, like the start of a system
        GameObject bar = UI("System Barline", staff.transform);
        var barImg = bar.AddComponent<Image>(); barImg.color = Ink; barImg.raycastTarget = false;
        var brt = (RectTransform)bar.transform;
        brt.anchorMin = brt.anchorMax = new Vector2(0, 0.5f);
        brt.pivot = new Vector2(0.5f, 0.5f);
        brt.sizeDelta = new Vector2(2f, space * 4 + 1.6f);
        brt.anchoredPosition = new Vector2(4, 0);

        menu.clefGlyph = StaffGlyph(staff.transform, "Clef", "\uE050", menu.staffSpace, false);
        menu.accidentals = new TMP_Text[7];
        for (int i = 0; i < 7; i++)
            menu.accidentals[i] = StaffGlyph(staff.transform, "Accidental " + (i + 1), "\uE262", menu.staffSpace, false);
        menu.timeSigNumerator = StaffGlyph(staff.transform, "Time Numerator", "\uE084", menu.staffSpace, true);
        menu.timeSigDenominator = StaffGlyph(staff.transform, "Time Denominator", "\uE084", menu.staffSpace, true);
    }

    static TMP_Text StaffGlyph(Transform parent, string name, string glyph, float space, bool centered)
    {
        GameObject go = UI(name, parent);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0, 0.5f);
        rt.pivot = new Vector2(centered ? 0.5f : 0f, 0.5f);
        rt.sizeDelta = new Vector2(centered ? 80 : 60, space * 10);
        var t = go.AddComponent<TextMeshProUGUI>();
        Style(t, glyph, bravura, space * 4, Ink, centered ? TextAlignmentOptions.Baseline : TextAlignmentOptions.BaselineLeft);
        return t;
    }

    static void NoteButton(Transform parent, NoteValue value, string glyph, bool stemmed)
    {
        const float size = 40;
        OptionButton(parent, "Note " + PracticeSettings.ValueName(value), OptionAction.ToggleNoteValue, 70, 74, true,
            glyph: glyph, glyphSize: size, glyphOffset: stemmed ? -0.375f * size : 0f, noteValue: value);
    }

    static void Arrow(Transform parent, OptionAction action, int amount) =>
        OptionButton(parent, (amount < 0 ? "Previous " : "Next ") + action, action, 40, 38, false,
            caption: amount < 0 ? "\u2039" : "\u203A", amount: amount, captionSize: 30);

    static PracticeOptionButton OptionButton(Transform parent, string name, OptionAction action, float width, float height,
        bool toggle, string glyph = null, float glyphSize = 40, float glyphOffset = 0, string caption = null,
        float captionSize = 24, NoteValue noteValue = NoteValue.Quarter, int amount = 1)
    {
        GameObject go = UI(name, parent);
        Layout(go, width, height);

        var img = go.AddComponent<Image>();
        img.sprite = buttonSprite; img.type = Image.Type.Sliced;
        img.color = toggle ? Parchment : ParchmentDark;

        var btn = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.93f, 0.90f, 0.84f, 1f);
        colors.pressedColor = new Color(0.78f, 0.72f, 0.62f, 1f);
        colors.selectedColor = Color.white;
        btn.colors = colors;
        var nav = btn.navigation; nav.mode = Navigation.Mode.None; btn.navigation = nav;

        var opt = go.AddComponent<PracticeOptionButton>();
        opt.action = action;
        opt.noteValue = noteValue;
        opt.amount = amount;
        opt.background = toggle ? img : null;
        var ink = new List<Graphic>();

        if (glyph != null && caption != null)
        {
            var h = go.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(10, 10, 0, 0); h.spacing = 8;
            h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = h.childControlHeight = true; h.childForceExpandWidth = h.childForceExpandHeight = false;
            ink.Add(GlyphHolder(go.transform, "Glyph", glyph, glyphSize, glyphSize * 0.5f, height, glyphOffset));
            ink.Add(Text(go.transform, "Caption", caption, serif, captionSize, Ink, TextAlignmentOptions.MidlineLeft, -1, height));
        }
        else if (glyph != null)
        {
            ink.Add(FillText(go.transform, "Glyph", glyph, bravura, glyphSize, TextAlignmentOptions.Baseline, glyphOffset));
        }
        else
        {
            ink.Add(FillText(go.transform, "Caption", caption, serif, captionSize, TextAlignmentOptions.Center, 0));
        }
        opt.ink = ink.ToArray();
        return opt;
    }

    // =====================================================================================
    // generic helpers

    static GameObject UI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        return go;
    }

    static LayoutElement Layout(GameObject go, float width, float height, float flexibleWidth = -1)
    {
        var le = go.GetComponent<LayoutElement>();
        if (le == null) le = go.AddComponent<LayoutElement>();
        if (width >= 0) le.minWidth = le.preferredWidth = width;
        if (height >= 0) le.minHeight = le.preferredHeight = height;
        le.flexibleWidth = flexibleWidth;
        return le;
    }

    static Transform Row(Transform parent, string name, float height)
    {
        GameObject go = UI(name, parent);
        Layout(go, -1, height);
        var h = go.AddComponent<HorizontalLayoutGroup>();
        h.spacing = 8;
        h.childAlignment = TextAnchor.MiddleLeft;
        h.childControlWidth = h.childControlHeight = true;
        h.childForceExpandWidth = h.childForceExpandHeight = false;
        return go.transform;
    }

    static void SectionLabel(Transform row, string text, float width = 170) =>
        Text(row, "Label " + text, text, serifItalic, 26, InkMuted, TextAlignmentOptions.MidlineLeft, width, 44);

    static void Spacer(Transform row, float width, bool flexible = false)
    {
        GameObject go = UI("Spacer", row);
        var le = Layout(go, width, -1);
        if (flexible) le.flexibleWidth = 1;
    }

    static void Divider(Transform parent)
    {
        GameObject go = UI("Divider", parent);
        Layout(go, -1, 2);
        var img = go.AddComponent<Image>();
        img.color = Gold; img.raycastTarget = false;
    }

    static TextMeshProUGUI Text(Transform parent, string name, string text, TMP_FontAsset font, float size, Color color,
        TextAlignmentOptions align, float width, float height)
    {
        GameObject go = UI(name, parent);
        Layout(go, width, height);
        var t = go.AddComponent<TextMeshProUGUI>();
        Style(t, text, font, size, color, align);
        return t;
    }

    /// <summary>Text stretched over its parent, moved vertically by yOffset (to centre stemmed glyphs).</summary>
    static TextMeshProUGUI FillText(Transform parent, string name, string text, TMP_FontAsset font, float size,
        TextAlignmentOptions align, float yOffset)
    {
        GameObject go = UI(name, parent);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(0, yOffset); rt.offsetMax = new Vector2(0, yOffset);
        var t = go.AddComponent<TextMeshProUGUI>();
        Style(t, text, font, size, Ink, align);
        return t;
    }

    /// <summary>A layout slot containing a Bravura glyph whose baseline sits at the slot centre + yOffset.</summary>
    static TextMeshProUGUI GlyphHolder(Transform parent, string name, string glyph, float size, float width, float height, float yOffset)
    {
        GameObject holder = UI(name, parent);
        Layout(holder, width, height);
        return FillText(holder.transform, "Glyph", glyph, bravura, size, TextAlignmentOptions.Baseline, yOffset);
    }

    static void Style(TextMeshProUGUI t, string text, TMP_FontAsset font, float size, Color color, TextAlignmentOptions align)
    {
        if (font != null) t.font = font;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.overflowMode = TextOverflowModes.Overflow;
        t.raycastTarget = false;
    }

    static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color c);
        return c;
    }

    // =====================================================================================
    // assets

    static void LoadAssets()
    {
        bravura = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(BravuraGuid));
        if (bravura == null)
        {
            foreach (string guid in AssetDatabase.FindAssets("Bravura t:TMP_FontAsset"))
            {
                bravura = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guid));
                if (bravura != null) break;
            }
        }
        if (bravura == null) Debug.LogWarning("Bravura SDF font asset not found - music glyphs will show as boxes.");

        serif = GetOrCreateFontAsset(GaramondTtf) ?? TMP_Settings.defaultFontAsset;
        serifItalic = GetOrCreateFontAsset(GaramondItalicTtf) ?? serif;

        buttonSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        panelSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
    }

    /// <summary>Creates (once) a dynamic TMP font asset next to a .ttf file.</summary>
    static TMP_FontAsset GetOrCreateFontAsset(string ttfPath)
    {
        string assetPath = System.IO.Path.ChangeExtension(ttfPath, null) + " SDF.asset";
        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
        if (existing != null) return existing;

        var font = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
        if (font == null)
        {
            Debug.LogWarning("Font not found at " + ttfPath + " - using the default TMP font instead.");
            return null;
        }

        TMP_FontAsset fa = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024,
                                                         AtlasPopulationMode.Dynamic, true);
        if (fa == null) return null;
        fa.name = System.IO.Path.GetFileNameWithoutExtension(assetPath);

        AssetDatabase.CreateAsset(fa, assetPath);
        fa.material.name = fa.name + " Material";
        AssetDatabase.AddObjectToAsset(fa.material, fa);
        fa.atlasTextures[0].name = fa.name + " Atlas";
        AssetDatabase.AddObjectToAsset(fa.atlasTextures[0], fa);
        EditorUtility.SetDirty(fa);
        AssetDatabase.SaveAssets();
        return fa;
    }

    static GameObject FindEscapePanel()
    {
        var handler = Object.FindFirstObjectByType<InputHandler>(FindObjectsInactive.Include);
        if (handler != null && handler.EscapePanel != null) return handler.EscapePanel;

        var scene = EditorSceneManager.GetActiveScene();
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == "EscapePanel") return t.gameObject;
        return null;
    }
}
