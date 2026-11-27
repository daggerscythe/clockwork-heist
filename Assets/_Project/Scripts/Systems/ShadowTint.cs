using UnityEngine;

// sets the purple every toon surface gets where no light reaches also in the editor so shadows can be placed by eye
[ExecuteAlways]
public class ShadowTint : MonoBehaviour
{
    [SerializeField] private Color tint = new Color(120f / 255f, 100f / 255f, 160f / 255f); // added to unlit surfaces and black turns it off

    // the tint is in place as soon as the scene opens
    private void OnEnable()
    {
        Apply();
    }

    // changing the color in the inspector shows at once
    private void OnValidate()
    {
        Apply();
    }

    // hands the color to every toon material
    private void Apply()
    {
        Shader.SetGlobalColor("_ShadowTint", tint);
    }
}