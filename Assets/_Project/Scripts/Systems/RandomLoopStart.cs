using UnityEngine;

// starts a looping sound at a random point so copies of it never play in step
[RequireComponent(typeof(AudioSource))]
public class RandomLoopStart : MonoBehaviour
{
    // plays the loop and jumps to a random spot in it
    private void Start()
    {
        AudioSource source = GetComponent<AudioSource>();
        source.Play();
        source.time = Random.Range(0f, source.clip.length);
    }
}