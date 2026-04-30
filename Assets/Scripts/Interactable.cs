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
//
// Setup:
//  1. Attach to any object the player should interact with.
//  2. Add a Collider (regular, not trigger) to the same GameObject.
//  3. In onInteract, wire up the action (DialogueManager.StartDialogue etc).
//  4. Optional: assign pickupPromptUI for a "Press E" indicator.

public class Interactable : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private Key interactKey = Key.E;
    [SerializeField] private float interactRange = 2.5f;
    [SerializeField] private string playerTag = "Player";

    [Tooltip("If true, only fires the first time. If false, can be interacted with repeatedly.")]
    [SerializeField] private bool fireOnce = true;

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

        float distance = Vector3.Distance(transform.position, player.position);
        bool wasInRange = playerInRange;
        playerInRange = distance <= interactRange;

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