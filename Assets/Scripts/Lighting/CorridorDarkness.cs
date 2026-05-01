using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CorridorDarkness : MonoBehaviour
{
    [Header("Setup")]
    public Transform lightsParent; 
    public float batchDelay = 1.5f; // 1 second delay between groups
    public int stingerID = 0; 
    
    private bool hasTriggered = false;
    private List<Light> lights = new List<Light>();

    void Start()
    {
        // Populate the list based on the order in the Hierarchy
        if (lightsParent != null)
        {
            foreach (Transform child in lightsParent)
            {
                Light l = child.GetComponent<Light>();
                if (l != null)
                {
                    lights.Add(l);
                }
            }
        }
        else
        {
            Debug.LogError("Please assign the Lights Parent transform in the inspector!");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Only trigger if it's the player and hasn't happened yet
        if (!hasTriggered && other.CompareTag("Player"))
        {
            hasTriggered = true;
            StartCoroutine(BatchShutOffSequence());
        }
    }

    IEnumerator BatchShutOffSequence()
    {
        // --- BATCH 1: Top 3 in Hierarchy (Lights 11, 10, 9) ---
        // Indices 0, 1, 2
        PlaySoundEffect();
        SetLightGroupState(new int[] { 0, 1, 2 }, false);
        
        yield return new WaitForSeconds(batchDelay);

        // --- BATCH 2: Next 4 in Hierarchy (Lights 1, 2, 3, 4) ---
        // Indices 3, 4, 5, 6
        PlaySoundEffect();
        SetLightGroupState(new int[] { 3, 4, 5, 6 }, false);

        yield return new WaitForSeconds(batchDelay);

        // --- BATCH 3: Final 4 in Hierarchy (Remaining lights) ---
        // Indices 7, 8, 9, 10
        PlaySoundEffect();
        SetLightGroupState(new int[] { 7, 8, 9, 10 }, false);
    }

    /// <summary>
    /// Loops through specific indices to enable/disable lights
    /// </summary>
    void SetLightGroupState(int[] indices, bool state)
    {
        foreach (int i in indices)
        {
            if (i >= 0 && i < lights.Count)
            {
                lights[i].enabled = state;
            }
        }
    }

    /// <summary>
    /// Triggers the sound stinger via the SoundManager
    /// </summary>
    void PlaySoundEffect()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayStinger(stingerID);
        }
        else
        {
            Debug.LogWarning("SoundManager.Instance is missing! Make sure your SoundManager is in the scene.");
        }
    }
}