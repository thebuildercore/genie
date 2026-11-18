using UnityEngine;
using TMPro;

/// <summary>
/// Ensures the Beer Pong Arcade Station is present and ready in the shared arena space.
/// Places the complete game station (table, 9 cups, ball physics, score manager) beside Cocasnap
/// so both games are reachable in the same environment, and the player can physically walk between them.
/// </summary>
public class BeerPongStationBuilder : MonoBehaviour
{
    private static BeerPongStationBuilder _instance;
    public static BeerPongStationBuilder Instance => _instance;

    [Header("Station Layout in Arena")]
    [Tooltip("World position of the Beer Pong station (Station 2, beside Cocasnap).")]
    public Vector3 stationPosition = new Vector3(1.6f, 0f, 1.4f);
    public Vector3 playingPosition = new Vector3(1.6f, 0f, 0.2f); // Player standing at throwing line

    [Header("Model Paths")]
    public string tableModelPath = "Assets/_BeerPong/Models/table_1_wooden_top_wooden_legs.glb";
    public string cupModelPath = "Assets/_BeerPong/Models/red_plastic_cup.glb";

    [Header("Runtime Generated References")]
    public GameObject stationRoot;
    public BallManager ballManager;
    public BeerPongScoreManager scoreManager;
    public CupSetManager cupSetManager;

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
        EnsureBeerPongStation();
    }

    /// <summary>
    /// Builds or finds the Beer Pong station in the scene.
    /// </summary>
    public GameObject EnsureBeerPongStation()
    {
        if (stationRoot != null) return stationRoot;

        // Check if an existing BeerPong root exists in scene
        GameObject existing = GameObject.Find("BeerPongStation");
        if (existing != null)
        {
            stationRoot = existing;
            ballManager = stationRoot.GetComponentInChildren<BallManager>();
            scoreManager = stationRoot.GetComponentInChildren<BeerPongScoreManager>();
            cupSetManager = stationRoot.GetComponentInChildren<CupSetManager>();
            ConnectCompanionBot();
            return stationRoot;
        }

        // Build new self-contained Beer Pong station
        stationRoot = new GameObject("BeerPongStation");
        stationRoot.transform.position = stationPosition;
        stationRoot.transform.rotation = Quaternion.identity;

        // 1. Table
        GameObject tableObj = BuildTable(stationRoot.transform);

        // 2. 9 Cups Formation
        GameObject cupsFormation = BuildCupFormation(tableObj.transform);

        // 3. Ping Pong Ball
        GameObject ballObj = BuildBall(stationRoot.transform);

        // 4. Arcade Scoreboard
        BuildScoreboard(tableObj.transform);

        // 5. Connect Companion Robot
        ConnectCompanionBot();

        Debug.Log($"[BeerPongStationBuilder] Beer Pong station successfully established at {stationPosition}!");
        return stationRoot;
    }

    private GameObject BuildTable(Transform parent)
    {
        GameObject table = null;

#if UNITY_EDITOR
        GameObject tablePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(tableModelPath);
        if (tablePrefab != null)
        {
            table = Instantiate(tablePrefab, parent);
            table.name = "BeerPongTable_Model";
            table.transform.localPosition = Vector3.zero;
            table.transform.localRotation = Quaternion.Euler(0f, 90f, 0f); // oriented along length
            
            // Ensure table has a top collider
            BoxCollider col = table.GetComponent<BoxCollider>();
            if (col == null)
            {
                BoxCollider tc = table.AddComponent<BoxCollider>();
                tc.center = new Vector3(0f, 0.72f, 0f);
                tc.size = new Vector3(0.9f, 0.06f, 1.8f);
            }
            return table;
        }
#endif

        // Procedural Table Fallback (clean dark wooden arcade table)
        table = new GameObject("BeerPongTable_Procedural");
        table.transform.SetParent(parent, false);

        // Top
        GameObject top = GameObject.CreatePrimitive(PrimitiveType.Cube);
        top.name = "TableTop";
        top.transform.SetParent(table.transform, false);
        top.transform.localPosition = new Vector3(0f, 0.72f, 0f);
        top.transform.localScale = new Vector3(0.85f, 0.05f, 1.75f);
        Renderer topRend = top.GetComponent<Renderer>();
        if (topRend != null)
        {
            topRend.material.color = new Color(0.12f, 0.15f, 0.20f); // Sleek slate surface
        }

        // 4 Legs
        Vector3[] legOffsets = new Vector3[]
        {
            new Vector3(-0.38f, 0.35f, -0.80f),
            new Vector3( 0.38f, 0.35f, -0.80f),
            new Vector3(-0.38f, 0.35f,  0.80f),
            new Vector3( 0.38f, 0.35f,  0.80f)
        };

        for (int i = 0; i < legOffsets.Length; i++)
        {
            GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            leg.name = $"TableLeg_{i}";
            leg.transform.SetParent(table.transform, false);
            leg.transform.localPosition = legOffsets[i];
            leg.transform.localScale = new Vector3(0.045f, 0.35f, 0.045f);
            Renderer legRend = leg.GetComponent<Renderer>();
            if (legRend != null)
            {
                legRend.material.color = new Color(0.2f, 0.22f, 0.25f);
            }
        }

        return table;
    }

    private GameObject BuildCupFormation(Transform tableTr)
    {
        GameObject formation = new GameObject("CupFormation");
        formation.transform.SetParent(tableTr, false);
        formation.transform.localPosition = new Vector3(0f, 0.75f, 0.45f); // Near far end of table

        cupSetManager = formation.AddComponent<CupSetManager>();

        // 9 Cups Triangle Formation: Row 1 = 4 cups, Row 2 = 3 cups, Row 3 = 2 cups
        float cupRadius = 0.048f;
        float spacingX = cupRadius * 2.2f;
        float spacingZ = cupRadius * 2.0f;

        int cupIndex = 1;

        // Row 1 (back row, 4 cups)
        for (int i = 0; i < 4; i++)
        {
            float x = (i - 1.5f) * spacingX;
            float z = spacingZ * 1.5f;
            CreateCup(formation.transform, $"red_plastic_cup_{cupIndex++}", new Vector3(x, 0f, z));
        }

        // Row 2 (middle row, 3 cups)
        for (int i = 0; i < 3; i++)
        {
            float x = (i - 1.0f) * spacingX;
            float z = spacingZ * 0.5f;
            CreateCup(formation.transform, $"red_plastic_cup_{cupIndex++}", new Vector3(x, 0f, z));
        }

        // Row 3 (front row, 2 cups)
        for (int i = 0; i < 2; i++)
        {
            float x = (i - 0.5f) * spacingX;
            float z = -spacingZ * 0.5f;
            CreateCup(formation.transform, $"red_plastic_cup_{cupIndex++}", new Vector3(x, 0f, z));
        }

        return formation;
    }

    private GameObject CreateCup(Transform parent, string name, Vector3 localPos)
    {
        GameObject cup = null;

#if UNITY_EDITOR
        GameObject cupPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(cupModelPath);
        if (cupPrefab != null)
        {
            cup = Instantiate(cupPrefab, parent);
            cup.name = name;
            cup.transform.localPosition = localPos;
            cup.transform.localRotation = Quaternion.identity;
        }
#endif

        if (cup == null)
        {
            // Procedural stylized red cup
            cup = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cup.name = name;
            cup.transform.SetParent(parent, false);
            cup.transform.localPosition = localPos + Vector3.up * 0.06f;
            cup.transform.localScale = new Vector3(0.085f, 0.06f, 0.085f);

            Renderer rend = cup.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material.color = new Color(0.88f, 0.12f, 0.14f); // Classic vibrant red cup
            }
        }

        // Ensure Rigidbody
        Rigidbody rb = cup.GetComponent<Rigidbody>();
        if (rb == null) rb = cup.AddComponent<Rigidbody>();
        rb.mass = 0.05f;
        rb.isKinematic = true; // Stable until hit hard

        // Scoring Trigger inside the cup rim
        GameObject triggerObj = new GameObject("ScoringTrigger");
        triggerObj.transform.SetParent(cup.transform, false);
        triggerObj.transform.localPosition = new Vector3(0f, 0.8f, 0f); // Top rim

        SphereCollider sc = triggerObj.AddComponent<SphereCollider>();
        sc.radius = 0.045f;
        sc.isTrigger = true;

        CupScoringTrigger scoring = triggerObj.AddComponent<CupScoringTrigger>();
        if (scoreManager != null)
        {
            scoring.OnScore.AddListener(scoreManager.AddPoint);
        }

        return cup;
    }

    private GameObject BuildBall(Transform parent)
    {
        GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ball.name = "BeerPongBall";
        ball.transform.SetParent(parent, false);
        Vector3 spawnPos = new Vector3(0f, 0.85f, -0.55f); // Front edge of table
        ball.transform.localPosition = spawnPos;
        ball.transform.localScale = Vector3.one * 0.042f; // 42mm ping-pong ball

        Renderer rend = ball.GetComponent<Renderer>();
        if (rend != null)
        {
            rend.material.color = Color.white;
        }

        Rigidbody rb = ball.GetComponent<Rigidbody>();
        if (rb == null) rb = ball.AddComponent<Rigidbody>();
        rb.mass = 0.0027f; // Standard 2.7g ping pong ball
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // Ball physics
        ball.AddComponent<BallBouncePhysics>();

        // Ball Manager
        ballManager = ball.AddComponent<BallManager>();

        // XRI Grabbable for VR hand tracking & controller grab
        var grab = ball.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        grab.movementType = UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable.MovementType.VelocityTracking;

        return ball;
    }

    private void BuildScoreboard(Transform tableTr)
    {
        GameObject boardObj = new GameObject("BeerPongScoreboard");
        boardObj.transform.SetParent(tableTr, false);
        boardObj.transform.localPosition = new Vector3(0f, 1.45f, 0.85f); // Elevated above back of table
        boardObj.transform.localRotation = Quaternion.Euler(0f, 180f, 0f); // Facing player

        Canvas c = boardObj.AddComponent<Canvas>();
        c.renderMode = RenderMode.WorldSpace;
        c.worldCamera = Camera.main;

        RectTransform rt = boardObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(380f, 120f);
        boardObj.transform.localScale = Vector3.one * 0.002f;

        // Background
        var bg = new GameObject("BG").AddComponent<UnityEngine.UI.Image>();
        bg.transform.SetParent(boardObj.transform, false);
        bg.color = new Color(0.02f, 0.05f, 0.12f, 0.90f);
        RectTransform bgRt = bg.GetComponent<RectTransform>();
        bgRt.sizeDelta = new Vector2(380f, 120f);

        var outline = bg.gameObject.AddComponent<UnityEngine.UI.Outline>();
        outline.effectColor = new Color(1.0f, 1.0f, 1.0f, 0.95f);
        outline.effectDistance = new Vector2(2f, -2f);

        // Score Text
        GameObject txtObj = new GameObject("ScoreText");
        txtObj.transform.SetParent(boardObj.transform, false);
        TextMeshProUGUI scoreTmp = txtObj.AddComponent<TextMeshProUGUI>();
        scoreTmp.alignment = TextAlignmentOptions.Center;
        scoreTmp.fontSize = 32f;
        scoreTmp.fontStyle = FontStyles.Bold;
        scoreTmp.color = Color.white;
        scoreTmp.text = "<size=20><color=#C8E0FF>BEER PONG</color></size>\nSCORE: 0";
        RectTransform txtRt = scoreTmp.GetComponent<RectTransform>();
        txtRt.sizeDelta = new Vector2(360f, 110f);

        // Score Manager
        scoreManager = boardObj.AddComponent<BeerPongScoreManager>();

        // Private reflection injection of scoreText & ballManager
        var fieldScoreText = typeof(BeerPongScoreManager).GetField("scoreText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (fieldScoreText != null) fieldScoreText.SetValue(scoreManager, scoreTmp);

        var fieldBall = typeof(BeerPongScoreManager).GetField("ballManager", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (fieldBall != null) fieldBall.SetValue(scoreManager, ballManager);
    }

    private void ConnectCompanionBot()
    {
        CompanionBeerPong bot = Object.FindFirstObjectByType<CompanionBeerPong>();
        if (bot != null && ballManager != null)
        {
            var fieldBall = typeof(CompanionBeerPong).GetField("ballManager", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (fieldBall != null) fieldBall.SetValue(bot, ballManager);
            Debug.Log("[BeerPongStationBuilder] Connected CompanionBeerPong to BallManager!");
        }
    }
}
