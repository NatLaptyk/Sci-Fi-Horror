using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// Main dialogue controller. Singleton.
//
// Plays a chain of DialogueNodes: Sophie speaks her opening lines, the player
// picks a choice from 3 buttons (or auto-advances if no choices), Adam replies,
// Sophie responds, advance to next node.
//
// Setup:
//  1. Create an empty GameObject named "DialogueManager".
//  2. Attach this script.
//  3. Add an AudioSource component on the same GameObject (it'll be auto-found).
//  4. Set up the DialogueUI prefab in your scene and assign it to the dialogueUI field.
//  5. Call DialogueManager.Instance.StartDialogue(node) from a TriggerZone.

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private DialogueUI dialogueUI;
    [SerializeField] private AudioSource voiceSource;

    [Header("Player control")]
    [Tooltip("Drag in the FirstPersonController GameObject so the dialogue can disable movement.")]
    [SerializeField] private MonoBehaviour playerController;

    [Tooltip("Drag in the camera-look script (StarterAssetsInputs or similar). Optional.")]
    [SerializeField] private MonoBehaviour playerLookController;

    [Header("Pacing")]
    [Tooltip("Delay between Sophie's lines and the choice buttons appearing.")]
    [SerializeField] private float beatBeforeChoices = 0.5f;

    [Tooltip("Delay between the player's choice and Adam's voiced reply.")]
    [SerializeField] private float beatBeforeAdamReply = 0.3f;

    [Header("Events")]
    [Tooltip("Fires when any dialogue starts.")]
    [SerializeField] private UnityEvent onDialogueStarted;

    [Tooltip("Fires when the entire dialogue chain finishes.")]
    [SerializeField] private UnityEvent onDialogueEnded;

    [Tooltip("Fires when a node with a completionEventName matching one of these labels completes.")]
    [SerializeField] private NamedEvent[] namedCompletionEvents;

    private DialogueNode currentNode;
    private bool isRunning = false;
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

        if (voiceSource == null) voiceSource = GetComponent<AudioSource>();
        if (voiceSource == null) voiceSource = gameObject.AddComponent<AudioSource>();
        voiceSource.playOnAwake = false;
        voiceSource.loop = false;
    }

    // Call from a TriggerZone or anywhere else to begin a dialogue.
    public void StartDialogue(DialogueNode startNode)
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
        SetPlayerControlEnabled(false);
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

        // Play Sophie's opening lines in sequence.
        if (node.sophieOpeningLines != null)
        {
            foreach (DialogueLine line in node.sophieOpeningLines)
            {
                yield return StartCoroutine(PlayLine(line));
            }
        }

        // Fire the node's named completion event, if any.
        if (!string.IsNullOrEmpty(node.completionEventName))
        {
            FireNamedEvent(node.completionEventName);
        }

        // End if marked as end node.
        if (node.isEndNode)
        {
            EndDialogue();
            yield break;
        }

        // No choices → auto-advance after the opening lines.
        if (node.choices == null || node.choices.Length == 0)
        {
            if (node.autoAdvanceNode != null)
            {
                yield return new WaitForSeconds(0.4f);
                activeRoutine = StartCoroutine(PlayNode(node.autoAdvanceNode));
            }
            else
            {
                EndDialogue();
            }
            yield break;
        }

        // Show the response buttons and wait for the player to pick.
        yield return new WaitForSeconds(beatBeforeChoices);

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

        // Play Adam's reply, then Sophie's response.
        yield return new WaitForSeconds(beatBeforeAdamReply);
        if (picked.adamReply != null && !string.IsNullOrEmpty(picked.adamReply.text))
        {
            yield return StartCoroutine(PlayLine(picked.adamReply));
        }

        if (picked.sophieResponse != null && !string.IsNullOrEmpty(picked.sophieResponse.text))
        {
            yield return StartCoroutine(PlayLine(picked.sophieResponse));
        }

        // Advance to the next node.
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

        if (line.audioClip != null)
        {
            voiceSource.clip = line.audioClip;
            voiceSource.Play();
            // Wait for clip duration (or interruption).
            float t = 0f;
            while (t < line.audioClip.length && voiceSource.isPlaying)
            {
                t += Time.deltaTime;
                yield return null;
            }
        }
        else
        {
            // Subtitle-only fallback.
            yield return new WaitForSeconds(line.displayDuration);
        }
    }

    private void EndDialogue()
    {
        isRunning = false;
        currentNode = null;
        if (dialogueUI != null) dialogueUI.Hide();
        SetPlayerControlEnabled(true);
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
