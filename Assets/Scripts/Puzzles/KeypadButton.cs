using System.Collections;
using UnityEngine;

// One physical button in a PuzzleKeypad. Receives press events from its
// Interactable.onInteract → KeypadButton.Press, and reports up to the
// parent PuzzleKeypad. Also handles its own visual feedback (flash on
// correct/wrong, persistent lit state for in-progress correct presses).

public class KeypadButton : MonoBehaviour
{
    [Header("Wiring")]
    [Tooltip("Which puzzle this button belongs to.")]
    [SerializeField] private PuzzleKeypad keypad;

    [Header("Visual feedback (optional)")]
    [Tooltip("Renderer used for color flashing. Auto-grabs from this GameObject if empty.")]
    [SerializeField] private MeshRenderer buttonRenderer;
    [SerializeField] private Color idleColor = new Color(0.5f, 0.5f, 0.5f);
    [SerializeField] private Color correctColor = new Color(0.3f, 1f, 0.5f);
    [SerializeField] private Color wrongColor = new Color(1f, 0.3f, 0.3f);
    [Tooltip("How long the press flash lasts (seconds). Correct presses stay lit until reset.")]
    [SerializeField] private float wrongFlashDuration = 0.4f;
    [Tooltip("If true, this button stays lit green after a correct press until the sequence resets.")]
    [SerializeField] private bool persistOnCorrect = true;

    private Color baseColor;
    private Material runtimeMaterial;
    private Coroutine activeFlash;

    private void Awake()
    {
        if (buttonRenderer == null) buttonRenderer = GetComponent<MeshRenderer>();
        if (buttonRenderer != null)
        {
            runtimeMaterial = buttonRenderer.material; // instance, not shared
            baseColor = runtimeMaterial.color;
            // Apply idleColor on Awake to ensure a consistent starting visual.
            runtimeMaterial.color = idleColor;
            baseColor = idleColor;
        }
    }

    // Hook this to Interactable.onInteract in the Inspector.
    public void Press()
    {
        if (keypad != null) keypad.HandleButtonPress(this);
    }

    public void ShowCorrect()
    {
        if (runtimeMaterial == null) return;
        if (activeFlash != null) StopCoroutine(activeFlash);
        if (persistOnCorrect)
        {
            runtimeMaterial.color = correctColor;
        }
        else
        {
            activeFlash = StartCoroutine(FlashRoutine(correctColor, wrongFlashDuration));
        }
    }

    public void ShowWrong()
    {
        if (runtimeMaterial == null) return;
        if (activeFlash != null) StopCoroutine(activeFlash);
        activeFlash = StartCoroutine(FlashRoutine(wrongColor, wrongFlashDuration));
    }

    public void ResetVisual()
    {
        if (runtimeMaterial == null) return;
        if (activeFlash != null) StopCoroutine(activeFlash);
        runtimeMaterial.color = idleColor;
    }

    private IEnumerator FlashRoutine(Color color, float duration)
    {
        runtimeMaterial.color = color;
        yield return new WaitForSeconds(duration);
        runtimeMaterial.color = idleColor;
        activeFlash = null;
    }
}
