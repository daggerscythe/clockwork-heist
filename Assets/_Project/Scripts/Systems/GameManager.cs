using System.Collections.Generic;
using TMPro;
using UnityEngine;

// runs game over and puts the player and the level back the way they were at the last checkpoint
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private GameSettings settings;
    [SerializeField] private PlayerController player;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerInteract playerInteract;
    [SerializeField] private HUD hud;
    [SerializeField] private Behaviour[] playerScripts; // switched off during game over so no input gets through
    [SerializeField] private GameObject gameOverScreen;
    [SerializeField] private TMP_Text gameOverText;
    [SerializeField] private GameObject checkpointNotice;

    private const float NoticeTime = 2f; // real s the checkpoint notice stays up

    private readonly List<MonoBehaviour> savedObjects = new List<MonoBehaviour>(); // everything that saved its state at the checkpoint
    private Vector3 checkpointPosition;
    private Quaternion checkpointRotation;
    private bool isOver;
    private float respawnTime; // real time when the respawn happens
    private float noticeEndTime; // real time when the checkpoint notice hides

    // there is only one of these
    private void Awake()
    {
        Instance = this;
        gameOverScreen.SetActive(false);
        checkpointNotice.SetActive(false);
    }

    // the level start is the first checkpoint
    private void Start()
    {
        SaveCheckpoint(player.transform.position, player.transform.rotation);
    }

    // waits in real time because game time is frozen during game over
    private void Update()
    {
        if (isOver && Time.unscaledTime >= respawnTime) Respawn();
        if (checkpointNotice.activeSelf && Time.unscaledTime >= noticeEndTime) checkpointNotice.SetActive(false);
    }

    // a checkpoint trigger saves the spot and the level and says so
    public void SetCheckpoint(Vector3 position, Quaternion rotation)
    {
        SaveCheckpoint(position, rotation);
        checkpointNotice.SetActive(true);
        noticeEndTime = Time.unscaledTime + NoticeTime;
    }

    // freezes the game shows why it ended and respawns after a short wait
    public void GameOver(string reason)
    {
        if (isOver) return;
        isOver = true;
        TimeScaleManager.Instance.Request(this, 0f);
        foreach (Behaviour script in playerScripts) script.enabled = false;
        hud.CrosshairVisible = false;
        gameOverText.text = reason;
        gameOverScreen.SetActive(true);
        respawnTime = Time.unscaledTime + settings.gameOverDelay;
    }

    // remembers the spot and asks everything in the loaded scenes that can be saved to save itself
    private void SaveCheckpoint(Vector3 position, Quaternion rotation)
    {
        checkpointPosition = position;
        checkpointRotation = rotation;
        savedObjects.Clear();

        // switched off objects count too like taken pickups and a carried guard
        foreach (MonoBehaviour behaviour in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (behaviour is ICheckpointState state)
            {
                state.SaveState();
                savedObjects.Add(behaviour);
            }
        }
    }

    // puts the level and the player back the way they were at the checkpoint
    private void Respawn()
    {
        // a body on the shoulder goes back through its own restore
        playerInteract.ForgetCarried();

        // gadgets thrown since the checkpoint go away because the inventory gets them back
        foreach (ThrownGadget gadget in FindObjectsByType<ThrownGadget>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            Destroy(gadget.gameObject);
        foreach (Decoy decoy in FindObjectsByType<Decoy>(FindObjectsSortMode.None))
            Destroy(decoy.gameObject);

        foreach (MonoBehaviour behaviour in savedObjects)
        {
            if (behaviour != null) ((ICheckpointState)behaviour).RestoreState();
        }

        player.TeleportTo(checkpointPosition);
        player.transform.rotation = checkpointRotation;
        playerHealth.Restore();

        gameOverScreen.SetActive(false);
        hud.CrosshairVisible = true;
        foreach (Behaviour script in playerScripts) script.enabled = true;
        TimeScaleManager.Instance.Release(this);
        isOver = false;
    }
}