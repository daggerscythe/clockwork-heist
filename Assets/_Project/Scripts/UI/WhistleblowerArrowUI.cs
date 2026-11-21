using System.Collections.Generic;
using UnityEngine;

// turns an arrow in one fixed spot toward every whistleblower running for an alarm so the player knows where to look
public class WhistleblowerArrowUI : MonoBehaviour
{
    [SerializeField] private RectTransform arrowTemplate; // hidden arrow copied once per runner and placed where the arrows sit
    [SerializeField] private GameObject warning; // text shown while anyone is running

    private const float AimHeight = 1f; // m above a guard's feet the arrow points at
    private const float OnScreenAngle = 25f; // degrees from straight ahead inside which the runner is in view and the arrow hides

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
        while (arrows.Count < runners.Count) arrows.Add(MakeArrow());
        for (int i = 0; i < arrows.Count; i++)
        {
            bool used = i < runners.Count;
            arrows[i].gameObject.SetActive(used && AimArrow(arrows[i], runners[i].transform.position + Vector3.up * AimHeight));
        }
    }

    // a copy of the template in the same spot and drawn where the template sits so the screens and menus above it stay on top
    private RectTransform MakeArrow()
    {
        RectTransform arrow = Instantiate(arrowTemplate, arrowTemplate.parent);
        arrow.SetSiblingIndex(arrowTemplate.GetSiblingIndex() + 1);
        return arrow;
    }

    // turns an arrow toward a world point and returns false when the point is already in front of the player
    private bool AimArrow(RectTransform arrow, Vector3 target)
    {
        // the direction to the runner seen from above with only the way the player faces taken out
        Vector3 toTarget = Quaternion.Inverse(Quaternion.Euler(0f, cameraTransform.eulerAngles.y, 0f)) * (target - cameraTransform.position);

        // 0 is straight ahead 90 is to the right and 180 is behind
        float angle = Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg;

        // the arrow sprite points down so half a turn makes it point ahead and it turns clockwise toward the right
        arrow.localRotation = Quaternion.Euler(0f, 0f, 180f - angle);
        return Mathf.Abs(angle) > OnScreenAngle;
    }
}