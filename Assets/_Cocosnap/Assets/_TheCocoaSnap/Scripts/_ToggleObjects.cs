using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

public class _ToggleObjects : MonoBehaviour
{
    [Header("Target Objects")]
    public GameObject[] targetObjects;

    [Header("Toggle Behavior")]
    public ToggleMode toggleMode = ToggleMode.ToggleAll;

    [Header("Input")]
    public InputType inputType = InputType.ActionReference;
    public InputActionReference toggleActionReference;
    public InputActionProperty toggleActionProperty;

    [Header("UI Feedback")]
    public TextMeshProUGUI feedbackText;

    [Header("Messages")]
    public string onMessage = "Objects Toggled ON";
    public string offMessage = "Objects Toggled OFF";

    [Header("Display Settings")]
    [Range(0.5f, 10f)]
    public float displayDuration = 3.0f;

    [Header("Text Appearance")]
    public Color onMessageColor = Color.green;
    public Color offMessageColor = Color.red;
    [Range(8f, 72f)]
    public float fontSize = 24f;

    [Header("Fade Settings")]
    public bool enableFade = true;
    [Range(0.1f, 2f)]
    public float fadeDuration = 0.5f;

    [Header("Initial State")]
    public bool objectsActiveOnStart = true;

    private Coroutine textCoroutine;
    private int currentIndex = 0;

    private InputAction GetInputAction()
    {
        return inputType == InputType.ActionReference
            ? toggleActionReference?.action
            : toggleActionProperty.action;
    }

    public enum InputType
    {
        ActionReference,
        ActionProperty
    }

    public enum ToggleMode
    {
        ToggleAll,          // Toggle all objects together
        AlwaysOn,           // Always turn ON all objects
        AlwaysOff,          // Always turn OFF all objects
        CycleThrough,       // Cycle through objects one at a time
        Random              // Toggle a random object
    }

    private void Start()
    {
        // Apply initial state to all target objects
        foreach (GameObject obj in targetObjects)
        {
            if (obj != null) obj.SetActive(objectsActiveOnStart);
        }

        if (feedbackText != null)
        {
            feedbackText.text = "";
            feedbackText.fontSize = fontSize;
        }
    }

    private void OnEnable()
    {
        InputAction action = GetInputAction();
        if (action != null)
        {
            action.Enable();
            action.performed += OnTogglePress;
        }

        if (feedbackText != null) feedbackText.text = "";
    }

    private void OnDisable()
    {
        InputAction action = GetInputAction();
        if (action != null)
            action.performed -= OnTogglePress;
    }

    private void OnTogglePress(InputAction.CallbackContext context)
    {
        if (targetObjects.Length == 0) return;

        switch (toggleMode)
        {
            case ToggleMode.ToggleAll:
                ToggleAllObjects();
                break;

            case ToggleMode.AlwaysOn:
                SetAllObjectsState(true);
                break;

            case ToggleMode.AlwaysOff:
                SetAllObjectsState(false);
                break;

            case ToggleMode.CycleThrough:
                CycleThroughObjects();
                break;

            case ToggleMode.Random:
                ToggleRandomObject();
                break;
        }
    }

    private void ToggleAllObjects()
    {
        bool newState = !targetObjects[0].activeSelf;

        foreach (GameObject obj in targetObjects)
        {
            if (obj != null) obj.SetActive(newState);
        }

        string message = newState ? onMessage : offMessage;
        Color messageColor = newState ? onMessageColor : offMessageColor;
        ShowFeedback(message, messageColor);
    }

    private void SetAllObjectsState(bool state)
    {
        foreach (GameObject obj in targetObjects)
        {
            if (obj != null) obj.SetActive(state);
        }

        string message = state ? onMessage : offMessage;
        Color messageColor = state ? onMessageColor : offMessageColor;
        ShowFeedback(message, messageColor);
    }

    private void CycleThroughObjects()
    {
        // Turn off all objects
        foreach (GameObject obj in targetObjects)
        {
            if (obj != null) obj.SetActive(false);
        }

        // Turn on the current object
        if (targetObjects[currentIndex] != null)
        {
            targetObjects[currentIndex].SetActive(true);
            ShowFeedback($"{targetObjects[currentIndex].name} ON", onMessageColor);
        }

        // Move to next index
        currentIndex = (currentIndex + 1) % targetObjects.Length;
    }

    private void ToggleRandomObject()
    {
        int randomIndex = Random.Range(0, targetObjects.Length);

        if (targetObjects[randomIndex] != null)
        {
            bool newState = !targetObjects[randomIndex].activeSelf;
            targetObjects[randomIndex].SetActive(newState);

            string message = newState ?
                $"{targetObjects[randomIndex].name} ON" :
                $"{targetObjects[randomIndex].name} OFF";
            Color messageColor = newState ? onMessageColor : offMessageColor;
            ShowFeedback(message, messageColor);
        }
    }

    private void ShowFeedback(string message, Color color)
    {
        if (feedbackText == null) return;

        if (textCoroutine != null) StopCoroutine(textCoroutine);
        textCoroutine = StartCoroutine(DisplayRoutine(message, color));
    }

    private IEnumerator DisplayRoutine(string message, Color color)
    {
        // Apply properties
        feedbackText.text = message;
        feedbackText.fontSize = fontSize;
        feedbackText.color = new Color(color.r, color.g, color.b, 1f);

        if (enableFade)
        {
            // Fade in
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Clamp01(elapsed / fadeDuration);
                feedbackText.color = new Color(color.r, color.g, color.b, alpha);
                yield return null;
            }
        }

        // Hold for display duration minus fade times
        float holdTime = displayDuration - (enableFade ? fadeDuration * 2f : 0f);
        yield return new WaitForSeconds(Mathf.Max(0f, holdTime));

        if (enableFade)
        {
            // Fade out
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Clamp01(1f - (elapsed / fadeDuration));
                feedbackText.color = new Color(color.r, color.g, color.b, alpha);
                yield return null;
            }
        }

        feedbackText.text = "";
    }
}