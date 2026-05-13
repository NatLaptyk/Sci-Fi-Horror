using System.Collections;
using UnityEngine;

// Forces the player camera to smoothly rotate toward a target Transform,
// then holds it there for a specified duration. Used for scripted cinematic
// moments where the player must be looking at something specific (like Sophie's
// head turn).
//
// Setup:
//  1. Attach to any GameObject (it'll find the camera automatically, or assign one).
//  2. Drag the camera Transform you want to control (usually the Starter Assets
//     PlayerCameraRoot or MainCamera).
//  3. From a UnityEvent, call ForceLookAt(target) with the Transform to look at.
//
// IMPORTANT: this script directly sets the camera's rotation. While active,
// player look input should be disabled (e.g. by toggling StarterAssetsInputs.enabled).

public class CameraLookAt : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The camera Transform to control. Leave null to auto-find Camera.main.")]
    [SerializeField] private Transform cameraTransform;

    [Header("Behavior")]
    [Tooltip("Time to smoothly rotate from current view to look at target.")]
    [SerializeField] private float rotateDuration = 0.6f;

    [Tooltip("Easing curve for the rotation.")]
    [SerializeField] private AnimationCurve rotateCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Coroutine activeRoutine;
    private Transform currentTarget;
    private bool isHolding = false;

    private void Awake()
    {
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    // Call from a UnityEvent. Smoothly rotates the camera to look at `target`,
    // then holds the camera in that orientation until ReleaseLook() is called.
    public void ForceLookAt(Transform target)
    {
        if (target == null || cameraTransform == null) return;
        if (activeRoutine != null) StopCoroutine(activeRoutine);
        currentTarget = target;
        activeRoutine = StartCoroutine(LookAtRoutine(target));
    }

    // Stop forcing the camera. Player look input should be re-enabled separately.
    public void ReleaseLook()
    {
        isHolding = false;
        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
            activeRoutine = null;
        }
        currentTarget = null;
    }

    private IEnumerator LookAtRoutine(Transform target)
    {
        Quaternion startRotation = cameraTransform.rotation;
        Vector3 toTarget = target.position - cameraTransform.position;
        if (toTarget.sqrMagnitude < 0.001f) yield break;

        Quaternion targetRotation = Quaternion.LookRotation(toTarget);

        // Phase 1: smoothly rotate toward target.
        float elapsed = 0f;
        while (elapsed < rotateDuration)
        {
            elapsed += Time.deltaTime;
            float k = rotateCurve.Evaluate(elapsed / rotateDuration);
            cameraTransform.rotation = Quaternion.Slerp(startRotation, targetRotation, k);
            yield return null;
        }
        cameraTransform.rotation = targetRotation;

        // Phase 2: hold view on target for as long as ReleaseLook isn't called.
        // Continuously update target rotation in case the target moves slightly (Sophie's head bone).
        isHolding = true;
        while (isHolding)
        {
            if (target != null)
            {
                Vector3 t = target.position - cameraTransform.position;
                if (t.sqrMagnitude > 0.001f)
                {
                    cameraTransform.rotation = Quaternion.LookRotation(t);
                }
            }
            yield return null;
        }
    }
}