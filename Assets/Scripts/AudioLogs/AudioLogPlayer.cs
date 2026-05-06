using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

// Singleton that plays an AudioLog: shows the journal panel, plays the
// audio clip, reveals the transcript in sync, and lets the player
// dismiss the entry early.
//
// Wire it up like DialogueManager:
//  1. Create an empty GameObject "AudioLogPlayer" in the scene.
//  2. Attach this script. An AudioSource is auto-added.
//  3. Drag the AudioLogUI canvas into audioLogUI.
//  4. From an Interactable's onInteract, call AudioLogPlayer.Play(yourLog).
//
// Trigger from Interactable:
//   onInteract → AudioLogPlayer.Instance.Play(audioLogAsset)
// or use the convenience component AudioLogTrigger if you prefer.

public class AudioLogPlayer : MonoBehaviour
{
    public static AudioLogPlayer Instance { get; private set; }

    [Header("References")]
    [SerializeField] private AudioLogUI audioLogUI;
    [SerializeField] private AudioSource voiceSource;

    [Header("Dismiss control")]
    [Tooltip("Key the player presses to dismiss the entry early. Also stops the audio.")]
    [SerializeField] private Key dismissKey = Key.E;

    [Tooltip("If true, the entry auto-dismisses a moment after the audio finishes. If false, the player must press the dismiss key.")]
    [SerializeField] private bool autoDismissOnFinish = false;

    [Tooltip("Seconds to leave the fully-revealed text on screen after audio ends, before auto-dismissing. Only used if autoDismissOnFinish is true.")]
    [SerializeField] private float autoDismissDelay = 2f;

    [Header("Events")]
    [SerializeField] private UnityEvent onLogStarted;
    [SerializeField] private UnityEvent onLogEnded;

    // Fires with the specific AudioLog that just finished. Used by AudioLogTrigger
    // so each log's trigger can react to ITS own log finishing (not all logs).
    public event System.Action<AudioLog> OnLogEndedTyped;

    private Coroutine activeRoutine;
    private bool isPlaying;
    private AudioLog currentLog;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (voiceSource == null) voiceSource = GetComponent<AudioSource>();
        if (voiceSource == null) voiceSource = gameObject.AddComponent<AudioSource>();
        voiceSource.playOnAwake = false;
        voiceSource.loop = false;
        voiceSource.spatialBlend = 0f;
    }

    private void Update()
    {
        if (!isPlaying) return;
        if (Keyboard.current == null) return;

        if (Keyboard.current[dismissKey].wasPressedThisFrame)
        {
            Dismiss();
        }
    }

    public void Play(AudioLog log)
    {
        if (log == null)
        {
            Debug.LogWarning("AudioLogPlayer: Play called with null log.");
            return;
        }
        if (isPlaying)
        {
            // Replace the current log with the new one.
            Stop();
        }

        if (activeRoutine != null) StopCoroutine(activeRoutine);
        activeRoutine = StartCoroutine(PlayRoutine(log));
    }

    public void Dismiss()
    {
        Stop();
    }

    private void Stop()
    {
        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
            activeRoutine = null;
        }
        if (voiceSource != null && voiceSource.isPlaying) voiceSource.Stop();
        if (audioLogUI != null) audioLogUI.Hide();

        if (isPlaying)
        {
            isPlaying = false;
            onLogEnded?.Invoke();
            OnLogEndedTyped?.Invoke(currentLog);
            currentLog = null;
        }
    }

    private IEnumerator PlayRoutine(AudioLog log)
    {
        isPlaying = true;
        currentLog = log;
        onLogStarted?.Invoke();

        if (audioLogUI != null) audioLogUI.Show(log.entryTitle, log.transcript);

        if (log.audioClip != null)
        {
            voiceSource.clip = log.audioClip;
            voiceSource.Play();

            float clipLength = log.audioClip.length;
            float t = 0f;

            while (t < clipLength && voiceSource.isPlaying)
            {
                t += Time.deltaTime;
                float progress = clipLength > 0f ? t / clipLength : 1f;
                if (audioLogUI != null) audioLogUI.SetRevealProgress(progress);
                yield return null;
            }
        }

        if (audioLogUI != null) audioLogUI.RevealAll();

        if (autoDismissOnFinish)
        {
            yield return new WaitForSeconds(autoDismissDelay);
            Stop();
        }
        // Otherwise: stay open until player presses the dismiss key.
    }
}
