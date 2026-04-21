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
    [SerializeField] private bool fireOnce = true;

    [Header("Events")]
    [Tooltip("Fires when the player first enters this zone.")]
    [SerializeField] private UnityEvent onPlayerEnter;

    [Tooltip("Fires when the player leaves (useful for ambient crossfades).")]
    [SerializeField] private UnityEvent onPlayerExit;

    private bool hasFired = false;

    private void Reset()
    {
        // Auto-configure the collider as a trigger when the script is added.
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Starter Assets puts the CharacterController on the root GameObject
        // which is tagged "Player" — so CompareTag works correctly.
        if (!other.CompareTag(playerTag)) return;
        if (fireOnce && hasFired) return;

        hasFired = true;
        onPlayerEnter?.Invoke();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        onPlayerExit?.Invoke();
    }

    public void ResetTrigger()
    {
        hasFired = false;
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
