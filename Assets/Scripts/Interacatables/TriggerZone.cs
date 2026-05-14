using UnityEngine;
using UnityEngine.Events;

// The workhorse of your level design. Attach to an empty GameObject
// with a Collider (any shape) set to "Is Trigger".
//
// In the Inspector, hook up UnityEvents to:
//  - SoundManager (PlayAmbient, PlayStinger)
//  - Visitor (Appear, SpeakLine, StartWalkingTowardPlayer)
//  - Doors (open/close)
//  - Lights (turn off, flicker)
//  - PerceptionHack (TriggerHack)
//  - Anything else that reacts to the player entering a zone.
//
// This is exactly the pattern from Course 12 slide 6 (Salim's workshop).

[RequireComponent(typeof(Collider))]
public class TriggerZone : MonoBehaviour
{
    [Header("Trigger settings")]
    [SerializeField] private string playerTag = "Player";
    [Tooltip("If true, the Enter event only fires the first time the player enters " +
             "(after skipping any enters specified by Enters To Skip).")]
    [SerializeField] private bool fireOnce = true;
    [Tooltip("If true, the Exit event only fires the first time the player exits. " +
             "Tick this for one-shot beats like the door bang sequence.")]
    [SerializeField] private bool fireExitOnce = false;
    [Tooltip("Number of enters to ignore before the Enter event starts firing. " +
             "0 = fire on first enter (default). 1 = skip first enter, fire on second. " +
             "Useful when the player has to revisit a zone before something happens.")]
    [SerializeField] private int entersToSkip = 0;
    [Tooltip("If false, the trigger ignores enter/exit events until Arm() is called externally. " +
             "Use to gate triggers behind story progress (e.g. only activate after Sophie's chase begins).")]
    [SerializeField] private bool startsArmed = true;

    [Header("Events")]
    [Tooltip("Fires when the player first enters this zone.")]
    [SerializeField] private UnityEvent onPlayerEnter;

    [Tooltip("Fires when the player leaves (useful for ambient crossfades, " +
             "or one-shot beats if fireExitOnce is checked).")]
    [SerializeField] private UnityEvent onPlayerExit;

    private bool hasFired = false;
    private bool hasExited = false;
    private int enterCount = 0;
    private bool isArmed = true;

    public bool IsArmed => isArmed;

    private void Awake()
    {
        isArmed = startsArmed;
    }

    private void Reset()
    {
        // Auto-configure the collider as a trigger when the script is added.
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isArmed) return;
        // Starter Assets puts the CharacterController on the root GameObject
        // which is tagged "Player" — so CompareTag works correctly.
        if (!other.CompareTag(playerTag)) return;

        // Skip the first N enters if configured (e.g. fire on second visit).
        if (enterCount < entersToSkip)
        {
            enterCount++;
            return;
        }

        if (fireOnce && hasFired) return;

        hasFired = true;
        onPlayerEnter?.Invoke();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!isArmed) return;
        if (!other.CompareTag(playerTag)) return;
        if (fireExitOnce && hasExited) return;

        hasExited = true;
        onPlayerExit?.Invoke();
    }

    public void ResetTrigger()
    {
        hasFired = false;
        hasExited = false;
        enterCount = 0;
    }

    // Wire to a story event (e.g. Sophie's chase start) to enable this trigger.
    public void Arm()
    {
        isArmed = true;
    }

    // Wire to disable a trigger after a beat is over (e.g. when player escapes).
    public void Disarm()
    {
        isArmed = false;
    }

    // Makes the zone visible in Scene view so designers can place it.
    private void OnDrawGizmos()
    {
        Gizmos.color = hasFired ? new Color(1f, 0.3f, 0.3f, 0.25f) : new Color(0.3f, 1f, 0.5f, 0.25f);
        Collider col = GetComponent<Collider>();
        if (col is BoxCollider box)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
        }
    }
}
