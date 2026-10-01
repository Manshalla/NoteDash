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
    [Tooltip("Milliseconds per frame spent creating measures and notes (shared by all pages that are being built). " +
             "Lower = smoother frame rate, but a page fills up over more frames.")]
    [Range(1f, 16f)] public float buildBudgetMs = 4f;
    [Tooltip("Build the next page in advance (invisible, while you play), so Page right only has to animate")]
    public bool prepareNextPage = true;

    MusicGenerator generator;

    // one running build/refill job per page; a newer job makes the older one stop
    readonly Dictionary<Transform, int> pageJob = new Dictionary<Transform, int>();
    int jobCounter;

    // shared per-frame build budget
    int budgetFrame = -1;
    double budgetUsedMs;

    // the next page, already built and waiting invisibly to the right
    Transform preparedPage;
    bool preparing;

    // measures of every page, in reading order (used to refill the current page on Regenerate)
    readonly Dictionary<Transform, List<(Measure measure, bool lineStart)>> pageMeasures =
        new Dictionary<Transform, List<(Measure measure, bool lineStart)>>();

    // pages that were turned away: they lie under the escape panel page, newest last
    readonly List<Transform> storedPages = new List<Transform>();

    bool turning;
    public bool IsTurning => turning;

    public void Awake()
    {
        PracticeSettings.EnsureLoaded();
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
        PrepareNextPage();
        UpdateButtons();
    }

    static Button FindButton(string name)
    {
        GameObject go = GameObject.Find(name);
        return go != null ? go.GetComponent<Button>() : null;
    }

    Vector3 StoredPosition =>
        (escapePanelPage != null ? escapePanelPage.position : pagePosition - newPageOffset) + Vector3.forward * storedPageZOffset;

    Vector3 NextPagePosition => pagePosition + newPageOffset;

    // ------------------------------------------------------------------ page turning

    /// <summary>
    /// The next page (normally already built invisibly to the right) fades in and slides into place,
    /// while the current page slides under the escape panel page at the same speed.
    /// </summary>
    public void PageRight()
    {
        if (turning || curPage == null) return;

        Transform oldPage = curPage;

        // use the page that was prepared in advance; only build one now if there is none
        Transform newPage = preparedPage != null ? preparedPage : generatePage(NextPagePosition, 0, hidden: true);
        preparedPage = null;
        newPage.position = NextPagePosition;
        SetPageNumber(newPage, storedPages.Count + 2);   // current page = stored pages + 1, new one is one more
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
            Fade(newPage, 0f, 1f),
            onDone: () => { ForgetFade(newPage); PrepareNextPage(); }));
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
            Move(leavingPage, NextPagePosition),
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

    // ------------------------------------------------------------------ fading

    /// <summary>
    /// Everything on a page that can be faded. Collected once (not every frame).
    /// Sprites fade through their colour; texts fade through a MaterialPropertyBlock (_FaceColor),
    /// which changes no text mesh and creates no garbage.
    /// </summary>
    class FadeState
    {
        public float k = 1f;
        readonly List<SpriteRenderer> sprites = new List<SpriteRenderer>();
        readonly List<float> spriteAlpha = new List<float>();
        readonly List<Renderer> textRenderers = new List<Renderer>();
        readonly HashSet<Component> known = new HashSet<Component>();
        static MaterialPropertyBlock block;
        static readonly int FaceColor = Shader.PropertyToID("_FaceColor");

        /// <summary>Registers everything under root that isn't known yet and gives it the current opacity.</summary>
        public void Add(Transform root)
        {
            foreach (SpriteRenderer sr in root.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (!known.Add(sr)) continue;
                sprites.Add(sr);
                spriteAlpha.Add(sr.color.a);
                SetSprite(sprites.Count - 1);
            }
            foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text is TextMeshProUGUI || !known.Add(text)) continue;   // world-space texts only
                Renderer r = text.GetComponent<Renderer>();
                if (r == null) continue;
                textRenderers.Add(r);
                SetText(r);
            }
        }

        public void Apply(float value)
        {
            k = value;
            for (int i = 0; i < sprites.Count; i++) SetSprite(i);
            for (int i = 0; i < textRenderers.Count; i++) SetText(textRenderers[i]);
        }

        void SetSprite(int i)
        {
            SpriteRenderer sr = sprites[i];
            if (sr == null) return;
            Color c = sr.color; c.a = spriteAlpha[i] * k; sr.color = c;
        }

        void SetText(Renderer r)
        {
            if (r == null) return;
            if (block == null) block = new MaterialPropertyBlock();
            r.GetPropertyBlock(block);
            block.SetColor(FaceColor, new Color(1f, 1f, 1f, k));
            r.SetPropertyBlock(block);
        }
    }

    readonly Dictionary<Transform, FadeState> fades = new Dictionary<Transform, FadeState>();

    FadeState GetFade(Transform page, float startValue)
    {
        if (!fades.TryGetValue(page, out FadeState state))
        {
            state = new FadeState { k = startValue };
            fades[page] = state;
            state.Add(page);
        }
        return state;
    }

    void ForgetFade(Transform page)
    {
        // back at full opacity: nothing needs tracking any more
        if (page != null && fades.TryGetValue(page, out FadeState s) && s.k >= 1f) fades.Remove(page);
    }

    /// <summary>
    /// Fades a whole page (paper, staff lines, notes) over pageTurnTime,
    /// following the page's movement curve so fade and movement feel like one motion.
    /// </summary>
    IEnumerator Fade(Transform page, float from, float to)
    {
        FadeState state = GetFade(page, from);
        state.Add(page);          // pick up anything created since the state was made
        state.Apply(from);

        Entity entity = page.GetComponent<Entity>();
        AnimationCurve curve = entity != null ? entity.movementCurve : null;

        for (float t = 0; t < pageTurnTime; t += Time.deltaTime)
        {
            if (page == null) yield break;
            float p = Mathf.Clamp01(t / pageTurnTime);
            if (curve != null) p = curve.Evaluate(p);
            state.Apply(Mathf.LerpUnclamped(from, to, p));
            yield return null;
        }

        if (page == null) yield break;
        state.Apply(to);
    }

    // ------------------------------------------------------------------ helpers

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
        if (preparedPage == page) preparedPage = null;
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
    /// Creates the page right away and fills it asynchronously:
    /// the notes are computed on a background thread, the measures are built over several frames.
    /// A hidden page stays fully transparent (also everything built on it later) until it is faded in.
    /// </summary>
    public Transform generatePage(Vector3 position, int pageNumber, bool hidden = false)
    {
        Transform page = Instantiate(PagePrefab, position, Quaternion.identity, this.transform).transform;
        pageMeasures[page] = new List<(Measure measure, bool lineStart)>();
        SetOrder(page, currentPageSortingOrder);
        SetPageNumber(page, pageNumber);

        if (hidden) GetFade(page, 0f).Apply(0f);   // invisible from the very first frame

        StartJob(page);
        return page;
    }

    /// <summary>Builds the next page invisibly to the right, once nothing else is being built or animated.</summary>
    void PrepareNextPage()
    {
        if (!prepareNextPage || preparedPage != null || preparing) return;
        StartCoroutine(PrepareRoutine());
    }

    IEnumerator PrepareRoutine()
    {
        preparing = true;
        // don't compete with the current page or with an animation for frame time
        while (turning || IsBuilding(curPage)) yield return null;
        if (preparedPage == null)
            preparedPage = generatePage(NextPagePosition, 0, hidden: true);
        preparing = false;
    }

    /// <summary>
    /// Refills the current page (and the prepared next page) with new music from the current PracticeSettings
    /// (also a page that was brought back with Page left). Pages under the settings page are never touched.
    /// Called from the pause menu. Runs asynchronously like generatePage.
    /// </summary>
    public void Regenerate()
    {
        PracticeSettings.ApplyToClock();
        generator = new MusicGenerator();

        if (curPage != null && pageMeasures.ContainsKey(curPage)) StartJob(curPage);
        if (preparedPage != null && pageMeasures.ContainsKey(preparedPage)) StartJob(preparedPage);
    }

    /// <summary>True while a page is still being built or refilled.</summary>
    public bool IsBuilding(Transform page) => page != null && pageJob.ContainsKey(page);

    void StartJob(Transform page)
    {
        int id = ++jobCounter;
        pageJob[page] = id;     // an older job for this page sees the new id and stops
        StartCoroutine(FillPage(page, id));
    }

    /// <summary>True if this frame's build budget is used up (shared by all pages being built).</summary>
    bool BudgetExhausted()
    {
        if (budgetFrame != Time.frameCount) { budgetFrame = Time.frameCount; budgetUsedMs = 0; }
        return budgetUsedMs >= buildBudgetMs;
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

        // 2) build / refill the measures on the main thread, within the per-frame budget
        var watch = new Stopwatch();
        for (int k = 0; k < count; k++)
        {
            while (BudgetExhausted()) yield return null;
            if (Stale()) yield break;

            watch.Restart();
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

                // the page is hidden or fading right now -> give the new objects the same opacity
                if (fades.TryGetValue(page, out FadeState fade)) fade.Add(measure.transform);
            }

            budgetUsedMs += watch.Elapsed.TotalMilliseconds;
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
