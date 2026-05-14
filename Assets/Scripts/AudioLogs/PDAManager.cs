using System;
using System.Collections.Generic;
using UnityEngine;

// Tracks discovery + listened state for the level's 5 PDAs.
//
// Subscribes to AudioLogPlayer's typed start/end events. When the player triggers
// a PDA, the matching entry flips to Discovered; when the log finishes, it flips
// to Listened. Fires OnStateChanged so PDAStatusUI can repaint the icons.
//
// Setup:
//  1. Create an empty GameObject "PDAManager" in the scene.
//  2. Attach this script.
//  3. In the Inspector, set the "Pdas" list size to 5.
//  4. For each slot, drag the matching AudioLog ScriptableObject into the Log field.
//  5. The PDAManager will register with AudioLogPlayer automatically at runtime.

public class PDAManager : MonoBehaviour
{
    public static PDAManager Instance { get; private set; }

    [Serializable]
    public class PDAEntry
    {
        [Tooltip("The AudioLog ScriptableObject for this PDA slot.")]
        public AudioLog log;
        [HideInInspector] public bool discovered;
        [HideInInspector] public bool listened;
    }

    [Tooltip("Ordered list of the level's PDAs. The UI shows them in this order. " +
             "Typical setup: 5 entries.")]
    [SerializeField] private List<PDAEntry> pdas = new List<PDAEntry>();

    public IReadOnlyList<PDAEntry> Entries => pdas;
    public int DiscoveredCount { get; private set; }
    public int ListenedCount { get; private set; }

    // Subscribers get notified when any PDA changes state.
    public event Action OnStateChanged;

    private bool subscribed = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void Start()
    {
        // Subscribe again in case AudioLogPlayer.Instance wasn't ready in OnEnable.
        TrySubscribe();
        OnStateChanged?.Invoke(); // initial paint
    }

    private void OnDisable()
    {
        if (AudioLogPlayer.Instance != null && subscribed)
        {
            AudioLogPlayer.Instance.OnLogStartedTyped -= HandleLogStarted;
            AudioLogPlayer.Instance.OnLogEndedTyped -= HandleLogEnded;
            subscribed = false;
        }
    }

    private void TrySubscribe()
    {
        if (subscribed) return;
        if (AudioLogPlayer.Instance == null) return;
        AudioLogPlayer.Instance.OnLogStartedTyped += HandleLogStarted;
        AudioLogPlayer.Instance.OnLogEndedTyped += HandleLogEnded;
        subscribed = true;
    }

    private void HandleLogStarted(AudioLog log)
    {
        MarkDiscovered(log);
    }

    private void HandleLogEnded(AudioLog log)
    {
        MarkListened(log);
    }

    public void MarkDiscovered(AudioLog log)
    {
        if (log == null) return;
        PDAEntry entry = FindEntry(log);
        if (entry == null)
        {
            Debug.LogWarning($"PDAManager: AudioLog '{log.name}' is not registered in the PDA list.");
            return;
        }
        if (entry.discovered) return;
        entry.discovered = true;
        Recount();
        OnStateChanged?.Invoke();
    }

    public void MarkListened(AudioLog log)
    {
        if (log == null) return;
        PDAEntry entry = FindEntry(log);
        if (entry == null) return;
        // A log can be Listened without first being Discovered (defensive: mark both).
        if (!entry.discovered) entry.discovered = true;
        if (entry.listened) return;
        entry.listened = true;
        Recount();
        OnStateChanged?.Invoke();
    }

    private PDAEntry FindEntry(AudioLog log)
    {
        foreach (PDAEntry e in pdas)
        {
            if (e != null && e.log == log) return e;
        }
        return null;
    }

    private void Recount()
    {
        int d = 0, l = 0;
        foreach (PDAEntry e in pdas)
        {
            if (e == null) continue;
            if (e.discovered) d++;
            if (e.listened) l++;
        }
        DiscoveredCount = d;
        ListenedCount = l;
    }
}
