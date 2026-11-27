using UnityEngine;

// finds where the player capsule can stand against a surface for blink and the teleport gadget
public static class TeleportSpot
{
    private const float SurfaceGap = 0.05f; // m between the capsule and the surface it lands on
    private const float NudgeStep = 0.1f; // m pushed away from the surface each try
    private const int NudgeTries = 3; // extra tries before the spot counts as blocked

    // pushes the capsule off the hit at any angle and nudges it until it fits and center is the last spot tried
    public static bool Find(RaycastHit hit, float radius, float height, int solidMask, out Vector3 center)
    {
        float halfHeight = height / 2f;
        float offset = radius + (halfHeight - radius) * Mathf.Abs(hit.normal.y) + SurfaceGap;
        center = hit.point + hit.normal * offset;

        bool fits = Fits(center, radius, halfHeight, solidMask);
        for (int i = 0; i < NudgeTries && !fits; i++)
        {
            center += hit.normal * NudgeStep;
            fits = Fits(center, radius, halfHeight, solidMask);
        }
        return fits;
    }

    // true when a capsule of this size fits with its center at this point
    public static bool FitsAt(Vector3 center, float radius, float height, int solidMask)
    {
        return Fits(center, radius, height / 2f, solidMask);
    }

    // true when the capsule at this center touches nothing
    private static bool Fits(Vector3 center, float radius, float halfHeight, int solidMask)
    {
        Vector3 toEnd = Vector3.up * (halfHeight - radius);
        return !Physics.CheckCapsule(center - toEnd, center + toEnd, radius, solidMask, QueryTriggerInteraction.Ignore);
    }
}