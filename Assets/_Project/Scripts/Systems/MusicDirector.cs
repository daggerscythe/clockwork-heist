using UnityEngine;

// plays the chase music while a guard chases the player and the alarm run music while a whistleblower runs
public class MusicDirector : MonoBehaviour
{
    [SerializeField] private AudioSource chaseMusic; // looping source on the music group
    [SerializeField] private AudioSource alarmMusic; // looping source on the music group

    private bool wasChased; // a guard was chasing last frame
    private bool chasePlaying;
    private bool alarmPlaying;

    // looks at every guard and moves the music to match
    private void Update()
    {
        bool chased = false;
        bool alarmRun = false;
        foreach (EnemyHealth guard in EnemyHealth.All)
        {
            if (!guard.IsAlive) continue;
            EnemyAI ai = guard.GetComponent<EnemyAI>();
            if (ai.IsRaisingAlarm) alarmRun = true;
            else if (ai.IsAlerted) chased = true;
        }

        // the run ending stops the music so the game over sound plays alone
        bool over = GameManager.Instance.IsOver;
        if (chased && !wasChased && !over) Sfx.Flat(Sfx.Bank.spottedSting, 0.5f);
        wasChased = chased;

        // the alarm run music starts and stops at once
        bool alarmOn = alarmRun && !over;
        if (alarmOn != alarmPlaying)
        {
            alarmPlaying = alarmOn;
            if (alarmOn) alarmMusic.Play();
            else alarmMusic.Stop();
        }

        // the chase music fades in and out and goes quiet under the alarm run
        bool chaseOn = chased && !alarmRun && !over;
        if (chaseOn && !chasePlaying)
        {
            chasePlaying = true;
            chaseMusic.volume = 0f;
            chaseMusic.Play();
        }
        chaseMusic.volume = Mathf.MoveTowards(chaseMusic.volume, chaseOn ? 1f : 0f, Time.unscaledDeltaTime / Sfx.Bank.musicFade);
        if (chasePlaying && !chaseOn && chaseMusic.volume <= 0f)
        {
            chasePlaying = false;
            chaseMusic.Stop();
        }
    }
}