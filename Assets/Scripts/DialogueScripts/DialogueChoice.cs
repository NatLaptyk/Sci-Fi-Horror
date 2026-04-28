using UnityEngine;

// One of the three response options the player can pick at a dialogue turn.
// Has the button label, Adam's reply line (when picked), Sophie's reply variant,
// and which DialogueNode to transition to next.

[System.Serializable]
public class DialogueChoice
{
    [Tooltip("Short label shown on the response button (e.g. 'This isn't possible.').")]
    [TextArea(1, 3)]
    public string buttonText;

    [Tooltip("Adam's spoken line when this choice is selected. Plays before Sophie's response.")]
    public DialogueLine adamReply;

    [Tooltip("Sophie's response to this choice. Plays after Adam's line.")]
    public DialogueLine sophieResponse;

    [Tooltip("Which dialogue node to play next. Leave null to end the dialogue.")]
    public DialogueNode nextNode;
}
