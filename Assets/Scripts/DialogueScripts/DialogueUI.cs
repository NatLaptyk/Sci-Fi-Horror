using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// UI for dialogue. Shows the current speaker's subtitle and three response buttons.
//
// Setup:
//  1. Create a Canvas in your scene (Render Mode: Screen Space - Overlay).
//  2. Inside the Canvas, create:
//     - A Panel at the bottom containing a TextMeshProUGUI for the speaker name
//       and another TextMeshProUGUI for the subtitle text.
//     - Three Button objects (use TextMeshPro buttons), each with a TextMeshProUGUI label.
//  3. Attach this script to the Canvas.
//  4. Drag all the references into the Inspector slots.
//  5. Assign the Canvas to DialogueManager.dialogueUI.

public class DialogueUI : MonoBehaviour
{
    [Header("Subtitle area")]
    [SerializeField] private GameObject subtitlePanel;
    [SerializeField] private TextMeshProUGUI speakerNameText;
    [SerializeField] private TextMeshProUGUI subtitleText;

    [Header("Choice buttons")]
    [SerializeField] private GameObject choicesPanel;
    [SerializeField] private Button[] choiceButtons;
    [SerializeField] private TextMeshProUGUI[] choiceButtonLabels;

    [Header("Cursor handling")]
    [Tooltip("If true, unlocks the cursor while choices are visible (so player can click).")]
    [SerializeField] private bool unlockCursorForChoices = true;

    private DialogueChoice picked;
    private bool waitingForChoice = false;

    private void Awake()
    {
        Hide();
    }

    public void Show()
    {
        if (subtitlePanel != null) subtitlePanel.SetActive(true);
        if (choicesPanel != null) choicesPanel.SetActive(false);
    }

    public void Hide()
    {
        if (subtitlePanel != null) subtitlePanel.SetActive(false);
        if (choicesPanel != null) choicesPanel.SetActive(false);
        if (speakerNameText != null) speakerNameText.text = "";
        if (subtitleText != null) subtitleText.text = "";
    }

    public void ShowLine(string speaker, string text)
    {
        if (speakerNameText != null) speakerNameText.text = speaker;
        if (subtitleText != null) subtitleText.text = text;
    }

    // Coroutine: shows up to N choices, waits for one to be clicked, calls back with the result.
    public IEnumerator ShowChoices(DialogueChoice[] choices, Action<DialogueChoice> callback)
    {
        if (choices == null || choices.Length == 0 || choiceButtons == null)
        {
            callback?.Invoke(null);
            yield break;
        }

        picked = null;
        waitingForChoice = true;

        // Configure each button.
        for (int i = 0; i < choiceButtons.Length; i++)
        {
            if (i < choices.Length && choices[i] != null)
            {
                int captured = i;
                choiceButtons[i].gameObject.SetActive(true);
                choiceButtons[i].onClick.RemoveAllListeners();
                choiceButtons[i].onClick.AddListener(() => OnChoicePicked(choices[captured]));

                if (i < choiceButtonLabels.Length && choiceButtonLabels[i] != null)
                {
                    choiceButtonLabels[i].text = choices[i].buttonText;
                }
            }
            else
            {
                choiceButtons[i].gameObject.SetActive(false);
            }
        }

        if (choicesPanel != null) choicesPanel.SetActive(true);

        // Unlock cursor so player can click.
        CursorLockMode previousLock = Cursor.lockState;
        bool previousVisibility = Cursor.visible;
        if (unlockCursorForChoices)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // Wait until a choice is picked.
        while (waitingForChoice)
        {
            yield return null;
        }

        // Restore cursor state.
        if (unlockCursorForChoices)
        {
            Cursor.lockState = previousLock;
            Cursor.visible = previousVisibility;
        }

        if (choicesPanel != null) choicesPanel.SetActive(false);
        callback?.Invoke(picked);
    }

    private void OnChoicePicked(DialogueChoice choice)
    {
        picked = choice;
        waitingForChoice = false;
    }
}
