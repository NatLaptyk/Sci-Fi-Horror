using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// Head tracking + 360 spin for the Visitor.
//
// Two modes:
//  1. TrackTarget — head smoothly follows a target (the player). Owl-style.
//  2. Spin360 — performs one 360° rotation with ease curve and optional sound.
//
// Setup:
//  1. Attach this script to the Visitor's root GameObject (same one with VisitorController).
//  2. Drag the head bone into the headBone field.
//     - If you can't find it: expand the Visitor in the Hierarchy until you find a
//       bone named like "Head", "mixamorig:Head", "Bip01_Head" etc. Drag it in.
//     - Or: leave it null and the script will try to auto-find a Transform named "Head"
//       containing it (Mixamo: "mixamorig:Head"). Auto-find is best-effort.
//  3. Drag the Animator into the animator field (auto-found if on same GameObject).
//  4. Optional: assign turn360Sound for the neck-crack stinger.
//
// Why LateUpdate is critical:
//  Unity's Animator runs in Update. To override its head rotation, we must run
//  AFTER it, in LateUpdate. If you put this in Update, the Animator will overwrite
//  our rotation every frame and the head won't visibly move.

public class HeadTracker : MonoBehaviour
{
    public enum HeadMode { Idle, TrackTarget, Spin360 }

    [Header("References")]
    [Tooltip("The head bone Transform. Auto-found by name if left null.")]
    [SerializeField] private Transform headBone;

    [Tooltip("The Visitor's Animator. Used to detect when its rotations might fight ours.")]
    [SerializeField] private Animator animator;

    [Header("Tracking")]
    [Tooltip("Target the head should look at in TrackTarget mode. Auto-finds Player tag if null.")]
    [SerializeField] private Transform target;

    [Tooltip("How quickly the head rotates to face the target. Higher = snappier.")]
    [SerializeField] private float trackSpeed = 4f;

    [Tooltip("Maximum rotation offset from rest, in degrees. Set high (e.g. 180) for owl-style. Set ~70 for natural human.")]
    [SerializeField] private float maxYawOffset = 180f;

    [Tooltip("Maximum pitch (up/down) offset from rest, in degrees.")]
    [SerializeField] private float maxPitchOffset = 30f;

    [Header("360 Spin")]
    [Tooltip("How long the full 360 spin takes, in seconds.")]
    [SerializeField] private float spinDuration = 3f;

    [Tooltip("Curve controlling the spin's easing. Default ease-in-out feels mechanical and wrong.")]
    [SerializeField] private AnimationCurve spinCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Tooltip("Direction of the 360. 1 = clockwise (looking down), -1 = counter-clockwise.")]
    [SerializeField] private float spinDirection = 1f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [Tooltip("Played when the 360 spin starts. A neck-crack or wet snap.")]
    [SerializeField] private AudioClip turn360Sound;

    [Header("Events")]
    [Tooltip("Fires when the 360 spin completes.")]
    [SerializeField] private UnityEvent onSpinComplete;

    // Internal state.
    private HeadMode currentMode = HeadMode.Idle;
    private Quaternion restRotation;     // The "natural" head pose, captured at start.
    private float currentSpinAngle = 0f; // Used during Spin360.
    private bool restCaptured = false;

    private void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();

        if (headBone == null)
        {
            // Try to auto-find the head bone.
            headBone = FindHeadBone(transform);
            if (headBone == null)
            {
                Debug.LogWarning("HeadTracker: head bone not found. Drag it manually into the Inspector.");
            }
        }

        if (target == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
            {
                // Prefer the camera if the player has one (looks more natural — head follows where the camera is).
                Camera cam = p.GetComponentInChildren<Camera>();
                target = cam != null ? cam.transform : p.transform;
            }
        }
    }

    // Recursively search children for a bone whose name suggests it's the head.
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

    // Capture the rest pose the first time we modify the head (after the Animator has set it for the first frame).
    private void CaptureRestRotation()
    {
        if (headBone != null)
        {
            restRotation = headBone.localRotation;
            restCaptured = true;
        }
    }

    // Public API — call from UnityEvents or other scripts.
    public void TrackPlayer()
    {
        if (!restCaptured) CaptureRestRotation();
        currentMode = HeadMode.TrackTarget;
    }

    public void StopTracking()
    {
        currentMode = HeadMode.Idle;
        // Reset head to rest on next LateUpdate via the Idle case.
    }

    public void StartSpin360()
    {
        if (!restCaptured) CaptureRestRotation();
        if (currentMode == HeadMode.Spin360) return;
        currentSpinAngle = 0f;
        currentMode = HeadMode.Spin360;
        StartCoroutine(SpinRoutine());
    }

    private IEnumerator SpinRoutine()
    {
        // Play the neck sound timed to spin start.
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

        currentSpinAngle = 360f * spinDirection; // Snap to exactly 360.
        yield return null; // Allow one frame at full rotation.
        currentSpinAngle = 0f; // Reset (since 360 = 0).
        currentMode = HeadMode.Idle;
        onSpinComplete?.Invoke();
    }

    // CRITICAL: this MUST be in LateUpdate, after the Animator has set its rotations.
    private void LateUpdate()
    {
        if (headBone == null) return;
        if (!restCaptured) CaptureRestRotation();

        switch (currentMode)
        {
            case HeadMode.Idle:
                // Smoothly return to the rest rotation set by the Animator.
                // We don't snap because the Animator is also setting it — they should agree.
                break;

            case HeadMode.TrackTarget:
                if (target != null)
                {
                    UpdateTracking();
                }
                break;

            case HeadMode.Spin360:
                ApplySpinRotation();
                break;
        }
    }

    private void UpdateTracking()
    {
        // Convert target's world position into head's parent space.
        Vector3 toTargetWorld = target.position - headBone.position;
        Vector3 toTargetLocal = headBone.parent != null
            ? headBone.parent.InverseTransformDirection(toTargetWorld)
            : toTargetWorld;

        // Build a "look at" rotation in local space.
        if (toTargetLocal.sqrMagnitude < 0.001f) return;

        Quaternion lookLocal = Quaternion.LookRotation(toTargetLocal);

        // Decompose into yaw and pitch, clamp each.
        Vector3 euler = lookLocal.eulerAngles;
        float yaw = NormalizeAngle(euler.y);
        float pitch = NormalizeAngle(euler.x);

        yaw = Mathf.Clamp(yaw, -maxYawOffset, maxYawOffset);
        pitch = Mathf.Clamp(pitch, -maxPitchOffset, maxPitchOffset);

        Quaternion clampedLook = Quaternion.Euler(pitch, yaw, 0f);

        // Blend smoothly toward this rotation, starting from the rest pose.
        Quaternion targetRotation = restRotation * clampedLook;
        headBone.localRotation = Quaternion.Slerp(headBone.localRotation, targetRotation, Time.deltaTime * trackSpeed);
    }

    private void ApplySpinRotation()
    {
        // Spin around the head's local up axis. This is what makes it feel like
        // the head is rotating on top of the spine, not the whole body.
        headBone.localRotation = restRotation * Quaternion.Euler(0f, currentSpinAngle, 0f);
    }

    private float NormalizeAngle(float a)
    {
        // Convert 0..360 to -180..180.
        a = a % 360f;
        if (a > 180f) a -= 360f;
        if (a < -180f) a += 360f;
        return a;
    }
}