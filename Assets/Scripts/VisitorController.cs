using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// The Visitor — Sophie. Scripted behavior, no AI.
//
// Cinematic chase mode: she walks/runs toward the player but cannot catch them.
// She stops moving when within minDistanceToPlayer of the player so she doesn't
// embarrassingly walk into them or push them through walls.
//
// Wall safety: a forward-facing raycast prevents her from translating into walls.
// If a wall is in her way, she rotates to face the player but stops moving forward.

public class VisitorController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AudioSource voiceSource;
    [SerializeField] private AudioClip[] voiceLines;

    [Header("Movement")]
    [Tooltip("Walking speed. Match against player walk (~2 m/s) so she's slower than walking.")]
    [SerializeField] private float walkSpeed = 1.4f;

    [Tooltip("Running speed. Faster than player walk, slower than player sprint (~5 m/s).")]
    [SerializeField] private float runSpeed = 3f;

    [Tooltip("Auto-finds Player tag if null.")]
    [SerializeField] private Transform player;

    [Header("Cinematic constraints")]
    [Tooltip("She stops moving when within this distance of the player. Cinematic only \u2014 prevents catching.")]
    [SerializeField] private float minDistanceToPlayer = 2f;

    [Tooltip("If true, a forward raycast prevents her from clipping into walls.")]
    [SerializeField] private bool useWallSafety = true;

    [Tooltip("Distance the wall raycast checks. Must be at least 0.5m.")]
    [SerializeField] private float wallCheckDistance = 0.7f;

    [Tooltip("Layers considered as walls/obstacles for wall safety raycast.")]
    [SerializeField] private LayerMask wallLayers = ~0; // All layers by default

    [Header("Appearance")]
    [SerializeField] private float fadeInDuration = 0.5f;

    [Header("Spawn-in-front-of-player (used by AppearInFrontOfPlayer)")]
    [Tooltip("Distance in front of the player where the visitor spawns. ~3m feels close but not in-your-face.")]
    [SerializeField] private float spawnDistanceFromPlayer = 3f;
    [Tooltip("Layers considered as walls when checking if the spawn spot is blocked. Default = all.")]
    [SerializeField] private LayerMask spawnWallLayers = ~0;
    [Tooltip("Animator state to snap into when spawning in front of the player " +
             "(e.g. 'Idle', 'Standing'). Leave empty to keep the default Animator state.")]
    [SerializeField] private string appearInFrontPoseState = "Idle";

    [Header("Events")]
    [SerializeField] private UnityEvent onPlayerCaught;

    private enum MovementMode { Idle, Walking, Running }
    private MovementMode mode = MovementMode.Idle;
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

    // Reposition the visitor to a spot in front of the player, then fade in.
    // Use this for "she appears wherever you are right now" moments.
    public void AppearInFrontOfPlayer()
    {
        // Re-acquire player if needed (in case scene was reloaded).
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
        if (player == null)
        {
            Debug.LogWarning("VisitorController.AppearInFrontOfPlayer: no player found.");
            return;
        }

        // Use the player's flat forward direction (ignore vertical look).
        Vector3 forward = player.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
        forward.Normalize();

        // If a wall is between the player and the desired spawn point,
        // pull the spawn distance back to just in front of the wall.
        float effectiveDistance = spawnDistanceFromPlayer;
        Vector3 rayOrigin = player.position + Vector3.up * 1.0f; // chest height
        if (Physics.Raycast(rayOrigin, forward, out RaycastHit hit,
                            spawnDistanceFromPlayer, spawnWallLayers, QueryTriggerInteraction.Ignore))
        {
            effectiveDistance = Mathf.Max(0.5f, hit.distance - 0.5f);
        }

        // Horizontal spawn position (X/Z only).
        Vector3 horizontalSpawn = player.position + forward * effectiveDistance;

        // Find the floor under the spawn point by casting downward from the
        // player's own y (which is guaranteed to be inside the room — below the
        // ceiling and above the floor). Casting from way above the player risks
        // starting above the ceiling and hitting the ceiling top instead.
        // Temporarily disable any colliders on Sophie so the ray can't hit her own bones.
        Collider[] myColliders = GetComponentsInChildren<Collider>();
        foreach (Collider c in myColliders) if (c != null) c.enabled = false;

        float spawnY = player.position.y; // fallback if no floor is found
        Vector3 floorRayStart = new Vector3(horizontalSpawn.x, player.position.y, horizontalSpawn.z);
        bool floorFound = Physics.Raycast(floorRayStart, Vector3.down, out RaycastHit floorHit,
                                          50f, spawnWallLayers, QueryTriggerInteraction.Ignore);
        if (floorFound)
        {
            spawnY = floorHit.point.y;
        }

        foreach (Collider c in myColliders) if (c != null) c.enabled = true;

        Vector3 spawnPos = new Vector3(horizontalSpawn.x, spawnY, horizontalSpawn.z);
        transform.position = spawnPos;

        Debug.Log($"VisitorController.AppearInFrontOfPlayer: player at {player.position}, " +
                  $"spawned at {spawnPos}. Floor raycast {(floorFound ? $"hit {floorHit.collider.name} at y={floorHit.point.y:F2}" : "missed")}.");

        // Face the player.
        Vector3 lookDir = player.position - transform.position;
        lookDir.y = 0f;
        if (lookDir.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(lookDir);
        }

        // Clear any leftover walk/run state so the standing pose isn't fighting movement bools.
        mode = MovementMode.Idle;
        if (anim != null)
        {
            anim.SetBool("Walking", false);
            anim.SetBool("Running", false);
        }

        Appear();

        // Force the desired pose. Animator.Play(name, layer, normalizedTime)
        // jumps directly into the state and resets it to frame 0.
        if (anim != null && !string.IsNullOrEmpty(appearInFrontPoseState))
        {
            anim.Play(appearInFrontPoseState, 0, 0f);
        }
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
        Debug.Log("VisitorController: StartWalkingTowardPlayer");
        mode = MovementMode.Walking;
        if (anim != null)
        {
            anim.SetBool("Walking", true);
            anim.SetBool("Running", false);
        }
    }

    public void StartRunningTowardPlayer()
    {
        Debug.Log("VisitorController: StartRunningTowardPlayer");
        mode = MovementMode.Running;
        if (anim != null)
        {
            anim.SetBool("Walking", false);
            anim.SetBool("Running", true);
        }
    }

    public void EscalateToRunning()
    {
        Debug.Log("VisitorController: EscalateToRunning");
        if (mode == MovementMode.Walking || mode == MovementMode.Running)
        {
            mode = MovementMode.Running;
            if (anim != null)
            {
                anim.SetBool("Walking", false);
                anim.SetBool("Running", true);
            }
        }
    }

    public void StopMoving()
    {
        Debug.Log("VisitorController: StopMoving");
        mode = MovementMode.Idle;
        if (anim != null)
        {
            anim.SetBool("Walking", false);
            anim.SetBool("Running", false);
        }
    }

    public void WalkToTarget(Transform target)
    {
        walkTarget = target;
        StartWalkingTowardPlayer();
    }

    private void Update()
    {
        if (mode == MovementMode.Idle) return;

        Transform target = walkTarget != null ? walkTarget : player;
        if (target == null) return;

        Vector3 direction = target.position - transform.position;
        direction.y = 0f;
        float distance = direction.magnitude;

        bool isTranslatingThisFrame = false;

        if (direction.sqrMagnitude > 0.01f)
        {
            Vector3 dirNormalized = direction.normalized;
            float alignment = Vector3.Dot(transform.forward, dirNormalized);

            // Always rotate toward the target.
            float rotateSpeed = Mathf.Lerp(4f, 1.5f, (alignment + 1f) * 0.5f);
            Quaternion lookRot = Quaternion.LookRotation(dirNormalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * rotateSpeed);

            // Translate forward only if:
            // - Reasonably aligned with target.
            // - Not too close to the player (cinematic constraint).
            // - No wall blocking forward motion.
            bool aligned = alignment > 0.5f;
            bool farEnough = walkTarget != null || distance > minDistanceToPlayer;
            bool clearAhead = !useWallSafety || !IsWallAhead();

            if (aligned && farEnough && clearAhead)
            {
                float speed = (mode == MovementMode.Running) ? runSpeed : walkSpeed;
                transform.position += transform.forward * speed * Time.deltaTime;
                isTranslatingThisFrame = true;
            }
        }

        // Drive walk/run animation by whether we actually translated this frame.
        if (anim != null)
        {
            bool walking = isTranslatingThisFrame && mode == MovementMode.Walking;
            bool running = isTranslatingThisFrame && mode == MovementMode.Running;
            anim.SetBool("Walking", walking);
            anim.SetBool("Running", running);
        }
    }

    // Forward raycast to detect walls.
    private bool IsWallAhead()
    {
        // Cast from chest height to avoid floor false-positives.
        Vector3 origin = transform.position + Vector3.up * 1.0f;
        return Physics.Raycast(origin, transform.forward, wallCheckDistance, wallLayers, QueryTriggerInteraction.Ignore);
    }

    // Visualize the wall check in Scene view for debugging.
    private void OnDrawGizmosSelected()
    {
        if (!useWallSafety) return;
        Gizmos.color = Color.yellow;
        Vector3 origin = transform.position + Vector3.up * 1.0f;
        Gizmos.DrawLine(origin, origin + transform.forward * wallCheckDistance);
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