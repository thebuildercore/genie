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
        ConfigureCameraForPassthrough();

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
        if (gameManager == null)
        {
            gameManager = Object.FindFirstObjectByType<GameEventManager>();
        }

        if (companion == null)
        {
            companion = Object.FindFirstObjectByType<CompanionBrain>();
        }

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

        Canvas canvas = panel.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            if (canvas.renderMode == RenderMode.WorldSpace && canvas.worldCamera == null)
            {
                canvas.worldCamera = Camera.main;
            }

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null && scaler.dynamicPixelsPerUnit < 3f)
            {
                scaler.dynamicPixelsPerUnit = 3f;
            }

            if (canvas.GetComponent<GraphicRaycaster>() == null)
            {
                canvas.gameObject.AddComponent<GraphicRaycaster>();
            }

            canvas.overrideSorting = true;
            if (canvas.sortingOrder < 5)
            {
                canvas.sortingOrder = 5;
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
            subtitlePanel = existingSub.gameObject;
            subtitlePanel.transform.localEulerAngles = subtitleRotation;
            subtitleText = subtitlePanel.GetComponentInChildren<TextMeshProUGUI>();
            return;
        }

        GameObject subObj = new GameObject("KinexusSubtitleBar");
        subObj.transform.SetParent(canvasTr, false);
        RectTransform subRt = subObj.AddComponent<RectTransform>();
        subRt.anchorMin = new Vector2(0.5f, 0.5f);
        subRt.anchorMax = new Vector2(0.5f, 0.5f);
        subRt.pivot = new Vector2(0.5f, 0.5f);
        subRt.anchoredPosition = new Vector2(0f, -240f);
        subRt.sizeDelta = new Vector2(750f, 65f);
        subRt.localEulerAngles = subtitleRotation; // Fixes mirrored view

        Image subBg = subObj.AddComponent<Image>();
        subBg.color = new Color(0.04f, 0.08f, 0.14f, 0.85f);

        Outline subOutline = subObj.AddComponent<Outline>();
        subOutline.effectColor = new Color(0.48f, 0.75f, 1.0f, 0.70f);
        subOutline.effectDistance = new Vector2(2f, -2f);

        GameObject txtObj = new GameObject("SubtitleText");
        txtObj.transform.SetParent(subObj.transform, false);
        RectTransform txtRt = txtObj.AddComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.sizeDelta = new Vector2(-40f, -10f);
        txtRt.anchoredPosition = Vector2.zero;

        subtitleText = txtObj.AddComponent<TextMeshProUGUI>();
        subtitleText.fontSize = 20;
        subtitleText.alignment = TextAlignmentOptions.Center;
        subtitleText.color = Color.white;
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
    // SCREEN 2: DISCOVER YOUR NEXT GAME (Auto-creates panel if null)
    // =========================================================

    [ContextMenu("Create & Build Game Selection Panel")]
    public void BuildDiscoverGamesUIContextMenu()
    {
        EnsureDiscoverGamesLayoutExists(forceRebuild: true);
        Debug.Log("Kinexus UI: Created & Built Discover Games Screen!");
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
                rt.sizeDelta = new Vector2(1100f, 620f);

                Image img = newPanelObj.AddComponent<Image>();
                img.color = Color.clear;
                img.raycastTarget = false;

                gameSelectionPanel = newPanelObj;
            }
        }

        Transform existingLayout = gameSelectionPanel.transform.Find("DiscoverGamesLayout");
        Transform existingCocasnap = gameSelectionPanel.transform.Find("DiscoverGamesLayout/GameCardsCarousel/Card_Cocasnap");
        Transform existingClose = gameSelectionPanel.transform.Find("DiscoverCloseButton");

        if (existingLayout != null && existingCocasnap != null && existingClose != null && !forceRebuild) return;

        if (existingLayout != null) DestroyImmediate(existingLayout.gameObject);
        if (existingClose != null) DestroyImmediate(existingClose.gameObject);

        RectTransform panelRt = gameSelectionPanel.GetComponent<RectTransform>();
        if (panelRt != null)
        {
            panelRt.sizeDelta = new Vector2(1120f, 630f);
        }

        GameObject root = new GameObject("DiscoverGamesLayout");
        root.transform.SetParent(gameSelectionPanel.transform, false);
        RectTransform rootRt = root.AddComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.sizeDelta = Vector2.zero;

        // 1. Sidebar Dock (Glowing White Quest 3 theme)
        CreateSidebar(root.transform);

        // 2. Top Bar (Glowing White Quest 3 theme)
        CreateTopBar(root.transform);

        // 3. Carousel with Cocasnap & Beer Pong Game Cards
        CreateGameCarousel(root.transform);

        // 4. Bottom Widgets (Glowing White Quest 3 theme)
        CreateBottomWidgets(root.transform);

        // Close Button at top-right of Discover Games panel
        CreateCloseButton(gameSelectionPanel.transform, "DiscoverCloseButton", new Vector2(-28f, -28f), CloseGameSelectionPanel);
    }

    private void CreateSidebar(Transform parent)
    {
        GameObject sidebar = new GameObject("SidebarDock");
        sidebar.transform.SetParent(parent, false);
        RectTransform sbRt = sidebar.AddComponent<RectTransform>();
        sbRt.anchorMin = new Vector2(0f, 0.5f);
        sbRt.anchorMax = new Vector2(0f, 0.5f);
        sbRt.pivot = new Vector2(0f, 0.5f);
        sbRt.anchoredPosition = new Vector2(25f, 15f);
        sbRt.sizeDelta = new Vector2(170f, 480f);

        Image sbImg = sidebar.AddComponent<Image>();
        sbImg.color = new Color(0.05f, 0.08f, 0.15f, 0.65f);

        Outline sbOutline = sidebar.AddComponent<Outline>();
        sbOutline.effectColor = new Color(1.0f, 1.0f, 1.0f, 0.85f); // Glowing white
        sbOutline.effectDistance = new Vector2(2f, -2f);

        GameObject logoObj = new GameObject("Logo");
        logoObj.transform.SetParent(sidebar.transform, false);
        RectTransform logoRt = logoObj.AddComponent<RectTransform>();
        logoRt.anchorMin = new Vector2(0.5f, 1f);
        logoRt.anchorMax = new Vector2(0.5f, 1f);
        logoRt.pivot = new Vector2(0.5f, 1f);
        logoRt.anchoredPosition = new Vector2(0f, -20f);
        logoRt.sizeDelta = new Vector2(140f, 40f);

        TextMeshProUGUI logoTmp = logoObj.AddComponent<TextMeshProUGUI>();
        logoTmp.text = "\u2728 <b>KINEXUS</b>";
        logoTmp.fontSize = 20;
        logoTmp.color = Color.white;
        logoTmp.alignment = TextAlignmentOptions.Center;

        GameObject menuObj = new GameObject("MenuItems");
        menuObj.transform.SetParent(sidebar.transform, false);
        RectTransform menuRt = menuObj.AddComponent<RectTransform>();
        menuRt.anchorMin = new Vector2(0.5f, 0.5f);
        menuRt.anchorMax = new Vector2(0.5f, 0.5f);
        menuRt.pivot = new Vector2(0.5f, 0.5f);
        menuRt.anchoredPosition = new Vector2(0f, -10f);
        menuRt.sizeDelta = new Vector2(140f, 340f);

        VerticalLayoutGroup vlg = menuObj.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 14f;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;

        CreateSidebarItem(menuObj.transform, "\uD83C\uDFE0 Home", isActive: false);
        CreateSidebarItem(menuObj.transform, "\uD83C\uDFAE Games", isActive: true);
        CreateSidebarItem(menuObj.transform, "\uD83C\uDFC6 Leaderboard", isActive: false);
        CreateSidebarItem(menuObj.transform, "\u2B50 Rewards", isActive: false);
        CreateSidebarItem(menuObj.transform, "\u2699\uFE0F Settings", isActive: false);
    }

    private void CreateSidebarItem(Transform parent, string label, bool isActive)
    {
        GameObject item = new GameObject("Item_" + label);
        item.transform.SetParent(parent, false);
        RectTransform rt = item.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(140f, 44f);

        Image img = item.AddComponent<Image>();
        img.color = isActive ? new Color(1.0f, 1.0f, 1.0f, 0.22f) : new Color(0.08f, 0.12f, 0.20f, 0.35f);

        Outline outline = item.AddComponent<Outline>();
        outline.effectColor = isActive ? new Color(1.0f, 1.0f, 1.0f, 0.95f) : new Color(1.0f, 1.0f, 1.0f, 0.35f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(item.transform, false);
        RectTransform txtRt = txtObj.AddComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.sizeDelta = new Vector2(-16f, 0f);

        TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 15;
        tmp.color = isActive ? Color.white : new Color(0.85f, 0.92f, 1.0f, 0.85f);
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
    }

    private void CreateTopBar(Transform parent)
    {
        GameObject titleObj = new GameObject("TopTitle");
        titleObj.transform.SetParent(parent, false);
        RectTransform titleRt = titleObj.AddComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.55f, 1f);
        titleRt.anchorMax = new Vector2(0.55f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -22f);
        titleRt.sizeDelta = new Vector2(550f, 75f);

        TextMeshProUGUI titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "<size=32><b><color=#FFFFFF>Choose Your Arena \u2728</color></b></size>\n<size=15><color=#DCE8FF>Select a game below to enter</color></size>";
        titleTmp.alignment = TextAlignmentOptions.Center;

        GameObject searchObj = new GameObject("SearchBar");
        searchObj.transform.SetParent(parent, false);
        RectTransform searchRt = searchObj.AddComponent<RectTransform>();
        searchRt.anchorMin = new Vector2(1f, 1f);
        searchRt.anchorMax = new Vector2(1f, 1f);
        searchRt.pivot = new Vector2(1f, 1f);
        searchRt.anchoredPosition = new Vector2(-90f, -26f);
        searchRt.sizeDelta = new Vector2(170f, 38f);

        Image searchImg = searchObj.AddComponent<Image>();
        searchImg.color = new Color(1.0f, 1.0f, 1.0f, 0.12f);

        Outline searchOutline = searchObj.AddComponent<Outline>();
        searchOutline.effectColor = new Color(1.0f, 1.0f, 1.0f, 0.85f);
        searchOutline.effectDistance = new Vector2(1.5f, -1.5f);

        GameObject sTxtObj = new GameObject("Text");
        sTxtObj.transform.SetParent(searchObj.transform, false);
        RectTransform sTxtRt = sTxtObj.AddComponent<RectTransform>();
        sTxtRt.anchorMin = Vector2.zero;
        sTxtRt.anchorMax = Vector2.one;
        sTxtRt.sizeDelta = new Vector2(-20f, 0f);

        TextMeshProUGUI sTmp = sTxtObj.AddComponent<TextMeshProUGUI>();
        sTmp.text = "\uD83D\uDD0D Search games...";
        sTmp.fontSize = 13;
        sTmp.color = new Color(0.85f, 0.92f, 1.0f, 0.85f);
        sTmp.alignment = TextAlignmentOptions.MidlineLeft;

        GameObject profileObj = new GameObject("ProfileAvatar");
        profileObj.transform.SetParent(parent, false);
        RectTransform profRt = profileObj.AddComponent<RectTransform>();
        profRt.anchorMin = new Vector2(1f, 1f);
        profRt.anchorMax = new Vector2(1f, 1f);
        profRt.pivot = new Vector2(1f, 1f);
        profRt.anchoredPosition = new Vector2(-35f, -25f);
        profRt.sizeDelta = new Vector2(40f, 40f);

        Image profImg = profileObj.AddComponent<Image>();
        profImg.color = new Color(1.0f, 1.0f, 1.0f, 0.15f);

        Outline profOutline = profileObj.AddComponent<Outline>();
        profOutline.effectColor = new Color(1.0f, 1.0f, 1.0f, 0.85f);
        profOutline.effectDistance = new Vector2(1.5f, -1.5f);

        GameObject pTxtObj = new GameObject("Icon");
        pTxtObj.transform.SetParent(profileObj.transform, false);
        RectTransform pTxtRt = pTxtObj.AddComponent<RectTransform>();
        pTxtRt.anchorMin = Vector2.zero;
        pTxtRt.anchorMax = Vector2.one;
        pTxtRt.sizeDelta = Vector2.zero;

        TextMeshProUGUI pTmp = pTxtObj.AddComponent<TextMeshProUGUI>();
        pTmp.text = "\uD83D\uDC64";
        pTmp.fontSize = 20;
        pTmp.color = Color.white;
        pTmp.alignment = TextAlignmentOptions.Center;
    }

    private void CreateGameCarousel(Transform parent)
    {
        GameObject carousel = new GameObject("GameCardsCarousel");
        carousel.transform.SetParent(parent, false);
        RectTransform carRt = carousel.AddComponent<RectTransform>();
        carRt.anchorMin = new Vector2(0.57f, 0.5f);
        carRt.anchorMax = new Vector2(0.57f, 0.5f);
        carRt.pivot = new Vector2(0.5f, 0.5f);
        carRt.anchoredPosition = new Vector2(0f, -10f);
        carRt.sizeDelta = new Vector2(820f, 370f);

        // 1. Cocasnap Game Card
        CreateGameCard(carousel.transform, "Card_Cocasnap",
            badge: "🧩 3D Puzzle Challenge",
            title: "Cocasnap",
            subtitle: "Snap, match & solve the 3D cube puzzle with your companion robot!",
            buttonLabel: "Enter Cocasnap  \u2794",
            pos: new Vector2(-195f, 0f),
            size: new Vector2(360f, 350f),
            onEnterAction: LaunchCocasnap
        );

        // 2. Beer Pong Game Card
        CreateGameCard(carousel.transform, "Card_BeerPong",
            badge: "🏓 Physics Arcade",
            title: "Beer Pong",
            subtitle: "Aim your toss, bounce the ping-pong balls & sink them into the red cups!",
            buttonLabel: "Enter Beer Pong  \u2794",
            pos: new Vector2(195f, 0f),
            size: new Vector2(360f, 350f),
            onEnterAction: LaunchBeerPong
        );

        // Pager indicator
        GameObject dotsObj = new GameObject("CarouselDots");
        dotsObj.transform.SetParent(carousel.transform, false);
        RectTransform dotsRt = dotsObj.AddComponent<RectTransform>();
        dotsRt.anchorMin = new Vector2(0.5f, 0f);
        dotsRt.anchorMax = new Vector2(0.5f, 0f);
        dotsRt.pivot = new Vector2(0.5f, 0f);
        dotsRt.anchoredPosition = new Vector2(0f, -24f);
        dotsRt.sizeDelta = new Vector2(200f, 20f);

        TextMeshProUGUI dotsTmp = dotsObj.AddComponent<TextMeshProUGUI>();
        dotsTmp.text = "<color=#FFFFFF>\u25CF</color>    <color=#A5CCFF>\u25CF</color>";
        dotsTmp.fontSize = 18;
        dotsTmp.alignment = TextAlignmentOptions.Center;
    }

    private void CreateGameCard(Transform parent, string name, string badge, string title, string subtitle, string buttonLabel, Vector2 pos, Vector2 size, UnityEngine.Events.UnityAction onEnterAction)
    {
        GameObject card = new GameObject(name);
        card.transform.SetParent(parent, false);
        RectTransform rt = card.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        // Frosted dark glass with luminous white glow
        Image img = card.AddComponent<Image>();
        img.color = new Color(0.06f, 0.10f, 0.18f, 0.70f);

        Outline outline = card.AddComponent<Outline>();
        outline.effectColor = new Color(1.0f, 1.0f, 1.0f, 0.95f); // Glowing white border
        outline.effectDistance = new Vector2(2.5f, -2.5f);

        // Badge at top
        GameObject badgeObj = new GameObject("Badge");
        badgeObj.transform.SetParent(card.transform, false);
        RectTransform badgeRt = badgeObj.AddComponent<RectTransform>();
        badgeRt.anchorMin = new Vector2(0.5f, 1f);
        badgeRt.anchorMax = new Vector2(0.5f, 1f);
        badgeRt.pivot = new Vector2(0.5f, 1f);
        badgeRt.anchoredPosition = new Vector2(0f, -16f);
        badgeRt.sizeDelta = new Vector2(250f, 32f);

        Image bImg = badgeObj.AddComponent<Image>();
        bImg.color = new Color(1.0f, 1.0f, 1.0f, 0.18f);

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

        // Game Title & Subtitle
        GameObject infoObj = new GameObject("InfoBlock");
        infoObj.transform.SetParent(card.transform, false);
        RectTransform infoRt = infoObj.AddComponent<RectTransform>();
        infoRt.anchorMin = new Vector2(0.5f, 0.5f);
        infoRt.anchorMax = new Vector2(0.5f, 0.5f);
        infoRt.pivot = new Vector2(0.5f, 0.5f);
        infoRt.anchoredPosition = new Vector2(0f, 22f);
        infoRt.sizeDelta = new Vector2(320f, 110f);

        TextMeshProUGUI infoTmp = infoObj.AddComponent<TextMeshProUGUI>();
        infoTmp.text = $"<size=30><b><color=#FFFFFF>{title}</color></b></size>\n\n<size=14><color=#E6F0FF>{subtitle}</color></size>";
        infoTmp.alignment = TextAlignmentOptions.Center;

        // Enter Game Button (below card contents)
        GameObject enterBtn = new GameObject("EnterButton");
        enterBtn.transform.SetParent(card.transform, false);
        RectTransform ebRt = enterBtn.AddComponent<RectTransform>();
        ebRt.anchorMin = new Vector2(0.5f, 0f);
        ebRt.anchorMax = new Vector2(0.5f, 0f);
        ebRt.pivot = new Vector2(0.5f, 0f);
        ebRt.anchoredPosition = new Vector2(0f, 24f);
        ebRt.sizeDelta = new Vector2(280f, 52f);

        Image ebImg = enterBtn.AddComponent<Image>();
        ebImg.color = new Color(1.0f, 1.0f, 1.0f, 0.25f); // Glowing white translucent capsule

        Outline ebOutline = enterBtn.AddComponent<Outline>();
        ebOutline.effectColor = new Color(1.0f, 1.0f, 1.0f, 0.95f); // High-contrast glowing border
        ebOutline.effectDistance = new Vector2(2f, -2f);

        Button btn = enterBtn.AddComponent<Button>();
        ColorBlock btnCb = btn.colors;
        btnCb.normalColor = Color.white;
        btnCb.highlightedColor = new Color(1.0f, 1.0f, 1.0f, 0.45f);
        btnCb.pressedColor = new Color(0.9f, 0.95f, 1.0f, 0.70f);
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
        ebTmp.fontSize = 18;
        ebTmp.color = Color.white;
        ebTmp.alignment = TextAlignmentOptions.Center;
    }

    private void CreateBottomWidgets(Transform parent)
    {
        GameObject questObj = new GameObject("QuestBanner");
        questObj.transform.SetParent(parent, false);
        RectTransform qRt = questObj.AddComponent<RectTransform>();
        qRt.anchorMin = new Vector2(0.24f, 0f);
        qRt.anchorMax = new Vector2(0.24f, 0f);
        qRt.pivot = new Vector2(0f, 0f);
        qRt.anchoredPosition = new Vector2(0f, 20f);
        qRt.sizeDelta = new Vector2(320f, 44f);

        Image qImg = questObj.AddComponent<Image>();
        qImg.color = new Color(1.0f, 1.0f, 1.0f, 0.12f);

        Outline qOutline = questObj.AddComponent<Outline>();
        qOutline.effectColor = new Color(1.0f, 1.0f, 1.0f, 0.85f);
        qOutline.effectDistance = new Vector2(1.5f, -1.5f);

        GameObject qTxt = new GameObject("Text");
        qTxt.transform.SetParent(questObj.transform, false);
        RectTransform qTxtRt = qTxt.AddComponent<RectTransform>();
        qTxtRt.anchorMin = Vector2.zero;
        qTxtRt.anchorMax = Vector2.one;
        qTxtRt.sizeDelta = new Vector2(-20f, 0f);

        TextMeshProUGUI qTmp = qTxt.AddComponent<TextMeshProUGUI>();
        qTmp.text = "\uD83C\uDF81  Complete quests to earn exclusive rewards!  \u27E9";
        qTmp.fontSize = 12;
        qTmp.color = new Color(0.90f, 0.94f, 1.0f, 0.95f);
        qTmp.alignment = TextAlignmentOptions.Center;

        GameObject balObj = new GameObject("BalancePill");
        balObj.transform.SetParent(parent, false);
        RectTransform bRt = balObj.AddComponent<RectTransform>();
        bRt.anchorMin = new Vector2(1f, 0f);
        bRt.anchorMax = new Vector2(1f, 0f);
        bRt.pivot = new Vector2(1f, 0f);
        bRt.anchoredPosition = new Vector2(-35f, 20f);
        bRt.sizeDelta = new Vector2(190f, 44f);

        Image bImg = balObj.AddComponent<Image>();
        bImg.color = new Color(1.0f, 1.0f, 1.0f, 0.12f);

        Outline bOutline = balObj.AddComponent<Outline>();
        bOutline.effectColor = new Color(1.0f, 1.0f, 1.0f, 0.85f);
        bOutline.effectDistance = new Vector2(1.5f, -1.5f);

        GameObject bTxt = new GameObject("Text");
        bTxt.transform.SetParent(balObj.transform, false);
        RectTransform bTxtRt = bTxt.AddComponent<RectTransform>();
        bTxtRt.anchorMin = Vector2.zero;
        bTxtRt.anchorMax = Vector2.one;
        bTxtRt.sizeDelta = new Vector2(-20f, 0f);

        TextMeshProUGUI bTmp = bTxt.AddComponent<TextMeshProUGUI>();
        bTmp.text = "\u26A1  Your Balance:  \uD83D\uDFE1 250  +";
        bTmp.fontSize = 13;
        bTmp.color = Color.white;
        bTmp.alignment = TextAlignmentOptions.Center;
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
    // GAME LAUNCHERS & PANEL DISMISSAL
    // =========================================================

    /// <summary>
    /// Launches the Cocasnap game and immediately dismisses all panels from the screen.
    /// </summary>
    public void LaunchCocasnap()
    {
        Debug.Log("IntroSequenceManager: Launching Cocasnap Game!");
        CloseAllPanels();

        onEnterCocasnap?.Invoke();

        // 1. If in-scene game object assigned, enable it
        if (cocasnapInSceneRoot != null)
        {
            cocasnapInSceneRoot.SetActive(true);
            if (beerPongInSceneRoot != null) beerPongInSceneRoot.SetActive(false);
        }
        else
        {
            // Auto-detect and activate Cocasnap components in scene
            PuzzleLogic puzzle = Object.FindFirstObjectByType<PuzzleLogic>(FindObjectsInactive.Include);
            if (puzzle != null) puzzle.gameObject.SetActive(true);

            CubeResetter resetter = Object.FindFirstObjectByType<CubeResetter>(FindObjectsInactive.Include);
            if (resetter != null) resetter.gameObject.SetActive(true);
        }

        TryLoadScene(cocasnapSceneName);
    }

    /// <summary>
    /// Launches the Beer Pong game and immediately dismisses all panels from the screen.
    /// </summary>
    public void LaunchBeerPong()
    {
        Debug.Log("IntroSequenceManager: Launching Beer Pong Game!");
        CloseAllPanels();

        onEnterBeerPong?.Invoke();

        // 1. If in-scene game object assigned, enable it
        if (beerPongInSceneRoot != null)
        {
            beerPongInSceneRoot.SetActive(true);
            if (cocasnapInSceneRoot != null) cocasnapInSceneRoot.SetActive(false);
        }
        else
        {
            // Auto-detect and activate Beer Pong components in scene
            BeerPongScoreManager bpScore = Object.FindFirstObjectByType<BeerPongScoreManager>(FindObjectsInactive.Include);
            if (bpScore != null) bpScore.gameObject.SetActive(true);

            BallManager ballMgr = Object.FindFirstObjectByType<BallManager>(FindObjectsInactive.Include);
            if (ballMgr != null) ballMgr.gameObject.SetActive(true);
        }

        TryLoadScene(beerPongSceneName);
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
                UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
                return;
            }
        }
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
