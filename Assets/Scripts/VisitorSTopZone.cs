using UnityEngine;
using UnityEngine.Events;

// Trigger zone that fires when a specific GameObject enters (not the player).
// Use this to stop Sophie when she reaches a certain point during the chase.
//
// Setup:
//  1. Create an empty GameObject in the bedroom between Sophie and the door.
//  2. Add a Box Collider with Is Trigger CHECKED.
//  3. Drag Sophie's GameObject into the targetObject field.
//  4. In onTargetEnter, wire VisitorController.StopMoving (and Disappear if you want).

[RequireComponent(typeof(Collider))]
public class VisitorStopZone : MonoBehaviour
{
    [Tooltip("The GameObject that triggers this zone when it enters (e.g. Sophie).")]
    [SerializeField] private GameObject targetObject;

    [SerializeField] private bool fireOnce = true;

    [SerializeField] private UnityEvent onTargetEnter;

    private bool hasFired = false;

    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (targetObject == null) return;

        // Check if the entering collider belongs to the target object (or its children).
        if (other.transform == targetObject.transform || other.transform.IsChildOf(targetObject.transform))
        {
            if (fireOnce && hasFired) return;
            hasFired = true;
            onTargetEnter?.Invoke();
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = hasFired ? new Color(1f, 0.3f, 0.3f, 0.25f) : new Color(1f, 0.7f, 0.3f, 0.25f);
        Collider col = GetComponent<Collider>();
        if (col is BoxCollider box)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
        }
    }
}