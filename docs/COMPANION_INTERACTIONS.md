# Kinexus MR — Companion Interactions Guide

This document describes the interactive behaviors, architecture, Meta Quest 3 hand tracking integration, and PC testing controls for the Kinexus robot companion.

All interaction modules are modularly located in [`Assets/_Genie_CoreLogic/Companion/`](file:///d:/kinexus/Genie/Assets/_Genie_CoreLogic/Companion/) and decoupled from external systems via [`GameEventManager.cs`](file:///d:/kinexus/Genie/Assets/SharedInterfaces/GameEventManager.cs).

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
                        │     Assets/_Genie_CoreLogic/Scripts/         │
                        │          CompanionBrain.cs                   │
                        │  • NavMesh & Kinematic locomotion            │
                        │  • Toy fetch & return choreography           │
                        │  • Auto-attaches modules at runtime          │
                        └──────────────────────────────────────────────┘
```

---

## 2. Interaction Details

### 1. The High-Five
- **Script**: [`Assets/_Genie_CoreLogic/Companion/Scripts/CompanionHighFive.cs`](file:///d:/kinexus/Genie/Assets/_Genie_CoreLogic/Companion/Scripts/CompanionHighFive.cs)
- **Audio Assets**:
  - [`Assets/_Genie_CoreLogic/Companion/Audio/highfive_smack.wav`](file:///d:/kinexus/Genie/Assets/_Genie_CoreLogic/Companion/Audio/highfive_smack.wav)
  - [`Assets/_Genie_CoreLogic/Companion/Audio/robot_happy_chirp.wav`](file:///d:/kinexus/Genie/Assets/_Genie_CoreLogic/Companion/Audio/robot_happy_chirp.wav)
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
- **Script**: [`Assets/_Genie_CoreLogic/Companion/Scripts/CompanionPokeInteraction.cs`](file:///d:/kinexus/Genie/Assets/_Genie_CoreLogic/Companion/Scripts/CompanionPokeInteraction.cs)
- **Audio Assets**:
  - [`Assets/_Genie_CoreLogic/Companion/Audio/robot_ouch.wav`](file:///d:/kinexus/Genie/Assets/_Genie_CoreLogic/Companion/Audio/robot_ouch.wav)
  - [`Assets/_Genie_CoreLogic/Companion/Audio/robot_giggle.wav`](file:///d:/kinexus/Genie/Assets/_Genie_CoreLogic/Companion/Audio/robot_giggle.wav)
  - [`Assets/_Genie_CoreLogic/Companion/Audio/robot_annoyed.wav`](file:///d:/kinexus/Genie/Assets/_Genie_CoreLogic/Companion/Audio/robot_annoyed.wav)
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
- **Script**: [`Assets/_Genie_CoreLogic/Companion/Scripts/CompanionDizzyShake.cs`](file:///d:/kinexus/Genie/Assets/_Genie_CoreLogic/Companion/Scripts/CompanionDizzyShake.cs)
- **Audio Asset**:
  - [`Assets/_Genie_CoreLogic/Companion/Audio/robot_dizzy.wav`](file:///d:/kinexus/Genie/Assets/_Genie_CoreLogic/Companion/Audio/robot_dizzy.wav)
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

### 4. Puzzle Block Helping & Dynamic Reactions
- **Script**: [`Assets/_Genie_CoreLogic/Companion/Scripts/CompanionBlockHelper.cs`](file:///d:/kinexus/Genie/Assets/_Genie_CoreLogic/Companion/Scripts/CompanionBlockHelper.cs)
- **Audio Assets**:
  - [`Assets/_Genie_CoreLogic/Companion/Audio/robot_sad_aww.wav`](file:///d:/kinexus/Genie/Assets/_Genie_CoreLogic/Companion/Audio/robot_sad_aww.wav)
  - [`Assets/_Genie_CoreLogic/Companion/Audio/robot_struggle_heavy.wav`](file:///d:/kinexus/Genie/Assets/_Genie_CoreLogic/Companion/Audio/robot_struggle_heavy.wav)
  - [`Assets/_Genie_CoreLogic/Companion/Audio/robot_fall_oof.wav`](file:///d:/kinexus/Genie/Assets/_Genie_CoreLogic/Companion/Audio/robot_fall_oof.wav)
  - [`Assets/_Genie_CoreLogic/Companion/Audio/block_drop_thud.wav`](file:///d:/kinexus/Genie/Assets/_Genie_CoreLogic/Companion/Audio/block_drop_thud.wav)
- **Behaviors**:
  1. **Puzzle Block Assistance**:
     - Robot navigates across the room to the nearest unplaced puzzle block.
     - Bends down, picks it up, and carries it toward the puzzle slot or player.
  2. **Height Too High ("AWW Sad / Regret")**:
     - If the target slot or shelf height is higher than the robot's physical reach ($> 0.55$m relative to robot), it attempts to reach upward on tiptoes, realizes it is too short, and reacts with sorrow.
     - Plays loud `robot_sad_aww.wav` (high volume, cuts through background music).
     - Head and body droop down $30^\circ$, holding the block low.
     - Slowly shakes head left and right twice in regret ("I'm too short... I'm sorry!").
     - Turns toward the player, looks up pleadingly, and gently places the block down on the floor/table for the user to place.
  3. **Block Too Big / Heavy ("Leg Tremble, Drop & Fall")**:
     - The robot can only comfortably lift single standard cubes ($\le 0.35$m size).
     - When faced with an oversized or heavy multi-block, it tries to lift it.
     - **Leg Tremble**: Plays `robot_struggle_heavy.wav`; its legs and chassis jitter vigorously ($65$Hz tremor) for 1.3 seconds as it leans backward trying to hold it.
     - **Loss of Balance & Drop**: Drops the block with `block_drop_thud.wav`.
     - **Tumble & Fall**: Stumbles and falls backward onto its back/rear on the floor with comical `robot_fall_oof.wav`!
     - **Recovery**: Sits dazed for 0.85s with a slight head shake, pushes itself back up with its hands, dusts off its chassis, and returns to `Idle`.
- **PC Test Keys**:
  - <kbd>B</kbd> or <kbd>T</kbd>: Help with nearest block.
  - <kbd>1</kbd>: Trigger "Height Too High / AWW Sad" regret reaction.
  - <kbd>2</kbd>: Trigger "Block Too Big / Leg Tremble, Drop & Fall".

---

## 3. Controls & Testing Matrix

| Interaction | Quest 3 (Hand Tracking / XR) | PC Tester (`DebugPCTester.cs`) |
| :--- | :--- | :--- |
| **Puzzle Block Help** | Point / gesture towards puzzle block | Press <kbd>B</kbd> or <kbd>T</kbd> |
| **Height Too High ("AWW Sad")** | Direct robot to place block on high shelf | Press <kbd>1</kbd> |
| **Block Too Big (Tremble & Fall)** | Direct robot to lift oversized block | Press <kbd>2</kbd> |
| **High-Five** | Hold hand flat in air (Pose Recognition) | Press <kbd>H</kbd> |
| **Escalating Poke** | Index finger poke on companion | Press <kbd>P</kbd> / Click companion |
| **The Dizzy Shake** | Grab and shake robot rapidly, drop on floor | Press <kbd>D</kbd> |
| **Pet Companion** | Physical hand stroke on robot | Press <kbd>Space</kbd> |
| **Continue / Enter Arena** | Finger poke / pinch button | Press <kbd>E</kbd> or <kbd>Return</kbd> |

---

## 4. Audio Specifications

All companion audio files were procedurally synthesized as uncompressed 44.1 kHz, 16-bit Mono PCM `.wav` files located in [`Assets/_Genie_CoreLogic/Companion/Audio/`](file:///d:/kinexus/Genie/Assets/_Genie_CoreLogic/Companion/Audio/):

| File | Duration | Pitch / Harmonic Profile | Usage |
| :--- | :--- | :--- | :--- |
| `robot_sad_aww.wav` | 950 ms | 440Hz $\rightarrow$ 220Hz downward melodic whimper with breathy vibrato | Regret reaction when slot is too high |
| `robot_struggle_heavy.wav` | 850 ms | 160Hz strained servo hum with 22Hz pulsating tremolo & motor jitter | Leg trembling under heavy block |
| `robot_fall_oof.wav` | 650 ms | 750Hz gasp squeak followed by 140Hz soft body floor thud | Comedic robot tumble onto floor |
| `block_drop_thud.wav` | 400 ms | 190Hz $\rightarrow$ 60Hz solid cube impact with secondary bounce | Heavy block dropping to floor |
| `highfive_smack.wav` | 350 ms | Transient noise attack + 180Hz drop + 1450Hz sparkle ring | High-five palm impact |
| `robot_happy_chirp.wav` | 550 ms | 660Hz $\rightarrow$ 880Hz $\rightarrow$ 1480Hz ascending dual-tone with vibrato | High-five success & block delivery |
| `robot_ouch.wav` | 300 ms | 520Hz $\rightarrow$ 340Hz frequency glide with decay | Poke 1 startled reaction |
| `robot_giggle.wav` | 600 ms | 780Hz/920Hz dual-burst giggle pattern | Poke 2 playful reaction |
| `robot_annoyed.wav` | 450 ms | 310Hz $\rightarrow$ 210Hz descending grumpy pulse | Poke 3 retreat reaction |
| `robot_dizzy.wav` | 1250 ms | 480Hz undulating sine with 6Hz tremolo & FM detune | Dizzy shake wobble |
