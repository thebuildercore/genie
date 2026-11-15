
using UnityEngine;
using UnityEngine.UI;

public class ShortcutKey : MonoBehaviour
{
    [Header("Shortcut Settings")]
    [SerializeField] private KeyCode shortcutKey = KeyCode.None;

    [Header("Button Reference")]
    [SerializeField] private Button targetButton;

    private void Update()
    {
        if (shortcutKey != KeyCode.None &&
            Input.GetKeyDown(shortcutKey))
        {
            if (targetButton != null && targetButton.interactable)
            {
                targetButton.onClick.Invoke();
            }
        }
    }
}
