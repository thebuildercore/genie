// CompanionHandAnimator.cs
// Attach to: Companion Robot root (auto-attached by CompanionBrain).
// Controls procedural movement of the robot's arm and hand bones:
//   Right Arm: Bone.003.R (upper), Bone.004.R (forearm), Bone.004.R.001 (hand), Bone.005.R
//   Left Arm:  Bone.003.L (upper), Bone.004.L (forearm), Bone.004.L.001 (hand), Bone.005.L
//
// Operates in LateUpdate() so procedural animations seamlessly override & blend
// on top of the Unity Animator without clipping or fighting root motion.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CompanionHandAnimator : MonoBehaviour
{
    public enum HandPose
    {
        Idle,           // Gentle breathing sway
        Walking,        // Natural opposite arm swings
        HighFivePrep,   // Right arm raised, palm open forward
        HighFiveSmack,  // Right arm slapping forward
        HoldBlock,      // Both arms forward, holding cube
        HoldBall,       // Both arms cupped forward holding ball
        Trembling,      // Both arms shaking vigorously (struggling with heavy object)
        ReachHigh,      // Both arms reaching up high sadly
        AngryShaking,   // Arms raised, elbows bent, shaking with frustration
        DizzyFlail,     // Arms spinning / flailing erratically
        PokeFlinch,     // Quick recoil / flinch
        ArmsCrossed,    // Arms folded across chest
        Wave            // Right hand waving back and forth
    }

    [Header("Current Pose")]
    public HandPose currentPose = HandPose.Idle;
    [Range(0.5f, 20f)]
    public float blendSpeed = 8.0f;

    [Header("Arm Bones (Auto-discovered if null)")]
    public Transform boneR_Upper;     // Bone.003.R
    public Transform boneR_Forearm;   // Bone.004.R
    public Transform boneR_Hand;      // Bone.004.R.001
    public Transform boneR_Fingers;   // Bone.005.R

    public Transform boneL_Upper;     // Bone.003.L
    public Transform boneL_Forearm;   // Bone.004.L
    public Transform boneL_Hand;      // Bone.004.L.001
    public Transform boneL_Fingers;   // Bone.005.L

    [Header("Walk Swing Parameters")]
    public float walkSwingAmount = 26f;
    public float walkSwingFrequency = 7.5f;

    [Header("Idle Breathe Parameters")]
    public float idleSwayAmount = 5.0f;
    public float idleBreatheFrequency = 1.8f;

    // Initial base rotations
    private Quaternion initRotR_Upper;
    private Quaternion initRotR_Forearm;
    private Quaternion initRotR_Hand;
    private Quaternion initRotR_Fingers;

    private Quaternion initRotL_Upper;
    private Quaternion initRotL_Forearm;
    private Quaternion initRotL_Hand;
    private Quaternion initRotL_Fingers;

    // Current blended offsets
    private Quaternion curOffsetR_Upper = Quaternion.identity;
    private Quaternion curOffsetR_Forearm = Quaternion.identity;
    private Quaternion curOffsetR_Hand = Quaternion.identity;

    private Quaternion curOffsetL_Upper = Quaternion.identity;
    private Quaternion curOffsetL_Forearm = Quaternion.identity;
    private Quaternion curOffsetL_Hand = Quaternion.identity;

    // Target offsets for the current frame
    private Quaternion targetOffsetR_Upper = Quaternion.identity;
    private Quaternion targetOffsetR_Forearm = Quaternion.identity;
    private Quaternion targetOffsetR_Hand = Quaternion.identity;

    private Quaternion targetOffsetL_Upper = Quaternion.identity;
    private Quaternion targetOffsetL_Forearm = Quaternion.identity;
    private Quaternion targetOffsetL_Hand = Quaternion.identity;

    private CompanionBrain brain;
    private Coroutine temporaryPoseCoroutine;

    private void Awake()
    {
        brain = GetComponent<CompanionBrain>();
        FindBones();
        CacheInitialRotations();
    }

    public void FindBones()
    {
        Transform[] all = GetComponentsInChildren<Transform>(true);
        foreach (var t in all)
        {
            string n = t.name;
            // Right arm
            if (boneR_Upper == null && (n == "Bone.003.R" || n.EndsWith(".003.R") || n.Contains("arm_upper_r"))) boneR_Upper = t;
            if (boneR_Forearm == null && (n == "Bone.004.R" || n.EndsWith(".004.R") || n.Contains("arm_lower_r"))) boneR_Forearm = t;
            if (boneR_Hand == null && (n == "Bone.004.R.001" || n.EndsWith(".004.R.001") || n.Contains("hand_r"))) boneR_Hand = t;
            if (boneR_Fingers == null && (n == "Bone.005.R" || n.EndsWith(".005.R") || n.Contains("finger_r"))) boneR_Fingers = t;

            // Left arm
            if (boneL_Upper == null && (n == "Bone.003.L" || n.EndsWith(".003.L") || n.Contains("arm_upper_l"))) boneL_Upper = t;
            if (boneL_Forearm == null && (n == "Bone.004.L" || n.EndsWith(".004.L") || n.Contains("arm_lower_l"))) boneL_Forearm = t;
            if (boneL_Hand == null && (n == "Bone.004.L.001" || n.EndsWith(".004.L.001") || n.Contains("hand_l"))) boneL_Hand = t;
            if (boneL_Fingers == null && (n == "Bone.005.L" || n.EndsWith(".005.L") || n.Contains("finger_l"))) boneL_Fingers = t;
        }

        // Expose rightHandBone to CompanionBrain if unassigned
        if (boneR_Hand != null && brain != null && brain.rightHandBone == null)
        {
            brain.rightHandBone = boneR_Hand;
        }
    }

    private void CacheInitialRotations()
    {
        if (boneR_Upper != null) initRotR_Upper = boneR_Upper.localRotation;
        if (boneR_Forearm != null) initRotR_Forearm = boneR_Forearm.localRotation;
        if (boneR_Hand != null) initRotR_Hand = boneR_Hand.localRotation;
        if (boneR_Fingers != null) initRotR_Fingers = boneR_Fingers.localRotation;

        if (boneL_Upper != null) initRotL_Upper = boneL_Upper.localRotation;
        if (boneL_Forearm != null) initRotL_Forearm = boneL_Forearm.localRotation;
        if (boneL_Hand != null) initRotL_Hand = boneL_Hand.localRotation;
        if (boneL_Fingers != null) initRotL_Fingers = boneL_Fingers.localRotation;
    }

    private void Update()
    {
        // Auto-detect walk vs idle if in default states
        if (temporaryPoseCoroutine == null)
        {
            if (brain != null && brain.currentState == CompanionBrain.CompanionState.Wandering)
            {
                currentPose = HandPose.Walking;
            }
            else if (currentPose == HandPose.Walking && (brain == null || brain.currentState == CompanionBrain.CompanionState.Idle))
            {
                currentPose = HandPose.Idle;
            }
        }

        CalculateTargetBoneOffsets();
    }

    private void LateUpdate()
    {
        // Smoothly interpolate current bone offsets towards targets
        float dt = Time.deltaTime * blendSpeed;
        curOffsetR_Upper = Quaternion.Slerp(curOffsetR_Upper, targetOffsetR_Upper, dt);
        curOffsetR_Forearm = Quaternion.Slerp(curOffsetR_Forearm, targetOffsetR_Forearm, dt);
        curOffsetR_Hand = Quaternion.Slerp(curOffsetR_Hand, targetOffsetR_Hand, dt);

        curOffsetL_Upper = Quaternion.Slerp(curOffsetL_Upper, targetOffsetL_Upper, dt);
        curOffsetL_Forearm = Quaternion.Slerp(curOffsetL_Forearm, targetOffsetL_Forearm, dt);
        curOffsetL_Hand = Quaternion.Slerp(curOffsetL_Hand, targetOffsetL_Hand, dt);

        // Apply procedural offsets on top of base rotations
        if (boneR_Upper != null) boneR_Upper.localRotation = initRotR_Upper * curOffsetR_Upper;
        if (boneR_Forearm != null) boneR_Forearm.localRotation = initRotR_Forearm * curOffsetR_Forearm;
        if (boneR_Hand != null) boneR_Hand.localRotation = initRotR_Hand * curOffsetR_Hand;

        if (boneL_Upper != null) boneL_Upper.localRotation = initRotL_Upper * curOffsetL_Upper;
        if (boneL_Forearm != null) boneL_Forearm.localRotation = initRotL_Forearm * curOffsetL_Forearm;
        if (boneL_Hand != null) boneL_Hand.localRotation = initRotL_Hand * curOffsetL_Hand;
    }

    private void CalculateTargetBoneOffsets()
    {
        float time = Time.time;

        switch (currentPose)
        {
            case HandPose.Idle:
                // Gentle breathing micro-sway
                float idleSway = Mathf.Sin(time * idleBreatheFrequency) * idleSwayAmount;
                targetOffsetR_Upper = Quaternion.Euler(idleSway, 0f, idleSway * 0.5f);
                targetOffsetR_Forearm = Quaternion.Euler(idleSway * 0.4f, 0f, 0f);
                targetOffsetR_Hand = Quaternion.identity;

                targetOffsetL_Upper = Quaternion.Euler(-idleSway, 0f, -idleSway * 0.5f);
                targetOffsetL_Forearm = Quaternion.Euler(-idleSway * 0.4f, 0f, 0f);
                targetOffsetL_Hand = Quaternion.identity;
                break;

            case HandPose.Walking:
                // Natural alternating arm swings
                float walkCycle = Mathf.Sin(time * walkSwingFrequency) * walkSwingAmount;
                targetOffsetR_Upper = Quaternion.Euler(walkCycle, 0f, walkCycle * 0.35f);
                targetOffsetR_Forearm = Quaternion.Euler(Mathf.Abs(walkCycle) * 0.4f, 0f, 0f);
                targetOffsetR_Hand = Quaternion.Euler(walkCycle * 0.2f, 0f, 0f);

                targetOffsetL_Upper = Quaternion.Euler(-walkCycle, 0f, -walkCycle * 0.35f);
                targetOffsetL_Forearm = Quaternion.Euler(Mathf.Abs(walkCycle) * 0.4f, 0f, 0f);
                targetOffsetL_Hand = Quaternion.Euler(-walkCycle * 0.2f, 0f, 0f);
                break;

            case HandPose.HighFivePrep:
                // Right arm raised, palm held open facing forward
                targetOffsetR_Upper = Quaternion.Euler(-75f, 25f, -30f);
                targetOffsetR_Forearm = Quaternion.Euler(-30f, 0f, 15f);
                targetOffsetR_Hand = Quaternion.Euler(20f, 10f, 0f);

                // Left arm stays balanced at side
                targetOffsetL_Upper = Quaternion.Euler(15f, 0f, -10f);
                targetOffsetL_Forearm = Quaternion.Euler(20f, 0f, 0f);
                targetOffsetL_Hand = Quaternion.identity;
                break;

            case HandPose.HighFiveSmack:
                // Right arm snaps forward for the high-five smack!
                targetOffsetR_Upper = Quaternion.Euler(-85f, 35f, -15f);
                targetOffsetR_Forearm = Quaternion.Euler(-10f, 0f, 5f);
                targetOffsetR_Hand = Quaternion.Euler(35f, 0f, 0f);

                targetOffsetL_Upper = Quaternion.Euler(20f, 0f, -15f);
                targetOffsetL_Forearm = Quaternion.Euler(15f, 0f, 0f);
                targetOffsetL_Hand = Quaternion.identity;
                break;

            case HandPose.HoldBlock:
                // Both arms extended forward and cupped inward to hold block
                targetOffsetR_Upper = Quaternion.Euler(-65f, 22f, -18f);
                targetOffsetR_Forearm = Quaternion.Euler(-45f, 0f, 25f);
                targetOffsetR_Hand = Quaternion.Euler(15f, 20f, 0f);

                targetOffsetL_Upper = Quaternion.Euler(-65f, -22f, 18f);
                targetOffsetL_Forearm = Quaternion.Euler(-45f, 0f, -25f);
                targetOffsetL_Hand = Quaternion.Euler(15f, -20f, 0f);
                break;

            case HandPose.HoldBall:
                // Both arms cupped gently forward presenting the ball
                targetOffsetR_Upper = Quaternion.Euler(-55f, 20f, -15f);
                targetOffsetR_Forearm = Quaternion.Euler(-35f, 0f, 20f);
                targetOffsetR_Hand = Quaternion.Euler(25f, 15f, 0f);

                targetOffsetL_Upper = Quaternion.Euler(-55f, -20f, 15f);
                targetOffsetL_Forearm = Quaternion.Euler(-35f, 0f, -20f);
                targetOffsetL_Hand = Quaternion.Euler(25f, -15f, 0f);
                break;

            case HandPose.Trembling:
                // Heavy struggle: holding pose with rapid high-frequency tremble
                float tremble = Mathf.Sin(time * 42f) * 9.5f;
                float trembleSlow = Mathf.Sin(time * 12f) * 3f;
                targetOffsetR_Upper = Quaternion.Euler(-65f + tremble, 22f + trembleSlow, -18f);
                targetOffsetR_Forearm = Quaternion.Euler(-45f + tremble * 0.8f, 0f, 25f);
                targetOffsetR_Hand = Quaternion.Euler(15f + tremble, 20f, 0f);

                targetOffsetL_Upper = Quaternion.Euler(-65f - tremble, -22f - trembleSlow, 18f);
                targetOffsetL_Forearm = Quaternion.Euler(-45f - tremble * 0.8f, 0f, -25f);
                targetOffsetL_Hand = Quaternion.Euler(15f - tremble, -20f, 0f);
                break;

            case HandPose.ReachHigh:
                // Both arms reaching high overhead sadly (height too high)
                float reachWave = Mathf.Sin(time * 3f) * 5f;
                targetOffsetR_Upper = Quaternion.Euler(-115f + reachWave, 10f, -10f);
                targetOffsetR_Forearm = Quaternion.Euler(-20f, 0f, 0f);
                targetOffsetR_Hand = Quaternion.Euler(20f, 0f, 0f);

                targetOffsetL_Upper = Quaternion.Euler(-115f - reachWave, -10f, 10f);
                targetOffsetL_Forearm = Quaternion.Euler(-20f, 0f, 0f);
                targetOffsetL_Hand = Quaternion.Euler(20f, 0f, 0f);
                break;

            case HandPose.AngryShaking:
                // Fists raised, arms bent, shaking in rage
                float angryShake = Mathf.Sin(time * 26f) * 14f;
                targetOffsetR_Upper = Quaternion.Euler(-60f + angryShake, 25f, -35f);
                targetOffsetR_Forearm = Quaternion.Euler(-70f + angryShake * 0.5f, 0f, 30f);
                targetOffsetR_Hand = Quaternion.Euler(30f + angryShake, 0f, 0f);

                targetOffsetL_Upper = Quaternion.Euler(-60f - angryShake, -25f, 35f);
                targetOffsetL_Forearm = Quaternion.Euler(-70f - angryShake * 0.5f, 0f, -30f);
                targetOffsetL_Hand = Quaternion.Euler(30f - angryShake, 0f, 0f);
                break;

            case HandPose.DizzyFlail:
                // Arms flailing around in circles dizzily
                float flailAngle = time * 14f;
                targetOffsetR_Upper = Quaternion.Euler(Mathf.Sin(flailAngle) * 50f, Mathf.Cos(flailAngle) * 40f, 30f);
                targetOffsetR_Forearm = Quaternion.Euler(-40f, 0f, 0f);
                targetOffsetR_Hand = Quaternion.Euler(Mathf.Sin(flailAngle * 2f) * 30f, 0f, 0f);

                targetOffsetL_Upper = Quaternion.Euler(Mathf.Cos(flailAngle) * 50f, -Mathf.Sin(flailAngle) * 40f, -30f);
                targetOffsetL_Forearm = Quaternion.Euler(-40f, 0f, 0f);
                targetOffsetL_Hand = Quaternion.Euler(-Mathf.Sin(flailAngle * 2f) * 30f, 0f, 0f);
                break;

            case HandPose.PokeFlinch:
                // Startled flinch: arms retract protectively
                targetOffsetR_Upper = Quaternion.Euler(25f, 25f, -25f);
                targetOffsetR_Forearm = Quaternion.Euler(-55f, 0f, 20f);
                targetOffsetR_Hand = Quaternion.Euler(20f, 0f, 0f);

                targetOffsetL_Upper = Quaternion.Euler(25f, -25f, 25f);
                targetOffsetL_Forearm = Quaternion.Euler(-55f, 0f, -20f);
                targetOffsetL_Hand = Quaternion.Euler(20f, 0f, 0f);
                break;

            case HandPose.ArmsCrossed:
                // Arms crossed over chest in cute pout
                targetOffsetR_Upper = Quaternion.Euler(-45f, 40f, -45f);
                targetOffsetR_Forearm = Quaternion.Euler(-90f, 0f, 30f);
                targetOffsetR_Hand = Quaternion.Euler(15f, 0f, 0f);

                targetOffsetL_Upper = Quaternion.Euler(-45f, -40f, 45f);
                targetOffsetL_Forearm = Quaternion.Euler(-90f, 0f, -30f);
                targetOffsetL_Hand = Quaternion.Euler(15f, 0f, 0f);
                break;

            case HandPose.Wave:
                // Right hand waves back and forth enthusiastically
                float waveAngle = Mathf.Sin(time * 10f) * 32f;
                targetOffsetR_Upper = Quaternion.Euler(-80f, 20f, -25f);
                targetOffsetR_Forearm = Quaternion.Euler(-40f, waveAngle, 20f);
                targetOffsetR_Hand = Quaternion.Euler(10f, waveAngle * 0.7f, 0f);

                targetOffsetL_Upper = Quaternion.Euler(10f, 0f, -10f);
                targetOffsetL_Forearm = Quaternion.Euler(15f, 0f, 0f);
                targetOffsetL_Hand = Quaternion.identity;
                break;
        }
    }

    // =========================================================================
    //  Public Control API
    // =========================================================================

    public void SetPose(HandPose pose)
    {
        if (temporaryPoseCoroutine != null)
        {
            StopCoroutine(temporaryPoseCoroutine);
            temporaryPoseCoroutine = null;
        }
        currentPose = pose;
    }

    public void PlayPoseTemporary(HandPose pose, float duration, HandPose returnPose = HandPose.Idle)
    {
        if (temporaryPoseCoroutine != null) StopCoroutine(temporaryPoseCoroutine);
        temporaryPoseCoroutine = StartCoroutine(TemporaryPoseRoutine(pose, duration, returnPose));
    }

    private IEnumerator TemporaryPoseRoutine(HandPose pose, float duration, HandPose returnPose)
    {
        currentPose = pose;
        yield return new WaitForSeconds(duration);
        currentPose = returnPose;
        temporaryPoseCoroutine = null;
    }

    public void PlayWave(float duration = 2.5f) => PlayPoseTemporary(HandPose.Wave, duration);
    public void PlayFlinch(float duration = 0.5f) => PlayPoseTemporary(HandPose.PokeFlinch, duration);
    public void PlayAngryShaking(float duration = 3.0f) => PlayPoseTemporary(HandPose.AngryShaking, duration);
    public void PlayDizzy(float duration = 3.0f) => PlayPoseTemporary(HandPose.DizzyFlail, duration);
    public void PlayArmsCrossed(float duration = 2.0f) => PlayPoseTemporary(HandPose.ArmsCrossed, duration);
}
