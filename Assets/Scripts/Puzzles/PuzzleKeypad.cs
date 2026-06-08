using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// Sequence-input puzzle: player presses 3D buttons in the correct order to unlock something.
//
// Each button is a separate GameObject with a KeypadButton script + an Interactable
// component. The button's Interactable.onInteract fires KeypadButton.Press(), which
// calls back into this controller via HandleButtonPress(this).

public class PuzzleKeypad : MonoBehaviour
{
    [Header("Solution")]
    [Tooltip("Buttons in the order the player must press them to solve. Drag them in here.")]
    [SerializeField] private List<KeypadButton> correctSequence = new List<KeypadButton>();

    [Header("Audio (optional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip correctTone;
    [SerializeField] private AudioClip wrongTone;
    [SerializeField] private AudioClip solvedTone;

    [Header("Events")]
    [Tooltip("Fires every time a correct button is pressed (use for progress beeps, lights).")]
    [SerializeField] private UnityEvent onCorrectInput;
    [Tooltip("Fires when a wrong button is pressed (wire PlayerStats.DrainSanity with a value here).")]
    [SerializeField] private UnityEvent onWrongInput;
    [Tooltip("Fires when the full sequence is solved. Wire SlidingDoor.Unlock + Open here.")]
    [SerializeField] private UnityEvent onSolved;

    private int progress = 0;
    private bool solved = false;

    public bool IsSolved => solved;
    public int Progress => progress;

    public void HandleButtonPress(KeypadButton button)
    {
        if (solved) return;
        if (correctSequence == null || correctSequence.Count == 0) return;
        if (button == null) return;

        KeypadButton expected = correctSequence[progress];
        if (button == expected)
        {
            // Correct input
            progress++;
            button.ShowCorrect();
            PlayClip(correctTone);
            onCorrectInput?.Invoke();
            Debug.Log($"PuzzleKeypad: correct press {progress}/{correctSequence.Count} ({button.name})");

            if (progress >= correctSequence.Count)
            {
                solved = true;
                PlayClip(solvedTone);
                onSolved?.Invoke();
                Debug.Log("PuzzleKeypad: SOLVED");
            }
        }
        else
        {
            // Wrong input — reset progress and flash the offending button
            Debug.Log($"PuzzleKeypad: wrong press ({button.name}) — expected {expected.name}. Resetting.");
            progress = 0;
            button.ShowWrong();
            // Also reset the visual state of any buttons that had been marked correct.
            foreach (KeypadButton b in correctSequence)
            {
                if (b != null) b.ResetVisual();
            }
            PlayClip(wrongTone);
            onWrongInput?.Invoke();
        }
    }

    private void PlayClip(AudioClip clip)
    {
        if (audioSource != null && clip != null) audioSource.PlayOneShot(clip);
    }
}
