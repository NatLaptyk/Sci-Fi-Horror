using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

// Tracks keycards collected, exposes win/lose state, handles scene reset.
//
// Setup:
//  1. Create an empty GameObject named "GameManager" in your scene.
//  2. Attach this script.
//  3. Wire each Pickup's onPickup event to call GameManager.AddKey.
//  4. Wire the airlock TriggerZone's onPlayerEnter to GameManager.TryEscape.
//  5. Wire VisitorController.onPlayerCaught to GameManager.GameOver.

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Goal")]
    [SerializeField] private int keysRequired = 3;

    [Header("Events")]
    [Tooltip("Fires each time a key is collected. Int parameter = total collected.")]
    [SerializeField] private IntEvent onKeyCountChanged;

    [Tooltip("Fires when all keys are collected (goal complete).")]
    [SerializeField] private UnityEvent onAllKeysCollected;

    [Tooltip("Fires when the player reaches the airlock with all keys.")]
    [SerializeField] private UnityEvent onEscape;

    [Tooltip("Fires when the player is caught by the Visitor or otherwise loses.")]
    [SerializeField] private UnityEvent onGameOver;

    private int keysCollected = 0;
    private bool finished = false;

    public int KeysCollected => keysCollected;
    public int KeysRequired => keysRequired;
    public bool HasAllKeys => keysCollected >= keysRequired;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void AddKey()
    {
        if (finished) return;
        keysCollected++;
        onKeyCountChanged?.Invoke(keysCollected);

        if (keysCollected >= keysRequired)
        {
            onAllKeysCollected?.Invoke();
        }
    }

    public void TryEscape()
    {
        if (finished) return;
        if (!HasAllKeys) return;

        finished = true;
        onEscape?.Invoke();
    }

    public void GameOver()
    {
        if (finished) return;
        finished = true;
        onGameOver?.Invoke();
    }

    public void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    [System.Serializable]
    public class IntEvent : UnityEvent<int> { }
}
