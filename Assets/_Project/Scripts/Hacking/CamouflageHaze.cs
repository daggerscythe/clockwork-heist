using UnityEngine;

// shimmers the screen edges while the player stands where a hacked camera is looking
public class CamouflageHaze : MonoBehaviour
{
    private const float FadeSpeed = 4f; // full haze per real s so a fade takes a quarter second

    private static readonly int StrengthId = Shader.PropertyToID("_HazeStrength");

    private float strength; // 0 to 1

    // fades toward full while masked and toward none when not
    private void Update()
    {
        float goal = IsMasked() ? 1f : 0f;
        strength = Mathf.MoveTowards(strength, goal, FadeSpeed * Time.unscaledDeltaTime);
        Shader.SetGlobalFloat(StrengthId, strength);
    }

    // no haze left on screen once the scene closes
    private void OnDisable()
    {
        Shader.SetGlobalFloat(StrengthId, 0f);
    }

    // true when any hacked camera can see the player right now
    private static bool IsMasked()
    {
        foreach (SecurityCamera securityCamera in SecurityCamera.All)
        {
            if (securityCamera.IsHacked && securityCamera.SeesPlayer) return true;
        }
        return false;
    }
}