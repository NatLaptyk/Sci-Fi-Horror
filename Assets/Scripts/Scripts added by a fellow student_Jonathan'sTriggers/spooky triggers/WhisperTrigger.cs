using UnityEngine;

public class WhisperTrigger : MonoBehaviour
{
    public int stingerID = 1;
    private bool hasTriggered = false;

    void OnTriggerEnter(Collider other)
    {
        if (!hasTriggered && other.CompareTag("Player"))
        {
            hasTriggered = true;
            
            // This line "Calls" the logic you just pasted into the SoundManager
            
                        SoundManager.Instance.PlayStinger(stingerID);

        }
    }
}