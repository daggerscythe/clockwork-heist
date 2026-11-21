using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// the title screen with play settings and quit
public class MainMenu : MonoBehaviour
{
    [SerializeField] private GameObject settingsPanel; // opens on top of the main buttons
    [SerializeField] private string firstScene = "TestRoom"; // scene the play button loads

    // free cursor and settings hidden
    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        settingsPanel.SetActive(false);
    }

    // esc closes the settings layer
    private void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame && settingsPanel.activeSelf) CloseSettings();
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

    // the quit button which only works in a build
    public void Quit()
    {
        Application.Quit();
    }
}