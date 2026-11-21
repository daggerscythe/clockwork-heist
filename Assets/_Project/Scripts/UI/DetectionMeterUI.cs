using UnityEngine;
using UnityEngine.UI;

// shows a guard's detection meter above its head facing the camera
public class DetectionMeterUI : MonoBehaviour
{
    [SerializeField] private EnemySenses senses;
    [SerializeField] private EnemyAI ai;
    [SerializeField] private Canvas canvas;
    [SerializeField] private Image fill;
    [SerializeField] private GameObject eye; // shown in the middle while the guard is not alerted
    [SerializeField] private GameObject alertMark; // shown in the middle while the guard is alerted or raising the alarm
    [SerializeField] private Color fillingColor = new Color32(232, 176, 64, 255); // while the meter is going up
    [SerializeField] private Color fullColor = new Color32(200, 48, 40, 255); // while the guard is alerted or raising alarm

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
        eye.SetActive(!alarmed);
        alertMark.SetActive(alarmed);

        // match the camera so the meter always lies flat toward the screen
        transform.rotation = cameraTransform.rotation;
    }
}