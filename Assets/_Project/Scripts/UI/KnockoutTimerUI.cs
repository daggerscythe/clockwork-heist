using UnityEngine;
using UnityEngine.UI;

// shows how long until a knocked out guard wakes up
public class KnockoutTimerUI : MonoBehaviour
{
    [SerializeField] private EnemyHealth health;
    [SerializeField] private Canvas canvas;
    [SerializeField] private Image fill;

    private Transform cameraTransform;

    // find the camera once
    private void Start()
    {
        cameraTransform = Camera.main.transform;
    }

    // runs after everything has moved so the timer never lags behind
    private void LateUpdate()
    {
        canvas.enabled = health.IsKnockedOut && !health.IsHidden;
        fill.fillAmount = health.WakeFraction;

        // match the camera so the timer always lies flat toward the screen
        transform.rotation = cameraTransform.rotation;
    }
}