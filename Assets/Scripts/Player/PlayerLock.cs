using UnityEngine;

// PlayerLock — a simple wrapper that disables/enables player movement and look
// in one call. Better than trying to wire individual script.enabled
// toggles in UnityEvents (which is fiddly and easy to misconfigure).

public class PlayerLock : MonoBehaviour
{
    [Tooltip("The movement controller to disable. Auto-found on this GameObject if null.")]
    [SerializeField] private MonoBehaviour movementController;

    [Tooltip("The input reader to disable. Auto-found on this GameObject if null.")]
    [SerializeField] private MonoBehaviour inputReader;

    private bool isLocked = false;

    private void Awake()
    {
        // Auto-find the FirstPersonController by class name (without hard-binding to the type).
        if (movementController == null)
        {
            foreach (MonoBehaviour mb in GetComponents<MonoBehaviour>())
            {
                if (mb == this) continue;
                if (mb.GetType().Name == "FirstPersonController")
                {
                    movementController = mb;
                    break;
                }
            }
        }

        // Auto-find StarterAssetsInputs the same way.
        if (inputReader == null)
        {
            foreach (MonoBehaviour mb in GetComponents<MonoBehaviour>())
            {
                if (mb == this) continue;
                if (mb.GetType().Name == "StarterAssetsInputs")
                {
                    inputReader = mb;
                    break;
                }
            }
        }

        if (movementController == null) Debug.LogWarning("PlayerLock: FirstPersonController not found on " + gameObject.name);
        if (inputReader == null) Debug.LogWarning("PlayerLock: StarterAssetsInputs not found on " + gameObject.name);
    }

    public void Lock()
    {
        Debug.Log("PlayerLock: Lock called.");
        if (movementController != null) movementController.enabled = false;
        if (inputReader != null) inputReader.enabled = false;
        isLocked = true;
    }

    public void Unlock()
    {
        Debug.Log("PlayerLock: Unlock called.");
        if (movementController != null) movementController.enabled = true;
        if (inputReader != null) inputReader.enabled = true;
        isLocked = false;
    }

    public bool IsLocked => isLocked;
}