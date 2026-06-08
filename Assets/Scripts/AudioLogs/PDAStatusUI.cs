using UnityEngine;
using UnityEngine.UI;

// Repaints a fixed row of PDA icon Images based on PDAManager state.
//
// Each Image in the iconSlots array maps 1:1 to a PDA entry in PDAManager.Entries.
// Three visual states are applied via color (sprite can be the same for all).

public class PDAStatusUI : MonoBehaviour
{
    [Tooltip("Image components for each PDA slot, in order. Index 0 = first PDA, etc.")]
    [SerializeField] private Image[] iconSlots;

    [Header("State colors")]
    [Tooltip("Color for an undiscovered PDA. Faint = 'something is here but you haven't found it.'")]
    [SerializeField] private Color lockedColor = new Color(1f, 1f, 1f, 0.15f);
    [Tooltip("Color for a discovered but not-yet-listened PDA. Medium presence.")]
    [SerializeField] private Color discoveredColor = new Color(1f, 1f, 1f, 0.55f);
    [Tooltip("Color for a fully-listened PDA. Full brightness, optional accent tint.")]
    [SerializeField] private Color listenedColor = new Color(0.4f, 1f, 0.7f, 1f);

    private PDAManager manager;
    private bool subscribed = false;

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void Start()
    {
        // PDAManager.Instance might not be ready in OnEnable; try again.
        TrySubscribe();
        Repaint();
    }

    private void OnDisable()
    {
        if (manager != null && subscribed)
        {
            manager.OnStateChanged -= Repaint;
            subscribed = false;
        }
    }

    private void TrySubscribe()
    {
        if (subscribed) return;
        manager = PDAManager.Instance;
        if (manager == null) return;
        manager.OnStateChanged += Repaint;
        subscribed = true;
    }

    private void Repaint()
    {
        if (manager == null || iconSlots == null) return;

        int count = Mathf.Min(iconSlots.Length, manager.Entries.Count);
        for (int i = 0; i < count; i++)
        {
            if (iconSlots[i] == null) continue;
            PDAManager.PDAEntry entry = manager.Entries[i];

            if (entry == null || entry.log == null)
            {
                iconSlots[i].color = lockedColor;
                continue;
            }

            if (entry.listened) iconSlots[i].color = listenedColor;
            else if (entry.discovered) iconSlots[i].color = discoveredColor;
            else iconSlots[i].color = lockedColor;
        }
    }
}
