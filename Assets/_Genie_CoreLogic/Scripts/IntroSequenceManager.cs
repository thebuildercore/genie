using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Controls the initial Mixed Reality Intro Sequence, Subtitles, and Game Selection HUD.
/// Built to be completely independent of Scene API and 3D Physics.
/// Supports Meta Quest 3 Color Passthrough and PC Tester mode.
/// </summary>
public class IntroSequenceManager : MonoBehaviour
{
    public enum PanelTransparencyMode
    {
        CompletelyTransparent,  // 100% invisible panel background (only contents/cards visible)
        SubtleGlass,            // Sleek translucent dark glass for VR legibility
        CustomColor             // Custom RGBA defined in Inspector
    }

    [Header("Core Links")]
    public CompanionBrain companion;
    public GameEventManager gameManager;

    private static IntroSequenceManager _instance;
    public static IntroSequenceManager Instance => _instance;

    [Header("Lobby & Arena Layout")]
    [Tooltip("Lobby standing position for player during Welcome & Game Selection.")]
    public Vector3 lobbyPlayerPosition = new Vector3(0f, 0f, -2.6f);

    [Tooltip("World position for the floating panels during Welcome & Game Selection (comfortable distance away from tables).")]
    public Vector3 lobbyPanelPosition = new Vector3(0f, 1.25f, -1.25f);

    [Tooltip("Playing station position in front of Cocasnap puzzle table.")]
    public Vector3 cocasnapPlayingPosition = new Vector3(-1.4f, 0f, 0.4f);

    [Tooltip("Playing station position at Beer Pong throwing line.")]
    public Vector3 beerPongPlayingPosition = new Vector3(1.6f, 0f, 0.2f);

    [Header("UI Panels (World Space)")]
    public GameObject enterArenaPanel;
    public GameObject gameSelectionPanel;

    [Header("UI Controls")]
    public Button enterArenaButton;
    public TextMeshProUGUI enterArenaButtonText;

    [Header("Subtitles")]
    public GameObject subtitlePanel;
    public TextMeshProUGUI subtitleText;
    [TextArea(2, 4)]
    public string continueSubtitleText = "<color=#72EFDD><b>Genie:</b></color> \"Welcome to Kinexus! Step into the arena and discover your next game!\"";
    public float subtitleDisplayDuration = 3.5f;
    [Tooltip("Adjust if subtitles appear backwards/mirrored due to World Space Canvas orientation (default 180).")]
    public Vector3 subtitleRotation = new Vector3(0f, 180f, 0f);

    [Header("Mixed Reality & Passthrough")]
    [Tooltip("Ensures camera background is transparent so real-world Passthrough is visible on Quest 3.")]
    public bool enableMRPassthrough = true;

    [Header("Panel Visual & Transparency Settings")]
    [Tooltip("Default is CompletelyTransparent so no blue background box shows on screen.")]
    public PanelTransparencyMode transparencyMode = PanelTransparencyMode.CompletelyTransparent;

    [Tooltip("Used if transparencyMode is set to CustomColor.")]
    public Color customTransparentColor = new Color(0f, 0f, 0f, 0f);

    [Tooltip("Border / Accent tint for Kinexus glow aesthetic (Quest 3 Glowy White).")]
    public Color neonAccentColor = new Color(1.0f, 1.0f, 1.0f, 0.95f);

    [Header("Game Launch Integration")]
    [Tooltip("Scene name for Cocasnap puzzle game.")]
    public string cocasnapSceneName = "_TheCocoaSnap";
    [Tooltip("Scene name for Beer Pong game.")]
    public string beerPongSceneName = "_BeerPong";
    [Tooltip("Optional in-scene root for Cocasnap game objects.")]
    public GameObject cocasnapInSceneRoot;
    [Tooltip("Optional in-scene root for Beer Pong game objects.")]
    public GameObject beerPongInSceneRoot;

    public UnityEngine.Events.UnityEvent onEnterCocasnap;
    public UnityEngine.Events.UnityEvent onEnterBeerPong;

    [Header("Auto UI Builder")]
    [Tooltip("If true, automatically builds both Welcome screen and Discover Games screen if missing.")]
    public bool autoBuildFullLayoutIfMissing = true;

    private void Awake()
    {
        _instance = this;

        EnsureSingleEventSystem();
        ConfigureCameraForPassthrough();
        EnsureQuickGameSwitcherHUDExists();
        EnsureBeerPongStationExists();

        // Position panels in the spacious Welcome Lobby (away from tables)
        PositionPanelsInLobby();

        if (enterArenaPanel != null)
        {
            ApplyTransparency(enterArenaPanel);
            OptimizeCanvasForVRAndPC(enterArenaPanel);

            if (autoBuildFullLayoutIfMissing)
            {
                EnsureKinexusLayoutExists();
            }
        }

        // Auto-create gameSelectionPanel if not assigned
        if (autoBuildFullLayoutIfMissing && gameSelectionPanel == null)
        {
            EnsureDiscoverGamesLayoutExists();
        }

        if (gameSelectionPanel != null)
        {
            ApplyTransparency(gameSelectionPanel);
            OptimizeCanvasForVRAndPC(gameSelectionPanel);
        }

        EnsureSubtitleUIExists();
    }

    private void Start()
    {
        EnsureSingleEventSystem();

        // Spawn/Place player standing in the Lobby area facing the panels
        RelocatePlayer(lobbyPlayerPosition);

        if (gameManager == null)
        {
            gameManager = Object.FindFirstObjectByType<GameEventManager>();
        }

        if (companion == null)
        {
            companion = Object.FindFirstObjectByType<CompanionBrain>();
        }

        // Place companion near the welcome panels in Lobby
        AccompanyCompanion(lobbyPlayerPosition + new Vector3(0.75f, 0f, 0.9f), lobbyPlayerPosition);

        if (gameManager != null)
        {
            gameManager.OnEnterArenaRequested -= OnEnterArenaClicked;
            gameManager.OnEnterArenaRequested += OnEnterArenaClicked;
        }

        WireButton();

        // Hide UI on boot
        if (enterArenaPanel != null) enterArenaPanel.SetActive(false);
        if (gameSelectionPanel != null) gameSelectionPanel.SetActive(false);
        if (subtitlePanel != null) subtitlePanel.SetActive(false);

        StartCoroutine(PlayIntroSequence());
    }

    private void Update()
    {
        // Periodic check to guarantee exactly ONE EventSystem in scene
        EnsureSingleEventSystem();

        // PC Mouse click handling for ALL panels, buttons, cards, and close icons
        if (Input.GetMouseButtonDown(0))
        {
            HandlePCMouseInput();
        }

        // Keyboard hotkey navigation for PC testing & rapid switching
        HandleKeyboardNavigation();
    }

    private void HandleKeyboardNavigation()
    {
        // Continue / Advance (E, Enter, Space)
        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            if (enterArenaPanel != null && enterArenaPanel.activeSelf)
            {
                TriggerEnterArena();
            }
        }

        // Close active panels (Escape, X)
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.X))
        {
            CloseAllPanels();
        }

        // Quick Game Launch / Switch hotkeys
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.C))
        {
            LaunchCocasnap();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.V))
        {
            LaunchBeerPong();
        }
        else if (Input.GetKeyDown(KeyCode.M) || Input.GetKeyDown(KeyCode.H))
        {
            RelocateToLobbyAndShowMenu();
        }
    }

    private void HandlePCMouseInput()
    {
        if (UnityEngine.EventSystems.EventSystem.current == null) return;

        // 1. Raycast all UGUI Graphic Raycasters across active panels
        var pointerData = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
        {
            position = Input.mousePosition
        };

        var results = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
        UnityEngine.EventSystems.EventSystem.current.RaycastAll(pointerData, results);

        foreach (var r in results)
        {
            if (r.gameObject == null) continue;

            Button btn = r.gameObject.GetComponentInParent<Button>();
            if (btn != null && btn.interactable && btn.gameObject.activeInHierarchy)
            {
                Debug.Log($"[IntroSequenceManager] PC Mouse clicked UGUI Button: {btn.gameObject.name}");
                btn.onClick.Invoke();
                return;
            }
        }

        // 2. Secondary fallback: 3D physics raycast against cards, buttons, or colliders
        Camera cam = Camera.main;
        if (cam != null)
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(ray, 35f);
            foreach (var h in hits)
            {
                if (h.collider == null) continue;

                Button btn = h.collider.GetComponentInParent<Button>();
                if (btn != null && btn.interactable && btn.gameObject.activeInHierarchy)
                {
                    Debug.Log($"[IntroSequenceManager] PC Mouse clicked 3D Button Collider: {btn.gameObject.name}");
                    btn.onClick.Invoke();
                    return;
                }

                string n = h.collider.gameObject.name.ToLower();
                if (n.Contains("cocasnap"))
                {
                    Debug.Log("[IntroSequenceManager] PC Mouse clicked Cocasnap card!");
                    LaunchCocasnap();
                    return;
                }
                if (n.Contains("beerpong"))
                {
                    Debug.Log("[IntroSequenceManager] PC Mouse clicked Beer Pong card!");
                    LaunchBeerPong();
                    return;
                }
            }
        }
    }

    public static void EnsureSingleEventSystem()
    {
        var systems = Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (systems == null || systems.Length <= 1) return;

        UnityEngine.EventSystems.EventSystem primary = null;
        for (int i = 0; i < systems.Length; i++)
        {
            if (systems[i].gameObject.activeInHierarchy && primary == null)
            {
                primary = systems[i];
            }
        }
        if (primary == null && systems.Length > 0) primary = systems[0];

        for (int i = 0; i < systems.Length; i++)
        {
            if (systems[i] != null && systems[i] != primary)
            {
                Debug.Log($"[IntroSequenceManager] Destroying duplicate EventSystem '{systems[i].gameObject.name}' to enforce exactly 1 EventSystem.");
                if (Application.isPlaying)
                {
                    Destroy(systems[i].gameObject);
                }
                else
                {
                    DestroyImmediate(systems[i].gameObject);
                }
            }
        }

        if (primary != null)
        {
            var inputModule = primary.GetComponent<UnityEngine.EventSystems.BaseInputModule>();
            if (inputModule == null)
            {
                primary.gameObject.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
            else if (!inputModule.enabled)
            {
                inputModule.enabled = true;
            }
        }
    }

    private void OnDestroy()
    {
        if (gameManager != null)
        {
            gameManager.OnEnterArenaRequested -= OnEnterArenaClicked;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (enterArenaPanel != null) ApplyTransparency(enterArenaPanel);
        if (gameSelectionPanel != null) ApplyTransparency(gameSelectionPanel);

        if (subtitlePanel != null)
        {
            subtitlePanel.transform.localEulerAngles = subtitleRotation;
        }
    }
#endif

    private void WireButton()
    {
        if (enterArenaButton != null)
        {
            enterArenaButton.onClick.RemoveAllListeners();
            enterArenaButton.onClick.AddListener(OnEnterArenaClicked);

            if (enterArenaButtonText != null)
            {
                enterArenaButtonText.text = "Continue  \u2192";
            }
        }
    }

    public void ConfigureCameraForPassthrough()
    {
        if (!enableMRPassthrough) return;

        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
        }
    }

    public void ApplyTransparency(GameObject panel)
    {
        if (panel == null) return;

        Image panelImage = panel.GetComponent<Image>();
        if (panelImage != null)
        {
            Color targetColor = Color.clear;
            switch (transparencyMode)
            {
                case PanelTransparencyMode.CompletelyTransparent:
                    targetColor = new Color(0f, 0f, 0f, 0f);
                    break;
                case PanelTransparencyMode.SubtleGlass:
                    targetColor = new Color(0.04f, 0.08f, 0.14f, 0.35f);
                    break;
                case PanelTransparencyMode.CustomColor:
                    targetColor = customTransparentColor;
                    break;
            }

            panelImage.color = targetColor;
            panelImage.raycastTarget = false;
        }

        Outline outline = panel.GetComponent<Outline>();
        if (outline != null && transparencyMode == PanelTransparencyMode.CompletelyTransparent)
        {
            outline.enabled = false;
        }

        CanvasGroup group = panel.GetComponent<CanvasGroup>();
        if (group != null)
        {
            group.alpha = 1f;
            group.blocksRaycasts = true;
            group.interactable = true;
        }
    }

    public void OptimizeCanvasForVRAndPC(GameObject panel)
    {
        if (panel == null) return;

        EnsureSingleEventSystem();

        Canvas canvas = panel.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            if (canvas.renderMode == RenderMode.WorldSpace)
            {
                canvas.worldCamera = Camera.main;
            }

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.dynamicPixelsPerUnit = 4f;
            }

            GraphicRaycaster gr = canvas.GetComponent<GraphicRaycaster>();
            if (gr == null)
            {
                gr = canvas.gameObject.AddComponent<GraphicRaycaster>();
            }
            gr.blockingObjects = GraphicRaycaster.BlockingObjects.None;

            canvas.overrideSorting = true;
            if (canvas.sortingOrder < 10)
            {
                canvas.sortingOrder = 10;
            }
        }
    }

    // =========================================================
    // SUBTITLES SYSTEM (Rotated to fix mirroring)
    // =========================================================

    private void EnsureSubtitleUIExists()
    {
        Transform canvasTr = GetActiveCanvasTransform();
        if (canvasTr == null) return;

        Transform existingSub = canvasTr.Find("KinexusSubtitleBar");
        if (existingSub != null)
        {
            DestroyImmediate(existingSub.gameObject);
        }

        GameObject subObj = new GameObject("KinexusSubtitleBar");
        subObj.transform.SetParent(canvasTr, false);
        RectTransform subRt = subObj.AddComponent<RectTransform>();
        subRt.anchorMin = new Vector2(0.5f, 0.5f);
        subRt.anchorMax = new Vector2(0.5f, 0.5f);
        subRt.pivot = new Vector2(0.5f, 0.5f);
        subRt.anchoredPosition = new Vector2(0f, -240f);
        subRt.sizeDelta = new Vector2(880f, 85f);
        subRt.localEulerAngles = subtitleRotation; // Fixes mirrored view

        Image subBg = subObj.AddComponent<Image>();
        subBg.color = new Color(0.01f, 0.02f, 0.05f, 0.92f); // High-contrast crisp black backdrop

        Outline subOutline = subObj.AddComponent<Outline>();
        subOutline.effectColor = new Color(1.0f, 1.0f, 1.0f, 0.95f); // Glowing white border
        subOutline.effectDistance = new Vector2(2f, -2f);

        GameObject txtObj = new GameObject("SubtitleText");
        txtObj.transform.SetParent(subObj.transform, false);
        RectTransform txtRt = txtObj.AddComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.sizeDelta = new Vector2(-40f, -10f);
        txtRt.anchoredPosition = Vector2.zero;

        subtitleText = txtObj.AddComponent<TextMeshProUGUI>();
        subtitleText.fontSize = 28;
        subtitleText.fontStyle = FontStyles.Bold;
        subtitleText.alignment = TextAlignmentOptions.Center;
        subtitleText.color = Color.white;
        subtitleText.enableWordWrapping = true;
        subtitleText.text = continueSubtitleText;

        subtitlePanel = subObj;
        subtitlePanel.SetActive(false);
    }

    private Transform GetActiveCanvasTransform()
    {
        if (enterArenaPanel != null && enterArenaPanel.transform.parent != null)
        {
            return enterArenaPanel.transform.parent;
        }
        Canvas c = Object.FindFirstObjectByType<Canvas>();
        return c != null ? c.transform : transform;
    }

    // =========================================================
    // SCREEN 1: KINEXUS WELCOME UI
    // =========================================================

    [ContextMenu("Auto-Build Kinexus Welcome Screen UI")]
    public void BuildKinexusWelcomeUIContextMenu()
    {
        EnsureKinexusLayoutExists(forceRebuild: true);
        Debug.Log("Kinexus UI: Built Welcome Screen!");
    }

    public void EnsureKinexusLayoutExists(bool forceRebuild = false)
    {
        if (enterArenaPanel == null) return;

        Transform existingCards = enterArenaPanel.transform.Find("TutorialCardsContainer");
        Transform existingClose = enterArenaPanel.transform.Find("WelcomeCloseButton");
        if (existingCards != null && existingClose != null && !forceRebuild) return;

        if (existingCards != null) DestroyImmediate(existingCards.gameObject);
        if (existingClose != null) DestroyImmediate(existingClose.gameObject);

        RectTransform panelRect = enterArenaPanel.GetComponent<RectTransform>();
        if (panelRect != null)
        {
            if (panelRect.sizeDelta.x < 700 || panelRect.sizeDelta.y < 400)
            {
                panelRect.sizeDelta = new Vector2(920f, 530f);
            }
        }

        // Header (Luminous Glowy White Quest 3 aesthetic)
        Transform headerTr = enterArenaPanel.transform.Find("KinexusHeader");
        if (headerTr != null) DestroyImmediate(headerTr.gameObject);

        GameObject headerObj = new GameObject("KinexusHeader");
        headerObj.transform.SetParent(enterArenaPanel.transform, false);
        RectTransform headerRt = headerObj.AddComponent<RectTransform>();
        headerRt.anchorMin = new Vector2(0.5f, 1f);
        headerRt.anchorMax = new Vector2(0.5f, 1f);
        headerRt.pivot = new Vector2(0.5f, 1f);
        headerRt.anchoredPosition = new Vector2(0f, -25f);
        headerRt.sizeDelta = new Vector2(750f, 115f);

        TextMeshProUGUI headerTmp = headerObj.AddComponent<TextMeshProUGUI>();
        headerTmp.text = "<size=20><color=#DCE8FF>WELCOME TO</color></size>\n<size=52><b><color=#FFFFFF>KINEXUS</color></b></size>\n<size=17><color=#E6F2FF>Step into a new reality \u2728</color></size>";
        headerTmp.alignment = TextAlignmentOptions.Center;

        // 3 Glass Cards
        GameObject cardsContainer = new GameObject("TutorialCardsContainer");
        cardsContainer.transform.SetParent(enterArenaPanel.transform, false);
        RectTransform cardsRt = cardsContainer.AddComponent<RectTransform>();
        cardsRt.anchorMin = new Vector2(0.5f, 0.5f);
        cardsRt.anchorMax = new Vector2(0.5f, 0.5f);
        cardsRt.pivot = new Vector2(0.5f, 0.5f);
        cardsRt.anchoredPosition = new Vector2(0f, -15f);
        cardsRt.sizeDelta = new Vector2(800f, 210f);

        HorizontalLayoutGroup hlg = cardsContainer.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 24f;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;

        CreateGlassCard(cardsContainer.transform, "Card_LookExplore", "\u25CE", "Look & Explore", "Move your head\nto look around");
        CreateGlassCard(cardsContainer.transform, "Card_Interact", "\u2726", "Interact", "Pinch to select\nor tap");
        CreateGlassCard(cardsContainer.transform, "Card_Move", "\u25C8", "Move", "Point and teleport\nto explore");

        // Continue Button (Glowing White Quest 3 styling)
        Transform existingBtn = enterArenaPanel.transform.Find("ContinueButton");
        if (existingBtn != null) DestroyImmediate(existingBtn.gameObject);

        GameObject btnObj = new GameObject("ContinueButton");
        btnObj.transform.SetParent(enterArenaPanel.transform, false);
        RectTransform btnRt = btnObj.AddComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0.5f, 0f);
        btnRt.anchorMax = new Vector2(0.5f, 0f);
        btnRt.pivot = new Vector2(0.5f, 0f);
        btnRt.anchoredPosition = new Vector2(0f, 50f);
        btnRt.sizeDelta = new Vector2(250f, 54f);

        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(1.0f, 1.0f, 1.0f, 0.22f); // Luminous white frosted capsule

        Outline btnOutline = btnObj.AddComponent<Outline>();
        btnOutline.effectColor = new Color(1.0f, 1.0f, 1.0f, 0.95f); // High-contrast glowing border
        btnOutline.effectDistance = new Vector2(2.5f, -2.5f);

        enterArenaButton = btnObj.AddComponent<Button>();
        ColorBlock cb = enterArenaButton.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.0f, 1.0f, 1.0f, 0.45f);
        cb.pressedColor = new Color(0.9f, 0.95f, 1.0f, 0.65f);
        enterArenaButton.colors = cb;

        GameObject btnTxtObj = new GameObject("ButtonText");
        btnTxtObj.transform.SetParent(btnObj.transform, false);
        RectTransform txtRt = btnTxtObj.AddComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.sizeDelta = Vector2.zero;

        enterArenaButtonText = btnTxtObj.AddComponent<TextMeshProUGUI>();
        enterArenaButtonText.text = "<b>Continue  \u2192</b>";
        enterArenaButtonText.fontSize = 21;
        enterArenaButtonText.alignment = TextAlignmentOptions.Center;
        enterArenaButtonText.color = Color.white;

        // Pager Dots
        Transform pagerTr = enterArenaPanel.transform.Find("PagerDots");
        if (pagerTr == null)
        {
            GameObject pagerObj = new GameObject("PagerDots");
            pagerObj.transform.SetParent(enterArenaPanel.transform, false);
            RectTransform pagerRt = pagerObj.AddComponent<RectTransform>();
            pagerRt.anchorMin = new Vector2(0.5f, 0f);
            pagerRt.anchorMax = new Vector2(0.5f, 0f);
            pagerRt.pivot = new Vector2(0.5f, 0f);
            pagerRt.anchoredPosition = new Vector2(0f, 18f);
            pagerRt.sizeDelta = new Vector2(200f, 25f);

            TextMeshProUGUI pagerTmp = pagerObj.AddComponent<TextMeshProUGUI>();
            pagerTmp.text = "<color=#FFFFFF>\u25CF</color>  <color=#8EB8EE>\u25CB  \u25CB</color>";
            pagerTmp.fontSize = 18;
            pagerTmp.alignment = TextAlignmentOptions.Center;
        }

        // Close Button (at top-right of Welcome panel with glowing white aesthetic)
        CreateCloseButton(enterArenaPanel.transform, "WelcomeCloseButton", new Vector2(-28f, -28f), CloseWelcomePanel);

        WireButton();
    }

    private void CreateGlassCard(Transform parent, string cardName, string iconSymbol, string title, string description)
    {
        GameObject cardObj = new GameObject(cardName);
        cardObj.transform.SetParent(parent, false);
        RectTransform cardRt = cardObj.AddComponent<RectTransform>();
        cardRt.sizeDelta = new Vector2(236f, 195f);

        Image cardImg = cardObj.AddComponent<Image>();
        cardImg.color = new Color(0.06f, 0.10f, 0.18f, 0.65f);

        Outline cardOutline = cardObj.AddComponent<Outline>();
        cardOutline.effectColor = new Color(1.0f, 1.0f, 1.0f, 0.90f); // Glowing white border
        cardOutline.effectDistance = new Vector2(2f, -2f);

        // Icon
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(cardObj.transform, false);
        RectTransform iconRt = iconObj.AddComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0.5f, 1f);
        iconRt.anchorMax = new Vector2(0.5f, 1f);
        iconRt.pivot = new Vector2(0.5f, 1f);
        iconRt.anchoredPosition = new Vector2(0f, -18f);
        iconRt.sizeDelta = new Vector2(80f, 45f);

        TextMeshProUGUI iconTmp = iconObj.AddComponent<TextMeshProUGUI>();
        iconTmp.text = iconSymbol;
        iconTmp.fontSize = 32;
        iconTmp.color = Color.white;
        iconTmp.alignment = TextAlignmentOptions.Center;

        // Title
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(cardObj.transform, false);
        RectTransform titleRt = titleObj.AddComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 0.5f);
        titleRt.anchorMax = new Vector2(0.5f, 0.5f);
        titleRt.pivot = new Vector2(0.5f, 0.5f);
        titleRt.anchoredPosition = new Vector2(0f, 10f);
        titleRt.sizeDelta = new Vector2(200f, 32f);

        TextMeshProUGUI titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
        titleTmp.text = $"<b>{title}</b>";
        titleTmp.fontSize = 19;
        titleTmp.color = Color.white;
        titleTmp.alignment = TextAlignmentOptions.Center;

        // Description
        GameObject descObj = new GameObject("Description");
        descObj.transform.SetParent(cardObj.transform, false);
        RectTransform descRt = descObj.AddComponent<RectTransform>();
        descRt.anchorMin = new Vector2(0.5f, 0f);
        descRt.anchorMax = new Vector2(0.5f, 0f);
        descRt.pivot = new Vector2(0.5f, 0f);
        descRt.anchoredPosition = new Vector2(0f, 18f);
        descRt.sizeDelta = new Vector2(200f, 50f);

        TextMeshProUGUI descTmp = descObj.AddComponent<TextMeshProUGUI>();
        descTmp.text = description;
        descTmp.fontSize = 14;
        descTmp.color = new Color(0.90f, 0.94f, 1.0f, 0.95f);
        descTmp.alignment = TextAlignmentOptions.Center;
    }

    // =========================================================
    // SCREEN 2: GAME SELECTION SCREEN (2 CARDS: COCASNAP & BEER PONG)
    // =========================================================

    [ContextMenu("Create & Build Game Selection Panel")]
    public void BuildDiscoverGamesUIContextMenu()
    {
        EnsureDiscoverGamesLayoutExists(forceRebuild: true);
        Debug.Log("Kinexus UI: Created & Built Game Selection Screen with 2 Cards!");
    }

    public void EnsureDiscoverGamesLayoutExists(bool forceRebuild = false)
    {
        Transform canvasTr = GetActiveCanvasTransform();
        if (canvasTr == null) return;

        // If gameSelectionPanel is null, create it automatically under Canvas!
        if (gameSelectionPanel == null)
        {
            Transform existingPanel = canvasTr.Find("GameSelectionPanel");
            if (existingPanel != null)
            {
                gameSelectionPanel = existingPanel.gameObject;
            }
            else
            {
                GameObject newPanelObj = new GameObject("GameSelectionPanel");
                newPanelObj.transform.SetParent(canvasTr, false);
                RectTransform rt = newPanelObj.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(1100f, 640f);

                Image img = newPanelObj.AddComponent<Image>();
                img.color = Color.clear;
                img.raycastTarget = false;

                gameSelectionPanel = newPanelObj;
            }
        }

        Transform existingLayout = gameSelectionPanel.transform.Find("DiscoverGamesLayout");
        Transform existingCocasnap = gameSelectionPanel.transform.Find("DiscoverGamesLayout/TwoCardsContainer/Card_Cocasnap");
        Transform existingClose = gameSelectionPanel.transform.Find("DiscoverCloseButton");

        if (existingLayout != null && existingCocasnap != null && existingClose != null && !forceRebuild) return;

        if (existingLayout != null) DestroyImmediate(existingLayout.gameObject);
        if (existingClose != null) DestroyImmediate(existingClose.gameObject);

        RectTransform panelRt = gameSelectionPanel.GetComponent<RectTransform>();
        if (panelRt != null)
        {
            panelRt.sizeDelta = new Vector2(1100f, 640f);
        }

        GameObject root = new GameObject("DiscoverGamesLayout");
        root.transform.SetParent(gameSelectionPanel.transform, false);
        RectTransform rootRt = root.AddComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.sizeDelta = Vector2.zero;

        // 1. Top Header (Luminous Glowy White Quest 3 theme)
        CreateTopHeader(root.transform);

        // 2. Exactly TWO Game Cards (Cocasnap and Beer Pong)
        CreateTwoGameCards(root.transform);

        // 3. Close Button at top-right of panel
        CreateCloseButton(gameSelectionPanel.transform, "DiscoverCloseButton", new Vector2(-28f, -28f), CloseGameSelectionPanel);
    }

    private void CreateTopHeader(Transform parent)
    {
        GameObject headerObj = new GameObject("TopHeader");
        headerObj.transform.SetParent(parent, false);
        RectTransform headerRt = headerObj.AddComponent<RectTransform>();
        headerRt.anchorMin = new Vector2(0.5f, 1f);
        headerRt.anchorMax = new Vector2(0.5f, 1f);
        headerRt.pivot = new Vector2(0.5f, 1f);
        headerRt.anchoredPosition = new Vector2(0f, -22f);
        headerRt.sizeDelta = new Vector2(900f, 80f);

        TextMeshProUGUI headerTmp = headerObj.AddComponent<TextMeshProUGUI>();
        headerTmp.text = "<size=34><b><color=#FFFFFF>CHOOSE YOUR GAME</color></b></size>\n<size=17><color=#D0E4FF>Select an experience to enter with your companion robot \u2728</color></size>";
        headerTmp.alignment = TextAlignmentOptions.Center;
    }

    private void CreateTwoGameCards(Transform parent)
    {
        GameObject cardsContainer = new GameObject("TwoCardsContainer");
        cardsContainer.transform.SetParent(parent, false);
        RectTransform cRt = cardsContainer.AddComponent<RectTransform>();
        cRt.anchorMin = new Vector2(0.5f, 0.5f);
        cRt.anchorMax = new Vector2(0.5f, 0.5f);
        cRt.pivot = new Vector2(0.5f, 0.5f);
        cRt.anchoredPosition = new Vector2(0f, -25f);
        cRt.sizeDelta = new Vector2(980f, 480f);

        // 1. Cocasnap Game Card
        CreateGameCard(cardsContainer.transform, "Card_Cocasnap",
            badge: "🧩 3D PUZZLE GAME",
            title: "Cocasnap",
            subtitle: "Match colors, snap blocks & solve 3D cube puzzles with your companion helping lift pieces!",
            imagePath: "Assets/_Cocosnap/Assets/_TheCocoaSnap/Images/logo.png",
            buttonLabel: "Enter Cocasnap  \u2794",
            pos: new Vector2(-245f, 0f),
            size: new Vector2(460f, 460f),
            onEnterAction: LaunchCocasnap
        );

        // 2. Beer Pong Game Card
        CreateGameCard(cardsContainer.transform, "Card_BeerPong",
            badge: "🏓 PHYSICS ARCADE",
            title: "Beer Pong",
            subtitle: "Toss balls into cups! Your companion fetches balls or gets angry with stars if thrown!",
            imagePath: "Assets/_BeerPong/Images/logo.png",
            buttonLabel: "Enter Beer Pong  \u2794",
            pos: new Vector2(245f, 0f),
            size: new Vector2(460f, 460f),
            onEnterAction: LaunchBeerPong
        );
    }

    private void CreateGameCard(Transform parent, string name, string badge, string title, string subtitle, string imagePath, string buttonLabel, Vector2 pos, Vector2 size, UnityEngine.Events.UnityAction onEnterAction)
    {
        GameObject card = new GameObject(name);
        card.transform.SetParent(parent, false);
        RectTransform rt = card.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        // Frosted dark glass background with luminous white glow
        Image img = card.AddComponent<Image>();
        img.color = new Color(0.04f, 0.08f, 0.16f, 0.88f);

        Outline outline = card.AddComponent<Outline>();
        outline.effectColor = new Color(1.0f, 1.0f, 1.0f, 0.95f); // Glowing white border
        outline.effectDistance = new Vector2(2.5f, -2.5f);

        // Whole card is clickable for effortless PC & VR interaction!
        Button cardBtn = card.AddComponent<Button>();
        ColorBlock cardCb = cardBtn.colors;
        cardCb.normalColor = Color.white;
        cardCb.highlightedColor = new Color(1.1f, 1.15f, 1.3f, 1.0f);
        cardCb.pressedColor = new Color(0.85f, 0.92f, 1.0f, 1.0f);
        cardBtn.colors = cardCb;
        cardBtn.onClick.AddListener(onEnterAction);

        // Add 3D BoxCollider so physics raycasts directly hit the card
        BoxCollider boxCol = card.AddComponent<BoxCollider>();
        boxCol.size = new Vector3(size.x, size.y, 5f);

        // Badge at top
        GameObject badgeObj = new GameObject("Badge");
        badgeObj.transform.SetParent(card.transform, false);
        RectTransform badgeRt = badgeObj.AddComponent<RectTransform>();
        badgeRt.anchorMin = new Vector2(0.5f, 1f);
        badgeRt.anchorMax = new Vector2(0.5f, 1f);
        badgeRt.pivot = new Vector2(0.5f, 1f);
        badgeRt.anchoredPosition = new Vector2(0f, -14f);
        badgeRt.sizeDelta = new Vector2(240f, 30f);

        Image bImg = badgeObj.AddComponent<Image>();
        bImg.color = new Color(1.0f, 1.0f, 1.0f, 0.20f);
        bImg.raycastTarget = false;

        Outline bOutline = badgeObj.AddComponent<Outline>();
        bOutline.effectColor = new Color(1.0f, 1.0f, 1.0f, 0.85f);
        bOutline.effectDistance = new Vector2(1.5f, -1.5f);

        GameObject bTxt = new GameObject("Text");
        bTxt.transform.SetParent(badgeObj.transform, false);
        RectTransform bTxtRt = bTxt.AddComponent<RectTransform>();
        bTxtRt.anchorMin = Vector2.zero;
        bTxtRt.anchorMax = Vector2.one;
        bTxtRt.sizeDelta = Vector2.zero;

        TextMeshProUGUI bTmp = bTxt.AddComponent<TextMeshProUGUI>();
        bTmp.text = badge;
        bTmp.fontSize = 13;
        bTmp.fontStyle = FontStyles.Bold;
        bTmp.color = Color.white;
        bTmp.alignment = TextAlignmentOptions.Center;

        // Image Preview Block
        GameObject imgBlock = new GameObject("ImagePreview");
        imgBlock.transform.SetParent(card.transform, false);
        RectTransform imgRt = imgBlock.AddComponent<RectTransform>();
        imgRt.anchorMin = new Vector2(0.5f, 1f);
        imgRt.anchorMax = new Vector2(0.5f, 1f);
        imgRt.pivot = new Vector2(0.5f, 1f);
        imgRt.anchoredPosition = new Vector2(0f, -54f);
        imgRt.sizeDelta = new Vector2(400f, 155f);

        RawImage previewRaw = imgBlock.AddComponent<RawImage>();
        previewRaw.color = Color.white;
        previewRaw.raycastTarget = false;

        Texture2D loadedTex = LoadTextureFromFile(imagePath);
        if (loadedTex != null)
        {
            previewRaw.texture = loadedTex;
        }

        Outline imgOutline = imgBlock.AddComponent<Outline>();
        imgOutline.effectColor = new Color(1.0f, 1.0f, 1.0f, 0.70f);
        imgOutline.effectDistance = new Vector2(1.5f, -1.5f);

        // Game Title & Subtitle Block
        GameObject infoObj = new GameObject("InfoBlock");
        infoObj.transform.SetParent(card.transform, false);
        RectTransform infoRt = infoObj.AddComponent<RectTransform>();
        infoRt.anchorMin = new Vector2(0.5f, 0.5f);
        infoRt.anchorMax = new Vector2(0.5f, 0.5f);
        infoRt.pivot = new Vector2(0.5f, 0.5f);
        infoRt.anchoredPosition = new Vector2(0f, -65f);
        infoRt.sizeDelta = new Vector2(410f, 95f);

        TextMeshProUGUI infoTmp = infoObj.AddComponent<TextMeshProUGUI>();
        infoTmp.text = $"<size=28><b><color=#FFFFFF>{title}</color></b></size>\n\n<size=15><color=#D8E8FF>{subtitle}</color></size>";
        infoTmp.alignment = TextAlignmentOptions.Center;
        infoTmp.raycastTarget = false;

        // Enter Game Button (at bottom of card)
        GameObject enterBtn = new GameObject("EnterButton");
        enterBtn.transform.SetParent(card.transform, false);
        RectTransform ebRt = enterBtn.AddComponent<RectTransform>();
        ebRt.anchorMin = new Vector2(0.5f, 0f);
        ebRt.anchorMax = new Vector2(0.5f, 0f);
        ebRt.pivot = new Vector2(0.5f, 0f);
        ebRt.anchoredPosition = new Vector2(0f, 24f);
        ebRt.sizeDelta = new Vector2(340f, 52f);

        Image ebImg = enterBtn.AddComponent<Image>();
        ebImg.color = new Color(1.0f, 1.0f, 1.0f, 0.25f); // Glowing white translucent capsule

        Outline ebOutline = enterBtn.AddComponent<Outline>();
        ebOutline.effectColor = new Color(1.0f, 1.0f, 1.0f, 0.95f); // High-contrast glowing border
        ebOutline.effectDistance = new Vector2(2f, -2f);

        Button btn = enterBtn.AddComponent<Button>();
        ColorBlock btnCb = btn.colors;
        btnCb.normalColor = Color.white;
        btnCb.highlightedColor = new Color(1.0f, 1.0f, 1.0f, 0.50f);
        btnCb.pressedColor = new Color(0.9f, 0.95f, 1.0f, 0.75f);
        btn.colors = btnCb;
        btn.onClick.AddListener(onEnterAction);

        GameObject ebTxt = new GameObject("Text");
        ebTxt.transform.SetParent(enterBtn.transform, false);
        RectTransform ebTxtRt = ebTxt.AddComponent<RectTransform>();
        ebTxtRt.anchorMin = Vector2.zero;
        ebTxtRt.anchorMax = Vector2.one;
        ebTxtRt.sizeDelta = Vector2.zero;

        TextMeshProUGUI ebTmp = ebTxt.AddComponent<TextMeshProUGUI>();
        ebTmp.text = $"<b>{buttonLabel}</b>";
        ebTmp.fontSize = 19;
        ebTmp.fontStyle = FontStyles.Bold;
        ebTmp.color = Color.white;
        ebTmp.alignment = TextAlignmentOptions.Center;
        ebTmp.raycastTarget = false;
    }

    private Texture2D LoadTextureFromFile(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
#if UNITY_EDITOR
        Texture2D assetTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (assetTex != null) return assetTex;
#endif
        if (System.IO.File.Exists(path))
        {
            try
            {
                byte[] bytes = System.IO.File.ReadAllBytes(path);
                Texture2D tex = new Texture2D(2, 2);
                if (tex.LoadImage(bytes))
                {
                    return tex;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"IntroSequenceManager: Failed loading texture at {path}: {ex.Message}");
            }
        }
        return null;
    }

    // =========================================================
    // SEQUENCING & TRANSITIONS
    // =========================================================

    private IEnumerator PlayIntroSequence()
    {
        if (companion != null && companion.companionAnimator != null)
        {
            companion.companionAnimator.CrossFade("Wave", 0.2f);
            yield return new WaitForSeconds(3.0f);
            companion.companionAnimator.CrossFade("Idle", 0.2f);
        }

        if (enterArenaPanel != null)
        {
            ConfigureCameraForPassthrough();
            ApplyTransparency(enterArenaPanel);
            OptimizeCanvasForVRAndPC(enterArenaPanel);
            enterArenaPanel.SetActive(true);
        }
    }

    public void TriggerEnterArena()
    {
        OnEnterArenaClicked();
    }

    private void OnEnterArenaClicked()
    {
        Debug.Log("Kinexus: Continue clicked!");
        if (enterArenaPanel != null) enterArenaPanel.SetActive(false);
        StartCoroutine(TransitionToGameMenu());
    }

    private IEnumerator TransitionToGameMenu()
    {
        // 1. Play companion excited talk animation
        if (companion != null && companion.companionAnimator != null)
        {
            companion.companionAnimator.CrossFade("Talk_Excited", 0.2f);
        }

        // 2. Show Subtitles with script line
        if (subtitlePanel != null)
        {
            subtitlePanel.transform.localEulerAngles = subtitleRotation;
            if (subtitleText != null)
            {
                subtitleText.text = continueSubtitleText;
            }
            subtitlePanel.SetActive(true);
        }

        yield return new WaitForSeconds(subtitleDisplayDuration);

        // 3. Hide subtitles and return companion to idle
        if (subtitlePanel != null)
        {
            subtitlePanel.SetActive(false);
        }

        if (companion != null && companion.companionAnimator != null)
        {
            companion.companionAnimator.CrossFade("Idle", 0.2f);
        }

        // 4. Ensure gameSelectionPanel exists and show it
        if (gameSelectionPanel == null)
        {
            EnsureDiscoverGamesLayoutExists();
        }

        if (gameSelectionPanel != null)
        {
            ApplyTransparency(gameSelectionPanel);
            OptimizeCanvasForVRAndPC(gameSelectionPanel);
            gameSelectionPanel.SetActive(true);
            Debug.Log("Kinexus: Discover Your Next Game screen is now ACTIVE!");
        }
    }

    // =========================================================
    // GAME LAUNCHERS, RELOCATION & PANEL DISMISSAL
    // =========================================================

    /// <summary>
    /// Launches Cocasnap: relocates player to the puzzle playing station, dismisses panels, and accompanies companion.
    /// </summary>
    public void LaunchCocasnap()
    {
        Debug.Log("[IntroSequenceManager] Entering Cocasnap Game Station!");
        CloseAllPanels();

        onEnterCocasnap?.Invoke();

        // 1. Enable Cocasnap puzzle components
        if (cocasnapInSceneRoot != null)
        {
            cocasnapInSceneRoot.SetActive(true);
        }
        else
        {
            PuzzleLogic puzzle = Object.FindFirstObjectByType<PuzzleLogic>(FindObjectsInactive.Include);
            if (puzzle != null) puzzle.gameObject.SetActive(true);

            CubeResetter resetter = Object.FindFirstObjectByType<CubeResetter>(FindObjectsInactive.Include);
            if (resetter != null) resetter.gameObject.SetActive(true);
        }

        // 2. Relocate Player to Cocasnap Station
        Vector3 targetPos = cocasnapPlayingPosition;
        PuzzleLogic activePuzzle = Object.FindFirstObjectByType<PuzzleLogic>();
        if (activePuzzle != null)
        {
            // Position player 1.1m back from the puzzle table facing it
            targetPos = activePuzzle.transform.position - activePuzzle.transform.forward * 1.1f;
            targetPos.y = 0f;
        }

        RelocatePlayer(targetPos);

        // 3. Companion robot accompanies player to Cocasnap station
        AccompanyCompanion(targetPos + new Vector3(0.55f, 0f, 0.75f), targetPos);

        // 4. Update Quick Switcher HUD indicator
        if (QuickGameSwitcherHUD.Instance != null)
        {
            QuickGameSwitcherHUD.Instance.HighlightActive(1);
        }
    }

    /// <summary>
    /// Launches Beer Pong: ensures station is present, relocates player to throwing line, and accompanies companion.
    /// </summary>
    public void LaunchBeerPong()
    {
        Debug.Log("[IntroSequenceManager] Entering Beer Pong Game Station!");
        CloseAllPanels();

        onEnterBeerPong?.Invoke();

        // 1. Ensure Beer Pong station exists in arena
        EnsureBeerPongStationExists();

        // 2. Relocate Player to Beer Pong Throwing Line
        RelocatePlayer(beerPongPlayingPosition);

        // 3. Companion robot accompanies player to Beer Pong station
        AccompanyCompanion(beerPongPlayingPosition + new Vector3(-0.55f, 0f, 0.75f), beerPongPlayingPosition);

        // 4. Update Quick Switcher HUD indicator
        if (QuickGameSwitcherHUD.Instance != null)
        {
            QuickGameSwitcherHUD.Instance.HighlightActive(2);
        }
    }

    /// <summary>
    /// Relocates player back to the Welcome Lobby area and presents the Game Selection Cards menu.
    /// </summary>
    public void RelocateToLobbyAndShowMenu()
    {
        Debug.Log("[IntroSequenceManager] Relocating to Lobby & opening Game Selection Cards!");

        RelocatePlayer(lobbyPlayerPosition);
        PositionPanelsInLobby();

        if (enterArenaPanel != null) enterArenaPanel.SetActive(false);
        if (gameSelectionPanel == null) EnsureDiscoverGamesLayoutExists();
        if (gameSelectionPanel != null)
        {
            ApplyTransparency(gameSelectionPanel);
            OptimizeCanvasForVRAndPC(gameSelectionPanel);
            gameSelectionPanel.SetActive(true);
        }

        AccompanyCompanion(lobbyPlayerPosition + new Vector3(0.75f, 0f, 0.9f), lobbyPlayerPosition);

        if (QuickGameSwitcherHUD.Instance != null)
        {
            QuickGameSwitcherHUD.Instance.HighlightActive(0);
        }
    }

    /// <summary>
    /// Relocates the active player (OVRCameraRig / XROrigin / Camera.main) to target world position.
    /// </summary>
    public static void RelocatePlayer(Vector3 targetPos, Quaternion? targetRot = null)
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Transform rig = cam.transform;
        while (rig.parent != null)
        {
            string pName = rig.parent.name.ToLower();
            if (pName.Contains("rig") || pName.Contains("origin") || pName.Contains("tracking") || pName.Contains("player") || pName.Contains("meta"))
            {
                rig = rig.parent;
            }
            else
            {
                break;
            }
        }

        if (rig != cam.transform)
        {
            Vector3 camLocalOffset = cam.transform.position - rig.position;
            camLocalOffset.y = 0f;
            rig.position = targetPos - camLocalOffset;
            if (targetRot.HasValue) rig.rotation = targetRot.Value;
        }
        else
        {
            cam.transform.position = targetPos;
            if (targetRot.HasValue) cam.transform.rotation = targetRot.Value;
        }

        Debug.Log($"[IntroSequenceManager] Relocated player rig '{rig.name}' to {targetPos}");
    }

    /// <summary>
    /// Positions the Welcome / Game Selection Canvas root comfortably in the Welcome Lobby away from tables.
    /// </summary>
    public void PositionPanelsInLobby()
    {
        Transform canvasTr = GetActiveCanvasTransform();
        if (canvasTr != null)
        {
            canvasTr.position = lobbyPanelPosition;
            canvasTr.rotation = Quaternion.identity; // Facing player standing at Z = -2.6 looking +Z
            canvasTr.localScale = Vector3.one * 0.0016f;
        }
    }

    /// <summary>
    /// Moves the companion robot to accompany the player at a target position.
    /// </summary>
    public void AccompanyCompanion(Vector3 targetPos, Vector3 lookAtPos)
    {
        if (companion == null) companion = Object.FindFirstObjectByType<CompanionBrain>();
        if (companion != null)
        {
            var agent = companion.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
            {
                agent.Warp(targetPos);
            }
            else
            {
                companion.transform.position = targetPos;
            }

            Vector3 lookDir = lookAtPos - targetPos;
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude > 0.01f)
            {
                companion.transform.rotation = Quaternion.LookRotation(lookDir.normalized);
            }
        }
    }

    public void EnsureQuickGameSwitcherHUDExists()
    {
        QuickGameSwitcherHUD hud = Object.FindFirstObjectByType<QuickGameSwitcherHUD>();
        if (hud == null)
        {
            GameObject hudObj = new GameObject("QuickGameSwitcherHUD");
            hudObj.AddComponent<QuickGameSwitcherHUD>();
        }
    }

    public void EnsureBeerPongStationExists()
    {
        BeerPongStationBuilder builder = Object.FindFirstObjectByType<BeerPongStationBuilder>();
        if (builder == null)
        {
            GameObject builderObj = new GameObject("BeerPongStationBuilder");
            builder = builderObj.AddComponent<BeerPongStationBuilder>();
        }
        builder.EnsureBeerPongStation();
    }

    /// <summary>
    /// Closes all panels and subtitles, making UI completely disappear from the screen.
    /// </summary>
    public void CloseAllPanels()
    {
        Debug.Log("IntroSequenceManager: Dismissing all panels from screen.");
        if (enterArenaPanel != null) enterArenaPanel.SetActive(false);
        if (gameSelectionPanel != null) gameSelectionPanel.SetActive(false);
        if (subtitlePanel != null) subtitlePanel.SetActive(false);
    }

    /// <summary>
    /// Closes and removes the Welcome tutorial panel.
    /// </summary>
    public void CloseWelcomePanel()
    {
        Debug.Log("IntroSequenceManager: Closed Welcome panel.");
        CloseAllPanels();
    }

    /// <summary>
    /// Closes and removes the Discover Your Next Game panel.
    /// </summary>
    public void CloseGameSelectionPanel()
    {
        Debug.Log("IntroSequenceManager: Closed Game Selection panel.");
        CloseAllPanels();
    }

    private void TryLoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return;

        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(i);
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            if (string.Equals(name, sceneName, System.StringComparison.OrdinalIgnoreCase))
            {
                Debug.Log($"IntroSequenceManager: Loading scene '{sceneName}' (Build Settings index {i})...");
                UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
                return;
            }
        }
        Debug.LogWarning($"IntroSequenceManager: Scene '{sceneName}' was not found in Build Settings!");
    }

    private GameObject CreateCloseButton(Transform parent, string name, Vector2 anchoredPos, UnityEngine.Events.UnityAction onClickAction)
    {
        GameObject closeObj = new GameObject(name);
        closeObj.transform.SetParent(parent, false);
        RectTransform rt = closeObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(44f, 44f); // Comfortable Quest 3 poke/click target

        Image img = closeObj.AddComponent<Image>();
        img.color = new Color(1.0f, 1.0f, 1.0f, 0.16f); // Frosted luminous white glass

        Outline outline = closeObj.AddComponent<Outline>();
        outline.effectColor = new Color(1.0f, 1.0f, 1.0f, 0.95f); // Glowing white border
        outline.effectDistance = new Vector2(2f, -2f);

        Button btn = closeObj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.0f, 1.0f, 1.0f, 0.45f);
        cb.pressedColor = new Color(0.9f, 0.95f, 1.0f, 0.70f);
        btn.colors = cb;
        btn.onClick.AddListener(onClickAction);

        GameObject txtObj = new GameObject("Icon");
        txtObj.transform.SetParent(closeObj.transform, false);
        RectTransform txtRt = txtObj.AddComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.sizeDelta = Vector2.zero;

        TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
        tmp.text = "✕";
        tmp.fontSize = 22;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;

        return closeObj;
    }
}
