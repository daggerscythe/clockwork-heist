using UnityEngine;

// the thrown decoy that turns into a ghost copy where it lands on a floor
public class DecoyGadget : ThrownGadget
{
    [SerializeField] private Decoy decoyPrefab;

    private const float MinFloorUp = 0.7f; // how much the surface must face up to stand on

    // given back to the inventory as a decoy
    protected override Ability Kind => Ability.Decoy;

    // true for lit or dark teleportable surfaces that face up enough to stand on
    public static bool CanStandOn(RaycastHit hit)
    {
        return IsTeleportable(hit) && hit.normal.y >= MinFloorUp;
    }

    // becomes a ghost on a floor and fizzles anywhere else
    protected override void Land(RaycastHit hit)
    {
        if (CanStandOn(hit)) Instantiate(decoyPrefab, hit.point, Quaternion.identity);
        Destroy(gameObject);
    }
}