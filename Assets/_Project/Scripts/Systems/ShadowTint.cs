using UnityEngine;

// gives every unlit surface a dark purple tint also in the editor so lamps can be placed by eye
[ExecuteAlways]
public class ShadowTint : MonoBehaviour
{
    [SerializeField] private Color tint = new Color(30f / 255f, 18f / 255f, 55f / 255f); // dark purple that unlit parts lean toward
    [SerializeField, Range(0f, 1f)] private float amount = 0.4f; // how far unlit parts lean toward it and 0 turns it off

    // the tint is in place as soon as the scene opens
    private void OnEnable()
    {
        Apply();
    }

    // scenes without this component like the main menu get no tint
    private void OnDisable()
    {
        Shader.SetGlobalFloat("_ShadowTintAmount", 0f);
    }

    // changing a value in the inspector shows at once
    private void OnValidate()
    {
        Apply();
    }

    // hands the values to every toon material
    private void Apply()
    {
        Shader.SetGlobalColor("_ShadowTint", tint);
        Shader.SetGlobalFloat("_ShadowTintAmount", amount);
    }
}