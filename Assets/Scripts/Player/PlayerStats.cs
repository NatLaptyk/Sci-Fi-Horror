using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Tracks Adam's health and sanity. Drives the bar UI and fires threshold events
// for breakdown effects (vignette, chromatic aberration, audio cues).
//
// Health: drains when Sophie is within healthDrainRange. Regenerates when she's far.
//         Represents physical proximity danger (her hands reaching for him).
//
// Sanity: drains continuously when Sophie is within sanityProximityRange OR within
//         line-of-sight of the camera. Regenerates after the player has been
//         "safe" (away + not looking) for sanityRegenDelay seconds.
//         Represents psychological dread.
//
// Threshold events fire when bars cross lowThreshold or hit zero. Wire those
// to Volume effects, audio sources, and Sophie's aggression toggle in the Inspector.

public class PlayerStats : MonoBehaviour
{
    [Header("Initial values")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float maxSanity = 100f;

    [Header("Health (Sophie proximity)")]
    [Tooltip("Sophie within this distance drains health.")]
    [SerializeField] private float healthDrainRange = 2f;
    [Tooltip("Health drained per second when she's within drain range.")]
    [SerializeField] private float healthDrainRate = 12f;
    [Tooltip("Sophie beyond this distance allows health to regenerate.")]
    [SerializeField] private float healthRegenSafeDistance = 8f;
    [Tooltip("Health regenerated per second when she's far away.")]
    [SerializeField] private float healthRegenRate = 4f;

    [Header("Sanity (Sophie proximity + line-of-sight)")]
    [Tooltip("Sanity drains when Sophie is within this distance, even if not visible.")]
    [SerializeField] private float sanityProximityRange = 6f;
    [Tooltip("Sanity drained per second from proximity alone.")]
    [SerializeField] private float sanityCloseDrainRate = 3f;
    [Tooltip("Extra sanity drained per second when the player can actually see Sophie.")]
    [SerializeField] private float sanityVisibleDrainRate = 4f;
    [Tooltip("Dot product threshold for 'looking at Sophie'. 0.5 ~= 60-degree FOV cone.")]
    [Range(0f, 1f)] [SerializeField] private float lookAtThreshold = 0.5f;
    [Tooltip("Sophie must be beyond this distance for sanity to regenerate.")]
    [SerializeField] private float sanitySafeDistance = 10f;
    [Tooltip("Seconds the player must be 'safe' before sanity starts regenerating.")]
    [SerializeField] private float sanityRegenDelay = 2f;
    [Tooltip("Sanity regenerated per second after the safety delay elapses.")]
    [SerializeField] private float sanityRegenRate = 2.5f;

    [Header("References")]
    [Tooltip("The player's root transform — used for distance-to-Sophie checks. " +
             "Auto-finds the GameObject tagged 'Player' if left empty.")]
    [SerializeField] private Transform playerTransform;
    [Tooltip("Sophie's root transform. Auto-finds by name 'Sophie' if left empty.")]
    [SerializeField] private Transform sophie;
    [Tooltip("The player's camera, used for line-of-sight check. Auto-finds Camera.main if empty.")]
    [SerializeField] private Camera playerCamera;
    [Tooltip("Layers the line-of-sight raycast checks. Default = Everything.")]
    [SerializeField] private LayerMask losBlockingLayers = ~0;

    [Header("UI fills (assign Image components with Filled type)")]
    [SerializeField] private Image healthBarFill;
    [SerializeField] private Image sanityBarFill;

    [Header("Breakdown thresholds")]
    [Tooltip("Bar value (0-100) below which the 'low' breakdown event fires.")]
    [SerializeField] private float lowHealthThreshold = 30f;
    [SerializeField] private float lowSanityThreshold = 30f;

    [Header("Events — wire breakdown effects here")]
    [Tooltip("Fires when health drops below lowHealthThreshold. Wire heavy breathing, red vignette, etc.")]
    [SerializeField] private UnityEvent onLowHealthEntered;
    [Tooltip("Fires when health rises back above lowHealthThreshold. Wire the inverse (stop breathing audio, fade out vignette).")]
    [SerializeField] private UnityEvent onLowHealthExited;
    [Tooltip("Fires when sanity drops below lowSanityThreshold. Wire distortion, whispers, Sophie aggression toggle.")]
    [SerializeField] private UnityEvent onLowSanityEntered;
    [SerializeField] private UnityEvent onLowSanityExited;
    [Tooltip("Fires once when health hits 0. Severe breakdown — heavy red screen, slow movement, etc.")]
    [SerializeField] private UnityEvent onHealthBottomedOut;
    [Tooltip("Fires once when sanity hits 0. Severe psychological breakdown — heavy distortion, chaos audio.")]
    [SerializeField] private UnityEvent onSanityBottomedOut;

    [Header("Drain master switch")]
    [Tooltip("If false, no proximity drain or regeneration runs. Use to disable stat changes during scripted segments (e.g. the bedroom dialogue) and arm them via EnableDrain() when the chase begins.")]
    [SerializeField] private bool drainEnabled = false;

    [Header("Debug")]
    [Tooltip("If true, prints a status line to the Console every second so you can confirm distances and drain are happening.")]
    [SerializeField] private bool debugLogging = true;

    private float currentHealth;
    private float currentSanity;
    private float timeSinceLastDrain;
    private bool wasLowHealth = false;
    private bool wasLowSanity = false;
    private bool firedHealthBottom = false;
    private bool firedSanityBottom = false;
    private float debugLogTimer = 0f;

    private void Awake()
    {
        currentHealth = maxHealth;
        currentSanity = maxSanity;

        if (playerTransform == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
            else playerTransform = transform; // fallback to self
        }

        if (sophie == null)
        {
            GameObject s = GameObject.Find("Sophie");
            if (s != null) sophie = s.transform;
        }
        if (playerCamera == null) playerCamera = Camera.main;

        Debug.Log($"PlayerStats Awake: player={(playerTransform != null ? playerTransform.name : "NULL")}, " +
                  $"sophie={(sophie != null ? sophie.name : "NULL")}, " +
                  $"camera={(playerCamera != null ? playerCamera.name : "NULL")}");
    }

    private void Update()
    {
        if (sophie == null || playerCamera == null || playerTransform == null)
        {
            UpdateUI();
            return;
        }

        Vector3 toSophie = sophie.position - playerTransform.position;
        toSophie.y = 0f;
        float distanceToSophie = toSophie.magnitude;

        // Visibility: is Sophie in front of the camera AND not blocked by geometry?
        bool sophieVisible = false;
        Vector3 toSophieFromCam = sophie.position + Vector3.up * 1.5f - playerCamera.transform.position;
        float lookDot = Vector3.Dot(playerCamera.transform.forward, toSophieFromCam.normalized);
        if (lookDot > lookAtThreshold)
        {
            // Line-of-sight raycast. If the first hit is Sophie or her child, we can see her.
            if (Physics.Linecast(playerCamera.transform.position, sophie.position + Vector3.up * 1.5f,
                                 out RaycastHit los, losBlockingLayers, QueryTriggerInteraction.Ignore))
            {
                if (los.transform == sophie || los.transform.IsChildOf(sophie))
                {
                    sophieVisible = true;
                }
            }
            else
            {
                // Nothing in the way at all.
                sophieVisible = true;
            }
        }

        // Master switch: skip all proximity drain/regen if disabled.
        if (!drainEnabled)
        {
            UpdateUI();
            return;
        }

        // === HEALTH update ===
        if (distanceToSophie < healthDrainRange)
        {
            currentHealth -= healthDrainRate * Time.deltaTime;
        }
        else if (distanceToSophie > healthRegenSafeDistance)
        {
            currentHealth += healthRegenRate * Time.deltaTime;
        }
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        // === SANITY update ===
        bool sanityDrainingThisFrame = false;
        if (distanceToSophie < sanityProximityRange)
        {
            currentSanity -= sanityCloseDrainRate * Time.deltaTime;
            sanityDrainingThisFrame = true;
        }
        if (sophieVisible)
        {
            currentSanity -= sanityVisibleDrainRate * Time.deltaTime;
            sanityDrainingThisFrame = true;
        }
        if (sanityDrainingThisFrame)
        {
            timeSinceLastDrain = 0f;
        }
        else
        {
            timeSinceLastDrain += Time.deltaTime;
            if (timeSinceLastDrain > sanityRegenDelay && distanceToSophie > sanitySafeDistance)
            {
                currentSanity += sanityRegenRate * Time.deltaTime;
            }
        }
        currentSanity = Mathf.Clamp(currentSanity, 0f, maxSanity);

        UpdateUI();
        UpdateThresholdEvents();

        if (debugLogging)
        {
            debugLogTimer += Time.deltaTime;
            if (debugLogTimer > 1f)
            {
                debugLogTimer = 0f;
                Debug.Log($"PlayerStats: distance={distanceToSophie:F2}m | " +
                          $"sophieVisible={sophieVisible} | " +
                          $"health={currentHealth:F1}/{maxHealth:F0} | " +
                          $"sanity={currentSanity:F1}/{maxSanity:F0} | " +
                          $"player.pos={playerTransform.position} | sophie.pos={sophie.position}");
            }
        }
    }

    private void UpdateUI()
    {
        if (healthBarFill != null) healthBarFill.fillAmount = currentHealth / maxHealth;
        if (sanityBarFill != null) sanityBarFill.fillAmount = currentSanity / maxSanity;
    }

    private void UpdateThresholdEvents()
    {
        bool isLowHealth = currentHealth < lowHealthThreshold;
        if (isLowHealth && !wasLowHealth) onLowHealthEntered?.Invoke();
        else if (!isLowHealth && wasLowHealth) onLowHealthExited?.Invoke();
        wasLowHealth = isLowHealth;

        bool isLowSanity = currentSanity < lowSanityThreshold;
        if (isLowSanity && !wasLowSanity) onLowSanityEntered?.Invoke();
        else if (!isLowSanity && wasLowSanity) onLowSanityExited?.Invoke();
        wasLowSanity = isLowSanity;

        if (currentHealth <= 0f && !firedHealthBottom)
        {
            onHealthBottomedOut?.Invoke();
            firedHealthBottom = true;
        }
        else if (currentHealth > 0f)
        {
            firedHealthBottom = false; // re-arm if it recovers
        }

        if (currentSanity <= 0f && !firedSanityBottom)
        {
            onSanityBottomedOut?.Invoke();
            firedSanityBottom = true;
        }
        else if (currentSanity > 0f)
        {
            firedSanityBottom = false;
        }
    }

    // Master drain toggle. Wire EnableDrain() to your chase-start event (e.g. Jenkins's
    // onThisLogFinished) so stats don't move during the bedroom dialogue.
    public void EnableDrain() { drainEnabled = true; }
    public void DisableDrain() { drainEnabled = false; }
    public bool DrainEnabled => drainEnabled;

    // Public API for other scripts.
    public float Health => currentHealth;
    public float Sanity => currentSanity;
    public float HealthNormalized => currentHealth / maxHealth;
    public float SanityNormalized => currentSanity / maxSanity;

    // External damage hooks — wire from UnityEvents (puzzle fails, scripted scares, etc.).
    public void DrainSanity(float amount)
    {
        currentSanity = Mathf.Clamp(currentSanity - amount, 0f, maxSanity);
    }

    public void DrainHealth(float amount)
    {
        currentHealth = Mathf.Clamp(currentHealth - amount, 0f, maxHealth);
    }

    public void RestoreSanity(float amount)
    {
        currentSanity = Mathf.Clamp(currentSanity + amount, 0f, maxSanity);
    }

    public void RestoreHealth(float amount)
    {
        currentHealth = Mathf.Clamp(currentHealth + amount, 0f, maxHealth);
    }
}
