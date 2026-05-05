using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GlobalMapBlackout : MonoBehaviour
{
    [Header("Swap Settings")]
    public GameObject civilianGirl;         // Drag from Hierarchy
    public GameObject monsterPrefab;        // Drag from Project Assets folder

    [Header("Sequential Setup (Lights4!)")]
    public Transform lights4;               
    public float shutOffDelay = 0.4f;

    [Header("Instant Blackout Groups")]
    public Transform[] otherLightParents;   

    [Header("Audio Settings")]
    public int switchStingerID = 0;         
    public int voiceStingerID = 4;          

    private List<Light> sequenceLights = new List<Light>();
    private List<Light> globalLights = new List<Light>();
    private bool hasTriggered = false;

    void Start()
    {
        // 1. Cache the sequence lights
        if (lights4 != null)
        {
            foreach (Transform child in lights4)
            {
                Light l = child.GetComponent<Light>();
                if (l != null) sequenceLights.Add(l);
            }
        }

        // 2. Cache all other lights
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
        // --- STEP 1: THE SEQUENCE (Lights 4 flickers out) ---
        int i = 1;
        foreach (Light l in sequenceLights)
        {
            l.enabled = false;
            
            if (i % 4 == 1 && SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayStinger(switchStingerID);
            }
            i++;
            yield return new WaitForSeconds(shutOffDelay);
        }

        // --- STEP 2: TOTAL BLACKOUT (Everything else goes dark) ---
        foreach (Light l in globalLights)
        {
            l.enabled = false;
        }

        // --- STEP 3: THE SWAP (Girl vanishes, Monster appears) ---
        if (civilianGirl != null)
        {
            // Capture position before she's gone
            Vector3 spawnPos = civilianGirl.transform.position;
            
            // Remove the woman right after lights go out
            Destroy(civilianGirl);

            // Spawn the Monster
            if (monsterPrefab != null)
            {
                // EXACT ROTATION: -95.3 on Y axis
                Quaternion rot = Quaternion.Euler(0, -95.3f, 0);
                
                GameObject monster = Instantiate(monsterPrefab, spawnPos, rot);
                
                // SCALE: 1.99 (40% bigger than 1.42)
                monster.transform.localScale = new Vector3(1.99f, 1.99f, 1.99f);
            }
        }

        // --- STEP 4: FINAL BEAT ---
        yield return new WaitForSeconds(1.0f);

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayStinger(voiceStingerID);
        }
    }
}
