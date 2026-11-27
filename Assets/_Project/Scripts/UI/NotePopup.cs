using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// a tutorial note that stops the game until the player closes it and can turn the tutorial off for the run
public class NotePopup : MonoBehaviour
{
    [SerializeField] private TMP_Text noteText; // the note on the card

    private readonly Queue<string> waiting = new Queue<string>(); // notes that came in while one was open
    private PlayerController player;
    private int openFrame; // frame the note went up so the e that took a pickup doesn't close it at once
    private bool inputWasOn; // the player scripts were on when the card opened
    private CursorLockMode cursorLock; // how the cursor was before the card opened
    private bool cursorVisible;

    // true while a note is on screen
    public static bool IsOpen { get; private set; }

    // find the player the first time the card opens
    private void Awake()
    {
        player = FindAnyObjectByType<PlayerController>();
    }

    // shows a note now or after the one already open
    public void Show(string text)
    {
        if (IsOpen)
        {
            waiting.Enqueue(text);
            return;
        }

        IsOpen = true;
        gameObject.SetActive(true);
        TimeScaleManager.Instance.Request(this, 0f);

        // during a hack the controls are already off and stay off after the note
        inputWasOn = player.enabled;
        if (inputWasOn) GameManager.Instance.SetPlayerInput(false);

        cursorLock = Cursor.lockState;
        cursorVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Write(text);
    }

    // e or enter does the same as the continue button
    private void Update()
    {
        if (Time.frameCount == openFrame) return;
        if (Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame)
        {
            Sfx.Ui(Sfx.Bank.uiClick);
            Continue();
        }
    }

    // the continue button shows the next waiting note or gives the game back
    public void Continue()
    {
        if (waiting.Count > 0) Write(waiting.Dequeue());
        else Close();
    }

    // the turn off tutorial button stops the notes and the arrow for the rest of the run
    public void TurnOffTutorial()
    {
        GameOptions.SetTutorialOff(true);
        waiting.Clear();
        Close();
    }

    // puts a note on the card with the pen sound
    private void Write(string text)
    {
        noteText.text = text;
        openFrame = Time.frameCount;
        Sfx.Ui(Sfx.Bank.noteScribble);
    }

    // gives the time the controls and the cursor back the way they were
    private void Close()
    {
        IsOpen = false;
        gameObject.SetActive(false);
        TimeScaleManager.Instance.Release(this);
        if (inputWasOn) GameManager.Instance.SetPlayerInput(true);
        Cursor.lockState = cursorLock;
        Cursor.visible = cursorVisible;
    }

    // a scene change never leaves the flag on
    private void OnDestroy()
    {
        IsOpen = false;
    }
}