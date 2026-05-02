using UnityEngine;

// Tiny helper: holds a reference to an AudioLog asset and exposes a
// no-arg Play() method. Drop this on the same GameObject as your
// Interactable / Pickup, then wire onInteract → AudioLogTrigger.Play.
//
// Saves designers from binding the AudioLog as a UnityEvent parameter.

public class AudioLogTrigger : MonoBehaviour
{
    [SerializeField] private AudioLog log;

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
        AudioLogPlayer.Instance.Play(log);
    }
}
