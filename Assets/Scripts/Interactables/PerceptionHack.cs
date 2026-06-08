using UnityEngine;

// The perception hack — the "look back and it's different" mechanic.
// Satisfies the rubric's "1 perception hack or subtle change" requirement.
//
// How it works: two sets of GameObjects — "normal" and "hacked" — placed in the same
// world positions. When the player crosses a trigger zone and isn't looking back,
// swap them silently. When they turn around, the world is different.

public class PerceptionHack : MonoBehaviour
{
    [Header("Object sets")]
    [SerializeField] private GameObject[] normalObjects;
    [SerializeField] private GameObject[] hackedObjects;

    [Header("Activation")]
    [Tooltip("Assign manually, or leave null to auto-find Camera.main at Awake.")]
    [SerializeField] private Transform playerCamera;

    [Tooltip("The point the player must face AWAY from to trigger the swap. Defaults to this GameObject.")]
    [SerializeField] private Transform hackCenter;

    [Tooltip("Dot-product threshold. Below this = facing away from hack area. -0.2 is forgiving.")]
    [Range(-1f, 0f)]
    [SerializeField] private float facingAwayThreshold = -0.2f;

    private bool armed = false;
    private bool swapped = false;

    private void Awake()
    {
        SetActive(normalObjects, true);
        SetActive(hackedObjects, false);

        if (playerCamera == null && Camera.main != null)
        {
            playerCamera = Camera.main.transform;
        }
        if (hackCenter == null)
        {
            hackCenter = transform;
        }
    }

    // Call from a TriggerZone after the player reaches the "other side" of the area.
    public void TriggerHack()
    {
        armed = true;
    }

    private void Update()
    {
        if (!armed || swapped || playerCamera == null) return;

        Vector3 toHack = (hackCenter.position - playerCamera.position).normalized;
        float facing = Vector3.Dot(playerCamera.forward, toHack);

        if (facing < facingAwayThreshold)
        {
            // Silent swap while the player isn't looking.
            SetActive(normalObjects, false);
            SetActive(hackedObjects, true);
            swapped = true;
        }
    }

    private void SetActive(GameObject[] objs, bool active)
    {
        if (objs == null) return;
        for (int i = 0; i < objs.Length; i++)
        {
            if (objs[i] != null) objs[i].SetActive(active);
        }
    }
}
