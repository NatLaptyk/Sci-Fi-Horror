using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

// Main dialogue controller. Singleton.
//
// Plays a chain of DialogueNodes: Sophie speaks her opening lines, the player
// picks a choice from 3 buttons (or auto-advances if no choices), Adam replies,
// Sophie responds, advance to next node.
//
// Two ways to start dialogue:
//  - StartDialogue(node) — locks player movement (cinematic mode).
//  - StartDialogueFreeMovement(node) — leaves player free to walk around.
//
// Skip controls (development / testing):
//  - Press skipLineKey to advance past the current spoken line immediately.
//  - Hold skipDialogueKey to fast-forward the entire dialogue.
//  - These can be disabled for shipping by setting allowSkip = false.

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private DialogueUI dialogueUI;
    [SerializeField] private AudioSource voiceSource;

    [Header("Player control")]
    [SerializeField] private MonoBehaviour playerController;
    [SerializeField] private MonoBehaviour playerLookController;

    [Header("Pacing")]
    [SerializeField] private float beatBeforeChoices = 0.5f;
    [SerializeField] private float beatBeforeAdamReply = 0.3f;

    [Header("Skip controls")]
    [Tooltip("If true, the player can press skipLineKey to advance past the current line. Set false for the shipped game.")]
    [SerializeField] private bool allowSkip = true;

    [Tooltip("Press this key to skip past the currently playing line.")]
    [SerializeField] private Key skipLineKey = Key.Space;

    [Tooltip("Hold this key to fast-forward the entire dialogue (skips lines and choice waits).")]
    [SerializeField] private Key skipDialogueKey = Key.Tab;

    [Header("Events")]
    [SerializeField] private UnityEvent onDialogueStarted;
    [SerializeField] private UnityEvent onDialogueEnded;
    [SerializeField] private NamedEvent[] namedCompletionEvents;

    private DialogueNode currentNode;
    private bool isRunning = false;
    private bool currentRunLocksPlayer = true;
    private bool skipCurrentLine = false;
    private Coroutine activeRoutine;

    [System.Serializable]
    public class NamedEvent
    {
        public string eventName;
        public UnityEvent action;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (voiceSource == null) TryGetComponent(out voiceSource);
        if (voiceSource == null) voiceSource = gameObject.AddComponent<AudioSource>();
        voiceSource.playOnAwake = false;
        voiceSource.loop = false;
    }

    private void Update()
    {
        if (!isRunning || !allowSkip) return;
        if (Keyboard.current == null) return;

        // Skip the current line on press.
        if (Keyboard.current[skipLineKey].wasPressedThisFrame)
        {
            skipCurrentLine = true;
            if (voiceSource.isPlaying) voiceSource.Stop();
        }
    }

    private bool IsHoldingFastForward()
    {
        if (!allowSkip) return false;
        if (Keyboard.current == null) return false;
        return Keyboard.current[skipDialogueKey].isPressed;
    }

    // Lock player and start dialogue. Use for cinematic moments where the player must stay put.
    public void StartDialogue(DialogueNode startNode)
    {
        BeginDialogue(startNode, lockPlayer: true);
    }

    // Start dialogue WITHOUT locking the player.
    public void StartDialogueFreeMovement(DialogueNode startNode)
    {
        BeginDialogue(startNode, lockPlayer: false);
    }

    private void BeginDialogue(DialogueNode startNode, bool lockPlayer)
    {
        if (isRunning)
        {
            Debug.LogWarning("DialogueManager: already running, ignoring StartDialogue.");
            return;
        }
        if (startNode == null)
        {
            Debug.LogError("DialogueManager: StartDialogue called with null node.");
            return;
        }

        isRunning = true;
        currentRunLocksPlayer = lockPlayer;
        skipCurrentLine = false;
        if (lockPlayer) SetPlayerControlEnabled(false);
        if (dialogueUI != null) dialogueUI.Show();
        onDialogueStarted?.Invoke();

        activeRoutine = StartCoroutine(PlayNode(startNode));
    }

    public void StopDialogue()
    {
        if (!isRunning) return;
        if (activeRoutine != null) StopCoroutine(activeRoutine);
        EndDialogue();
    }

    private IEnumerator PlayNode(DialogueNode node)
    {
        currentNode = node;

        if (node.sophieOpeningLines != null)
        {
            foreach (DialogueLine line in node.sophieOpeningLines)
            {
                yield return StartCoroutine(PlayLine(line));
            }
        }

        if (!string.IsNullOrEmpty(node.completionEventName))
        {
            FireNamedEvent(node.completionEventName);
        }

        if (node.isEndNode)
        {
            EndDialogue();
            yield break;
        }

        if (node.choices == null || node.choices.Length == 0)
        {
            if (node.autoAdvanceNode != null)
            {
                float wait = IsHoldingFastForward() ? 0f : 0.4f;
                if (wait > 0f) yield return new WaitForSeconds(wait);
                activeRoutine = StartCoroutine(PlayNode(node.autoAdvanceNode));
            }
            else
            {
                EndDialogue();
            }
            yield break;
        }

        float preChoiceWait = IsHoldingFastForward() ? 0f : beatBeforeChoices;
        if (preChoiceWait > 0f) yield return new WaitForSeconds(preChoiceWait);

        DialogueChoice picked = null;
        if (dialogueUI != null)
        {
            yield return StartCoroutine(dialogueUI.ShowChoices(node.choices, c => picked = c));
        }

        if (picked == null)
        {
            EndDialogue();
            yield break;
        }

        float preAdamWait = IsHoldingFastForward() ? 0f : beatBeforeAdamReply;
        if (preAdamWait > 0f) yield return new WaitForSeconds(preAdamWait);

        if (picked.adamReply != null && !string.IsNullOrEmpty(picked.adamReply.text))
        {
            yield return StartCoroutine(PlayLine(picked.adamReply));
        }

        if (picked.sophieResponse != null && !string.IsNullOrEmpty(picked.sophieResponse.text))
        {
            yield return StartCoroutine(PlayLine(picked.sophieResponse));
        }

        if (picked.nextNode != null)
        {
            activeRoutine = StartCoroutine(PlayNode(picked.nextNode));
        }
        else
        {
            EndDialogue();
        }
    }

    private IEnumerator PlayLine(DialogueLine line)
    {
        if (dialogueUI != null) dialogueUI.ShowLine(line.speakerName, line.text);

        skipCurrentLine = false;

        if (line.audioClip != null)
        {
            voiceSource.clip = line.audioClip;
            voiceSource.Play();
            float t = 0f;
            while (t < line.audioClip.length && voiceSource.isPlaying)
            {
                if (skipCurrentLine || IsHoldingFastForward())
                {
                    if (voiceSource.isPlaying) voiceSource.Stop();
                    break;
                }
                t += Time.deltaTime;
                yield return null;
            }
        }
        else
        {
            float duration = line.displayDuration;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (skipCurrentLine || IsHoldingFastForward()) break;
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        skipCurrentLine = false;
    }

    private void EndDialogue()
    {
        isRunning = false;
        currentNode = null;
        skipCurrentLine = false;
        if (dialogueUI != null) dialogueUI.Hide();
        if (currentRunLocksPlayer) SetPlayerControlEnabled(true);
        onDialogueEnded?.Invoke();
    }

    private void SetPlayerControlEnabled(bool enabled)
    {
        if (playerController != null) playerController.enabled = enabled;
        if (playerLookController != null) playerLookController.enabled = enabled;
    }

    private void FireNamedEvent(string eventName)
    {
        if (namedCompletionEvents == null) return;
        foreach (NamedEvent ne in namedCompletionEvents)
        {
            if (ne.eventName == eventName)
            {
                ne.action?.Invoke();
                return;
            }
        }
    }
}