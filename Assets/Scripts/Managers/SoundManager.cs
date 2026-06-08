using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

// Singleton sound manager for ambient music and stingers.
// Based on the pattern from Course 12 slide 6 (Salim's workshop).
//
// - Two AudioSources for crossfading between ambient tracks.
// - One AudioSource for one-shot stingers (door slams, jump scares, voice lines).
// - Call PlayAmbient(id) from trigger zones to change music.
// - Call PlayStinger(id) for one-off sound effects.

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Ambient tracks (loopable)")]
    [SerializeField] private AudioClip[] ambientTracks;
    [SerializeField] private float crossfadeDuration = 2f;
    [Range(0f, 1f)] [SerializeField] private float ambientVolume = 0.7f;

    [Header("Stingers (one-shot effects)")]
    [SerializeField] private AudioClip[] stingerClips;
    [Range(0f, 1f)] [SerializeField] private float stingerVolume = 1f;

    private AudioSource sourceA;
    private AudioSource sourceB;
    private AudioSource stingerSource;
    private bool usingSourceA = true;
    private Coroutine fadeRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Auto-add three AudioSources if missing.
        AudioSource[] existing = GetComponents<AudioSource>();
        sourceA = existing.Length > 0 ? existing[0] : gameObject.AddComponent<AudioSource>();
        sourceB = existing.Length > 1 ? existing[1] : gameObject.AddComponent<AudioSource>();
        stingerSource = existing.Length > 2 ? existing[2] : gameObject.AddComponent<AudioSource>();

        sourceA.loop = true;
        sourceB.loop = true;
        stingerSource.loop = false;

        sourceA.playOnAwake = false;
        sourceB.playOnAwake = false;
        stingerSource.playOnAwake = false;

        sourceA.volume = 0f;
        sourceB.volume = 0f;
    }

    public void PlayAmbient(int id)
    {
        if (id < 0 || id >= ambientTracks.Length)
        {
            Debug.LogWarning("SoundManager: ambient track id " + id + " out of range.");
            return;
        }

        AudioClip clip = ambientTracks[id];
        AudioSource incoming = usingSourceA ? sourceB : sourceA;
        AudioSource outgoing = usingSourceA ? sourceA : sourceB;

        incoming.clip = clip;
        incoming.volume = 0f;
        incoming.Play();

        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(Crossfade(incoming, outgoing, crossfadeDuration));

        usingSourceA = !usingSourceA;
    }

    public void StopAmbient()
    {
        AudioSource current = usingSourceA ? sourceA : sourceB;
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeOut(current, crossfadeDuration));
    }

    public void PlayStinger(int id)
    {
        if (id < 0 || id >= stingerClips.Length)
        {
            Debug.LogWarning("SoundManager: stinger id " + id + " out of range.");
            return;
        }
        stingerSource.PlayOneShot(stingerClips[id], stingerVolume);
    }

    private IEnumerator Crossfade(AudioSource incoming, AudioSource outgoing, float duration)
    {
        float t = 0f;
        float startOutgoingVol = outgoing.volume;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = t / duration;
            incoming.volume = Mathf.Lerp(0f, ambientVolume, k);
            outgoing.volume = Mathf.Lerp(startOutgoingVol, 0f, k);
            yield return null;
        }
        incoming.volume = ambientVolume;
        outgoing.volume = 0f;
        outgoing.Stop();
    }

    private IEnumerator FadeOut(AudioSource src, float duration)
    {
        float startVol = src.volume;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            src.volume = Mathf.Lerp(startVol, 0f, t / duration);
            yield return null;
        }
        src.volume = 0f;
        src.Stop();
    }
    public AudioClip GetStingerClip(int id)
{
    if (id >= 0 && id < stingerClips.Length)
        return stingerClips[id];
    return null;
}

       public void PlayStingerRightEar(int id)
    {
        if (id < 0 || id >= stingerClips.Length) return;

        // 1. FORCE 2D MODE: This makes the sound "global" so panning works perfectly
        // 0.0 is full 2D, 1.0 is full 3D.
        stingerSource.spatialBlend = 0.0f; 

        // 3. SCARY PITCH: Lower it for that demonic feel
        stingerSource.pitch = 0.75f;

        // 4. THE 10X BOOST: Stacking the sound for massive volume
        for (int i = 0; i < 10; i++)
        {
            stingerSource.PlayOneShot(stingerClips[id], 1.0f);
        }

        // 5. CLEANUP: Reset settings after the sound is done
        StartCoroutine(ResetScarySettings(stingerClips[id].length));
    }

    private IEnumerator ResetScarySettings(float delay)
    {
        yield return new WaitForSeconds(delay);
        stingerSource.panStereo = 0f;
        stingerSource.pitch = 1.0f;
        // Keep spatialBlend at 0 if you want all stingers to be 2D/Global
    }




}
