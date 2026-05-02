using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// Head tracking + turn-around-and-stare for humanoid characters.
//
// During the stare phase, the head can also tilt to look directly at the player
// (downward if the player is below her eyeline). This produces a "she's looking
// AT him specifically" effect — more menacing than a flat backward gaze.

public class HeadTracker : MonoBehaviour
{
    public enum HeadMode { Idle, TrackTarget, HeadTurn }

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Transform headBone;

    [Header("Tracking")]
    [SerializeField] private Transform target;
    [SerializeField] private float weightRampSpeed = 3f;

    [Header("IK Look-At weights (0..1 each)")]
    [Range(0f, 1f)] [SerializeField] private float lookAtMaxWeight = 1f;
    [Range(0f, 1f)] [SerializeField] private float bodyWeight = 0.1f;
    [Range(0f, 1f)] [SerializeField] private float headWeight = 1f;
    [Range(0f, 1f)] [SerializeField] private float eyesWeight = 0.5f;
    [Range(0f, 1f)] [SerializeField] private float clampWeight = 0.4f;

    [Header("Head Turn Behavior")]
    [SerializeField] private float headTurnAngle = 180f;
    [SerializeField] private float turnDirection = 1f;
    [SerializeField] private float turnOutDuration = 1.5f;
    [SerializeField] private float stareDuration = 4f;
    [SerializeField] private float turnBackDuration = 1f;
    [SerializeField] private AnimationCurve turnCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Stare-at-Player (during the stare phase)")]
    [Tooltip("When the head reaches the staring position, tilt it to look directly at the target. Adds a 'predator' quality to the stare.")]
    [SerializeField] private bool tiltToTargetDuringStare = true;

    [Tooltip("Time taken to tilt to the player's eyeline once the turn-out completes.")]
    [SerializeField] private float tiltDuration = 0.5f;

    [Tooltip("Maximum pitch (down or up). Limits how extreme the tilt can get to avoid mesh distortion.")]
    [Range(0f, 70f)] [SerializeField] private float maxPitchAngle = 35f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip turnOutSound;
    [SerializeField] private AudioClip turnBackSound;

    [Header("Events")]
    [SerializeField] private UnityEvent onStareReached;
    [SerializeField] private UnityEvent onStareEnded;
    [SerializeField] private UnityEvent onTurnSequenceComplete;

    // Internal state.
    private HeadMode currentMode = HeadMode.Idle;
    private float currentLookWeight = 0f;
    private float currentTurnAngle = 0f;
    private float currentTiltAngle = 0f;
    private Quaternion turnRestRotation;
    private bool turnRestCaptured = false;

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        if (headBone == null) headBone = FindHeadBone(transform);

        if (target == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
            {
                Camera cam = p.GetComponentInChildren<Camera>();
                if (cam == null) cam = Camera.main;
                target = cam != null ? cam.transform : p.transform;
            }
        }
    }

    private Transform FindHeadBone(Transform root)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>())
        {
            string n = child.name.ToLower();
            if (n == "head" || n.EndsWith(":head") || n.EndsWith("_head") || n.Contains("mixamorig:head"))
            {
                return child;
            }
        }
        return null;
    }

    public void TrackPlayer()
    {
        Debug.Log("HeadTracker: TrackPlayer called.");
        currentMode = HeadMode.TrackTarget;
    }

    public void StopTracking()
    {
        Debug.Log("HeadTracker: StopTracking called.");
        currentMode = HeadMode.Idle;
    }

    public void RecaptureRestPose() { /* No-op in IK mode */ }

    public void StartHeadTurn()
    {
        Debug.Log("HeadTracker: StartHeadTurn called.");
        if (currentMode == HeadMode.HeadTurn) return;
        if (headBone == null)
        {
            Debug.LogWarning("HeadTracker: cannot turn head without a head bone reference.");
            return;
        }
        turnRestRotation = headBone.localRotation;
        turnRestCaptured = true;
        currentTurnAngle = 0f;
        currentTiltAngle = 0f;
        currentMode = HeadMode.HeadTurn;
        StartCoroutine(HeadTurnRoutine());
    }

    public void StartSpin360() { StartHeadTurn(); }

    private IEnumerator HeadTurnRoutine()
    {
        // Phase 1: rotate from forward to backward.
        if (audioSource != null && turnOutSound != null)
        {
            audioSource.PlayOneShot(turnOutSound);
        }

        float targetAngle = headTurnAngle * turnDirection;
        float elapsed = 0f;
        while (elapsed < turnOutDuration)
        {
            elapsed += Time.deltaTime;
            float k = turnCurve.Evaluate(elapsed / turnOutDuration);
            currentTurnAngle = targetAngle * k;
            yield return null;
        }
        currentTurnAngle = targetAngle;

        // Phase 1.5: tilt the head to look directly at the player (if enabled).
        if (tiltToTargetDuringStare && target != null)
        {
            float requiredPitch = ComputePitchToTarget();
            requiredPitch = Mathf.Clamp(requiredPitch, -maxPitchAngle, maxPitchAngle);

            float t = 0f;
            float startTilt = currentTiltAngle;
            while (t < tiltDuration)
            {
                t += Time.deltaTime;
                float k = turnCurve.Evaluate(t / tiltDuration);
                currentTiltAngle = Mathf.Lerp(startTilt, requiredPitch, k);
                yield return null;
            }
            currentTiltAngle = requiredPitch;
        }

        // Phase 2: hold the stare. THIS is the horror moment.
        onStareReached?.Invoke();
        yield return new WaitForSeconds(stareDuration);
        onStareEnded?.Invoke();

        // Phase 3: rotate back to forward.
        if (turnBackDuration > 0f)
        {
            if (audioSource != null && turnBackSound != null)
            {
                audioSource.PlayOneShot(turnBackSound);
            }

            float startAngle = currentTurnAngle;
            float startTilt = currentTiltAngle;
            float t = 0f;
            while (t < turnBackDuration)
            {
                t += Time.deltaTime;
                float k = turnCurve.Evaluate(t / turnBackDuration);
                currentTurnAngle = Mathf.Lerp(startAngle, 0f, k);
                currentTiltAngle = Mathf.Lerp(startTilt, 0f, k);
                yield return null;
            }
            currentTurnAngle = 0f;
            currentTiltAngle = 0f;
        }

        currentMode = HeadMode.Idle;
        onTurnSequenceComplete?.Invoke();
    }

    // Compute how many degrees of pitch are needed to look directly at target,
    // measured from Sophie's current head position with her body facing forward.
    private float ComputePitchToTarget()
    {
        if (target == null || headBone == null) return 0f;

        // We need the angle in the LOCAL space of the head's parent (the neck),
        // but accounting for the fact that the head will be facing backward.
        // Simplest: compute world-space vector to target, then project pitch.
        Vector3 toTarget = target.position - headBone.position;

        // Horizontal distance vs vertical distance gives us the pitch.
        // But we want pitch relative to the head's "forward" direction during stare,
        // which is OPPOSITE to body forward.
        Vector3 forwardDuringStare = -transform.forward;

        // Project target direction onto the plane perpendicular to up, get horizontal length.
        Vector3 horizontal = Vector3.ProjectOnPlane(toTarget, Vector3.up);
        float horizontalDistance = horizontal.magnitude;

        if (horizontalDistance < 0.01f) return 0f;

        // Vertical difference: positive = target is below head (looking down = +pitch).
        float verticalDelta = headBone.position.y - target.position.y;

        // atan(vertical / horizontal) in degrees.
        float pitch = Mathf.Atan2(verticalDelta, horizontalDistance) * Mathf.Rad2Deg;

        return pitch;
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (animator == null) return;

        float targetWeight = (currentMode == HeadMode.TrackTarget && target != null) ? lookAtMaxWeight : 0f;
        currentLookWeight = Mathf.MoveTowards(currentLookWeight, targetWeight, weightRampSpeed * Time.deltaTime);

        if (currentLookWeight > 0.001f && target != null)
        {
            animator.SetLookAtPosition(target.position);
            animator.SetLookAtWeight(currentLookWeight, bodyWeight, headWeight, eyesWeight, clampWeight);
        }
    }

    private void LateUpdate()
    {
        if (currentMode == HeadMode.HeadTurn && headBone != null && turnRestCaptured)
        {
            // Combine yaw (turn) and pitch (tilt) into one rotation.
            headBone.localRotation = turnRestRotation * Quaternion.Euler(currentTiltAngle, currentTurnAngle, 0f);
        }
    }
}