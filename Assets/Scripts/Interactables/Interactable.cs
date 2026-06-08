using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

// Generic interactable. Player presses E within range to fire an event.
// Unlike Pickup, this does NOT destroy itself when interacted with.
//
// Use cases:
//  - Try the zipper on Sophie's back (interaction reveals the wrongness).
//  - Examine an object without removing it from the world.
//  - One-time interactions that should still leave the object in place.

public class Interactable : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private Key interactKey = Key.E;
    [SerializeField] private float interactRange = 2.5f;
    [SerializeField] private string playerTag = "Player";

    [Tooltip("If true, only fires the first time. If false, can be interacted with repeatedly.")]
    [SerializeField] private bool fireOnce = true;

    [Header("Selection mode")]
    [Tooltip("If true, uses a camera raycast — player must LOOK AT the interactable to trigger it. " +
             "Required for clustered objects like keypad buttons where distance-based selection would fire all of them at once.")]
    [SerializeField] private bool useRaycastSelection = false;
    [Tooltip("Max raycast distance for look-based selection.")]
    [SerializeField] private float raycastDistance = 3f;
    [Tooltip("Layers the raycast checks. Default = Everything.")]
    [SerializeField] private LayerMask raycastLayers = ~0;

    [Header("Prompt (optional)")]
    [SerializeField] private GameObject interactPromptUI;

    [Header("Events")]
    [SerializeField] private UnityEvent onInteract;

    private Transform player;
    private bool playerInRange = false;
    private bool hasFired = false;

    private void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag(playerTag);
        if (p != null) player = p.transform;

        if (interactPromptUI != null) interactPromptUI.SetActive(false);
    }

    private void Update()
    {
        if (player == null) return;
        if (fireOnce && hasFired) return;

        bool wasInRange = playerInRange;

        if (useRaycastSelection)
        {
            // Raycast from the camera: only one interactable can be "looked at" at a time.
            playerInRange = false;
            Camera cam = Camera.main;
            if (cam != null)
            {
                Ray ray = new Ray(cam.transform.position, cam.transform.forward);
                if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance, raycastLayers, QueryTriggerInteraction.Collide))
                {
                    if (hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform))
                    {
                        playerInRange = true;
                    }
                }
            }
        }
        else
        {
            // Distance-based: any interactable within range can be triggered.
            float distance = Vector3.Distance(transform.position, player.position);
            playerInRange = distance <= interactRange;
        }

        if (playerInRange != wasInRange && interactPromptUI != null)
        {
            interactPromptUI.SetActive(playerInRange);
        }

        if (playerInRange && Keyboard.current != null && Keyboard.current[interactKey].wasPressedThisFrame)
        {
            Interact();
        }
    }

    private void Interact()
    {
        hasFired = true;
        if (interactPromptUI != null) interactPromptUI.SetActive(false);
        onInteract?.Invoke();
    }

    // Allow re-arming if needed by external code.
    public void ResetInteraction()
    {
        hasFired = false;
    }
}