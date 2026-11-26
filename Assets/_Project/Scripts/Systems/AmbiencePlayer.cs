using UnityEngine;

// plays a random one shot from a floor's ambience every so often over its looping layers
public class AmbiencePlayer : MonoBehaviour
{
    [SerializeField] private AudioSource oneShotSource; // flat source on the ambience group
    [SerializeField] private AudioClip[] oneShots; // creaks or bell tolls
    [SerializeField] private float minGap = 10f; // s at least between one shots
    [SerializeField] private float maxGap = 25f; // s at most between one shots

    private float nextTime; // game time of the next one shot

    // the first one shot comes after a random gap
    private void Start()
    {
        nextTime = Time.time + Random.Range(minGap, maxGap);
    }

    // plays one and picks the next gap
    private void Update()
    {
        if (oneShots.Length == 0 || Time.time < nextTime) return;
        nextTime = Time.time + Random.Range(minGap, maxGap);
        oneShotSource.PlayOneShot(Sfx.Pick(oneShots));
    }
}