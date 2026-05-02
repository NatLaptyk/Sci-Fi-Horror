using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GlobalMapBlackout : MonoBehaviour
{
    [Header("Sequential Setup (Lights4!)")]
    public Transform lights4;               
    public float shutOffDelay = 0.4f;

    [Header("Instant Blackout Groups")]
    public Transform[] otherLightParents;   // Lights3!, Lights2!, Lights!, Kitchen, Bedroom

    [Header("Audio Settings")]
    public int switchStingerID = 0;         // Changed to 0 for the lever sound
    public int voiceStingerID = 4;          // The "Oh crap" voice line

    private List<Light> sequenceLights = new List<Light>();
    private List<Light> globalLights = new List<Light>();
    private bool hasTriggered = false;

    void Start()
    {
        // Cache the sequential corridor lights
        if (lights4 != null)
        {
            foreach (Transform child in lights4)
            {
                Light l = child.GetComponent<Light>();
                if (l != null) sequenceLights.Add(l);
            }
        }

        // Cache all other map lights (Kitchen, Bedroom, etc.)
        foreach (Transform parent in otherLightParents)
        {
            if (parent != null)
            {
                foreach (Transform child in parent)
                {
                    Light l = child.GetComponent<Light>();
                    if (l != null) globalLights.Add(l);
                }
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!hasTriggered && other.CompareTag("Player"))
        {
            hasTriggered = true;
            StartCoroutine(BlackoutSequence());
        }
    }

    IEnumerator BlackoutSequence()
    {
        // 1. THE SEQUENCE: Lights4! shut off one-by-one
        int i = 1;
        foreach (Light l in sequenceLights)
        {
            l.enabled = false;
            
            // Play the lever sound (Stinger 0) every 4th light
            if (i % 4 == 1 && SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayStinger(switchStingerID);
            }
            i++;
            yield return new WaitForSeconds(shutOffDelay);
        }

        // 2. THE TOTAL BLACKOUT: Everything else cuts out instantly
        foreach (Light l in globalLights)
        {
            l.enabled = false;
        }

        // 3. THE BEAT: 1 second of silence
        yield return new WaitForSeconds(1.0f);

        // 4. THE VOICE LINE: Play Stinger ID 4 ("Oh crap")
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayStinger(voiceStingerID);
        }
    }
}