using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// Periodically spawns visitor-monsters at random spawn points by repositioning
// pre-placed (but hidden) VisitorController instances. Uses a pool model so
// nothing is instantiated at runtime — just enabled/repositioned/disabled.
//
// Why pool instead of Instantiate prefabs:
//  - No GC spikes during gameplay
//  - All visitors are scene objects (debuggable in editor)
//  - UnityEvent references to specific visitors stay valid
//
// Setup:
//  1. Pre-place 3-5 VisitorController GameObjects in the level (clones of the
//     Sophie rig, or different meshes — the chase logic is the same).
//  2. On EACH visitor: check 'Deals Contact Damage' and configure the range
//     and per-second damage rate. Check 'Adapt To Player Speed' if you want
//     them to walk/run with the player. Disable 'Persistent Chase' on visitor
//     monsters so they don't teleport — that's Sophie's signature behavior.
//  3. Each visitor's GameObject must be ACTIVE at scene start (so UnityEvents
//     can reference them). Their Awake disables renderers automatically.
//  4. Place empty GameObjects around the level as spawn points (e.g.
//     SpawnPoint_LabCorner, SpawnPoint_Storage, etc.).
//  5. Create one "VisitorSpawner" GameObject. Drag visitors into visitorPool,
//     spawn points into spawnPoints, set min/max interval, hit Play.

public class VisitorSpawner : MonoBehaviour
{
    [Header("Pool")]
    [Tooltip("Pre-placed visitor monsters in the scene. The spawner activates one at a random point.")]
    [SerializeField] private List<VisitorController> visitorPool = new List<VisitorController>();

    [Header("Spawn points")]
    [Tooltip("Empty GameObjects placed around the level. The spawner picks one randomly each interval.")]
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();

    [Header("Timing")]
    [SerializeField] private float minSpawnInterval = 25f;
    [SerializeField] private float maxSpawnInterval = 55f;

    [Header("Constraints")]
    [Tooltip("Player to keep visitors away from at spawn time (avoids spawning right in front of them).")]
    [SerializeField] private Transform player;
    [Tooltip("A spawn point must be at least this far from the player to be eligible.")]
    [SerializeField] private float minDistanceFromPlayer = 8f;
    [Tooltip("Maximum visitors active at the same time. Keeps the game from overwhelming the player.")]
    [SerializeField] private int maxActiveVisitors = 2;

    [Header("Behavior")]
    [Tooltip("If true, the spawner starts automatically at scene start. " +
             "If false, call StartSpawning() from a UnityEvent (e.g. when a specific area is entered).")]
    [SerializeField] private bool autoStart = true;
    [Tooltip("Initial delay before the first spawn, so the player has time to orient.")]
    [SerializeField] private float firstSpawnDelay = 10f;

    [Header("Events")]
    [SerializeField] private UnityEvent onVisitorSpawned;

    private Coroutine spawnerRoutine;

    private void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
        if (autoStart) StartSpawning();
    }

    public void StartSpawning()
    {
        if (spawnerRoutine != null) StopCoroutine(spawnerRoutine);
        spawnerRoutine = StartCoroutine(SpawnLoop());
    }

    public void StopSpawning()
    {
        if (spawnerRoutine != null)
        {
            StopCoroutine(spawnerRoutine);
            spawnerRoutine = null;
        }
    }

    private IEnumerator SpawnLoop()
    {
        yield return new WaitForSeconds(firstSpawnDelay);

        while (true)
        {
            TrySpawnVisitor();
            float wait = Random.Range(minSpawnInterval, maxSpawnInterval);
            yield return new WaitForSeconds(wait);
        }
    }

    private void TrySpawnVisitor()
    {
        // Count currently-active visitors and find an inactive one to use.
        VisitorController available = null;
        int activeCount = 0;
        foreach (VisitorController v in visitorPool)
        {
            if (v == null) continue;
            if (IsVisitorActive(v)) activeCount++;
            else if (available == null) available = v;
        }

        if (activeCount >= maxActiveVisitors)
        {
            Debug.Log($"VisitorSpawner: at cap ({activeCount}/{maxActiveVisitors}), skipping spawn");
            return;
        }
        if (available == null)
        {
            Debug.Log("VisitorSpawner: no available visitors in pool, skipping spawn");
            return;
        }

        // Find spawn points far enough from the player.
        List<Transform> validPoints = new List<Transform>();
        foreach (Transform sp in spawnPoints)
        {
            if (sp == null) continue;
            if (player == null || Vector3.Distance(sp.position, player.position) >= minDistanceFromPlayer)
            {
                validPoints.Add(sp);
            }
        }
        if (validPoints.Count == 0)
        {
            Debug.Log("VisitorSpawner: no spawn points are far enough from the player, skipping");
            return;
        }

        Transform chosen = validPoints[Random.Range(0, validPoints.Count)];

        // Position, enable visuals, start chase.
        available.transform.position = chosen.position;
        available.transform.rotation = chosen.rotation;
        available.Appear();
        available.StartWalkingTowardPlayer();

        Debug.Log($"VisitorSpawner: spawned {available.name} at {chosen.name} ({chosen.position})");
        onVisitorSpawned?.Invoke();
    }

    // A visitor counts as "active" if its renderers are enabled.
    private bool IsVisitorActive(VisitorController v)
    {
        if (v == null) return false;
        Renderer r = v.GetComponentInChildren<Renderer>(includeInactive: false);
        return r != null && r.enabled;
    }
}
