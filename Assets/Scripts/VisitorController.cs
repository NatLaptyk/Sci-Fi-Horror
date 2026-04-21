using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// The Visitor — the Colleague from your Solaris design.
// NOT an AI. All behavior is scripted and triggered from TriggerZones.
//
// Why scripted instead of NavMesh AI:
//  - NavMesh debugging will eat your three-week timeline.
//  - Players cannot tell the difference between a scripted chase and a real one.
//  - Designer-controlled behavior via trigger zones is more cinematic.
//
// Setup:
//  1. Model the Visitor (Mixamo works great) or use a humanoid asset.
//  2. Place it where it FIRST appears in the scene.
//  3. Disable the GameObject in the Inspector (starts hidden).
//  4. Attach this script. Wire TriggerZone events to call Appear, SpeakLine, etc.

public class VisitorController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AudioSource voiceSource;
    [SerializeField] private AudioClip[] voiceLines;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 1.2f;
    [Tooltip("Assign manually, or leave null to auto-find Player tag.")]
    [SerializeField] private Transform player;

    [Header("Appearance")]
    [Tooltip("Fade-in duration when Appear() is called. 0 = instant.")]
    [SerializeField] private float fadeInDuration = 0.5f;

    [Header("Events")]
    [SerializeField] private UnityEvent onPlayerCaught;

    private bool walking = false;
    private Transform walkTarget;
    private Renderer[] renderers;
    private Animator anim;

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        anim = GetComponentInChildren<Animator>();

        if (gameObject.activeSelf)
        {
            SetRenderersVisible(false);
        }

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    public void Appear()
    {
        gameObject.SetActive(true);
        StartCoroutine(FadeIn());
    }

    public void Disappear()
    {
        SetRenderersVisible(false);
        gameObject.SetActive(false);
    }

    public void SpeakLine(int id)
    {
        if (voiceSource == null || voiceLines == null) return;
        if (id < 0 || id >= voiceLines.Length) return;
        voiceSource.PlayOneShot(voiceLines[id]);
    }

    public void StartWalkingTowardPlayer()
    {
        walking = true;
        if (anim != null) anim.SetBool("Walking", true);
    }

    public void StopWalking()
    {
        walking = false;
        if (anim != null) anim.SetBool("Walking", false);
    }

    public void WalkToTarget(Transform target)
    {
        walkTarget = target;
        walking = true;
        if (anim != null) anim.SetBool("Walking", true);
    }

    private void Update()
    {
        if (!walking) return;

        Transform target = walkTarget != null ? walkTarget : player;
        if (target == null) return;

        Vector3 direction = target.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion lookRot = Quaternion.LookRotation(direction.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * 3f);
        }

        transform.position += direction.normalized * walkSpeed * Time.deltaTime;

        // Player caught trigger: only fires when chasing the player (not a fixed target).
        if (walkTarget == null && direction.magnitude < 1.2f)
        {
            walking = false;
            onPlayerCaught?.Invoke();
        }
    }

    private IEnumerator FadeIn()
    {
        if (fadeInDuration <= 0f)
        {
            SetRenderersVisible(true);
            yield break;
        }

        float t = 0f;
        while (t < fadeInDuration)
        {
            t += Time.deltaTime;
            float a = t / fadeInDuration;
            SetRenderersAlpha(a);
            yield return null;
        }
        SetRenderersAlpha(1f);
    }

    private void SetRenderersVisible(bool visible)
    {
        foreach (Renderer r in renderers)
        {
            r.enabled = visible;
        }
    }

    private void SetRenderersAlpha(float alpha)
    {
        // Works with URP/Lit or Standard shaders that have a _BaseColor or _Color property.
        foreach (Renderer r in renderers)
        {
            r.enabled = true;
            Material m = r.material;
            if (m.HasProperty("_BaseColor"))
            {
                Color c = m.GetColor("_BaseColor");
                c.a = alpha;
                m.SetColor("_BaseColor", c);
            }
            else if (m.HasProperty("_Color"))
            {
                Color c = m.color;
                c.a = alpha;
                m.color = c;
            }
        }
    }
}
