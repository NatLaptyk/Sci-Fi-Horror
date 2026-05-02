using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class OldCorridorDarkness : MonoBehaviour
{
    [Header("Setup")]
    public Transform lightsParent;
    public float shutOffDelay = 0.4f;
    
    [Header("Audio")]
    public int stingerID = 2;              // Updated to ID 2
    public AudioSource protagonistSource; // Drag your Player's AudioSource here
    public AudioClip weirdLineClip;        // Drag the MP3 file here

    private List<Light> lights = new List<Light>();
    private bool hasTriggered = false;

    void Start()
    {
        // Cache lights to avoid performance hits during the flicker
        foreach (Transform child in lightsParent)
        {
            Light l = child.GetComponent<Light>();
            if (l != null) lights.Add(l);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!hasTriggered && other.CompareTag("Player"))
        {
            hasTriggered = true;
            StartCoroutine(ShutOffSequence());
        }
    }

    IEnumerator ShutOffSequence()
    {
        // 1. SEQUENTIAL SHUTOFF
        int i = 1;
        foreach (Light l in lights)
        {
            l.enabled = false;
            
            // Uses Stinger ID 2 for the clunks
            if (i % 4 == 1 && SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayStinger(stingerID);
            }
            i++;
            yield return new WaitForSeconds(shutOffDelay);
        }

        // 2. TOTAL DARKNESS (The "Wait for it..." moment)
        yield return new WaitForSeconds(1.5f); 

        // 3. COLLECTIVE FLICKER (All lights together)
        for (int f = 0; f < 4; f++)
        {
            SetAllLights(true);
            yield return new WaitForSeconds(0.05f);
            SetAllLights(false);
            yield return new WaitForSeconds(0.05f);
        }

        // 4. RESTORE & VOICE LINE
        SetAllLights(true);

        yield return new WaitForSeconds(1.0f);

        if (protagonistSource != null && weirdLineClip != null)
        {
            protagonistSource.PlayOneShot(weirdLineClip);
        }
    }

    void SetAllLights(bool state)
    {
        foreach (Light l in lights)
        {
            l.enabled = state;
        }
    }
}