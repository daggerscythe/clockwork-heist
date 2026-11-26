using UnityEngine;

// which footstep sounds a floor makes
public enum Surface { Marble, Wood, Metal, Grate }

// put on a floor stair or walkway so footsteps on it sound right and floors without one sound like marble
public class FloorSurface : MonoBehaviour
{
    [SerializeField] private Surface kind = Surface.Marble;

    // what this floor is made of
    public Surface Kind => kind;
}