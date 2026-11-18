using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Persistent top-corner Game Switcher HUD.
/// Floats gently in the upper peripheral view of the player (Quest 3 and PC Standalone).
/// Allows instantaneous switching/relocation between:
///   1. [🧩 Cocasnap] - 3D Puzzle Game
///   2. [🏓 Beer Pong] - Physics Arcade Game
///   3. [🏠 Hub Menu] - Return to Welcome Lobby & Game Selection Cards
/// Also supports hotkeys: '1' / 'C' (Cocasnap), '2' / 'V' (Beer Pong), 'M' / 'H' (Hub Menu).
/// </summary>
public class QuickGameSwitcherHUD : MonoBehaviour
{
    private static QuickGameSwitcherHUD _instance;
    public static QuickGameSwitcherHUD Instance => _instance;

    [Header("Positioning & Lazy Follow")]
    [Tooltip("Offset relative to the main camera (upper-right corner of vision).")]
    public Vector3 localOffsetFromCamera = new Vector3(0.32f, 0.22f, 0.90f);
    public float followSpeed = 6.0f;
    public bool lockToCameraRotation = true;

    [Header("Visual Aesthetic (Glowy White Theme)")]
    public Color normalBgColor = new Color(0.02f, 0.05f, 0.10f, 0.85f);
    public Color buttonBgColor = new Color(1.0f, 1.0f, 1.0f, 0.16f);
    public Color buttonBorderColor = new Color(1.0f, 1.0f, 1.0f, 0.95f);
    public Color activeAccentColor = new Color(0.70f, 0.90f, 1.0f, 1.0f);

    private Canvas _canvas;
    private RectTransform _rootContainer;
    private Button _btnCocasnap;
    private Button _btnBeerPong;
    private Button _btnLobby;

    private TextMeshProUGUI _txtCocasnap;
    private TextMeshProUGUI _txtBeerPong;

    private Transform _cameraTr;
    private IntroSequenceManager _introManager;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    private void Start()
    {
        if (Camera.main != null)
        {
            _cameraTr = Camera.main.transform;
        }
        _introManager = Object.FindFirstObjectByType<IntroSequenceManager>();

        BuildHUDLayout();
    }

    private void Update()
    {
        if (_cameraTr == null && Camera.main != null)
        {
            _cameraTr = Camera.main.transform;
        }

        if (_cameraTr != null)
        {
            // Smooth lazy follow in front-upper-right of camera
            Vector3 targetPos = _cameraTr.position +
                                _cameraTr.right * localOffsetFromCamera.x +
                                _cameraTr.up * localOffsetFromCamera.y +
                                _cameraTr.forward * localOffsetFromCamera.z;

            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * followSpeed);

            if (lockToCameraRotation)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, _cameraTr.rotation, Time.deltaTime * followSpeed);
            }
        }

        // Hotkey shortcuts
        HandleHotkeys();
    }

    private void HandleHotkeys()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.C))
        {
            OnCocasnapClicked();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.V))
        {
            OnBeerPongClicked();
        }
        else if (Input.GetKeyDown(KeyCode.M) || Input.GetKeyDown(KeyCode.H))
        {
            OnLobbyClicked();
        }
    }

    private void BuildHUDLayout()
    {
        _canvas = GetComponent<Canvas>();
        if (_canvas == null) _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.WorldSpace;
        _canvas.worldCamera = Camera.main;

        var scaler = GetComponent<CanvasScaler>();
        if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 4f;

        var raycaster = GetComponent<GraphicRaycaster>();
        if (raycaster == null) raycaster = gameObject.AddComponent<GraphicRaycaster>();
        raycaster.blockingObjects = GraphicRaycaster.BlockingObjects.None;

        RectTransform canvasRt = GetComponent<RectTransform>();
        canvasRt.sizeDelta = new Vector2(460f, 54f);
        transform.localScale = Vector3.one * 0.0012f; // Comfortable compact pill in upper peripheral

        // Background Bar
        GameObject bar = new GameObject("SwitcherBar");
        bar.transform.SetParent(transform, false);
        _rootContainer = bar.AddComponent<RectTransform>();
        _rootContainer.anchorMin = Vector2.zero;
        _rootContainer.anchorMax = Vector2.one;
        _rootContainer.sizeDelta = Vector2.zero;

        Image barBg = bar.AddComponent<Image>();
        barBg.color = normalBgColor;

        Outline barOutline = bar.AddComponent<Outline>();
        barOutline.effectColor = buttonBorderColor;
        barOutline.effectDistance = new Vector2(2f, -2f);

        HorizontalLayoutGroup hlg = bar.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(6, 6, 4, 4);
        hlg.spacing = 8f;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;

        // Button 1: Cocasnap
        _btnCocasnap = CreateSwitcherButton(bar.transform, "Btn_Cocasnap", "🧩 Cocasnap", OnCocasnapClicked, out _txtCocasnap);

        // Button 2: Beer Pong
        _btnBeerPong = CreateSwitcherButton(bar.transform, "Btn_BeerPong", "🏓 Beer Pong", OnBeerPongClicked, out _txtBeerPong);

        // Button 3: Hub Menu
        TextMeshProUGUI dummy;
        _btnLobby = CreateSwitcherButton(bar.transform, "Btn_Hub", "🏠 Hub", OnLobbyClicked, out dummy);
    }

    private Button CreateSwitcherButton(Transform parent, string name, string label, UnityEngine.Events.UnityAction onClickAction, out TextMeshProUGUI textComp)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);

        Image img = btnObj.AddComponent<Image>();
        img.color = buttonBgColor;

        Outline outline = btnObj.AddComponent<Outline>();
        outline.effectColor = buttonBorderColor;
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.0f, 1.0f, 1.0f, 0.50f);
        cb.pressedColor = new Color(0.85f, 0.95f, 1.0f, 0.85f);
        btn.colors = cb;
        btn.onClick.AddListener(onClickAction);

        // BoxCollider so direct VR rays and physics hits can also click it
        BoxCollider col = btnObj.AddComponent<BoxCollider>();
        col.size = new Vector3(140f, 44f, 5f);

        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(btnObj.transform, false);
        RectTransform txtRt = txtObj.AddComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.sizeDelta = Vector2.zero;

        textComp = txtObj.AddComponent<TextMeshProUGUI>();
        textComp.text = $"<b>{label}</b>";
        textComp.fontSize = 15;
        textComp.alignment = TextAlignmentOptions.Center;
        textComp.color = Color.white;
        textComp.raycastTarget = false;

        return btn;
    }

    public void OnCocasnapClicked()
    {
        Debug.Log("[QuickGameSwitcher] Switching to Cocasnap Game!");
        if (_introManager == null) _introManager = Object.FindFirstObjectByType<IntroSequenceManager>();
        if (_introManager != null)
        {
            _introManager.LaunchCocasnap();
        }
        HighlightActive(1);
    }

    public void OnBeerPongClicked()
    {
        Debug.Log("[QuickGameSwitcher] Switching to Beer Pong Game!");
        if (_introManager == null) _introManager = Object.FindFirstObjectByType<IntroSequenceManager>();
        if (_introManager != null)
        {
            _introManager.LaunchBeerPong();
        }
        HighlightActive(2);
    }

    public void OnLobbyClicked()
    {
        Debug.Log("[QuickGameSwitcher] Switching to Hub Lobby & Menu!");
        if (_introManager == null) _introManager = Object.FindFirstObjectByType<IntroSequenceManager>();
        if (_introManager != null)
        {
            _introManager.RelocateToLobbyAndShowMenu();
        }
        HighlightActive(0);
    }

    public void HighlightActive(int index)
    {
        // Highlight active button indicator if desired
        if (_txtCocasnap != null) _txtCocasnap.color = (index == 1) ? activeAccentColor : Color.white;
        if (_txtBeerPong != null) _txtBeerPong.color = (index == 2) ? activeAccentColor : Color.white;
    }
}
