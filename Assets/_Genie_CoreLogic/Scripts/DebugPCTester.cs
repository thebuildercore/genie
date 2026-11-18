using UnityEngine;

public class DebugPCTester : MonoBehaviour
{
    public GameEventManager gameManager;
    public IntroSequenceManager introManager;
    public CompanionDizzyShake dizzyModule;
    public CompanionHighFive highFiveModule;
    public CompanionBlockHelper blockHelper;

    [Header("Beer Pong Robot")]
    [Tooltip("CompanionBeerPong robot in the BeerPong scene. Auto-found if null.")]
    public CompanionBeerPong beerPongBot;

    [Header("Camera & Testing Perspective")]
    [Tooltip("If true, automatically adjusts the camera position up and back in PC mode to frame the table and robot perfectly.")]
    public bool autoFrameCameraForPC = true;
    public Vector3 pcCameraPosition = new Vector3(0.1f, 1.15f, -0.65f);
    public Vector3 pcCameraRotation = new Vector3(18f, 0f, 0f);

    [Header("Intro Flow")]
    [Tooltip("If true, skips the intro card and opens straight into the game.")]
    public bool skipIntroStraightToGame = false;
    
#if UNITY_EDITOR || UNITY_STANDALONE
    void Start()
    {
        if (gameManager == null)
        {
            gameManager = Object.FindFirstObjectByType<GameEventManager>();
        }

        if (introManager == null)
        {
            introManager = Object.FindFirstObjectByType<IntroSequenceManager>();
        }

        if (dizzyModule == null)
        {
            dizzyModule = Object.FindFirstObjectByType<CompanionDizzyShake>();
        }

        if (highFiveModule == null)
        {
            highFiveModule = Object.FindFirstObjectByType<CompanionHighFive>();
        }

        if (blockHelper == null)
        {
            blockHelper = Object.FindFirstObjectByType<CompanionBlockHelper>();
        }

        if (beerPongBot == null)
        {
            beerPongBot = Object.FindFirstObjectByType<CompanionBeerPong>();
        }

        // Adjust camera position in PC editor so table and robot are framed cleanly
        if (autoFrameCameraForPC && Application.isEditor)
        {
            AdjustCameraForPC();
        }

        if (skipIntroStraightToGame && introManager != null)
        {
            if (introManager.enterArenaPanel != null) introManager.enterArenaPanel.SetActive(false);
            if (introManager.gameSelectionPanel != null) introManager.gameSelectionPanel.SetActive(false);
            if (introManager.subtitlePanel != null) introManager.subtitlePanel.SetActive(false);
        }
    }

    private void AdjustCameraForPC()
    {
        Camera cam = Camera.main;
        if (cam != null)
        {
            Transform targetTr = cam.transform;
            // Check if inside OVRCameraRig or TrackingSpace
            if (targetTr.parent != null && targetTr.parent.name.Contains("TrackingSpace"))
            {
                targetTr = targetTr.parent.parent; // OVRCameraRig root
            }
            else if (targetTr.parent != null)
            {
                targetTr = targetTr.parent;
            }

            targetTr.position = pcCameraPosition;
            targetTr.eulerAngles = pcCameraRotation;
        }
    }

    void Update()
    {
        // 0. Close active panel (Escape or X key)
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.X))
        {
            if (introManager != null)
            {
                if (introManager.gameSelectionPanel != null && introManager.gameSelectionPanel.activeSelf)
                {
                    introManager.CloseGameSelectionPanel();
                }
                else if (introManager.enterArenaPanel != null && introManager.enterArenaPanel.activeSelf)
                {
                    introManager.CloseWelcomePanel();
                }
            }
        }

        // 1. Enter Arena / Continue (E or Enter key)
        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return))
        {
            Debug.Log("DEBUG: Simulating Enter Arena / Continue click");
            if (introManager != null)
            {
                introManager.TriggerEnterArena();
            }
            else if (gameManager != null)
            {
                gameManager.TriggerEnterArena();
            }
        }

        // 1b. Direct Game Selection simulation (C for Cocasnap, V for Beer Pong)
        if (Input.GetKeyDown(KeyCode.C))
        {
            Debug.Log("DEBUG: Simulating Enter Cocasnap click (C)");
            if (introManager != null)
            {
                introManager.LaunchCocasnap();
            }
        }

        if (Input.GetKeyDown(KeyCode.V))
        {
            Debug.Log("DEBUG: Simulating Enter Beer Pong click (V)");
            if (introManager != null)
            {
                introManager.LaunchBeerPong();
            }
        }

        // 2. Petting simulation (Space key)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("DEBUG: Simulating Hand Pet");
            if (gameManager != null)
            {
                gameManager.OnCompanionPetted();
            }
        }

        // 3. Escalating Poke simulation (P key)
        if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log("DEBUG: Simulating Companion Poke (P)");
            if (gameManager != null)
            {
                gameManager.PokeCompanion();
            }
        }

        // 4. Dizzy Shake simulation (D key)
        if (Input.GetKeyDown(KeyCode.D))
        {
            Debug.Log("DEBUG: Simulating Dizzy Shake (D)");
            if (dizzyModule == null)
            {
                dizzyModule = Object.FindFirstObjectByType<CompanionDizzyShake>();
            }

            if (dizzyModule != null)
            {
                dizzyModule.SimulateDizzyShake();
            }
        }

        // 5. High-Five simulation (H key)
        if (Input.GetKeyDown(KeyCode.H))
        {
            Debug.Log("DEBUG: Simulating Flat Hand High-Five (H)");
            if (highFiveModule == null)
            {
                highFiveModule = Object.FindFirstObjectByType<CompanionHighFive>();
            }

            if (highFiveModule != null)
            {
                highFiveModule.SimulateHighFive();
            }
            else if (gameManager != null)
            {
                gameManager.OnHandFlatDetected();
            }
        }

        // 6. Companion Puzzle Block Help (B or T key)
        // (Replaces old sphere toy ball with core puzzle assistance)
        if (Input.GetKeyDown(KeyCode.B) || Input.GetKeyDown(KeyCode.T))
        {
            Debug.Log("DEBUG: Simulating Companion Block Help (B / T)");
            if (blockHelper == null)
            {
                blockHelper = Object.FindFirstObjectByType<CompanionBlockHelper>();
            }

            if (blockHelper != null)
            {
                blockHelper.HelpWithNearestBlock();
            }
            else if (gameManager != null)
            {
                gameManager.RequestBlockHelp();
            }
        }

        // 7. Special Interaction 1: Height Too High ("AWW Sad" regret reaction) (1 key)
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
        {
            Debug.Log("DEBUG: Simulating Block Height Too High / AWW Sad (Key 1)");
            if (blockHelper == null)
            {
                blockHelper = Object.FindFirstObjectByType<CompanionBlockHelper>();
            }

            if (blockHelper != null)
            {
                blockHelper.DemonstrateHeightTooHigh();
            }
            else if (gameManager != null)
            {
                gameManager.RequestBlockDemo(true, false);
            }
        }

        // 8. Special Interaction 2: Block Too Big / Heavy (Key 2)
        if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
        {
            Debug.Log("DEBUG: Simulating Block Too Big / Tremble & Fall (Key 2)");
            if (blockHelper == null)
            {
                blockHelper = Object.FindFirstObjectByType<CompanionBlockHelper>();
            }

            if (blockHelper != null)
            {
                blockHelper.DemonstrateBlockTooBig();
            }
            else if (gameManager != null)
            {
                gameManager.RequestBlockDemo(false, true);
            }
        }

        // --- Beer Pong Robot Debug Keys ---
        // F = Force robot to fetch ball
        if (Input.GetKeyDown(KeyCode.F))
        {
            Debug.Log("DEBUG: BeerPong Robot - Force Fetch (F)");
            if (beerPongBot == null) beerPongBot = Object.FindFirstObjectByType<CompanionBeerPong>();
            beerPongBot?.DEBUG_ForceFetch();
        }

        // G = Force robot Hurt/Angry
        if (Input.GetKeyDown(KeyCode.G))
        {
            Debug.Log("DEBUG: BeerPong Robot - Force Hurt (G)");
            if (beerPongBot == null) beerPongBot = Object.FindFirstObjectByType<CompanionBeerPong>();
            beerPongBot?.DEBUG_ForceHurt();
        }

        // J = Force robot Calm Down
        if (Input.GetKeyDown(KeyCode.J))
        {
            Debug.Log("DEBUG: BeerPong Robot - Force Calm Down (J)");
            if (beerPongBot == null) beerPongBot = Object.FindFirstObjectByType<CompanionBeerPong>();
            beerPongBot?.DEBUG_ForceCalmDown();
        }

        // K = Force robot Offer Ball
        if (Input.GetKeyDown(KeyCode.K))
        {
            Debug.Log("DEBUG: BeerPong Robot - Force Offer Ball (K)");
            if (beerPongBot == null) beerPongBot = Object.FindFirstObjectByType<CompanionBeerPong>();
            beerPongBot?.DEBUG_ForceOffer();
        }
    }
#endif
}
