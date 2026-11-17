using System.Collections.Generic;
using UnityEngine;

// points an arrow at the screen edge toward every whistleblower running for an alarm
public class WhistleblowerArrowUI : MonoBehaviour
{
    [SerializeField] private RectTransform area; // the hud canvas the arrows move around in
    [SerializeField] private RectTransform arrowTemplate; // hidden arrow copied once per runner
    [SerializeField] private GameObject warning; // text shown while anyone is running

    private const float EdgeMargin = 60f; // px kept between an arrow and the screen edge
    private const float AimHeight = 1f; // m above a guard's feet the arrow points at

    private readonly List<RectTransform> arrows = new List<RectTransform>(); // copies made so far
    private readonly List<EnemyAI> runners = new List<EnemyAI>(); // whistleblowers running this frame
    private Transform cameraTransform;

    // find the camera and hide the template
    private void Start()
    {
        cameraTransform = Camera.main.transform;
        arrowTemplate.gameObject.SetActive(false);
    }

    // after everything has moved so the arrows never lag behind
    private void LateUpdate()
    {
        runners.Clear();
        foreach (EnemyHealth guard in EnemyHealth.All)
        {
            EnemyAI ai = guard.GetComponent<EnemyAI>();
            if (ai.IsRaisingAlarm) runners.Add(ai);
        }

        warning.SetActive(runners.Count > 0);

        // one arrow per runner and spare ones stay hidden
        while (arrows.Count < runners.Count) arrows.Add(Instantiate(arrowTemplate, arrowTemplate.parent));
        for (int i = 0; i < arrows.Count; i++)
        {
            bool used = i < runners.Count;
            arrows[i].gameObject.SetActive(used);
            if (used) PlaceArrow(arrows[i], runners[i].transform.position + Vector3.up * AimHeight);
        }
    }

    // puts an arrow on the screen edge in the direction of a world point and turns it to point that way
    private void PlaceArrow(RectTransform arrow, Vector3 target)
    {
        Vector3 local = cameraTransform.InverseTransformPoint(target);
        Vector2 direction = new Vector2(local.x, local.y);

        // straight ahead or straight behind gives no direction so point down
        if (direction.sqrMagnitude < 0.0001f) direction = Vector2.down;
        direction.Normalize();

        // slide out from the center until the arrow meets the edge rectangle
        Vector2 half = area.rect.size / 2f - Vector2.one * EdgeMargin;
        float reach = Mathf.Min(half.x / Mathf.Abs(direction.x), half.y / Mathf.Abs(direction.y));
        arrow.anchoredPosition = direction * reach;

        // the arrow sprite points down so turn it a quarter more than the direction
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        arrow.localRotation = Quaternion.Euler(0f, 0f, angle + 90f);
    }
}