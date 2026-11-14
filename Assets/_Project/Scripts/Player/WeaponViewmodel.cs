using UnityEngine;

// bobs the first person weapon while walking and animates switching and swinging
public class WeaponViewmodel : MonoBehaviour
{
    [SerializeField] private PlayerController player;
    [SerializeField] private GameObject blade;
    [SerializeField] private GameObject club;

    private const float BobSpeed = 10f; // radians per s of the bob wave
    private const float BobHeight = 0.015f; // m up and down while moving
    private const float SwitchDrop = 0.4f; // m the weapon drops out of view while switching
    private const float SwingAngle = 70f; // degrees the weapon chops forward

    private Vector3 restPosition;
    private Quaternion restRotation;
    private float bobTime; // s spent moving
    private float switchDuration; // s the current switch takes
    private float switchLeft; // s left of the switch
    private bool switchToBlade;
    private float swingDuration; // s the current swing takes
    private float swingLeft; // s left of the swing

    // remember where the hand rests
    private void Awake()
    {
        restPosition = transform.localPosition;
        restRotation = transform.localRotation;
    }

    // shows one weapon and hides the other right away
    public void Show(bool bladeOut)
    {
        blade.SetActive(bladeOut);
        club.SetActive(!bladeOut);
    }

    // drops the weapon out of view then raises the other one
    public void PlaySwitch(bool toBlade, float duration)
    {
        switchToBlade = toBlade;
        switchDuration = duration;
        switchLeft = duration;
    }

    // chops the weapon forward and back
    public void PlaySwing(float duration)
    {
        swingDuration = duration;
        swingLeft = duration;
    }

    // adds the bob the switch drop and the swing angle on top of the rest pose
    private void Update()
    {
        if (player.IsMoving) bobTime += Time.deltaTime;
        float bob = Mathf.Sin(bobTime * BobSpeed) * BobHeight;
        float drop = 0f;
        float angle = 0f;

        if (switchLeft > 0f)
        {
            switchLeft -= Time.deltaTime;
            float t = 1f - Mathf.Max(switchLeft, 0f) / switchDuration;

            // swap at the lowest point so the change is never seen
            if (t >= 0.5f) Show(switchToBlade);
            drop = Mathf.Sin(t * Mathf.PI) * SwitchDrop;
        }

        if (swingLeft > 0f)
        {
            swingLeft -= Time.deltaTime;
            float t = 1f - Mathf.Max(swingLeft, 0f) / swingDuration;
            angle = Mathf.Sin(t * Mathf.PI) * SwingAngle;
        }

        transform.localPosition = restPosition + Vector3.up * (bob - drop);
        transform.localRotation = restRotation * Quaternion.Euler(angle, 0f, 0f);
    }
}