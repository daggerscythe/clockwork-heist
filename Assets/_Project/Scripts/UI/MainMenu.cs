using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// the title screen with play settings notes and quit
public class MainMenu : MonoBehaviour
{
    [SerializeField] private GameObject settingsPanel; // opens on top of the main buttons
    [SerializeField] private GameObject notesPanel; // opens on top of the main buttons like the settings
    [SerializeField] private string firstScene = "TestRoom"; // scene the play button loads

    // free cursor and both layers hidden
    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        settingsPanel.SetActive(false);
        notesPanel.SetActive(false);
    }

    // esc closes whichever layer is open
    private void Update()
    {
        if (!Keyboard.current.escapeKey.wasPressedThisFrame) return;
        if (settingsPanel.activeSelf) CloseSettings();
        else if (notesPanel.activeSelf) notesPanel.SetActive(false);
    }

    // the play button
    public void Play()
    {
        SceneManager.LoadScene(firstScene);
    }

    // the settings button
    public void OpenSettings()
    {
        settingsPanel.SetActive(true);
    }

    // the back button
    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
    }

    // the notes button
    public void OpenNotes()
    {
        notesPanel.SetActive(true);
    }

    // the quit button which only works in a build
    public void Quit()
    {
        Application.Quit();
    }
}