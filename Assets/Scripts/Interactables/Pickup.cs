using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

// Generic pickup. Press E within range to collect. Unity 6 / Input System version.
//
// Used for keycards, batteries, any grab-me object.

public class Pickup : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private Key pickupKey = Key.E;
    [SerializeField] private float pickupRange = 2.5f;
    [SerializeField] private string playerTag = "Player";

    [Header("Prompt (optional)")]
    [Tooltip("A world-space UI element shown when the player is in range. Optional.")]
    [SerializeField] private GameObject pickupPromptUI;

    [Header("Events")]
    [SerializeField] private UnityEvent onPickup;

    private Transform player;
    private bool playerInRange = false;

    private void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag(playerTag);
        if (p != null) player = p.transform;

        if (pickupPromptUI != null) pickupPromptUI.SetActive(false);
    }

    private void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);
        bool wasInRange = playerInRange;
        playerInRange = distance <= pickupRange;

        // Show / hide the "Press E" prompt when entering or leaving range.
        if (playerInRange != wasInRange && pickupPromptUI != null)
        {
            pickupPromptUI.SetActive(playerInRange);
        }

        // Unity 6 Input System: read keyboard directly.
        if (playerInRange && Keyboard.current != null && Keyboard.current[pickupKey].wasPressedThisFrame)
        {
            Collect();
        }
    }

    private void Collect()
    {
        onPickup?.Invoke();
        if (pickupPromptUI != null) pickupPromptUI.SetActive(false);
        Destroy(gameObject);
    }
}
