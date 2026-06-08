using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// Fires an event when ALL of a configurable set of AudioLogs have been Listened
// (according to PDAManager). Use to gate door unlocks behind narrative progress.
public class PDAGate : MonoBehaviour
{
    [Header("Gate condition")]
    [Tooltip("All these AudioLogs must be marked as Listened in PDAManager " +
             "before OnAllListened fires.")]
    [SerializeField] private List<AudioLog> requiredLogs = new List<AudioLog>();

    [Header("Events")]
    [Tooltip("Fires once when ALL required logs have been listened. " +
             "Wire SlidingDoor.Unlock + Open here.")]
    [SerializeField] private UnityEvent onAllListened;

    private bool fired = false;
    private PDAManager manager;
    private bool subscribed = false;

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void Start()
    {
        TrySubscribe();
        CheckGate(); // initial check in case state already meets the condition
    }

    private void OnDisable()
    {
        if (manager != null && subscribed)
        {
            manager.OnStateChanged -= CheckGate;
            subscribed = false;
        }
    }

    private void TrySubscribe()
    {
        if (subscribed) return;
        manager = PDAManager.Instance;
        if (manager == null) return;
        manager.OnStateChanged += CheckGate;
        subscribed = true;
    }

    private void CheckGate()
    {
        if (fired) return;
        if (manager == null) return;
        if (requiredLogs == null || requiredLogs.Count == 0) return;

        foreach (AudioLog required in requiredLogs)
        {
            if (required == null) continue;

            bool listened = false;
            foreach (PDAManager.PDAEntry entry in manager.Entries)
            {
                if (entry != null && entry.log == required && entry.listened)
                {
                    listened = true;
                    break;
                }
            }

            if (!listened) return; // at least one required log is not yet listened
        }

        // All required logs listened
        fired = true;
        Debug.Log($"PDAGate '{name}': all required logs listened, firing onAllListened");
        onAllListened?.Invoke();
    }
}
