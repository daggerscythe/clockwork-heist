using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// esc stops the game and opens the pause menu with the settings or the notes layered on top
public class PauseMenu : MonoBehaviour
{
    [SerializeField] private PlayerController player;
    [SerializeField] private GameObject pausePanel; // dark backdrop and the pause buttons
    [SerializeField] private GameObject settingsPanel; // opens on top of the pause panel
    [SerializeField] private GameObject notesPanel; // opens on top of the pause panel like the settings
    [SerializeField] private string mainMenuScene = "MainMenu"; // scene quit to main menu loads

    private bool inputWasOn; // the player scripts were on when the menu opened
    private CursorLockMode cursorLock; // how the cursor was before the menu opened
    private bool cursorVisible;

    // true while the game is paused
    public bool IsOpen { get; private set; }

    // every layer starts hidden
    private void Awake()
    {
        pausePanel.SetActive(false);
        settingsPanel.SetActive(false);
        notesPanel.SetActive(false);
    }

    // esc opens the menu closes the settings or notes layer or resumes
    private void Update()
    {
        if (!Keyboard.current.escapeKey.wasPressedThisFrame) return;

        // a note card is up and closes with its own buttons
        if (NotePopup.IsOpen) return;

        if (settingsPanel.activeSelf)
        {
            Sfx.Ui(Sfx.Bank.uiClick);
            CloseSettings();
        }
        else if (notesPanel.activeSelf)
        {
            Sfx.Ui(Sfx.Bank.uiClick);
            notesPanel.SetActive(false);
        }
        else if (IsOpen)
        {
            Sfx.Ui(Sfx.Bank.uiClick);
            Resume();
        }
        else if (!GameManager.Instance.IsOver) Open();
    }

    // freezes time takes the controls away and shows the cursor
    private void Open()
    {
        IsOpen = true;
        AudioListener.pause = true;
        TimeScaleManager.Instance.Request(this, 0f);

        // during a hack the controls are already off and stay off after the pause
        inputWasOn = player.enabled;
        if (inputWasOn) GameManager.Instance.SetPlayerInput(false);

        cursorLock = Cursor.lockState;
        cursorVisible = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        pausePanel.SetActive(true);
    }

    // the resume button and esc put everything back the way it was
    public void Resume()
    {
        IsOpen = false;
        AudioListener.pause = false;
        pausePanel.SetActive(false);
        settingsPanel.SetActive(false);
        notesPanel.SetActive(false);
        TimeScaleManager.Instance.Release(this);
        if (inputWasOn) GameManager.Instance.SetPlayerInput(true);
        Cursor.lockState = cursorLock;
        Cursor.visible = cursorVisible;
    }

    // the settings button
    public void OpenSettings()
    {
        settingsPanel.SetActive(true);
    }

    // the back button and esc go back to the pause panel
    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
    }

    // the notes button
    public void OpenNotes()
    {
        notesPanel.SetActive(true);
    }

    // the restart button closes the menu and respawns at the last checkpoint
    public void RestartFromCheckpoint()
    {
        Resume();
        GameManager.Instance.RestartFromCheckpoint();
    }

    // the quit to main menu button
    public void QuitToMainMenu()
    {
        AudioListener.pause = false;
        SceneManager.LoadScene(mainMenuScene);
    }

    // the quit to desktop button which only works in a build
    public void QuitToDesktop()
    {
        Application.Quit();
    }
}