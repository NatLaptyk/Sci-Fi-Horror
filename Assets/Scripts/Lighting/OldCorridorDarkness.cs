using UnityEngine;
using System.Collections;

public class OldCorridorDarkness : MonoBehaviour
{
public Transform lightsParent;
public float delay = 0.4f;
public int stingerID = 0;
private bool hasTriggered = false;

void OnTriggerEnter(Collider other)
{
if (!hasTriggered && other.CompareTag("Player"))
{
hasTriggered = true;
StartCoroutine(ShutOffSequence());
}
}

// --- PASTE THIS NEW VERSION OVER THE OLD ONE ---
IEnumerator ShutOffSequence()
{
int i = 1; // 1. ADDED THIS: Keeps track of which light we are on

foreach (Transform lightTransform in lightsParent)
{
Light l = lightTransform.GetComponent<Light>();
if (l != null)
{
l.enabled = false;

// 2. UPDATED THIS: Triggers on 1, 5, 9, etc.
if (i % 4 == 1)
{
if (SoundManager.Instance != null)
{
SoundManager.Instance.PlayStinger(stingerID);
}
}
}
i++; // Move to next number
yield return new WaitForSeconds(delay);
}
}
}
