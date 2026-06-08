using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// Sliding door that opens/closes on command.

public class SlidingDoor : MonoBehaviour
{
    [Header("Slide settings")]
    [Tooltip("Direction the door slides to open (local space). Vector3.right = slides right, Vector3.up = slides up.")]
    [SerializeField] private Vector3 slideDirection = Vector3.right;

    [Tooltip("How far the door slides, in meters.")]
    [SerializeField] private float slideDistance = 2f;

    [Tooltip("Time in seconds for a full open or close.")]
    [SerializeField] private float slideDuration = 1.2f;

    [Tooltip("Animation curve for the slide. Ease-in-out feels mechanical.")]
    [SerializeField] private AnimationCurve slideCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Locked state")]
    [Tooltip("If true, the door won't open when Open() is called. Useful for the airlock before keys are collected.")]
    [SerializeField] private bool isLocked = false;

    [Header("Audio (optional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip openClip;
    [SerializeField] private AudioClip closeClip;
    [SerializeField] private AudioClip lockedClip;

    [Header("Events")]
    [SerializeField] private UnityEvent onOpened;
    [SerializeField] private UnityEvent onClosed;
    [SerializeField] private UnityEvent onLockedAttempt;

    private Vector3 closedPosition;
    private Vector3 openPosition;
    private bool isOpen = false;
    private Coroutine slideRoutine;

    private void Awake()
    {
        // Record the starting position as "closed" and compute the open position.
        closedPosition = transform.localPosition;
        openPosition = closedPosition + slideDirection.normalized * slideDistance;
    }

    // Public API: call from UnityEvents, other scripts, whatever.
    public void Open()
    {
        Debug.Log($"SlidingDoor.Open() called on {name}. isLocked={isLocked}, isOpen={isOpen}, " +
                  $"closedPos={closedPosition}, openPos={openPosition}, currentPos={transform.localPosition}\n" +
                  $"STACK:\n{System.Environment.StackTrace}");
        if (isLocked)
        {
            PlayClip(lockedClip);
            onLockedAttempt?.Invoke();
            return;
        }
        if (isOpen) return;
        StartSlide(openPosition, true);
    }

    public void Close()
    {
        if (!isOpen) return;
        StartSlide(closedPosition, false);
    }

    public void Toggle()
    {
        if (isOpen) Close();
        else Open();
    }

    // Call this from a GameManager event when the player collects the last keycard.
    public void Unlock()
    {
        isLocked = false;
    }

    public void Lock()
    {
        isLocked = true;
    }

    private void StartSlide(Vector3 target, bool opening)
    {
        if (slideRoutine != null) StopCoroutine(slideRoutine);
        slideRoutine = StartCoroutine(Slide(target, opening));
    }

    private IEnumerator Slide(Vector3 target, bool opening)
    {
        PlayClip(opening ? openClip : closeClip);

        Vector3 start = transform.localPosition;
        float elapsed = 0f;

        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float k = slideCurve.Evaluate(elapsed / slideDuration);
            transform.localPosition = Vector3.Lerp(start, target, k);
            yield return null;
        }

        transform.localPosition = target;
        isOpen = opening;

        if (opening) onOpened?.Invoke();
        else onClosed?.Invoke();
    }

    private void PlayClip(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    // Visualize the open position in Scene view so designers can see where the door will end up.
    private void OnDrawGizmosSelected()
    {
        Vector3 startPos = Application.isPlaying ? closedPosition : transform.localPosition;
        Vector3 endPos = startPos + slideDirection.normalized * slideDistance;

        Vector3 worldStart = transform.parent != null
            ? transform.parent.TransformPoint(startPos)
            : startPos;
        Vector3 worldEnd = transform.parent != null
            ? transform.parent.TransformPoint(endPos)
            : endPos;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(worldStart, worldEnd);
        Gizmos.DrawWireSphere(worldEnd, 0.1f);
    }
}
