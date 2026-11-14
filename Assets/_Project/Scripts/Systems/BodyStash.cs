using UnityEngine;

// a crate or closet that hides bodies from guards
public class BodyStash : MonoBehaviour
{
    [SerializeField] private Transform exitPoint; // where a hidden guard climbs out when it wakes

    // spot and facing a hidden body is kept at
    public Transform ExitPoint => exitPoint;
}