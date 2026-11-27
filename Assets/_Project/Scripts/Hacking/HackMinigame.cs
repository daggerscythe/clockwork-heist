using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// the camera hack where the player holds up a tablet memorizes the feed and clicks what changed while the world keeps going
public class HackMinigame : MonoBehaviour
{
    private enum Phase { Closed, Raising, Memorize, Glitch, Find, Result, Lowering }
    private enum Change { Move, Turn, Vanish }

    [SerializeField] private GameSettings settings;
    [SerializeField] private PlayerController player;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private PauseMenu pauseMenu;
    [SerializeField] private Transform tablet; // held under the player camera and raised to the face
    [SerializeField] private Transform tabletScreen; // quad on the tablet that shows the feed
    [SerializeField] private GameObject screen; // full screen feed and texts that take over once the tablet fills the view
    [SerializeField] private RawImage feed; // shows what the camera sees
    [SerializeField] private RenderTexture feedTexture; // the camera draws into this
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private GameObject timerDial; // clock dial with the seconds left shown while counting down
    [SerializeField] private Image timerFill;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text warningText; // tells the player when guards are spotting them
    [SerializeField] private RectTransform markTemplate; // hidden ring copied onto every change found

    private const float ResultTime = 1f; // s the result shows before the tablet goes down
    private const float WarningBeepGap = 1f; // s between warning beeps while a guard is onto the player
    private const float FeedEdge = 0.05f; // part of the feed at each edge where a prop counts as off screen
    private const float TurnAngle = 45f; // degrees a turned prop rotates
    private const float GlitchPeak = 1.5f; // above 1 so the static stays full for a moment around the switch
    private const float FillMargin = 0.97f; // pulls the raised tablet a bit closer so its screen surely covers the view

    private static readonly Vector3 LoweredPosition = new Vector3(0.05f, -0.45f, 0.35f); // m below the view where the tablet starts
    private static readonly Quaternion LoweredRotation = Quaternion.Euler(70f, 0f, 0f); // tipped back as if held low
    private static readonly int GlitchId = Shader.PropertyToID("_FeedGlitch");

    private readonly List<Transform> changed = new List<Transform>(); // props changed for this try
    private readonly List<Vector3> changedFrom = new List<Vector3>(); // where each changed prop was
    private readonly List<Quaternion> turnedFrom = new List<Quaternion>(); // how each changed prop was turned
    private readonly List<Renderer> hidden = new List<Renderer>(); // renderers switched off for vanished props
    private readonly List<Transform> notFound = new List<Transform>(); // changed props not clicked yet
    private readonly List<GameObject> marks = new List<GameObject>(); // rings on the changes found
    private SecurityCamera target;
    private Camera viewCamera; // the player camera the tablet is held up to
    private Phase phase;
    private float phaseStart; // game time the phase began
    private float phaseEnd; // game time the phase is over
    private float phaseLength; // s the phase lasts before any wrong click penalty
    private float nextWarningBeep; // game time the next warning beep may play
    private float raisedAmount; // 0 when the tablet is down and 1 when it fills the view
    private float lowerFrom; // how raised the tablet was when it started going down
    private bool noticed; // true once a guard has fully spotted the player or a whistleblower ran for the alarm during this hack
    private float lastHealth; // player health last frame so a hit shows up
    private int clickMask; // things a click on the feed can hit

    // hide the screen tablet and template and hook up the feed picture
    private void Awake()
    {
        screen.SetActive(false);
        tablet.gameObject.SetActive(false);
        markTemplate.gameObject.SetActive(false);
        timerDial.SetActive(false);
        feed.texture = feedTexture;
        viewCamera = tablet.GetComponentInParent<Camera>();
        clickMask = ~LayerMask.GetMask("Player", "ShadowVolume", "Ignore Raycast");
        Shader.SetGlobalFloat(GlitchId, 0f);
    }

    // stops the player and starts raising the tablet with the camera feed on it
    public void Begin(SecurityCamera securityCamera)
    {
        target = securityCamera;
        noticed = false;
        lastHealth = playerHealth.Health;
        player.Stop();
        GameManager.Instance.SetPlayerInput(false);

        target.StartFeed(feedTexture);
        titleText.text = "";
        HideTimer();
        warningText.text = "";
        tablet.gameObject.SetActive(true);
        PlaceTablet(0f);
        StartPhase(Phase.Raising, settings.hackTabletTime);
        Sfx.Flat(Sfx.Bank.hackTabletUp);
    }

    // runs on game time because the world keeps going during a hack
    private void Update()
    {
        if (phase == Phase.Closed) return;

        // a game over closes everything at once and the respawn gives the controls back
        if (GameManager.Instance.IsOver)
        {
            Shut();
            return;
        }

        // nothing moves and no clicks count while the pause menu is open
        if (pauseMenu.IsOpen) return;

        float progress = Mathf.InverseLerp(phaseStart, phaseEnd, Time.time);
        bool timeUp = Time.time >= phaseEnd;

        if (phase == Phase.Lowering)
        {
            PlaceTablet(lowerFrom * (1f - progress));
            if (timeUp) Shut();
            return;
        }

        if (phase == Phase.Result)
        {
            if (timeUp) Lower();
            return;
        }

        CheckGuards();

        // a hit from a guard pulls the tablet down at once and right click gets out by choice
        bool hit = playerHealth.Health < lastHealth;
        lastHealth = playerHealth.Health;
        if (hit) noticed = true;
        if (hit || Mouse.current.rightButton.wasPressedThisFrame)
        {
            Finish(false, false);
            return;
        }

        switch (phase)
        {
            case Phase.Raising: Raising(progress, timeUp); break;
            case Phase.Memorize: Memorize(timeUp); break;
            case Phase.Glitch: Glitch(progress, timeUp); break;
            case Phase.Find: Find(timeUp); break;
        }
    }

    // switches phase and starts its timer
    private void StartPhase(Phase next, float duration)
    {
        phase = next;
        phaseStart = Time.time;
        phaseEnd = Time.time + duration;
        phaseLength = duration;
    }

    // the tablet comes up and once it fills the view the full screen feed takes over
    private void Raising(float progress, bool timeUp)
    {
        PlaceTablet(progress);
        if (!timeUp) return;

        tablet.gameObject.SetActive(false);
        screen.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        titleText.text = "Memorize the room";
        StartPhase(Phase.Memorize, settings.hackMemorizeTime);
    }

    // counts down then the signal breaks up
    private void Memorize(bool timeUp)
    {
        ShowTimer();
        if (!timeUp) return;

        titleText.text = "";
        HideTimer();
        StartPhase(Phase.Glitch, settings.hackGlitchTime);
        Sfx.Flat(Sfx.Bank.hackStatic);
    }

    // static rises to full hides the changes and fades away again
    private void Glitch(float progress, bool timeUp)
    {
        float strength = (1f - Mathf.Abs(2f * progress - 1f)) * GlitchPeak;
        Shader.SetGlobalFloat(GlitchId, Mathf.Clamp01(strength));
        if (progress >= 0.5f && changed.Count == 0) MakeChanges();
        if (!timeUp) return;

        Shader.SetGlobalFloat(GlitchId, 0f);
        titleText.text = "Click the " + changed.Count + " things that changed";
        StartPhase(Phase.Find, settings.hackSolveTime);
    }

    // clicks count until every change is found or time runs out
    private void Find(bool timeUp)
    {
        ShowTimer();
        if (timeUp) Finish(false, true);
        else if (Mouse.current.leftButton.wasPressedThisFrame) Click(Mouse.current.position.ReadValue());
    }

    // whole seconds left in the phase in the middle of a dial that empties as time runs out
    private void ShowTimer()
    {
        float left = Mathf.Max(0f, phaseEnd - Time.time);
        timerDial.SetActive(true);
        timerFill.fillAmount = left / phaseLength;
        timerText.text = Mathf.CeilToInt(left).ToString();
    }

    // takes the dial off the screen
    private void HideTimer()
    {
        timerDial.SetActive(false);
    }

    // warns the player about guards spotting them or an alarm run and remembers either for the kit
    private void CheckGuards()
    {
        bool spotting = false;
        bool alarmRun = false;
        foreach (EnemyHealth guard in EnemyHealth.All)
        {
            if (!guard.IsAlive) continue;

            EnemyAI ai = guard.GetComponent<EnemyAI>();
            if (ai.IsRaisingAlarm) alarmRun = true;
            else if (ai.IsAlerted) noticed = true;
            else if (guard.GetComponent<EnemySenses>().Detection > 0f) spotting = true;
        }

        // a whistleblower running for the alarm is trouble whatever started it so the kit is kept
        if (alarmRun) noticed = true;

        if (alarmRun) warningText.text = "A whistleblower is running for the alarm!  Right click to get out";
        else if (noticed) warningText.text = "You've been spotted!  Right click to get out";
        else if (spotting) warningText.text = "A guard is spotting you";
        else warningText.text = "";

        // a beep every second while the warning shows
        if (warningText.text != "" && Time.time >= nextWarningBeep)
        {
            Sfx.Flat(Sfx.Bank.hackWarning);
            nextWarningBeep = Time.time + WarningBeepGap;
        }
    }

    // changes random props on the feed while the static hides the switch
    private void MakeChanges()
    {
        List<Transform> pool = new List<Transform>();
        foreach (Transform prop in target.Props)
        {
            if (IsOnFeed(prop.position)) pool.Add(prop);
        }

        Vector3 side = target.FeedCamera.transform.right;
        side.y = 0f;
        side.Normalize();

        for (int i = 0; i < settings.hackDifferences && pool.Count > 0; i++)
        {
            Transform prop = pool[Random.Range(0, pool.Count)];
            pool.Remove(prop);
            changed.Add(prop);
            changedFrom.Add(prop.position);
            turnedFrom.Add(prop.rotation);
            notFound.Add(prop);

            switch ((Change)Random.Range(0, 3))
            {
                case Change.Move: prop.position += side * (RandomSign() * settings.hackShiftDistance); break;
                case Change.Turn: prop.Rotate(0f, RandomSign() * TurnAngle, 0f, Space.World); break;
                case Change.Vanish: Hide(prop); break;
            }
        }

        // physics only catches up on its next step so it is told now that the props moved
        Physics.SyncTransforms();
    }

    // -1 or 1 at random
    private static float RandomSign()
    {
        return Random.value < 0.5f ? -1f : 1f;
    }

    // switches off what draws a prop but leaves its collider so clicking where it was still hits it
    private void Hide(Transform prop)
    {
        foreach (Renderer part in prop.GetComponentsInChildren<Renderer>())
        {
            part.enabled = false;
            hidden.Add(part);
        }
    }

    // true when a point is inside the feed picture and in front of the camera
    private bool IsOnFeed(Vector3 point)
    {
        Vector3 view = target.FeedCamera.WorldToViewportPoint(point);
        return view.z > 0f && view.x > FeedEdge && view.x < 1f - FeedEdge && view.y > FeedEdge && view.y < 1f - FeedEdge;
    }

    // turns a click on the feed into a ray from the camera and checks if it hit a changed prop
    private void Click(Vector2 screenPoint)
    {
        RectTransform area = feed.rectTransform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(area, screenPoint, null, out Vector2 local);
        if (!area.rect.Contains(local)) return;

        Ray ray = target.FeedCamera.ViewportPointToRay(Rect.PointToNormalized(area.rect, local));
        Transform clicked = null;
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, clickMask, QueryTriggerInteraction.Ignore))
        {
            foreach (Transform prop in changed)
            {
                if (hit.transform.IsChildOf(prop)) clicked = prop;
            }
        }

        // a wrong click costs time so clicking everything does not work
        if (clicked == null)
        {
            phaseEnd -= settings.hackWrongClickPenalty;
            Sfx.Flat(Sfx.Bank.hackWrong);
            return;
        }

        // a prop that was already found counts for nothing
        if (!notFound.Remove(clicked)) return;

        AddMark(local);
        Sfx.Flat(Sfx.Bank.hackCorrect);
        if (notFound.Count == 0) Finish(true, true);
    }

    // a ring on the feed where a change was found
    private void AddMark(Vector2 local)
    {
        RectTransform mark = Instantiate(markTemplate, markTemplate.parent);
        mark.anchoredPosition = local;
        mark.gameObject.SetActive(true);
        marks.Add(mark.gameObject);
    }

    // works out the kit and the wait then shows the result or lowers the tablet straight away
    private void Finish(bool success, bool showResult)
    {
        PutPropsBack();
        Shader.SetGlobalFloat(GlitchId, 0f);
        HideTimer();
        warningText.text = "";

        if (success)
        {
            inventory.TryUse(Ability.HackKit);
            target.Hack();
            Sfx.Flat(Sfx.Bank.hackSuccess);
            titleText.text = "Camera hacked";
        }
        else
        {
            // a guard spotting the player is the risk they took so the kit is kept
            if (!noticed) inventory.TryUse(Ability.HackKit);
            target.FailHack();
            Sfx.Flat(Sfx.Bank.hackFail);
            titleText.text = noticed ? "Hack interrupted. Kit kept" : "Hack failed. Kit used";
        }

        if (showResult) StartPhase(Phase.Result, ResultTime);
        else Lower();
    }

    // the full screen feed hands back to the tablet which goes down from wherever it is
    private void Lower()
    {
        Sfx.Flat(Sfx.Bank.hackTabletDown);
        screen.SetActive(false);
        tablet.gameObject.SetActive(true);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        lowerFrom = raisedAmount;
        PlaceTablet(lowerFrom);
        StartPhase(Phase.Lowering, settings.hackTabletTime * lowerFrom);
    }

    // slides the tablet between held low and filling the view with an ease in and out
    private void PlaceTablet(float amount)
    {
        raisedAmount = amount;
        float eased = Mathf.SmoothStep(0f, 1f, amount);
        tablet.localPosition = Vector3.Lerp(LoweredPosition, RaisedPosition(), eased);
        tablet.localRotation = Quaternion.Slerp(LoweredRotation, Quaternion.identity, eased);
    }

    // the spot in front of the eye where the tablet screen just covers the whole view
    private Vector3 RaisedPosition()
    {
        float tanHalf = Mathf.Tan(viewCamera.fieldOfView / 2f * Mathf.Deg2Rad);
        Vector3 size = tabletScreen.localScale;
        float distance = Mathf.Min(size.y / 2f / tanHalf, size.x / 2f / (tanHalf * viewCamera.aspect));
        return Vector3.forward * (distance * FillMargin);
    }

    // every changed prop goes back exactly as it was
    private void PutPropsBack()
    {
        for (int i = 0; i < changed.Count; i++) changed[i].SetPositionAndRotation(changedFrom[i], turnedFrom[i]);
        foreach (Renderer part in hidden) part.enabled = true;

        changed.Clear();
        changedFrom.Clear();
        turnedFrom.Clear();
        hidden.Clear();
        notFound.Clear();
        Physics.SyncTransforms();
    }

    // hides everything puts the world back and returns the controls unless a game over has them
    private void Shut()
    {
        foreach (GameObject mark in marks) Destroy(mark);
        marks.Clear();
        PutPropsBack();
        Shader.SetGlobalFloat(GlitchId, 0f);
        target.StopFeed();
        screen.SetActive(false);
        tablet.gameObject.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        phase = Phase.Closed;

        if (!GameManager.Instance.IsOver) GameManager.Instance.SetPlayerInput(true);
    }
}