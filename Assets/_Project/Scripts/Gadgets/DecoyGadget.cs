using UnityEngine;

// the thrown decoy that turns into a ghost copy wherever it lands
public class DecoyGadget : ThrownGadget
{
    [SerializeField] private Decoy decoyPrefab;

    private const float MinFloorUp = 0.7f; // how much a surface must face up to stand on
    private const float WallGap = 0.3f; // m out from a wall to start looking down for floor
    private const float FloorSearch = 10f; // m down to look for floor under a wall hit

    // given back to the inventory as a decoy
    protected override Ability Kind => Ability.Decoy;

    // stands where it lands on a floor or drops to the floor under a wall or guard it hit
    protected override void Land(RaycastHit hit)
    {
        Vector3 spot = hit.point;
        if (hit.normal.y < MinFloorUp && Physics.Raycast(hit.point + hit.normal * WallGap, Vector3.down, out RaycastHit floor, FloorSearch, solidMask, QueryTriggerInteraction.Ignore))
            spot = floor.point;

        Instantiate(decoyPrefab, spot, Quaternion.identity);
        Sfx.At(Sfx.Bank.decoySpawn, spot);
        Destroy(gameObject);
    }
}