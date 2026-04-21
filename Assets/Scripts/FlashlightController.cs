using UnityEngine;
using UnityEngine.InputSystem;

// Flashlight with battery drain. Unity 6 / Input System version.
//
// Attach to the player (or any GameObject) and assign a child Light component
// (type: Spot) to the flashlightLight field.
//
// Default: press F to toggle. You can rebind in the Inspector by changing toggleKey.
// Battery drains while on, regenerates slowly while off (regenRate = 0 for harder mode).
//
// Public Battery property (0..1) is available for UI bars.

public class FlashlightController : MonoBehaviour
{
    [Header("Light")]
    [SerializeField] private Light flashlightLight;
    [SerializeField] private Key toggleKey = Key.F;

    [Header("Battery")]
    [SerializeField] private float maxBattery = 100f;
    [SerializeField] private float drainPerSecond = 2f;
    [SerializeField] private float regenPerSecond = 0f;

    [Header("Audio (optional)")]
    [SerializeField] private AudioSource clickSource;
    [SerializeField] private AudioClip onClip;
    [SerializeField] private AudioClip offClip;

    private float currentBattery;
    private bool isOn = false;

    public float Battery => currentBattery / maxBattery;
    public bool IsOn => isOn;

    private void Awake()
    {
        currentBattery = maxBattery;
        if (flashlightLight != null)
        {
            flashlightLight.enabled = false;
        }
    }

    private void Update()
    {
        // Unity 6 Input System: Keyboard.current reads the keyboard directly.
        if (Keyboard.current != null && Keyboard.current[toggleKey].wasPressedThisFrame)
        {
            Toggle();
        }

        if (isOn)
        {
            currentBattery -= drainPerSecond * Time.deltaTime;
            if (currentBattery <= 0f)
            {
                currentBattery = 0f;
                SetOn(false);
            }
        }
        else if (regenPerSecond > 0f)
        {
            currentBattery = Mathf.Min(maxBattery, currentBattery + regenPerSecond * Time.deltaTime);
        }
    }

    public void Toggle()
    {
        if (!isOn && currentBattery <= 0f) return;
        SetOn(!isOn);
    }

    private void SetOn(bool on)
    {
        isOn = on;
        if (flashlightLight != null)
        {
            flashlightLight.enabled = on;
        }
        PlayClick(on ? onClip : offClip);
    }

    public void AddBattery(float amount)
    {
        currentBattery = Mathf.Min(maxBattery, currentBattery + amount);
    }

    private void PlayClick(AudioClip clip)
    {
        if (clickSource != null && clip != null)
        {
            clickSource.PlayOneShot(clip);
        }
    }
}
