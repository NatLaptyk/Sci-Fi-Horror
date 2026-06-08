using UnityEngine;
using UnityEngine.Events;
public class AudioLogTrigger : MonoBehaviour
{
    [SerializeField] private AudioLog log;

    [Header("Events")]
    [Tooltip("Fires when THIS specific log finishes (audio ends or player dismisses). " +
             "Wire scene-specific consequences here — spawning a visitor, opening a door, etc.")]
    [SerializeField] private UnityEvent onThisLogFinished;

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void Start()
    {
        // AudioLogPlayer.Instance might not be ready in OnEnable if our object
        // initializes first. Try again here.
        TrySubscribe();
    }

    private void OnDisable()
    {
        if (AudioLogPlayer.Instance != null)
        {
            AudioLogPlayer.Instance.OnLogEndedTyped -= HandleLogEnded;
        }
    }

    private void TrySubscribe()
    {
        if (AudioLogPlayer.Instance == null) return;
        // Avoid double-subscribing.
        AudioLogPlayer.Instance.OnLogEndedTyped -= HandleLogEnded;
        AudioLogPlayer.Instance.OnLogEndedTyped += HandleLogEnded;
    }

    private void HandleLogEnded(AudioLog ended)
    {
        if (ended == log)
        {
            onThisLogFinished?.Invoke();
        }
    }

    public void Play()
    {
        if (log == null)
        {
            Debug.LogWarning($"AudioLogTrigger on {name}: no log assigned.", this);
            return;
        }
        if (AudioLogPlayer.Instance == null)
        {
            Debug.LogWarning("AudioLogTrigger: no AudioLogPlayer in scene.");
            return;
        }
        TrySubscribe(); // make sure we're listening before playback
        AudioLogPlayer.Instance.Play(log);
    }
}
