using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// a wall alarm a whistleblower can pull to lock the building down
public class Alarm : MonoBehaviour
{
    [SerializeField] private Transform pullPoint; // where a guard stands to pull it

    private static readonly List<Alarm> all = new List<Alarm>(); // every alarm in the loaded scenes

    // spot on the floor in front of the alarm
    public Vector3 PullPoint => pullPoint.position;

    // join the list while in the world
    private void OnEnable()
    {
        all.Add(this);
    }

    // leave the list when the floor unloads
    private void OnDisable()
    {
        all.Remove(this);
    }

    // the nearest alarm on the same floor in a straight line or null when that floor has none
    public static Alarm Closest(Vector3 from, Scene floor)
    {
        Alarm closest = null;
        float best = float.MaxValue;
        foreach (Alarm alarm in all)
        {
            // every floor is its own scene so alarms on other floors never count
            if (alarm.gameObject.scene != floor) continue;

            float distance = (alarm.PullPoint - from).sqrMagnitude;
            if (distance < best)
            {
                best = distance;
                closest = alarm;
            }
        }
        return closest;
    }

    // locks the building down which ends the run
    public void Pull()
    {
        GameManager.Instance.GameOver("The alarm was raised");
    }
}