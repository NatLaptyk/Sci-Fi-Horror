using UnityEngine;

[CreateAssetMenu(fileName = "AudioLog", menuName = "Solaris/Audio Log")]
public class AudioLog : ScriptableObject
{
    [Tooltip("Specific entry name — shown beneath the 'Journal Entry' header.")]
    public string entryTitle;

    [Tooltip("The recorded voice clip. Text reveal is synced to this clip's length.")]
    public AudioClip audioClip;

    [TextArea(4, 20)]
    [Tooltip("Full transcript. Letters are revealed proportional to audio progress.")]
    public string transcript;
}
