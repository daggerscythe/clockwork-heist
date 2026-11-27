using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// tells if a point is inside a lamp's light the same way the purple air shows it
public static class Lamplight
{
    private const float FixtureClearance = 0.5f; // m around the bulb where a hit is the lamp's own fixture and not a wall

    private static Light[] lamps; // every spot and point light in the loaded floors
    private static int blockMask; // walls floors and props that stop light

    // finds the lamps again whenever a floor loads or unloads
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        lamps = null;
        SceneManager.sceneLoaded += (scene, mode) => lamps = null;
        SceneManager.sceneUnloaded += scene => lamps = null;
    }

    // true when a lamp reaches the point within its range and cone with no wall in between
    public static bool IsLit(Vector3 point)
    {
        if (lamps == null) FindLamps();
        foreach (Light lamp in lamps)
        {
            if (lamp == null || !lamp.isActiveAndEnabled || lamp.intensity <= 0f) continue;

            Vector3 toPoint = point - lamp.transform.position;
            float distance = toPoint.magnitude;
            if (distance > lamp.range) continue;
            if (lamp.type == LightType.Spot && Vector3.Angle(lamp.transform.forward, toPoint) > lamp.spotAngle / 2f) continue;

            // a wall between the point and the bulb keeps the point dark
            if (distance > FixtureClearance && Physics.Raycast(point, -toPoint / distance, distance - FixtureClearance, blockMask, QueryTriggerInteraction.Ignore)) continue;
            return true;
        }
        return false;
    }

    // collects every spot and point light since the moon never counts
    private static void FindLamps()
    {
        blockMask = ~LayerMask.GetMask("Player", "Enemy", "ShadowVolume", "Interactable", "Ignore Raycast");
        List<Light> found = new List<Light>();
        foreach (Light light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (light.type == LightType.Spot || light.type == LightType.Point) found.Add(light);
        lamps = found.ToArray();
    }
}