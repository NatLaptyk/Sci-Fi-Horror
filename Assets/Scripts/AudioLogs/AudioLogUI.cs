using UnityEngine;
using TMPro;

// UI panel for an audio log / journal entry. Owns the visuals only —
// playback timing is driven by AudioLogPlayer.

public class AudioLogUI : MonoBehaviour
{
    [Header("Panel root")]
    [SerializeField] private GameObject panel;

    [Header("Text fields")]
    [SerializeField] private TextMeshProUGUI headerText;
    [SerializeField] private TextMeshProUGUI entryTitleText;
    [SerializeField] private TextMeshProUGUI transcriptText;

    [Header("Optional dismiss hint")]
    [Tooltip("E.g. a small 'Press [E] to dismiss' label. Toggled with the panel.")]
    [SerializeField] private GameObject dismissHint;

    [Header("Header label")]
    [Tooltip("The fixed header shown above every entry's title.")]
    [SerializeField] private string headerLabel = "JOURNAL ENTRY";

    private void Awake()
    {
        Hide();
        if (headerText != null) headerText.text = headerLabel;
    }

    public void Show(string entryTitle, string transcript)
    {
        if (panel != null) panel.SetActive(true);
        if (dismissHint != null) dismissHint.SetActive(true);
        if (headerText != null) headerText.text = headerLabel;
        if (entryTitleText != null) entryTitleText.text = entryTitle;
        if (transcriptText != null)
        {
            transcriptText.text = transcript ?? "";
            transcriptText.maxVisibleCharacters = 0;
            transcriptText.ForceMeshUpdate();
        }
    }

    public void Hide()
    {
        if (panel != null) panel.SetActive(false);
        if (dismissHint != null) dismissHint.SetActive(false);
    }

    // Reveals text proportional to progress (0..1). Called every frame
    // by AudioLogPlayer while the clip is playing.
    public void SetRevealProgress(float progress01)
    {
        if (transcriptText == null) return;

        int total = transcriptText.textInfo != null
            ? transcriptText.textInfo.characterCount
            : transcriptText.text.Length;

        float p = Mathf.Clamp01(progress01);
        transcriptText.maxVisibleCharacters = Mathf.RoundToInt(total * p);
    }

    public void RevealAll()
    {
        if (transcriptText == null) return;
        transcriptText.maxVisibleCharacters = int.MaxValue;
    }
}
