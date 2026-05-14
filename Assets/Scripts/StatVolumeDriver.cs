using UnityEngine;
using UnityEngine.Rendering;

// Drives a URP Volume's weight smoothly based on PlayerStats.Health or Sanity.
//
// Attach to a GameObject that has a Volume component (or assign one). Point it
// at your PlayerStats, pick which stat to track, and configure the start/end
// values. The Volume's weight will lerp continuously from 0 (no effect) to 1
// (full effect) as the stat drops.
//
// Use case: smooth psychological breakdown instead of binary threshold toggling.
// At sanity 80, vignette starts faintly. At 40, it's noticeable. At 0, full effect.
//
// Setup:
//  1. Configure your Volume with the FULL-INTENSITY values you want at stat=0.
//  2. Make sure the Volume GameObject is ACTIVE in the Hierarchy (this script
//     manages the weight, not the active state).
//  3. Disable any previous PlayerStats event wiring that was toggling this
//     Volume's GameObject SetActive — that pattern is replaced by this script.

[RequireComponent(typeof(Volume))]
public class StatVolumeDriver : MonoBehaviour
{
    public enum StatType { Sanity, Health }

    [Header("Inputs")]
    [Tooltip("PlayerStats whose values drive this Volume. Auto-finds if left empty.")]
    [SerializeField] private PlayerStats playerStats;
    [Tooltip("Volume on this GameObject (auto-assigned via RequireComponent).")]
    [SerializeField] private Volume volume;

    [Header("Which stat drives this Volume")]
    [SerializeField] private StatType statToTrack = StatType.Sanity;

    [Header("Weight curve (stat 0-100 → volume weight 0-1)")]
    [Tooltip("Stat value above which the effect is INVISIBLE (weight 0). " +
             "E.g. 80 = no effect while sanity is above 80.")]
    [SerializeField] private float effectStartsAtStat = 80f;
    [Tooltip("Stat value at which the effect is FULLY VISIBLE (weight 1). " +
             "E.g. 0 = full intensity at sanity 0.")]
    [SerializeField] private float effectFullAtStat = 0f;
    [Tooltip("Optional non-linear curve. Default linear. Use ease-in to make the " +
             "effect ramp slowly at first then accelerate, or ease-out for the opposite.")]
    [SerializeField] private AnimationCurve weightCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Header("Smoothing")]
    [Tooltip("How fast the weight responds to stat changes. Higher = snappier, " +
             "lower = smoother fade in/out. 5 is a good default.")]
    [SerializeField] private float responseSpeed = 5f;

    private float currentWeight = 0f;

    private void Awake()
    {
        if (volume == null) volume = GetComponent<Volume>();
        if (playerStats == null) playerStats = FindFirstObjectByType<PlayerStats>();

        if (volume != null) volume.weight = 0f;
    }

    private void Update()
    {
        if (playerStats == null || volume == null) return;

        float currentStat = statToTrack == StatType.Sanity ? playerStats.Sanity : playerStats.Health;

        // Map currentStat from [effectStartsAtStat..effectFullAtStat] → [0..1] normalized.
        float range = effectStartsAtStat - effectFullAtStat;
        float targetWeight = 0f;
        if (range > 0f)
        {
            float normalized = Mathf.Clamp01((effectStartsAtStat - currentStat) / range);
            targetWeight = weightCurve.Evaluate(normalized);
        }

        // Smoothly approach target so the transition isn't jittery.
        currentWeight = Mathf.MoveTowards(currentWeight, targetWeight, responseSpeed * Time.deltaTime);
        volume.weight = currentWeight;
    }
}
