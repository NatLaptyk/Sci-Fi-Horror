using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// Head tracking + turn-around-and-stare for humanoid characters.
//
// IMPORTANT: this version uses Unity's Animator IK system (OnAnimatorIK + SetLookAtPosition)
// for natural look-at tracking. Direct bone manipulation is used only for the head-turn effect.
//
// SETUP REQUIREMENTS:
//  1. The character must be a Humanoid avatar.
//  2. The Animator Controller's Base Layer must have "IK Pass" enabled.
//  3. Drag the head bone into headBone field for the head-turn effect.

public class HeadTracker : MonoBehaviour
{
    public enum HeadMode { Idle, TrackTarget, HeadTurn }

    [Header("References")]
    [Tooltip("The Animator on the humanoid character. IK Pass must be enabled on its Base Layer.")]
    [SerializeField] private Animator animator;

    [Tooltip("The head bone Transform. Used for the head-turn effect. Auto-found by name if left null.")]
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
    [Tooltip("How far to rotate the head, in degrees. 180 = look directly backward.")]
    [SerializeField] private float headTurnAngle = 180f;

    [Tooltip("Direction. 1 = clockwise, -1 = counter-clockwise.")]
    [SerializeField] private float turnDirection = 1f;

    [Tooltip("Time to rotate from forward to facing-backward.")]
    [SerializeField] private float turnOutDuration = 1.5f;

    [Tooltip("Time to hold the staring-backward pose. The horror beat.")]
    [SerializeField] private float stareDuration = 4f;

    [Tooltip("Time to rotate back to forward. Set to 0 if you don't want the head to return.")]
    [SerializeField] private float turnBackDuration = 1f;

    [Tooltip("Easing curve. Default is ease-in-out, which feels mechanical and wrong.")]
    [SerializeField] private AnimationCurve turnCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [Tooltip("Played when the turn-out begins. A neck crack or wet snap.")]
    [SerializeField] private AudioClip turnOutSound;

    [Tooltip("Played when the head returns to forward. Optional.")]
    [SerializeField] private AudioClip turnBackSound;

    [Header("Events")]
    [Tooltip("Fires once the head reaches the staring-back position (start of the stare).")]
    [SerializeField] private UnityEvent onStareReached;

    [Tooltip("Fires after stareDuration ends (start of the return motion).")]
    [SerializeField] private UnityEvent onStareEnded;

    [Tooltip("Fires when the entire turn sequence (out + stare + return) is complete.")]
    [SerializeField] private UnityEvent onTurnSequenceComplete;

    // Internal state.
    private HeadMode currentMode = HeadMode.Idle;
    private float currentLookWeight = 0f;
    private float currentTurnAngle = 0f;
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

    // Public API.
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

    public void RecaptureRestPose()
    {
        // No-op in IK mode. Kept for backwards compatibility.
    }

    // Begin the turn-out → stare → return sequence.
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
        currentMode = HeadMode.HeadTurn;
        StartCoroutine(HeadTurnRoutine());
    }

    // Backwards-compatible alias for any existing wiring that calls StartSpin360.
    public void StartSpin360()
    {
        StartHeadTurn();
    }

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

        // Phase 2: hold the stare. THIS is the horror moment.
        onStareReached?.Invoke();
        yield return new WaitForSeconds(stareDuration);
        onStareEnded?.Invoke();

        // Phase 3: rotate back to forward (or stay if turnBackDuration is 0).
        if (turnBackDuration > 0f)
        {
            if (audioSource != null && turnBackSound != null)
            {
                audioSource.PlayOneShot(turnBackSound);
            }

            float startAngle = currentTurnAngle;
            elapsed = 0f;
            while (elapsed < turnBackDuration)
            {
                elapsed += Time.deltaTime;
                float k = turnCurve.Evaluate(elapsed / turnBackDuration);
                currentTurnAngle = Mathf.Lerp(startAngle, 0f, k);
                yield return null;
            }
            currentTurnAngle = 0f;
        }

        currentMode = HeadMode.Idle;
        onTurnSequenceComplete?.Invoke();
    }

    // Called by Unity when IK Pass is enabled on the Animator's Base Layer.
    private void OnAnimatorIK(int layerIndex)
    {
        if (animator == null) return;

        // Disable IK during head turn so it doesn't fight the manual rotation.
        float targetWeight = (currentMode == HeadMode.TrackTarget && target != null) ? lookAtMaxWeight : 0f;
        currentLookWeight = Mathf.MoveTowards(currentLookWeight, targetWeight, weightRampSpeed * Time.deltaTime);

        if (currentLookWeight > 0.001f && target != null)
        {
            animator.SetLookAtPosition(target.position);
            animator.SetLookAtWeight(currentLookWeight, bodyWeight, headWeight, eyesWeight, clampWeight);
        }
    }

    // Head-turn rotation runs in LateUpdate, after the Animator and IK pass.
    private void LateUpdate()
    {
        if (currentMode == HeadMode.HeadTurn && headBone != null && turnRestCaptured)
        {
            headBone.localRotation = turnRestRotation * Quaternion.Euler(0f, currentTurnAngle, 0f);
        }
    }
}