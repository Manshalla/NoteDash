using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class LineManager : MonoBehaviour
{
    public GameObject MeasurePrefab;
    public Transform curPage;

    public Transform Cam;

    Vector3 lineSpawnPos;

    public static List<Transform> gespLines = new List<Transform>();

    public int measuresPerLine = 3;
    public int linesPerPage = 7;

    public float lineYSpacing;

    int lineIndex = 0;       // line counter over all pages (for names)
    int pageLineIndex = 0;   // line counter on the page being built

    public Vector3 pagePosition;

    public GameObject PagePrefab;

    [Header("Page turning")]
    [Tooltip("The page the escape panel sits on. Stored pages slide underneath it. Found by name 'EscapePanelPage' if empty.")]
    public Transform escapePanelPage;
    [Tooltip("Where a new page is spawned, relative to pagePosition (to the right of the current page)")]
    public Vector3 newPageOffset = new Vector3(20, 0, 0);
    [Tooltip("Seconds a page turn takes")]
    public float pageTurnTime = 1.2f;
    [Tooltip("Found by name 'ButtonPageRight' / 'ButtonPageLeft' if empty")]
    public Button pageRightButton;
    public Button pageLeftButton;
    [Tooltip("Name of the page-number text inside the Page prefab")]
    public string pageCounterName = "PageCount";

    [Tooltip("Sorting order of the page you play on (above the settings page while it rests)")]
    public int currentPageSortingOrder = 0;
    [Tooltip("Sorting order of pages under the settings page. Must be above the background (-2); " +
             "ties with the settings page sprite (-1) are broken by the Z offset below.")]
    public int storedPageSortingOrder = -1;
    [Tooltip("Stored pages are pushed this far back in Z so they draw behind the settings page")]
    public float storedPageZOffset = 1f;

    MusicGenerator generator;

    // measures of every page, in reading order (used to refill the current page on Regenerate)
    readonly Dictionary<Transform, List<(Measure measure, bool lineStart)>> pageMeasures =
        new Dictionary<Transform, List<(Measure measure, bool lineStart)>>();

    // pages that were turned away: they lie under the escape panel page, newest last
    readonly List<Transform> storedPages = new List<Transform>();

    bool turning;
    public bool IsTurning => turning;

    public void Awake()
    {
        PracticeSettings.Load();
        PracticeSettings.ApplyToClock();
        generator = new MusicGenerator();
    }

    public void Start()
    {
        if (escapePanelPage == null)
        {
            GameObject found = GameObject.Find("EscapePanelPage");
            if (found != null) escapePanelPage = found.transform;
        }
        if (pageRightButton == null) pageRightButton = FindButton("ButtonPageRight");
        if (pageLeftButton == null) pageLeftButton = FindButton("ButtonPageLeft");
        // the settings page is a Page prefab too: its counter always shows 0
        if (escapePanelPage != null) SetPageNumber(escapePanelPage, 0);

        if (pageRightButton != null) pageRightButton.onClick.AddListener(PageRight);
        if (pageLeftButton != null) pageLeftButton.onClick.AddListener(PageLeft);

        curPage = generatePage(pagePosition, 1);   // the first music page is page 1
        UpdateButtons();
    }

    static Button FindButton(string name)
    {
        GameObject go = GameObject.Find(name);
        return go != null ? go.GetComponent<Button>() : null;
    }

    Vector3 StoredPosition =>
        (escapePanelPage != null ? escapePanelPage.position : pagePosition - newPageOffset) + Vector3.forward * storedPageZOffset;

    // ------------------------------------------------------------------ page turning

    /// <summary>
    /// A new page is generated to the right of the current page and slides into its place,
    /// while the current page slides under the escape panel page at the same speed.
    /// </summary>
    public void PageRight()
    {
        if (turning || curPage == null) return;

        Transform oldPage = curPage;
        // current page = number (stored pages + 1), so the new one is one more
        Transform newPage = generatePage(pagePosition + newPageOffset, storedPages.Count + 2);
        SetOrder(newPage, currentPageSortingOrder);

        // the old page goes under the escape panel page (pages there are never regenerated)
        SetOrder(oldPage, storedPageSortingOrder);
        oldPage.position += Vector3.forward * storedPageZOffset;   // behind the settings page from the start

        // only the newest stored page needs to be visible under the escape panel page
        if (storedPages.Count > 0) storedPages[storedPages.Count - 1].gameObject.SetActive(false);
        storedPages.Add(oldPage);

        curPage = newPage;
        // one combined movement: both pages travel the same distance with the same curve and time
        StartCoroutine(Turn(
            Move(newPage, pagePosition),
            Move(oldPage, StoredPosition),
            Fade(newPage, 0f, 1f)));
    }

    /// <summary>
    /// The previously played page comes out from under the escape panel page,
    /// while the current page moves to the right and fades out.
    /// </summary>
    public void PageLeft()
    {
        if (turning || curPage == null || storedPages.Count == 0) return;

        Transform leavingPage = curPage;
        Transform previous = storedPages[storedPages.Count - 1];
        storedPages.RemoveAt(storedPages.Count - 1);

        // the page below it becomes the visible one under the escape panel page
        if (storedPages.Count > 0) storedPages[storedPages.Count - 1].gameObject.SetActive(true);

        previous.gameObject.SetActive(true);
        previous.position = StoredPosition;
        SetOrder(previous, storedPageSortingOrder);   // still under the escape panel page while it slides out

        curPage = previous;
        StartCoroutine(Turn(
            Move(previous, pagePosition),
            Move(leavingPage, pagePosition + newPageOffset),
            Fade(leavingPage, 1f, 0f),
            onDone: () =>
            {
                SetOrder(previous, currentPageSortingOrder);
                previous.position = pagePosition;
                DestroyPage(leavingPage);
            }));
    }

    IEnumerator Turn(IEnumerator a, IEnumerator b, IEnumerator c = null, System.Action onDone = null)
    {
        turning = true;
        UpdateButtons();

        var running = new List<Coroutine> { StartCoroutine(a), StartCoroutine(b) };
        if (c != null) running.Add(StartCoroutine(c));
        foreach (var r in running) yield return r;

        onDone?.Invoke();
        turning = false;
        UpdateButtons();
    }

    /// <summary>Moves a page with its Entity (same curve and duration for every page = same speed).</summary>
    IEnumerator Move(Transform page, Vector3 target)
    {
        Entity entity = page.GetComponent<Entity>();
        if (entity == null) entity = page.gameObject.AddComponent<Entity>();

        while (entity.IsMoving) yield return null;   // let a running animation finish first
        entity.Animate(target, pageTurnTime);
        yield return null;
        while (entity.IsMoving) yield return null;
    }

    /// <summary>Fades a whole page (paper, staff lines, notes) from one opacity to another over pageTurnTime.</summary>
    IEnumerator Fade(Transform page, float from, float to)
    {
        var sprites = page.GetComponentsInChildren<SpriteRenderer>(true);
        var texts = page.GetComponentsInChildren<TMP_Text>(true);
        var spriteAlpha = new float[sprites.Length];
        var textAlpha = new float[texts.Length];
        for (int i = 0; i < sprites.Length; i++) spriteAlpha[i] = sprites[i].color.a;
        for (int i = 0; i < texts.Length; i++) textAlpha[i] = texts[i].alpha;

        void Apply(float k)
        {
            for (int i = 0; i < sprites.Length; i++)
            {
                if (sprites[i] == null) continue;
                Color col = sprites[i].color; col.a = spriteAlpha[i] * k; sprites[i].color = col;
            }
            for (int i = 0; i < texts.Length; i++)
                if (texts[i] != null) texts[i].alpha = textAlpha[i] * k;
        }

        Apply(from);
        for (float t = 0; t < pageTurnTime; t += Time.deltaTime)
        {
            Apply(Mathf.Lerp(from, to, t / pageTurnTime));
            yield return null;
        }
        Apply(to);
    }

    /// <summary>Writes the number into the page's counter text (child named pageCounterName).</summary>
    void SetPageNumber(Transform page, int number)
    {
        foreach (TMP_Text t in page.GetComponentsInChildren<TMP_Text>(true))
        {
            if (t.gameObject.name == pageCounterName)
            {
                t.text = number.ToString();
                return;
            }
        }
    }

    static void SetOrder(Transform page, int order)
    {
        // a SortingGroup sorts the page and everything on it (lines, notes) as one unit
        SortingGroup group = page.GetComponent<SortingGroup>();
        if (group == null) group = page.gameObject.AddComponent<SortingGroup>();
        group.sortingOrder = order;
    }

    void DestroyPage(Transform page)
    {
        if (page == null) return;
        if (pageMeasures.TryGetValue(page, out var list))
            foreach (var entry in list)
                if (entry.measure != null) gespLines.Remove(entry.measure.transform.parent);
        pageMeasures.Remove(page);
        Destroy(page.gameObject);
    }

    /// <summary>Page buttons are hidden while a page turn plays, and Page left also when there is no earlier page.</summary>
    void UpdateButtons()
    {
        if (pageRightButton != null) pageRightButton.gameObject.SetActive(!turning);
        if (pageLeftButton != null) pageLeftButton.gameObject.SetActive(!turning && storedPages.Count > 0);
    }

    // ------------------------------------------------------------------ generation

    public Transform generatePage(Vector3 position, int pageNumber)
    {
        Transform page = Instantiate(PagePrefab, position, Quaternion.identity, this.transform).transform;
        pageMeasures[page] = new List<(Measure measure, bool lineStart)>();
        SetOrder(page, currentPageSortingOrder);

        SetPageNumber(page, pageNumber);

        pageLineIndex = 0;
        for (int i = 0; i < linesPerPage; i++)
        {
            lineSpawnPos = page.position + new Vector3(-5, 9 - lineYSpacing * i, 3);
            GenerateLine(page);

            lineIndex++;
            pageLineIndex++;
        }
        return page;
    }

    /// <summary>
    /// Refills the current page with new music from the current PracticeSettings
    /// (also a page that was brought back with Page left). Pages under the settings page are never touched.
    /// Called from the pause menu.
    /// </summary>
    public void Regenerate()
    {
        PracticeSettings.ApplyToClock();
        generator = new MusicGenerator();

        if (curPage == null) return;
        if (!pageMeasures.TryGetValue(curPage, out var measures)) return;

        measures.RemoveAll(entry => entry.measure == null);
        for (int k = 0; k < measures.Count; k++)
        {
            Measure measure = measures[k].measure;
            bool showTimeSig = k == 0;
            if (showTimeSig)
                measure.timeSignature = new TimeSignature(PracticeSettings.TimeNum, PracticeSettings.TimeDen);

            MeasureRenderer.Render(measure, generator.NextMeasure(), measures[k].lineStart, showTimeSig);
        }
    }

    private void GenerateLine(Transform page)
    {
        GameObject line = new GameObject("Line " + lineIndex.ToString());
        line.transform.SetParent(page, true);

        gespLines.Add(line.transform);

        for (int i = 0; i < measuresPerLine; i++)
        {
            float xSize = 5f;
            Vector3 pos = new Vector3(i * xSize, 0, 0);
            Measure measure = Instantiate(MeasurePrefab, pos, Quaternion.identity, line.transform).GetComponent<Measure>();

            measure.RightLine.SetActive(i == measuresPerLine - 1);

            // time signature on the first measure of every page
            bool showTimeSig = i == 0 && pageLineIndex == 0;
            if (showTimeSig)
            {
                measure.timeSignature = new TimeSignature(PracticeSettings.TimeNum, PracticeSettings.TimeDen);
            }
            else
            {
                measure.numC.text = "";
                measure.domC.text = "";
            }

            // fill the measure with generated notes (clef + key signature at the start of every line)
            MeasureRenderer.Render(measure, generator.NextMeasure(), i == 0, showTimeSig);
            pageMeasures[page].Add((measure, i == 0));
        }

        line.transform.position = lineSpawnPos;
    }
}
