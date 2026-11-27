using UnityEngine;

// guides the player to the next tutorial step from the screen edge and bounces over the step once it is in view
public class TutorialArrow : MonoBehaviour
{
    [SerializeField] private RectTransform area; // the hud canvas the arrow moves around in
    [SerializeField] private RectTransform arrow; // image that points down
    [SerializeField] private float edgeMargin = 70f; // px kept between the arrow and the screen edge
    [SerializeField] private float hoverHeight = 60f; // px above the step while it is in view
    [SerializeField] private float bounceHeight = 18f; // px the arrow bounces
    [SerializeField] private float bounceSpeed = 1.5f; // bounces per s

    private Tutorial tutorial; // the floor's tutorial or null on floors without one
    private HUD hud;
    private Camera view;

    // find this floor's tutorial
    private void Start()
    {
        tutorial = FindAnyObjectByType<Tutorial>();
        hud = GetComponent<HUD>();
        view = Camera.main;
    }

    // after everything has moved so the arrow never lags behind
    private void LateUpdate()
    {
        // only during normal play so menus notes the hack screen and game over stay clear
        TutorialStep step = tutorial != null ? tutorial.Current : null;
        bool show = step != null && hud.CrosshairVisible;
        arrow.gameObject.SetActive(show);
        if (!show) return;

        Vector3 target = step.ArrowPoint;
        Vector3 viewport = view.WorldToViewportPoint(target);
        bool inView = viewport.z > 0f && viewport.x > 0f && viewport.x < 1f && viewport.y > 0f && viewport.y < 1f;
        if (inView) PlaceOver(viewport);
        else PlaceOnEdge(target);
    }

    // bounces the arrow just above the step pointing down at it
    private void PlaceOver(Vector3 viewport)
    {
        Vector2 size = area.rect.size;
        Vector2 position = new Vector2((viewport.x - 0.5f) * size.x, (viewport.y - 0.5f) * size.y);
        position.y += hoverHeight + Mathf.Abs(Mathf.Sin(Time.unscaledTime * Mathf.PI * bounceSpeed)) * bounceHeight;

        // a step right at the screen edge keeps the arrow on screen
        Vector2 half = size / 2f - Vector2.one * edgeMargin;
        position.x = Mathf.Clamp(position.x, -half.x, half.x);
        position.y = Mathf.Clamp(position.y, -half.y, half.y);
        arrow.anchoredPosition = position;
        arrow.localRotation = Quaternion.identity;
    }

    // puts the arrow on the screen edge in the direction of the step and turns it to point that way
    private void PlaceOnEdge(Vector3 target)
    {
        Vector3 local = view.transform.InverseTransformPoint(target);
        Vector2 direction = new Vector2(local.x, local.y);

        // straight ahead or straight behind gives no direction so point down
        if (direction.sqrMagnitude < 0.0001f) direction = Vector2.down;
        direction.Normalize();

        // slide out from the center until the arrow meets the edge rectangle
        Vector2 half = area.rect.size / 2f - Vector2.one * edgeMargin;
        float reach = Mathf.Min(half.x / Mathf.Abs(direction.x), half.y / Mathf.Abs(direction.y));
        arrow.anchoredPosition = direction * reach;

        // the sprite points down so turn it a quarter more than the direction
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        arrow.localRotation = Quaternion.Euler(0f, 0f, angle + 90f);
    }
}