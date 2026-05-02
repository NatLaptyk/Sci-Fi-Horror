using UnityEngine;
using TMPro;

// UI panel for an audio log / journal entry. Owns the visuals only —
// playback timing is driven by AudioLogPlayer.
//
// Setup:
//  1. Create a Canvas (Render Mode: Screen Space - Overlay).
//  2. Inside it, create a Panel with three TextMeshProUGUI children:
//       - headerText        → static label, e.g. "JOURNAL ENTRY"
//       - entryTitleText    → set per log (e.g. "Entry 03 — Reactor Bay")
//       - transcriptText    → the body text revealed letter-by-letter
//     Plus an optional "press [E] to dismiss" hint.
//  3. Attach this script to the Canvas (or the panel root) and wire up
//     the references in the inspector.
//  4. Drag the Canvas onto AudioLogPlayer.audioLogUI.

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
