using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// Escalating door bang for the Sophie pursuit beat.
//
// Three intensity stages, mapping to the three vocal escalations:
//  Stage 1: "I can hear you, why won't you answer" — soft thumps, slight shake.
//  Stage 2: "Adam." — harder strikes, visible shudder.
//  Stage 3: "Open. The. Door." — huge impacts, dent images appear, door barely holds.
//
// How it works:
//  - The door's local position offsets randomly within a small radius for each shake.
//  - Each shake has an audio clip played in sync.
//  - Optional dent decals appear at stage 3 to visually escalate the damage.

public class DoorShaker : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [Tooltip("Soft thumps for stage 1.")]
    [SerializeField] private AudioClip[] stage1Clips;
    [Tooltip("Harder strikes for stage 2.")]
    [SerializeField] private AudioClip[] stage2Clips;
    [Tooltip("Massive booms for stage 3. Each one matches a dent.")]
    [SerializeField] private AudioClip[] stage3Clips;

    [Header("Shake intensity (meters)")]
    [SerializeField] private float stage1Shake = 0.005f;
    [SerializeField] private float stage2Shake = 0.02f;
    [SerializeField] private float stage3Shake = 0.06f;

    [Header("Stage timing (seconds)")]
    [Tooltip("How long each stage lasts. Stage 3 is usually shorter and punchier.")]
    [SerializeField] private float stage1Duration = 4f;
    [SerializeField] private float stage2Duration = 3f;
    [SerializeField] private float stage3Duration = 2.5f;

    [Tooltip("Time between hits within stage 1 and 2.")]
    [SerializeField] private float stage1HitInterval = 0.7f;
    [SerializeField] private float stage2HitInterval = 0.5f;

    [Header("Dents (stage 3 only)")]
    [Tooltip("Dent visuals (decals, deformed mesh swaps, etc.) enabled in order during stage 3.")]
    [SerializeField] private GameObject[] dentVisuals;

    [Header("Sophie's voice (optional, separate track)")]
    [Tooltip("Played at the start of each stage. The vocal escalation lines.")]
    [SerializeField] private AudioSource voiceSource;
    [SerializeField] private AudioClip stage1Voice;
    [SerializeField] private AudioClip stage2Voice;
    [SerializeField] private AudioClip[] stage3VoiceClips; // "Open." "The." "Door." — three clips.

    [Header("Events")]
    [SerializeField] private UnityEvent onStage1Start;
    [SerializeField] private UnityEvent onStage2Start;
    [SerializeField] private UnityEvent onStage3Start;
    [SerializeField] private UnityEvent onSequenceComplete;

    private Vector3 restPosition;
    private Coroutine activeRoutine;
    private bool restCaptured = false;

    private void Awake()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        // Hide all dent visuals at start.
        if (dentVisuals != null)
        {
            foreach (GameObject d in dentVisuals)
            {
                if (d != null) d.SetActive(false);
            }
        }
    }

    private void CaptureRestPosition()
    {
        restPosition = transform.localPosition;
        restCaptured = true;
    }

    // Public API.

    public void PlayStage1()
    {
        if (!restCaptured) CaptureRestPosition();
        if (activeRoutine != null) StopCoroutine(activeRoutine);
        activeRoutine = StartCoroutine(StageRoutine(1));
    }

    public void PlayStage2()
    {
        if (!restCaptured) CaptureRestPosition();
        if (activeRoutine != null) StopCoroutine(activeRoutine);
        activeRoutine = StartCoroutine(StageRoutine(2));
    }

    public void PlayStage3()
    {
        if (!restCaptured) CaptureRestPosition();
        if (activeRoutine != null) StopCoroutine(activeRoutine);
        activeRoutine = StartCoroutine(StageRoutine(3));
    }

    // Runs all three stages back-to-back.
    public void PlayFullSequence()
    {
        if (!restCaptured) CaptureRestPosition();
        if (activeRoutine != null) StopCoroutine(activeRoutine);
        activeRoutine = StartCoroutine(FullSequence());
    }

    public void StopShaking()
    {
        if (activeRoutine != null) StopCoroutine(activeRoutine);
        activeRoutine = null;
        if (restCaptured) transform.localPosition = restPosition;
    }

    private IEnumerator FullSequence()
    {
        yield return StageRoutine(1);
        yield return StageRoutine(2);
        yield return StageRoutine(3);
        onSequenceComplete?.Invoke();
    }

    private IEnumerator StageRoutine(int stage)
    {
        // Fire the start event and play Sophie's voice line for this stage.
        switch (stage)
        {
            case 1:
                onStage1Start?.Invoke();
                PlayVoice(stage1Voice);
                yield return Stage1Loop();
                break;
            case 2:
                onStage2Start?.Invoke();
                PlayVoice(stage2Voice);
                yield return Stage2Loop();
                break;
            case 3:
                onStage3Start?.Invoke();
                yield return Stage3Loop();
                break;
        }

        // Settle door back to rest.
        transform.localPosition = restPosition;
        activeRoutine = null;
    }

    private IEnumerator Stage1Loop()
    {
        // Soft thumps every ~stage1HitInterval seconds for stage1Duration.
        float t = 0f;
        while (t < stage1Duration)
        {
            DoOneHit(stage1Shake, stage1Clips);
            t += stage1HitInterval;
            yield return new WaitForSeconds(stage1HitInterval);
        }
    }

    private IEnumerator Stage2Loop()
    {
        float t = 0f;
        while (t < stage2Duration)
        {
            DoOneHit(stage2Shake, stage2Clips);
            t += stage2HitInterval;
            yield return new WaitForSeconds(stage2HitInterval);
        }
    }

    private IEnumerator Stage3Loop()
    {
        // Three discrete heavy hits, each timed with one of Sophie's words.
        // Each hit also reveals a dent.
        int hitCount = Mathf.Max(1, dentVisuals != null ? dentVisuals.Length : 3);

        for (int i = 0; i < hitCount; i++)
        {
            // Sophie speaks her word (Open / The / Door).
            if (stage3VoiceClips != null && i < stage3VoiceClips.Length)
            {
                PlayVoice(stage3VoiceClips[i]);
            }

            // Brief delay so the word lands before the impact.
            yield return new WaitForSeconds(0.4f);

            // Big impact.
            DoOneHit(stage3Shake, stage3Clips);

            // Reveal the dent for this hit.
            if (dentVisuals != null && i < dentVisuals.Length && dentVisuals[i] != null)
            {
                dentVisuals[i].SetActive(true);
            }

            // Hold the shake briefly.
            yield return new WaitForSeconds(0.4f);
            transform.localPosition = restPosition;

            // Pause between hits.
            yield return new WaitForSeconds(0.6f);
        }
    }

    private void DoOneHit(float shakeRadius, AudioClip[] clips)
    {
        // Random offset within a small sphere.
        Vector3 offset = Random.insideUnitSphere * shakeRadius;
        transform.localPosition = restPosition + offset;

        // Play a random impact clip.
        if (audioSource != null && clips != null && clips.Length > 0)
        {
            AudioClip c = clips[Random.Range(0, clips.Length)];
            if (c != null) audioSource.PlayOneShot(c);
        }
    }

    private void PlayVoice(AudioClip clip)
    {
        if (clip == null) return;
        AudioSource src = voiceSource != null ? voiceSource : audioSource;
        if (src != null) src.PlayOneShot(clip);
    }
}
