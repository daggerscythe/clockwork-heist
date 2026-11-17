using UnityEngine;
using UnityEngine.UI;

// shows a guard's detection meter above its head facing the camera
public class DetectionMeterUI : MonoBehaviour
{
    [SerializeField] private EnemySenses senses;
    [SerializeField] private EnemyAI ai;
    [SerializeField] private Canvas canvas;
    [SerializeField] private Image fill;
    [SerializeField] private Color fillingColor = Color.yellow; // while the meter is going up
    [SerializeField] private Color fullColor = Color.red; // while the guard is alerted or raising alarm

    private Transform cameraTransform;

    // find the camera once
    private void Start()
    {
        cameraTransform = Camera.main.transform;
    }

    // runs after everything has moved so the meter never lags behind
    private void LateUpdate()
    {
        // hidden only while the guard is calm and has not noticed anything
        canvas.enabled = senses.Detection > 0f || !ai.IsCalm;
        bool alarmed = ai.IsAlerted || ai.IsRaisingAlarm;
        fill.fillAmount = alarmed ? 1f : senses.Detection;
        fill.color = alarmed ? fullColor : fillingColor;

        // match the camera so the meter always lies flat toward the screen
        transform.rotation = cameraTransform.rotation;
    }
}