using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// Simple delay helper for UnityEvent chains.
//
// Use case: a TriggerZone fires on player enter, but you want a 2-second pause
// before the next thing happens. Call FireAfter(2) on this script from the
// TriggerZone's onPlayerEnter, and put the delayed action in the onDelayElapsed event.
public class DelayedEvent : MonoBehaviour
{
    [Header("Default delay (used if you call Fire() without a parameter)")]
    [SerializeField] private float defaultDelay = 1f;

    [Header("Event")]
    [SerializeField] private UnityEvent onDelayElapsed;

    private bool isWaiting = false;

    // Call from a UnityEvent. Uses the defaultDelay set in the Inspector.
    public void Fire()
    {
        FireAfter(defaultDelay);
    }

    // Call from a UnityEvent that supports a float parameter.
    public void FireAfter(float seconds)
    {
        if (isWaiting) return;
        StartCoroutine(WaitAndFire(seconds));
    }

    private IEnumerator WaitAndFire(float seconds)
    {
        isWaiting = true;
        yield return new WaitForSeconds(seconds);
        isWaiting = false;
        onDelayElapsed?.Invoke();
    }
}