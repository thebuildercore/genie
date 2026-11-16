using UnityEngine;
using UnityEngine.Events;
using Oculus.Interaction;

public class PuzzleLogic : MonoBehaviour
{
    [Header("Snap Zone Interactors (the Physical Cubes)")]
    public SnapInteractor[] slots; // Assign: PhysicalCube_01, 02, 03

    [Header("Expected Piece IDs per Slot (in order)")]
    public int[] correctIDs = { 1, 2, 3 }; // Slot 0 expects pieceID 1, etc.

    [Header("References")]
    public GameObject winEffect;
    public GameObject ghostFrame;

    [Header("On Level Complete - Toggle Objects")]
    public GameObject[] objectsToEnable;
    public GameObject[] objectsToDisable;

    [Header("Events")]
    public UnityEvent onLevelComplete;
    public UnityEvent onWrongAnswer;

    private bool _levelSolved = false;

    // -------------------------------------------------------
    // Call this from your UI Submit Button (OnClick event)
    // -------------------------------------------------------
    public void OnSubmitButtonPressed()
    {
        if (_levelSolved) return;

        int correctCount = 0;

        for (int i = 0; i < slots.Length; i++)
        {
            SnapInteractor slot = slots[i];

            // Check snap state - try both HasSelectedInteractable and SelectedInteractable
            bool isSnapped = slot.HasSelectedInteractable && slot.SelectedInteractable != null;

            Debug.Log($"[PuzzleLogic] Slot {i} — isSnapped={isSnapped}, " +
                      $"HasSelectedInteractable={slot.HasSelectedInteractable}");

            if (!isSnapped)
            {
                Debug.Log($"[PuzzleLogic] Slot {i} is empty or not snapped.");
                continue;
            }

            // PuzzlePiece lives on the physical cube (the SnapInteractor's GameObject)
            var piece = slot.gameObject.GetComponent<PuzzlePiece>();

            if (piece == null)
            {
                Debug.Log($"[PuzzleLogic] Slot {i}: PuzzlePiece component not found on cube!");
                continue;
            }

            Debug.Log($"[PuzzleLogic] Slot {i}: PieceID={piece.pieceID}, Expected={correctIDs[i]}");

            if (piece.pieceID == correctIDs[i])
            {
                correctCount++;
            }
        }

        Debug.Log($"[PuzzleLogic] Submit check: {correctCount}/{slots.Length} correct.");

        if (correctCount == slots.Length)
        {
            OnLevelComplete();
        }
        else
        {
            OnWrongAnswer();
        }
    }

    private void OnLevelComplete()
    {
        if (_levelSolved) return;
        _levelSolved = true;

        Debug.Log("Puzzle Solved!");

        if (winEffect != null)
            winEffect.SetActive(true);

        if (ghostFrame != null)
            ghostFrame.SetActive(false);

        foreach (var obj in objectsToEnable)
            if (obj != null) obj.SetActive(true);

        foreach (var obj in objectsToDisable)
            if (obj != null) obj.SetActive(false);

        foreach (var slot in slots)
            if (slot != null) slot.enabled = false;

        onLevelComplete?.Invoke();
    }

    private void OnWrongAnswer()
    {
        Debug.Log("Wrong arrangement — try again!");
        onWrongAnswer?.Invoke();
    }

    public void ResetPuzzle()
    {
        _levelSolved = false;

        if (winEffect != null)
            winEffect.SetActive(false);

        if (ghostFrame != null)
            ghostFrame.SetActive(true);

        foreach (var obj in objectsToEnable)
            if (obj != null) obj.SetActive(false);

        foreach (var obj in objectsToDisable)
            if (obj != null) obj.SetActive(true);

        foreach (var slot in slots)
            if (slot != null) slot.enabled = true;
    }
}