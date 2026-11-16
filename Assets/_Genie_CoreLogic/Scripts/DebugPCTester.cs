using UnityEngine;

public class DebugPCTester : MonoBehaviour
{
    public GameEventManager gameManager;
    public IntroSequenceManager introManager;
    public CompanionDizzyShake dizzyModule;
    public CompanionHighFive highFiveModule;
    public CompanionBlockHelper blockHelper;
    
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
    }

    void Update()
    {
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

        // 8. Special Interaction 2: Block Too Big / Heavy ("Legs Tremble, Drop & Fall") (2 key)
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
    }
#endif
}
