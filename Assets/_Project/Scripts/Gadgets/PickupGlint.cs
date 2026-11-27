using UnityEngine;

// makes a pickup easy to spot with a slow spin a small bob and a twinkle above it
public class PickupGlint : MonoBehaviour
{
    [SerializeField] private Transform model; // the visible part that spins and bobs or empty to keep it still
    [SerializeField] private Transform glint; // sprite that always faces the camera and twinkles
    [SerializeField] private float glintHeight = 0.35f; // m above the pickup's pivot
    [SerializeField] private float spinSpeed = 60f; // degrees per s
    [SerializeField] private float bobHeight = 0.06f; // m the model rises and falls
    [SerializeField] private float bobTime = 2f; // s for one rise and fall
    [SerializeField] private float twinkleTime = 1.5f; // s between twinkles

    private Vector3 modelStart; // where the model sits at rest
    private Vector3 glintSize; // full glint scale
    private float phase; // random start so pickups next to each other don't twinkle together
    private Transform view;

    // remember the rest pose and pick a start point
    private void Start()
    {
        if (model != null) modelStart = model.localPosition;
        glintSize = glint.localScale;
        phase = Random.value * twinkleTime;
        view = Camera.main.transform;
    }

    // spin bob and twinkle in game time so slow motion slows them too
    private void Update()
    {
        float time = Time.time + phase;
        if (model != null)
        {
            model.localPosition = modelStart + Vector3.up * bobHeight * (0.5f - 0.5f * Mathf.Cos(time * 2f * Mathf.PI / bobTime));
            model.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
        }

        // a short sharp flash then a longer rest
        float flash = Mathf.Pow(Mathf.Abs(Mathf.Sin(time * Mathf.PI / twinkleTime)), 8f);
        glint.position = transform.position + Vector3.up * glintHeight;
        glint.rotation = view.rotation;
        glint.localScale = glintSize * Mathf.Lerp(0.35f, 1f, flash);
    }
}