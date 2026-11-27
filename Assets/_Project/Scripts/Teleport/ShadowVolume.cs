using UnityEngine;

// a hand placed box of shadow the player can teleport into and where guards spot them slower drawn purple in the scene view
[RequireComponent(typeof(BoxCollider))]
public class ShadowVolume : MonoBehaviour
{
    private static readonly Color Fill = new Color(0.45f, 0.25f, 0.75f, 0.12f); // see through purple inside the box
    private static readonly Color Edge = new Color(0.6f, 0.35f, 0.95f, 0.8f); // outline of the box

    // only the editor draws it so the box stays invisible in the game
    private void OnDrawGizmos()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = Fill;
        Gizmos.DrawCube(box.center, box.size);
        Gizmos.color = Edge;
        Gizmos.DrawWireCube(box.center, box.size);
    }
}