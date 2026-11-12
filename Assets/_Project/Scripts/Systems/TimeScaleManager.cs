using System.Collections.Generic;
using UnityEngine;

// the only script that changes the game speed
public class TimeScaleManager : MonoBehaviour
{
    public static TimeScaleManager Instance { get; private set; }

    // who asked for a speed and what speed they asked for with the newest last
    private readonly List<(Object owner, float scale)> requests = new List<(Object owner, float scale)>();
    private float normalFixedStep; // s between physics steps at normal speed

    private void Awake()
    {
        Instance = this;
        normalFixedStep = Time.fixedDeltaTime;
    }

    // ask for a game speed and asking again replaces the old request
    public void Request(Object owner, float scale)
    {
        requests.RemoveAll(request => request.owner == owner);
        requests.Add((owner, scale));
        Apply();
    }

    // drop a request when the owner is done with it
    public void Release(Object owner)
    {
        requests.RemoveAll(request => request.owner == owner);
        Apply();
    }

    // newest request wins and no requests means normal speed
    private void Apply()
    {
        float scale = requests.Count > 0 ? requests[requests.Count - 1].scale : 1f;
        Time.timeScale = scale;

        // physics steps shrink too so slow motion stays smooth
        Time.fixedDeltaTime = normalFixedStep * scale;
    }

    // put the speed back when the scene closes
    private void OnDestroy()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = normalFixedStep;
    }
}