using UnityEngine;

public class SoundBehindPlayer : MonoBehaviour
{
    public int stingerID = 1; // Assuming your "whisper" is at Element 1 in SoundManager
    public float distanceBehind = 3.0f; // How far back the sound should spawn
    private bool hasTriggered = false;

    void OnTriggerEnter(Collider other)
    {
        if (!hasTriggered && other.CompareTag("Player"))
        {
            hasTriggered = true;

            // 1. Get the player's position and direction
            Vector3 playerPos = other.transform.position;
            Vector3 playerForward = other.transform.forward;

            // 2. Calculate the point directly behind them
            Vector3 spawnPos = playerPos - (playerForward * distanceBehind);

            // 3. Play the sound at that specific 3D point
            if (SoundManager.Instance != null)
            {
                // We grab the clip from your SoundManager's list
                AudioClip clip = SoundManager.Instance.GetStingerClip(stingerID);
                if (clip != null)
                {
                    AudioSource.PlayClipAtPoint(clip, spawnPos);
                }
            }
        }
    }
}
