using TMPro;
using UnityEngine;
using UnityEngine.UI;

// hides the smoked glass plate behind a text while the text is empty
[RequireComponent(typeof(Image))]
public class TextPlate : MonoBehaviour
{
    [SerializeField] private TMP_Text text;

    private Image plate;

    // grab the plate image
    private void Awake()
    {
        plate = GetComponent<Image>();
    }

    // after the text was set this frame
    private void LateUpdate()
    {
        plate.enabled = text.text != "";
    }
}