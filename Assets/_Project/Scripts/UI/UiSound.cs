using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// plays the menu hover and click sounds on a button or toggle
public class UiSound : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    [SerializeField] private bool hover = true; // untick for controls that only click

    private Selectable control;

    // grab the button or toggle so a greyed out one stays quiet
    private void Awake()
    {
        control = GetComponent<Selectable>();
    }

    // the pointer moved onto it
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hover && IsLive()) Sfx.Ui(Sfx.Bank.uiHover);
    }

    // it was clicked
    public void OnPointerClick(PointerEventData eventData)
    {
        if (IsLive()) Sfx.Ui(Sfx.Bank.uiClick);
    }

    // true unless it is greyed out
    private bool IsLive()
    {
        return control == null || control.IsInteractable();
    }
}