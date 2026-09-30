using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

public class LineManager : MonoBehaviour
{
    public GameObject MeasurePrefab;
    public Transform curPage;

    public Transform Cam;


    public static List<Transform> gespLines = new List<Transform>();

    public int measuresPerLine = 3;
    public int linesPerPage = 7;

    public float lineYSpacing;

    int lineIndex = 0;       // line counter over all pages (for names)

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

    [Header("Async generation")]
    [Tooltip("Milliseconds per frame spent creating measures and notes. Lower = smoother frame rate, " +
             "but a page fills up over more frames.")]
    [Range(1f, 16f)] public float buildBudgetMs = 4f;

    MusicGenerator generator;

    // one running build/refill job per page; a newer job makes the older one stop
    readonly Dictionary<Transform, int> pageJob = new Dictionary<Transform, int>();
    int jobCounter;

    // pages that are currently fading: measures created meanwhile get the same opacity
    class FadeState { public float k = 1f; public readonly Dictionary<Component, float> baseAlpha = new Dictionary<Component, float>(); }
    readonly Dictionary<Transform, FadeState> fades = new Dictionary<Transform, FadeState>();

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

    /// <summary>
    /// Fades a whole page (paper, staff lines, notes) from one opacity to another over pageTurnTime.
    /// Measures that are still being built during the fade get the same opacity.
    /// </summary>
    IEnumerator Fade(Transform page, float from, float to)
    {
        var state = new FadeState { k = from };
        fades[page] = state;
        ApplyFade(page, state);

        for (float t = 0; t < pageTurnTime; t += Time.deltaTime)
        {
            if (page == null) yield break;
            state.k = Mathf.Lerp(from, to, t / pageTurnTime);
            ApplyFade(page, state);
            yield return null;
        }

        if (page == null) yield break;
        state.k = to;
        ApplyFade(page, state);
        fades.Remove(page);
    }

    static void ApplyFade(Transform root, FadeState state)
    {
        foreach (SpriteRenderer sr in root.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (!state.baseAlpha.TryGetValue(sr, out float a)) { a = sr.color.a; state.baseAlpha[sr] = a; }
            Color col = sr.color; col.a = a * state.k; sr.color = col;
        }
        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if (!state.baseAlpha.TryGetValue(text, out float a)) { a = text.alpha; state.baseAlpha[text] = a; }
            text.alpha = a * state.k;
        }
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
        pageJob.Remove(page);
        fades.Remove(page);
        Destroy(page.gameObject);
    }

    /// <summary>Page buttons are hidden while a page turn plays, and Page left also when there is no earlier page.</summary>
    void UpdateButtons()
    {
        if (pageRightButton != null) pageRightButton.gameObject.SetActive(!turning);
        if (pageLeftButton != null) pageLeftButton.gameObject.SetActive(!turning && storedPages.Count > 0);
    }

    // ------------------------------------------------------------------ generation

    /// <summary>
    /// Creates the page right away (so it can already be animated) and fills it asynchronously:
    /// the notes are computed on a background thread, the measures are built over several frames.
    /// </summary>
    public Transform generatePage(Vector3 position, int pageNumber)
    {
        Transform page = Instantiate(PagePrefab, position, Quaternion.identity, this.transform).transform;
        pageMeasures[page] = new List<(Measure measure, bool lineStart)>();
        SetOrder(page, currentPageSortingOrder);
        SetPageNumber(page, pageNumber);

        StartJob(page);
        return page;
    }

    /// <summary>
    /// Refills the current page with new music from the current PracticeSettings
    /// (also a page that was brought back with Page left). Pages under the settings page are never touched.
    /// Called from the pause menu. Runs asynchronously like generatePage.
    /// </summary>
    public void Regenerate()
    {
        PracticeSettings.ApplyToClock();
        generator = new MusicGenerator();

        if (curPage == null || !pageMeasures.ContainsKey(curPage)) return;
        StartJob(curPage);
    }

    /// <summary>True while a page is still being built or refilled.</summary>
    public bool IsBuilding(Transform page) => page != null && pageJob.ContainsKey(page);

    void StartJob(Transform page)
    {
        int id = ++jobCounter;
        pageJob[page] = id;     // an older job for this page sees the new id and stops
        StartCoroutine(FillPage(page, id));
    }

    IEnumerator FillPage(Transform page, int id)
    {
        bool Stale() => page == null || !pageJob.TryGetValue(page, out int current) || current != id;

        // 1) compute the notes on a worker thread (pure C#, no Unity objects involved)
        int count = linesPerPage * measuresPerLine;
        MusicGenerator gen = generator;   // created on the main thread, it copied the settings it needs
        Task<List<List<Note>>> task = Task.Run(() =>
        {
            var music = new List<List<Note>>(count);
            lock (gen)   // the generator keeps state between measures -> one caller at a time
                for (int k = 0; k < count; k++) music.Add(gen.NextMeasure());
            return music;
        });
        while (!task.IsCompleted) yield return null;

        if (task.IsFaulted) { Debug.LogException(task.Exception); FinishJob(page, id); yield break; }
        if (Stale()) yield break;
        List<List<Note>> notes = task.Result;

        // 2) build / refill the measures on the main thread, a few per frame
        var watch = Stopwatch.StartNew();
        for (int k = 0; k < count; k++)
        {
            if (Stale()) yield break;
            List<(Measure measure, bool lineStart)> measures = pageMeasures[page];

            // create the measure if the page doesn't have it yet (first fill, or an interrupted one)
            if (k >= measures.Count) CreateMeasure(page, measures, k);

            Measure measure = measures[k].measure;
            if (measure != null)
            {
                bool showTimeSig = k == 0;
                if (showTimeSig)
                    measure.timeSignature = new TimeSignature(PracticeSettings.TimeNum, PracticeSettings.TimeDen);

                // fill the measure with generated notes (clef + key signature at the start of every line)
                MeasureRenderer.Render(measure, notes[k], measures[k].lineStart, showTimeSig);

                // the page is fading in/out right now -> give the new objects the same opacity
                if (fades.TryGetValue(page, out FadeState fade)) ApplyFade(measure.transform, fade);
            }

            if (watch.Elapsed.TotalMilliseconds >= buildBudgetMs)
            {
                yield return null;
                watch.Restart();
            }
        }
        FinishJob(page, id);
    }

    void FinishJob(Transform page, int id)
    {
        if (page != null && pageJob.TryGetValue(page, out int current) && current == id) pageJob.Remove(page);
    }

    void CreateMeasure(Transform page, List<(Measure measure, bool lineStart)> measures, int k)
    {
        int lineOnPage = k / measuresPerLine;
        int i = k % measuresPerLine;

        // find or create the line; its position is relative to where the page is right now (it may be moving)
        Transform line;
        if (i == 0)
        {
            line = new GameObject("Line " + lineIndex++).transform;
            line.SetParent(page, true);
            line.position = page.position + new Vector3(-5, 9 - lineYSpacing * lineOnPage, 3);
            gespLines.Add(line);
        }
        else
        {
            line = measures[k - 1].measure.transform.parent;
        }

        float xSize = 5f;
        Vector3 pos = line.position + new Vector3(i * xSize, 0, 0);
        Measure measure = Instantiate(MeasurePrefab, pos, Quaternion.identity, line).GetComponent<Measure>();

        measure.RightLine.SetActive(i == measuresPerLine - 1);

        // time signature only on the first measure of every page
        if (k != 0)
        {
            measure.numC.text = "";
            measure.domC.text = "";
        }

        measures.Add((measure, i == 0));
    }
}
