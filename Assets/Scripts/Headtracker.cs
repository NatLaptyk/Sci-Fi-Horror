using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// Head tracking + 360 spin for humanoid characters.
//
// IMPORTANT: this version uses Unity's Animator IK system (OnAnimatorIK + SetLookAtPosition)
// rather than directly setting bone rotations. This is the correct approach for HUMANOID avatars
// because the Animator overwrites bone rotations every frame and direct manipulation gets
// fought by the animation system.
//
// SETUP REQUIREMENTS:
//  1. The character must be a Humanoid avatar (set Rig Type = Humanoid in the FBX import settings).
//  2. The Animator Controller's Base Layer must have "IK Pass" enabled:
//     - Open Animator Controller window.
//     - Click the gear icon next to "Base Layer".
//     - Check "IK Pass".
//  3. Attach this script to the same GameObject as the Animator.
//  4. Drag the Animator into the animator field (auto-found if on same GameObject).
//  5. Optional: Drag the head bone into headBone field for the Spin360 effect (Spin360 still
//     uses direct rotation since IK doesn't support arbitrary spins).
//
// HOW IT WORKS:
//  - TrackTarget mode: sets a look-at target in OnAnimatorIK. Unity's IK system smoothly
//    rotates the head, neck, and slightly the spine to look at the target — much more natural
//    than manual bone rotation.
//  - Spin360 mode: directly rotates the head bone in LateUpdate (IK doesn't do 360 spins).
//    During spin, IK is disabled so it doesn't fight us.

public class HeadTracker : MonoBehaviour
{
    public enum HeadMode { Idle, TrackTarget, Spin360 }

    [Header("References")]
    [Tooltip("The Animator on the humanoid character. IK Pass must be enabled on its Base Layer.")]
    [SerializeField] private Animator animator;

    [Tooltip("The head bone Transform. Used ONLY for Spin360 mode. Auto-found by name if left null.")]
    [SerializeField] private Transform headBone;

    [Header("Tracking")]
    [Tooltip("Target the head should look at. Auto-finds Player camera if null.")]
    [SerializeField] private Transform target;

    [Tooltip("How fast the IK look-at weight ramps up when tracking starts. Higher = snappier.")]
    [SerializeField] private float weightRampSpeed = 3f;

    [Header("IK Look-At weights (0..1 each)")]
    [Tooltip("Overall look-at strength. 1 = full strength.")]
    [Range(0f, 1f)] [SerializeField] private float lookAtMaxWeight = 1f;

    [Tooltip("How much the body twists toward the target. 0 = body stays still, 1 = full twist.")]
    [Range(0f, 1f)] [SerializeField] private float bodyWeight = 0.1f;

    [Tooltip("How much the head rotates toward the target. Usually high.")]
    [Range(0f, 1f)] [SerializeField] private float headWeight = 1f;

    [Tooltip("How much the eyes rotate toward the target. Only works if avatar has eye bones.")]
    [Range(0f, 1f)] [SerializeField] private float eyesWeight = 0.5f;

    [Tooltip("Limits how far the look can deviate from forward. 0 = unlimited, 0.5 = roughly 90 degrees, 1 = forward only.")]
    [Range(0f, 1f)] [SerializeField] private float clampWeight = 0.4f;

    [Header("360 Spin")]
    [Tooltip("How long the full 360 spin takes, in seconds.")]
    [SerializeField] private float spinDuration = 3f;

    [Tooltip("Curve controlling the spin's easing.")]
    [SerializeField] private AnimationCurve spinCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Tooltip("Direction of the 360. 1 = clockwise, -1 = counter-clockwise.")]
    [SerializeField] private float spinDirection = 1f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip turn360Sound;

    [Header("Events")]
    [SerializeField] private UnityEvent onSpinComplete;

    // Internal state.
    private HeadMode currentMode = HeadMode.Idle;
    private float currentLookWeight = 0f;
    private float currentSpinAngle = 0f;
    private Quaternion spinRestRotation;
    private bool spinRestCaptured = false;

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        if (headBone == null)
        {
            headBone = FindHeadBone(transform);
        }

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

        if (animator == null)
        {
            Debug.LogWarning("HeadTracker: Animator not found.");
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
        Debug.Log("HeadTracker: TrackPlayer called. Target: " + (target != null ? target.name : "NULL"));
        currentMode = HeadMode.TrackTarget;
    }

    public void StopTracking()
    {
        Debug.Log("HeadTracker: StopTracking called.");
        currentMode = HeadMode.Idle;
    }

    // Kept for backwards compatibility with existing wiring. The IK approach doesn't need
    // a captured rest pose because Unity's IK system handles blending with the current animation.
    public void RecaptureRestPose()
    {
        Debug.Log("HeadTracker: RecaptureRestPose called (no-op in IK mode, but kept for compatibility).");
    }

    public void StartSpin360()
    {
        Debug.Log("HeadTracker: StartSpin360 called.");
        if (currentMode == HeadMode.Spin360) return;
        if (headBone == null)
        {
            Debug.LogWarning("HeadTracker: cannot Spin360 without a head bone reference.");
            return;
        }
        spinRestRotation = headBone.localRotation;
        spinRestCaptured = true;
        currentSpinAngle = 0f;
        currentMode = HeadMode.Spin360;
        StartCoroutine(SpinRoutine());
    }

    private IEnumerator SpinRoutine()
    {
        if (audioSource != null && turn360Sound != null)
        {
            audioSource.PlayOneShot(turn360Sound);
        }

        float elapsed = 0f;
        while (elapsed < spinDuration)
        {
            elapsed += Time.deltaTime;
            float k = spinCurve.Evaluate(elapsed / spinDuration);
            currentSpinAngle = 360f * k * spinDirection;
            yield return null;
        }

        currentSpinAngle = 360f * spinDirection;
        yield return null;
        currentSpinAngle = 0f;
        currentMode = HeadMode.Idle;
        onSpinComplete?.Invoke();
    }

    // Called by Unity automatically when IK Pass is enabled on the Animator's Base Layer.
    // This is the correct hook for humanoid IK overrides.
    private void OnAnimatorIK(int layerIndex)
    {
        if (animator == null) return;

        // Smoothly ramp the weight up/down based on whether we're tracking.
        float targetWeight = (currentMode == HeadMode.TrackTarget && target != null) ? lookAtMaxWeight : 0f;
        currentLookWeight = Mathf.MoveTowards(currentLookWeight, targetWeight, weightRampSpeed * Time.deltaTime);

        if (currentLookWeight > 0.001f && target != null)
        {
            animator.SetLookAtPosition(target.position);
            animator.SetLookAtWeight(currentLookWeight, bodyWeight, headWeight, eyesWeight, clampWeight);
        }
    }

    // Spin360 still uses direct bone rotation since IK doesn't do arbitrary spins.
    // This runs in LateUpdate AFTER the Animator and IK pass.
    private void LateUpdate()
    {
        if (currentMode == HeadMode.Spin360 && headBone != null && spinRestCaptured)
        {
            headBone.localRotation = spinRestRotation * Quaternion.Euler(0f, currentSpinAngle, 0f);
        }
    }
}