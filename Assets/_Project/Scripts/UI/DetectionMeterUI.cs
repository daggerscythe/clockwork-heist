using UnityEngine;
using UnityEngine.UI;

// shows a guard's detection meter above its head facing the camera
public class DetectionMeterUI : MonoBehaviour
{
    [SerializeField] private EnemySenses senses;
    [SerializeField] private Canvas canvas;
    [SerializeField] private Image fill;
    [SerializeField] private Color fillingColor = Color.yellow; // while the meter is going up
    [SerializeField] private Color fullColor = Color.red; // when the player is fully detected

    private Transform cameraTransform;

    // find the camera once
    private void Start()
    {
        cameraTransform = Camera.main.transform;
    }

    // runs after everything has moved so the meter never lags behind
    private void LateUpdate()
    {
        canvas.enabled = senses.Detection > 0f;
        fill.fillAmount = senses.Detection;
        fill.color = senses.Detection >= 1f ? fullColor : fillingColor;

        // match the camera so the meter always lies flat toward the screen
        transform.rotation = cameraTransform.rotation;
    }
}