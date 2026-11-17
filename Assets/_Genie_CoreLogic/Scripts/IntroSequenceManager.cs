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

    [Tooltip("Border / Accent tint for Kinexus neon glow aesthetic.")]
    public Color neonAccentColor = new Color(0.48f, 0.75f, 1.0f, 0.85f);

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
        if (existingCards != null && !forceRebuild) return;

        if (existingCards != null && forceRebuild)
        {
            DestroyImmediate(existingCards.gameObject);
        }

        RectTransform panelRect = enterArenaPanel.GetComponent<RectTransform>();
        if (panelRect != null)
        {
            if (panelRect.sizeDelta.x < 700 || panelRect.sizeDelta.y < 400)
            {
                panelRect.sizeDelta = new Vector2(900f, 520f);
            }
        }

        // Header
        Transform headerTr = enterArenaPanel.transform.Find("KinexusHeader");
        if (headerTr == null)
        {
            GameObject headerObj = new GameObject("KinexusHeader");
            headerObj.transform.SetParent(enterArenaPanel.transform, false);
            RectTransform headerRt = headerObj.AddComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0.5f, 1f);
            headerRt.anchorMax = new Vector2(0.5f, 1f);
            headerRt.pivot = new Vector2(0.5f, 1f);
            headerRt.anchoredPosition = new Vector2(0f, -25f);
            headerRt.sizeDelta = new Vector2(750f, 110f);

            TextMeshProUGUI headerTmp = headerObj.AddComponent<TextMeshProUGUI>();
            headerTmp.text = "<size=22><color=#A5CCFF>Welcome to</color></size>\n<size=48><b><color=#FFFFFF>Kinexus</color></b></size>\n<size=18><color=#72EFDD>Step into a new reality \u2728</color></size>";
            headerTmp.alignment = TextAlignmentOptions.Center;
        }

        // 3 Cards
        GameObject cardsContainer = new GameObject("TutorialCardsContainer");
        cardsContainer.transform.SetParent(enterArenaPanel.transform, false);
        RectTransform cardsRt = cardsContainer.AddComponent<RectTransform>();
        cardsRt.anchorMin = new Vector2(0.5f, 0.5f);
        cardsRt.anchorMax = new Vector2(0.5f, 0.5f);
        cardsRt.pivot = new Vector2(0.5f, 0.5f);
        cardsRt.anchoredPosition = new Vector2(0f, -15f);
        cardsRt.sizeDelta = new Vector2(780f, 210f);

        HorizontalLayoutGroup hlg = cardsContainer.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 24f;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;

        CreateGlassCard(cardsContainer.transform, "Card_LookExplore", "\u25CE", "Look & Explore", "Move your head\nto look around");
        CreateGlassCard(cardsContainer.transform, "Card_Interact", "\u2726", "Interact", "Pinch to select\nor tap");
        CreateGlassCard(cardsContainer.transform, "Card_Move", "\u25C8", "Move", "Point and teleport\nto explore");

        // Continue Button
        if (enterArenaButton == null)
        {
            Transform existingBtn = enterArenaPanel.transform.Find("ContinueButton");
            if (existingBtn != null)
            {
                enterArenaButton = existingBtn.GetComponent<Button>();
            }
            else
            {
                GameObject btnObj = new GameObject("ContinueButton");
                btnObj.transform.SetParent(enterArenaPanel.transform, false);
                RectTransform btnRt = btnObj.AddComponent<RectTransform>();
                btnRt.anchorMin = new Vector2(0.5f, 0f);
                btnRt.anchorMax = new Vector2(0.5f, 0f);
                btnRt.pivot = new Vector2(0.5f, 0f);
                btnRt.anchoredPosition = new Vector2(0f, 50f);
                btnRt.sizeDelta = new Vector2(230f, 52f);

                Image btnImg = btnObj.AddComponent<Image>();
                btnImg.color = new Color(0.18f, 0.38f, 0.75f, 0.75f);

                Outline btnOutline = btnObj.AddComponent<Outline>();
                btnOutline.effectColor = neonAccentColor;
                btnOutline.effectDistance = new Vector2(2f, -2f);

                enterArenaButton = btnObj.AddComponent<Button>();

                GameObject btnTxtObj = new GameObject("ButtonText");
                btnTxtObj.transform.SetParent(btnObj.transform, false);
                RectTransform txtRt = btnTxtObj.AddComponent<RectTransform>();
                txtRt.anchorMin = Vector2.zero;
                txtRt.anchorMax = Vector2.one;
                txtRt.sizeDelta = Vector2.zero;

                enterArenaButtonText = btnTxtObj.AddComponent<TextMeshProUGUI>();
                enterArenaButtonText.text = "Continue  \u2192";
                enterArenaButtonText.fontSize = 21;
                enterArenaButtonText.alignment = TextAlignmentOptions.Center;
                enterArenaButtonText.color = Color.white;
            }
        }

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
            pagerTmp.text = "<color=#7EB6FF>\u25CF</color>  <color=#406288>\u25CB  \u25CB</color>";
            pagerTmp.fontSize = 18;
            pagerTmp.alignment = TextAlignmentOptions.Center;
        }

        // Close Button (removes Welcome panel)
        Transform existingClose = enterArenaPanel.transform.Find("WelcomeCloseButton");
        if (existingClose == null)
        {
            CreateCloseButton(enterArenaPanel.transform, "WelcomeCloseButton", new Vector2(-25f, -25f), CloseWelcomePanel);
        }

        WireButton();
    }

    private void CreateGlassCard(Transform parent, string cardName, string iconSymbol, string title, string description)
    {
        GameObject cardObj = new GameObject(cardName);
        cardObj.transform.SetParent(parent, false);
        RectTransform cardRt = cardObj.AddComponent<RectTransform>();
        cardRt.sizeDelta = new Vector2(230f, 190f);

        Image cardImg = cardObj.AddComponent<Image>();
        cardImg.color = new Color(0.06f, 0.12f, 0.22f, 0.50f);

        Outline cardOutline = cardObj.AddComponent<Outline>();
        cardOutline.effectColor = new Color(0.48f, 0.75f, 1.0f, 0.60f);
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
        descTmp.color = new Color(0.72f, 0.84f, 0.95f, 0.90f);
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
        if (existingLayout != null && !forceRebuild) return;

        if (existingLayout != null && forceRebuild)
        {
            DestroyImmediate(existingLayout.gameObject);
        }

        RectTransform panelRt = gameSelectionPanel.GetComponent<RectTransform>();
        if (panelRt != null)
        {
            panelRt.sizeDelta = new Vector2(1100f, 620f);
        }

        GameObject root = new GameObject("DiscoverGamesLayout");
        root.transform.SetParent(gameSelectionPanel.transform, false);
        RectTransform rootRt = root.AddComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.sizeDelta = Vector2.zero;

        // 1. Sidebar Dock
        CreateSidebar(root.transform);

        // 2. Top Bar
        CreateTopBar(root.transform);

        // 3. Carousel
        CreateGameCarousel(root.transform);

        // 4. Bottom Widgets
        CreateBottomWidgets(root.transform);

        // Close Button (removes Discover Games panel)
        Transform existingClose = gameSelectionPanel.transform.Find("DiscoverCloseButton");
        if (existingClose == null)
        {
            CreateCloseButton(gameSelectionPanel.transform, "DiscoverCloseButton", new Vector2(-25f, -25f), CloseGameSelectionPanel);
        }
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
        sbImg.color = new Color(0.05f, 0.09f, 0.16f, 0.55f);

        Outline sbOutline = sidebar.AddComponent<Outline>();
        sbOutline.effectColor = neonAccentColor;
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
        logoTmp.text = "\u2728 <b>Kinexus</b>";
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

        CreateSidebarItem(menuObj.transform, "\uD83C\uDFE0 Home", isActive: true);
        CreateSidebarItem(menuObj.transform, "\uD83C\uDFAE Games", isActive: false);
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
        img.color = isActive ? new Color(0.20f, 0.42f, 0.85f, 0.80f) : new Color(0.08f, 0.14f, 0.24f, 0.35f);

        Outline outline = item.AddComponent<Outline>();
        outline.effectColor = isActive ? neonAccentColor : new Color(0.3f, 0.5f, 0.8f, 0.3f);
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
        tmp.color = isActive ? Color.white : new Color(0.75f, 0.85f, 0.95f, 0.80f);
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
        titleRt.sizeDelta = new Vector2(500f, 75f);

        TextMeshProUGUI titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "<size=34><b>Discover Your Next Game \u2728</b></size>\n<size=16><color=#72EFDD>Play  \u2022  Explore  \u2022  Earn</color></size>";
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
        searchImg.color = new Color(0.08f, 0.14f, 0.24f, 0.60f);

        Outline searchOutline = searchObj.AddComponent<Outline>();
        searchOutline.effectColor = neonAccentColor;
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
        sTmp.color = new Color(0.70f, 0.82f, 0.95f, 0.70f);
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
        profImg.color = new Color(0.12f, 0.20f, 0.35f, 0.70f);

        Outline profOutline = profileObj.AddComponent<Outline>();
        profOutline.effectColor = neonAccentColor;
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
        carRt.sizeDelta = new Vector2(790f, 330f);

        // 1. Left Card: Velocity Rush
        CreateShowcaseCard(carousel.transform, "Card_VelocityRush",
            badge: "\uD83D\uDD25 Trending",
            title: "Velocity Rush",
            subtitle: "Race. Drift. Win.",
            isFeatured: false,
            pos: new Vector2(-260f, 0f),
            size: new Vector2(230f, 290f),
            bgColor: new Color(0.08f, 0.12f, 0.25f, 0.65f)
        );

        // 2. Center Card (Featured): Skyward Quest
        CreateShowcaseCard(carousel.transform, "Card_SkywardQuest",
            badge: "\u2B50 New",
            title: "Skyward Quest",
            subtitle: "Embark on a journey beyond the clouds",
            isFeatured: true,
            pos: new Vector2(0f, 0f),
            size: new Vector2(260f, 320f),
            bgColor: new Color(0.10f, 0.18f, 0.35f, 0.75f)
        );

        // 3. Right Card: Pet Paradise
        CreateShowcaseCard(carousel.transform, "Card_PetParadise",
            badge: "\u2B50 Popular",
            title: "Pet Paradise",
            subtitle: "Care. Play. Grow.",
            isFeatured: false,
            pos: new Vector2(260f, 0f),
            size: new Vector2(230f, 290f),
            bgColor: new Color(0.08f, 0.14f, 0.22f, 0.65f)
        );

        // Navigation Arrows
        CreateNavArrow(carousel.transform, "\u27E8", new Vector2(-390f, 0f));
        CreateNavArrow(carousel.transform, "\u27E9", new Vector2(390f, 0f));

        // Pager Dots
        GameObject dotsObj = new GameObject("CarouselDots");
        dotsObj.transform.SetParent(carousel.transform, false);
        RectTransform dotsRt = dotsObj.AddComponent<RectTransform>();
        dotsRt.anchorMin = new Vector2(0.5f, 0f);
        dotsRt.anchorMax = new Vector2(0.5f, 0f);
        dotsRt.pivot = new Vector2(0.5f, 0f);
        dotsRt.anchoredPosition = new Vector2(0f, -22f);
        dotsRt.sizeDelta = new Vector2(200f, 20f);

        TextMeshProUGUI dotsTmp = dotsObj.AddComponent<TextMeshProUGUI>();
        dotsTmp.text = "<color=#406288>\u25CB</color>  <color=#7EB6FF>\u25CF</color>  <color=#406288>\u25CB  \u25CB</color>";
        dotsTmp.fontSize = 16;
        dotsTmp.alignment = TextAlignmentOptions.Center;
    }

    private void CreateShowcaseCard(Transform parent, string name, string badge, string title, string subtitle, bool isFeatured, Vector2 pos, Vector2 size, Color bgColor)
    {
        GameObject card = new GameObject(name);
        card.transform.SetParent(parent, false);
        RectTransform rt = card.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Image img = card.AddComponent<Image>();
        img.color = bgColor;

        Outline outline = card.AddComponent<Outline>();
        outline.effectColor = isFeatured ? new Color(0.48f, 0.78f, 1.0f, 0.95f) : new Color(0.35f, 0.55f, 0.85f, 0.50f);
        outline.effectDistance = isFeatured ? new Vector2(3f, -3f) : new Vector2(2f, -2f);

        // Badge
        GameObject badgeObj = new GameObject("Badge");
        badgeObj.transform.SetParent(card.transform, false);
        RectTransform badgeRt = badgeObj.AddComponent<RectTransform>();
        badgeRt.anchorMin = new Vector2(1f, 1f);
        badgeRt.anchorMax = new Vector2(1f, 1f);
        badgeRt.pivot = new Vector2(1f, 1f);
        badgeRt.anchoredPosition = new Vector2(-12f, -12f);
        badgeRt.sizeDelta = new Vector2(90f, 26f);

        Image bImg = badgeObj.AddComponent<Image>();
        bImg.color = new Color(0.12f, 0.18f, 0.30f, 0.80f);

        Outline bOutline = badgeObj.AddComponent<Outline>();
        bOutline.effectColor = neonAccentColor;
        bOutline.effectDistance = new Vector2(1f, -1f);

        GameObject bTxt = new GameObject("Text");
        bTxt.transform.SetParent(badgeObj.transform, false);
        RectTransform bTxtRt = bTxt.AddComponent<RectTransform>();
        bTxtRt.anchorMin = Vector2.zero;
        bTxtRt.anchorMax = Vector2.one;
        bTxtRt.sizeDelta = Vector2.zero;

        TextMeshProUGUI bTmp = bTxt.AddComponent<TextMeshProUGUI>();
        bTmp.text = badge;
        bTmp.fontSize = 12;
        bTmp.color = Color.white;
        bTmp.alignment = TextAlignmentOptions.Center;

        // Title & Subtitle
        GameObject infoObj = new GameObject("InfoBlock");
        infoObj.transform.SetParent(card.transform, false);
        RectTransform infoRt = infoObj.AddComponent<RectTransform>();
        infoRt.anchorMin = new Vector2(0f, 0f);
        infoRt.anchorMax = new Vector2(1f, 0f);
        infoRt.pivot = new Vector2(0.5f, 0f);
        infoRt.anchoredPosition = new Vector2(0f, isFeatured ? 55f : 15f);
        infoRt.sizeDelta = new Vector2(-24f, 65f);

        TextMeshProUGUI infoTmp = infoObj.AddComponent<TextMeshProUGUI>();
        infoTmp.text = $"<size={(isFeatured ? 21 : 18)}><b>{title}</b></size>\n<size=12><color=#A0BDDD>{subtitle}</color></size>";
        infoTmp.alignment = TextAlignmentOptions.BottomLeft;

        // Play Button
        GameObject playBtn = new GameObject("PlayButton");
        playBtn.transform.SetParent(card.transform, false);
        RectTransform pbRt = playBtn.AddComponent<RectTransform>();
        if (isFeatured)
        {
            pbRt.anchorMin = new Vector2(0.5f, 0f);
            pbRt.anchorMax = new Vector2(0.5f, 0f);
            pbRt.pivot = new Vector2(0.5f, 0f);
            pbRt.anchoredPosition = new Vector2(0f, 14f);
            pbRt.sizeDelta = new Vector2(130f, 36f);
        }
        else
        {
            pbRt.anchorMin = new Vector2(1f, 0f);
            pbRt.anchorMax = new Vector2(1f, 0f);
            pbRt.pivot = new Vector2(1f, 0f);
            pbRt.anchoredPosition = new Vector2(-12f, 15f);
            pbRt.sizeDelta = new Vector2(36f, 36f);
        }

        Image pbImg = playBtn.AddComponent<Image>();
        pbImg.color = new Color(0.22f, 0.44f, 0.90f, 0.85f);

        Outline pbOutline = playBtn.AddComponent<Outline>();
        pbOutline.effectColor = neonAccentColor;
        pbOutline.effectDistance = new Vector2(1.5f, -1.5f);

        playBtn.AddComponent<Button>();

        GameObject pbTxt = new GameObject("Text");
        pbTxt.transform.SetParent(playBtn.transform, false);
        RectTransform pbTxtRt = pbTxt.AddComponent<RectTransform>();
        pbTxtRt.anchorMin = Vector2.zero;
        pbTxtRt.anchorMax = Vector2.one;
        pbTxtRt.sizeDelta = Vector2.zero;

        TextMeshProUGUI pbTmp = pbTxt.AddComponent<TextMeshProUGUI>();
        pbTmp.text = isFeatured ? "Play  \u25B6" : "\u25B6";
        pbTmp.fontSize = 15;
        pbTmp.color = Color.white;
        pbTmp.alignment = TextAlignmentOptions.Center;
    }

    private void CreateNavArrow(Transform parent, string arrowSymbol, Vector2 pos)
    {
        GameObject arrowObj = new GameObject("Arrow_" + arrowSymbol);
        arrowObj.transform.SetParent(parent, false);
        RectTransform rt = arrowObj.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(36f, 36f);

        Image img = arrowObj.AddComponent<Image>();
        img.color = new Color(0.08f, 0.14f, 0.25f, 0.60f);

        Outline outline = arrowObj.AddComponent<Outline>();
        outline.effectColor = neonAccentColor;
        outline.effectDistance = new Vector2(1f, -1f);

        arrowObj.AddComponent<Button>();

        GameObject txt = new GameObject("Text");
        txt.transform.SetParent(arrowObj.transform, false);
        RectTransform txtRt = txt.AddComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.sizeDelta = Vector2.zero;

        TextMeshProUGUI tmp = txt.AddComponent<TextMeshProUGUI>();
        tmp.text = arrowSymbol;
        tmp.fontSize = 20;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
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
        qImg.color = new Color(0.08f, 0.14f, 0.25f, 0.60f);

        Outline qOutline = questObj.AddComponent<Outline>();
        qOutline.effectColor = neonAccentColor;
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
        qTmp.color = new Color(0.85f, 0.92f, 1.0f, 0.90f);
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
        bImg.color = new Color(0.08f, 0.14f, 0.25f, 0.60f);

        Outline bOutline = balObj.AddComponent<Outline>();
        bOutline.effectColor = neonAccentColor;
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
    // PANEL DISMISSAL / CLOSE BUTTONS
    // =========================================================

    /// <summary>
    /// Closes and removes the Welcome tutorial panel.
    /// </summary>
    public void CloseWelcomePanel()
    {
        Debug.Log("IntroSequenceManager: Closed Welcome panel.");
        if (enterArenaPanel != null) enterArenaPanel.SetActive(false);
        if (subtitlePanel != null) subtitlePanel.SetActive(false);
    }

    /// <summary>
    /// Closes and removes the Discover Your Next Game panel, freeing view of the arena.
    /// </summary>
    public void CloseGameSelectionPanel()
    {
        Debug.Log("IntroSequenceManager: Closed Game Selection panel.");
        if (gameSelectionPanel != null) gameSelectionPanel.SetActive(false);
        if (subtitlePanel != null) subtitlePanel.SetActive(false);
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
        rt.sizeDelta = new Vector2(38f, 38f);

        Image img = closeObj.AddComponent<Image>();
        img.color = new Color(0.12f, 0.20f, 0.35f, 0.85f);

        Outline outline = closeObj.AddComponent<Outline>();
        outline.effectColor = neonAccentColor;
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        Button btn = closeObj.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1f, 0.4f, 0.4f, 1f);
        cb.pressedColor = new Color(0.9f, 0.2f, 0.2f, 1f);
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
        tmp.fontSize = 20;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = new Color(0.95f, 0.95f, 1f, 0.95f);
        tmp.alignment = TextAlignmentOptions.Center;

        return closeObj;
    }
}
