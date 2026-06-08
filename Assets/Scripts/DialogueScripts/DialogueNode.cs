using UnityEngine;

// One full turn of dialogue.
//
// Structure:
//  1. Sophie speaks her opening line (one or more lines played in sequence).
//  2. The three response buttons appear.
//  3. Player picks one → Adam's reply plays, then Sophie's response.
//  4. Transitions to the next DialogueNode.


[CreateAssetMenu(fileName = "NewDialogueNode", menuName = "Solaris/Dialogue Node")]
public class DialogueNode : ScriptableObject
{
    [Header("Sophie's opening lines")]
    [Tooltip("Sophie's lines played in sequence at the start of this turn. Add multiple for monologues.")]
    public DialogueLine[] sophieOpeningLines;

    [Header("Player choices")]
    [Tooltip("The three response options shown to the player. Leave empty for a forced/automatic line (no player input).")]
    public DialogueChoice[] choices;

    [Header("Auto-advance (no choice)")]
    [Tooltip("If choices is empty, automatically advance to this node after the opening lines finish.")]
    public DialogueNode autoAdvanceNode;

    [Header("End of dialogue")]
    [Tooltip("If checked, this node ends the dialogue (regardless of choices/autoAdvanceNode).")]
    public bool isEndNode = false;

    [Tooltip("Optional event name fired when this node ends. Read by DialogueManager events.")]
    public string completionEventName = "";
}
