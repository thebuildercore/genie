// CompanionLightController.cs
// Attach to: Companion Robot root (auto-attached by CompanionBrain).
// Adds minimal, soft, attractive lighting around the companion robot:
//   1. Core Chest Aura Light: Illuminates the robot's body, hands, and immediate space.
//   2. Soft Floor Glow Light: Gently pools light beneath the robot's feet to ground it in the scene.
//
// Designed to look visibly attractive, gentle, and organic — NEVER flashy or blinding.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CompanionLightController : MonoBehaviour
{
    public enum LightMood
    {
        Normal,     // Soft luminous warm-white / moonlight
        Happy,      // Gentle warm golden glow
        Angry,      // Soft warm ember/apricot tint
        Sad         // Cool soft lavender-blue tint
    }

    [Header("Mood & Settings")]
    public LightMood currentMood = LightMood.Normal;
    [Range(0.2f, 3.0f)]
    public float baseIntensity = 1.1f;
    [Range(0.5f, 5.0f)]
    public float auraRange = 2.0f;

    [Header("Light References (Auto-created if null)")]
    public Light auraLight;
    public Light floorLight;

    [Header("Smooth Transitions")]
    public float colorTransitionSpeed = 3.0f;
    public float intensityTransitionSpeed = 4.0f;

    // Mood color presets (soft, refined, non-flashy)
    private readonly Color colorNormal = new Color(0.94f, 0.97f, 1.00f);  // Luminous clean white/cyan
    private readonly Color colorHappy  = new Color(1.00f, 0.94f, 0.80f);  // Warm golden champagne
    private readonly Color colorAngry  = new Color(1.00f, 0.56f, 0.35f);  // Soft warm ember apricot
    private readonly Color colorSad    = new Color(0.68f, 0.78f, 1.00f);  // Soft lavender blue

    private Color targetColor;
    private float targetIntensity;
    private float currentPulse;
    private Coroutine flashCoroutine;

    private void Awake()
    {
        EnsureLightsExist();
        targetColor = colorNormal;
        targetIntensity = baseIntensity;
    }

    private void Start()
    {
        EnsureLightsExist();
    }

    public void EnsureLightsExist()
    {
        // 1. Chest / Core Aura Light
        if (auraLight == null)
        {
            Transform existing = transform.Find("Companion_AuraLight");
            if (existing != null)
            {
                auraLight = existing.GetComponent<Light>();
            }
            else
            {
                GameObject lightObj = new GameObject("Companion_AuraLight");
                lightObj.transform.SetParent(transform, false);
                lightObj.transform.localPosition = new Vector3(0f, 0.42f, 0.14f);

                auraLight = lightObj.AddComponent<Light>();
                auraLight.type = LightType.Point;
                auraLight.range = auraRange;
                auraLight.intensity = baseIntensity;
                auraLight.color = colorNormal;
                auraLight.shadows = LightShadows.None; // Maximum VR performance
            }
        }

        // 2. Soft Floor Glow Light
        if (floorLight == null)
        {
            Transform existing = transform.Find("Companion_FloorLight");
            if (existing != null)
            {
                floorLight = existing.GetComponent<Light>();
            }
            else
            {
                GameObject floorObj = new GameObject("Companion_FloorLight");
                floorObj.transform.SetParent(transform, false);
                floorObj.transform.localPosition = new Vector3(0f, 0.06f, 0f);

                floorLight = floorObj.AddComponent<Light>();
                floorLight.type = LightType.Point;
                floorLight.range = 1.1f;
                floorLight.intensity = baseIntensity * 0.40f;
                floorLight.color = colorNormal;
                floorLight.shadows = LightShadows.None;
            }
        }
    }

    private void Update()
    {
        if (auraLight == null) return;

        // Subtle organic breathing pulse (completely smooth & calm, not flashy)
        currentPulse = Mathf.Sin(Time.time * 1.6f) * 0.08f;

        // Resolve target color based on mood
        switch (currentMood)
        {
            case LightMood.Happy:  targetColor = colorHappy;  break;
            case LightMood.Angry:  targetColor = colorAngry;  break;
            case LightMood.Sad:    targetColor = colorSad;    break;
            default:               targetColor = colorNormal; break;
        }

        // Smooth color interpolation
        auraLight.color = Color.Lerp(auraLight.color, targetColor, Time.deltaTime * colorTransitionSpeed);
        if (floorLight != null)
        {
            floorLight.color = Color.Lerp(floorLight.color, targetColor, Time.deltaTime * colorTransitionSpeed);
        }

        // Smooth intensity interpolation
        if (flashCoroutine == null)
        {
            float desiredIntensity = targetIntensity + currentPulse;
            auraLight.intensity = Mathf.Lerp(auraLight.intensity, desiredIntensity, Time.deltaTime * intensityTransitionSpeed);
            if (floorLight != null)
            {
                floorLight.intensity = Mathf.Lerp(floorLight.intensity, desiredIntensity * 0.40f, Time.deltaTime * intensityTransitionSpeed);
            }
        }
    }

    // =========================================================================
    //  Public Mood & Trigger API
    // =========================================================================

    public void SetMood(LightMood mood)
    {
        currentMood = mood;
    }

    public void SetMoodTemporary(LightMood mood, float duration, LightMood returnMood = LightMood.Normal)
    {
        StartCoroutine(TemporaryMoodRoutine(mood, duration, returnMood));
    }

    private IEnumerator TemporaryMoodRoutine(LightMood mood, float duration, LightMood returnMood)
    {
        currentMood = mood;
        yield return new WaitForSeconds(duration);
        currentMood = returnMood;
    }

    /// <summary>
    /// Soft, brief brightening for high-fives or celebrations (never strobing).
    /// </summary>
    public void FlashLight(float peakIntensity = 1.5f, float duration = 0.35f)
    {
        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(FlashRoutine(peakIntensity, duration));
    }

    private IEnumerator FlashRoutine(float peak, float dur)
    {
        if (auraLight == null) yield break;
        float startInt = auraLight.intensity;
        float half = dur * 0.5f;

        for (float t = 0; t < half; t += Time.deltaTime)
        {
            auraLight.intensity = Mathf.Lerp(startInt, peak, t / half);
            yield return null;
        }
        for (float t = 0; t < half; t += Time.deltaTime)
        {
            auraLight.intensity = Mathf.Lerp(peak, baseIntensity, t / half);
            yield return null;
        }

        flashCoroutine = null;
    }
}
