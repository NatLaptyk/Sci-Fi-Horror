using UnityEngine;

// One line of dialogue. Has a speaker name, the text shown as subtitle,
// and an optional audio clip. Used inside DialogueNode.
//
// This is a [System.Serializable] struct.

[System.Serializable]
public class DialogueLine
{
    [Tooltip("Speaker name shown in the subtitle (e.g. 'Sophie', 'Adam').")]
    public string speakerName;

    [TextArea(2, 5)]
    [Tooltip("The line text shown as a subtitle.")]
    public string text;

    [Tooltip("Optional audio clip. If null, the line just shows as a subtitle for displayDuration seconds.")]
    public AudioClip audioClip;

    [Tooltip("How long the subtitle stays on screen if there's no audio clip. Ignored if audioClip is set.")]
    public float displayDuration = 3f;
}
