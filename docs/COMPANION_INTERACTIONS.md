# Kinexus MR — Companion Interactions Guide

This document describes the interactive behaviors, architecture, Meta Quest 3 hand tracking integration, and PC testing controls for the Kinexus robot companion.

All interaction modules are modularly located in [`Assets/CoreLogic/Companion/`](file:///d:/kinexus/Genie/Assets/CoreLogic/Companion/) and decoupled from external systems via [`GameEventManager.cs`](file:///d:/kinexus/Genie/Assets/SharedInterfaces/GameEventManager.cs).

---

## 1. Architectural Overview

```
                        ┌──────────────────────────────────────────────┐
                        │        Meta Quest 3 / XR Engine              │
                        │  (Hand Tracking, Poses, Grab, Controllers)   │
                        └──────────────────────┬───────────────────────┘
                                               │
                                               ▼
                        ┌──────────────────────────────────────────────┐
                        │      Assets/SharedInterfaces/                │
                        │        GameEventManager.cs                   │
                        │  • TriggerHighFive(Vector3 handPos)          │
                        │  • PokeCompanion()                           │
                        │  • OnObjectGrabbedEvent / ReleasedEvent      │
                        │  • OnToyThrown(...) / OnCompanionPetted()    │
                        └───────────┬───────────────────┬──────────────┘
                                    │                   │
            ┌───────────────────────┴───┐               │
            ▼                           ▼               ▼
┌───────────────────────┐   ┌───────────────────────┐   ┌────────────────────────┐
│ CompanionHighFive.cs  │   │ CompanionPoke...cs    │   │ CompanionDizzyShake.cs │
│ • High-five walk & jump│   │ • 3-stage poke logic  │   │ • Velocity detection   │
│ • Smack sound & chirp │   │ • Ouch/Giggle/Retreat │   │ • Wobble & recover     │
│ • Spark visual FX     │   │ • Escape step physics │   │ • Dizzy sound loop     │
└───────────┬───────────┘   └───────────┬───────────┘   └───────────┬────────────┘
            │                           │                           │
            └───────────────────────────┼───────────────────────────┘
                                        ▼
                        ┌──────────────────────────────────────────────┐
                        │        Assets/CoreLogic/Scripts/             │
                        │          CompanionBrain.cs                   │
                        │  • NavMesh & Kinematic locomotion            │
                        │  • Toy fetch & return choreography           │
                        │  • Auto-attaches modules at runtime          │
                        └──────────────────────────────────────────────┘
```

---

## 2. Interaction Details

### 1. The High-Five
- **Script**: [`Assets/CoreLogic/Companion/Scripts/CompanionHighFive.cs`](file:///d:/kinexus/Genie/Assets/CoreLogic/Companion/Scripts/CompanionHighFive.cs)
- **Audio Assets**:
  - [`Assets/CoreLogic/Companion/Audio/highfive_smack.wav`](file:///d:/kinexus/Genie/Assets/CoreLogic/Companion/Audio/highfive_smack.wav)
  - [`Assets/CoreLogic/Companion/Audio/robot_happy_chirp.wav`](file:///d:/kinexus/Genie/Assets/CoreLogic/Companion/Audio/robot_happy_chirp.wav)
- **Behavior Sequence**:
  1. **Hand Flat Detection**: The player raises their palm facing forward/flat in mid-air.
  2. **Approach**: Companion calculates a stopping point ~0.5m in front of the hand, turns smoothly, and walks over.
  3. **Anticipation**: Crouches with an elastic squash deformation for 0.18s.
  4. **Aerial Jump**: Springs into the air along a parabolic trajectory reaching the height of the player's palm.
  5. **The Smack Impact**:
     - At the apex, companion makes contact with the palm.
     - Plays `highfive_smack.wav` (crisp, high-transient smack).
     - Instantly spawns a golden/cyan star spark burst (`HighFive_Spark_FX`).
     - Plays `robot_happy_chirp.wav` (ascending dual-tone happy chirp).
  6. **Landing & Celebration**: Lands with a cushioned squash, performs a quick celebratory wiggle, awards **+25 points**, and returns to `Idle`.
- **Meta Interaction SDK Hook**:
  - Wire Meta's `ActiveStateUnityEventWrapper` (or pose recognizer) to `CompanionHighFive.OnHandFlatDetected(Vector3)` or `GameEventManager.OnHandFlatDetected()`.
- **PC Test Key**: <kbd>H</kbd>

---

### 2. The Escalating Poke
- **Script**: [`Assets/CoreLogic/Companion/Scripts/CompanionPokeInteraction.cs`](file:///d:/kinexus/Genie/Assets/CoreLogic/Companion/Scripts/CompanionPokeInteraction.cs)
- **Audio Assets**:
  - [`Assets/CoreLogic/Companion/Audio/robot_ouch.wav`](file:///d:/kinexus/Genie/Assets/CoreLogic/Companion/Audio/robot_ouch.wav)
  - [`Assets/CoreLogic/Companion/Audio/robot_giggle.wav`](file:///d:/kinexus/Genie/Assets/CoreLogic/Companion/Audio/robot_giggle.wav)
  - [`Assets/CoreLogic/Companion/Audio/robot_annoyed.wav`](file:///d:/kinexus/Genie/Assets/CoreLogic/Companion/Audio/robot_annoyed.wav)
- **Behavior Sequence**:
  - **Poke #1**: Quick startled flinch backwards, accompanied by `robot_ouch.wav`.
  - **Poke #2**: Playful side-to-side wobble and vertical bounce with `robot_giggle.wav`.
  - **Poke #3**: Annoyed "Huh/Hmph!" sound (`robot_annoyed.wav`), arms-crossed pout, and a physical **retreat step backward (~0.85m)** to escape the player's reach.
  - **Cooldown**: If unprovoked for 4.5 seconds, the escalation counter resets back to 0.
- **Meta Interaction SDK Hook**:
  - Hand poke contact triggers the companion's collider, calling `CompanionPokeInteraction.RegisterPoke()`.
- **PC Test Key**: <kbd>P</kbd> (or left-click on the companion).

---

### 3. The Dizzy Shake
- **Script**: [`Assets/CoreLogic/Companion/Scripts/CompanionDizzyShake.cs`](file:///d:/kinexus/Genie/Assets/CoreLogic/Companion/Scripts/CompanionDizzyShake.cs)
- **Audio Asset**:
  - [`Assets/CoreLogic/Companion/Audio/robot_dizzy.wav`](file:///d:/kinexus/Genie/Assets/CoreLogic/Companion/Audio/robot_dizzy.wav)
- **Behavior Sequence**:
  1. **Grab & Velocity Tracking**: When the player picks up the robot using Meta XR Grab Hands, the script measures linear and angular velocity vectors.
  2. **High-Velocity Detection**: If shaken rapidly (velocity threshold $> 2.2$ m/s or angular speed $> 360^\circ$/s), the dizzy flag is set.
  3. **Placement on Floor**: Upon release onto the floor:
     - Plays `robot_dizzy.wav`.
     - Executes procedural spiral precession wobble (unbalanced tilt that slowly dampens).
     - Rapid horizontal head-shake to clear its eyes/vision.
     - Regains balance after ~3.5 seconds and returns to `Idle`.
- **Meta Interaction SDK Hook**:
  - Listens to `GameEventManager.OnObjectGrabbedEvent` and `GameEventManager.OnObjectReleasedEvent`.
- **PC Test Key**: <kbd>D</kbd> (simulates a violent shake and release).

---

### 4. Toy Fetch & Petting
- **Script**: [`Assets/CoreLogic/Scripts/CompanionBrain.cs`](file:///d:/kinexus/Genie/Assets/CoreLogic/Scripts/CompanionBrain.cs)
- **Behavior Sequence**:
  1. Player throws a toy ball into the arena (<kbd>T</kbd>).
  2. Robot detects the target position, navigates across the floor (via `NavMeshAgent` or kinematic translation fallback).
  3. Reaches ball, snaps it to hand bone, and carries it back to the player.
  4. Drops ball at player's feet and transitions to `WaitingForReward`.
  5. Player pets the robot with hand (<kbd>Space</kbd>): awards **+50 points**, despawns the toy, and returns robot to `Idle`.
- **PC Test Keys**: <kbd>T</kbd> (Throw toy) &rarr; <kbd>Space</kbd> (Pet).

---

## 3. Controls & Testing Matrix

| Interaction | Quest 3 (Hand Tracking / XR) | PC Tester (`DebugPCTester.cs`) |
| :--- | :--- | :--- |
| **High-Five** | Hold hand flat in air (Pose Recognition) | Press <kbd>H</kbd> |
| **Escalating Poke** | Index finger poke on companion | Press <kbd>P</kbd> / Click companion |
| **The Dizzy Shake** | Grab and shake robot rapidly, drop on floor | Press <kbd>D</kbd> |
| **Toy Fetch** | Grab and throw toy ball | Press <kbd>T</kbd> |
| **Pet Companion** | Physical hand stroke on robot | Press <kbd>Space</kbd> |
| **Continue / Enter Arena** | Finger poke / pinch button | Press <kbd>E</kbd> or <kbd>Return</kbd> |

---

## 4. Audio Specifications

All companion audio files were procedurally synthesized as uncompressed 44.1 kHz, 16-bit Mono PCM `.wav` files located in [`Assets/CoreLogic/Companion/Audio/`](file:///d:/kinexus/Genie/Assets/CoreLogic/Companion/Audio/):

| File | Duration | Pitch / Harmonic Profile | Usage |
| :--- | :--- | :--- | :--- |
| `highfive_smack.wav` | 350 ms | Transient noise attack + 180Hz drop + 1450Hz sparkle ring | High-five palm impact |
| `robot_happy_chirp.wav` | 550 ms | 660Hz $\rightarrow$ 880Hz $\rightarrow$ 1480Hz ascending dual-tone with vibrato | High-five success celebration |
| `robot_ouch.wav` | 300 ms | 520Hz $\rightarrow$ 340Hz frequency glide with decay | Poke 1 startled reaction |
| `robot_giggle.wav` | 600 ms | 780Hz/920Hz dual-burst giggle pattern | Poke 2 playful reaction |
| `robot_annoyed.wav` | 450 ms | 310Hz $\rightarrow$ 210Hz descending grumpy pulse | Poke 3 retreat reaction |
| `robot_dizzy.wav` | 1250 ms | 480Hz undulating sine with 6Hz tremolo & FM detune | Dizzy shake wobble |
