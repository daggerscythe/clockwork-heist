using TMPro;
using UnityEngine;
using UnityEngine.UI;

// the list of every note the player has found shared by the pause menu and the main menu
public class NotesPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text entryTemplate; // hidden text in the scroll content copied once per note
    [SerializeField] private GameObject emptyText; // shown while no note has been found
    [SerializeField] private ScrollRect scroll; // starts at the top each time

    // fills the list every time the panel opens
    private void OnEnable()
    {
        Transform list = entryTemplate.transform.parent;
        for (int i = list.childCount - 1; i >= 0; i--)
        {
            Transform child = list.GetChild(i);
            if (child != entryTemplate.transform) Destroy(child.gameObject);
        }

        entryTemplate.gameObject.SetActive(false);
        foreach (string note in Notes.All)
        {
            TMP_Text entry = Instantiate(entryTemplate, list);
            entry.text = note;
            entry.gameObject.SetActive(true);
        }
        emptyText.SetActive(Notes.All.Count == 0);

        Canvas.ForceUpdateCanvases();
        scroll.verticalNormalizedPosition = 1f;
    }

    // the back button and esc
    public void Close()
    {
        gameObject.SetActive(false);
    }
}