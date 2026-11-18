# Companion Robot — All Interactions

> Simple reference of everything the companion robot can do, across both games.

---

## 🧩 Cocasnap Game

### 1. Block Help (Auto)
- Robot watches the player for **15 seconds of no activity** (no block touched, no VR movement)
- After 15s, robot walks to the **corner** of the nearest block (not the center)
- It tries to **lift and move** the block to help the player
- If the block is **too big** (scale > 0.5 on any side), the robot can't lift it and shows a reaction

### 2. Block Too Big Reaction
- Robot walks up to a large block
- Legs **tremble**, robot **falls/drops** trying to lift it
- Shows it can't do it → walks away sad

### 3. Block Height Too High Reaction
- If the block is placed too high for robot to reach
- Robot looks up, shows an **"aww sad"** reaction
- Gives up and steps back

---

## 🍺 Beer Pong Game

### 4. Ball Fetch (Auto)
- After **10 seconds** of player not doing anything
- Robot walks to the ball (corner approach, not center)
- Picks it up and **walks toward the player**
- Holds ball out — *"Here! Take it and throw! 🎯"*

### 5. Player Takes Ball from Robot
- Player grabs the ball from robot's hands
- Robot **lets go** cleanly
- Says *"Great throw! Good luck! ✨"*
- Returns to idle

### 6. Player Grabs the Robot (instead of ball)
- Player picks up the robot body itself
- Robot **protests** — *"HEY! Put me DOWN! 😤"*
- When thrown and hits something → triggers **Hurt Reaction**

### 7. Hurt Reaction (Robot hits wall / object)
- Triggered when robot collides with something hard (impact > 1.5 m/s)
- **Stars appear** orbiting the robot's head (cartoon style ⭐⭐⭐)
- Plays hurt sound
- Subtitle: *"OW! That HURT! 💫⭐"*
- Leads into **Angry Mode**

### 8. Angry Mode (lasts 30–60 seconds)
- Robot **circles around the player** slowly
- If player's hand gets too close → robot **moves away fast**
- Keeps saying angry things:
  - *"Back OFF! 😤"*
  - *"Don't come near me! 🚫"*
  - *"I am FUMING right now! 🔥"*
  - *"STAY AWAY! ⚠️"*
  - *"I'm not your toy! 🤖"*
- After 30–60 seconds, robot **calms down automatically**

### 9. Robot Calms Down
- Says *"...Fine. 😤"*
- Then *"I forgot about it. Let's keep playing! 🎳"*
- Stars disappear, returns to normal idle

---

## 🤝 Shared Interactions (Both Games)

### 10. High Five
- Player raises a **flat open hand** toward robot
- Robot raises its hand and does a high five animation
- Subtitle: *"Up top! ✋ Nice!"*
- Plays high five sound

### 11. Poke
- Player **pokes** the robot
- Robot reacts with escalating responses the more you poke:
  - 1st poke: mild surprise
  - 2nd poke: annoyed
  - 3rd+ poke: bigger reaction
- Subtitle: *"Hey! Stop poking me! 😒"*

### 12. Dizzy Shake
- Player **picks up and shakes** the robot rapidly
- Robot gets dizzy — plays dizzy animation
- Subtitle: *"Whoooa… dizzy… 🌀"*

---

## 💬 Subtitles & Audio
- All reactions show **text subtitles** above the robot's head (floating panel, auto-facing player)
- Built-in **cute robot audio clips** mapped automatically:
  - Hurt: `robot_ouch.wav` / `robot_fall_oof.wav`
  - Angry: `robot_annoyed.wav`
  - Calm / Happy: `robot_happy_chirp.wav`
  - Throw Protest: `robot_struggle_heavy.wav`
  - High Five: `highfive_smack.wav`
  - Poke: `robot_giggle.wav`
  - Fall / Drop: `block_drop_thud.wav`

---

## 🥽 Meta Quest 3 Tracking & Hand Colliders Compatibility

The companion supports **both** Quest 3 interaction modes seamlessly:
1. **Hands-Only Mode (Direct Touch / Hand Tracking)**:
   - Tracks finger joints and palm anchors (`r_index`, `l_index`, `b_r_index3`, `indextip`, `righthandanchor`)
   - Dynamically equips a physical trigger sphere collider (`radius = 0.08m`) on your index/hands
   - Proximity and direct touch/poke/high-five work without needing controllers
2. **Hands & Controllers Mode (Touch Plus Controllers)**:
   - Detects Touch Plus anchors (`RightControllerAnchor`, `RightHandOnControllerAnchor`, `RightControllerInHandAnchor`)
   - Ensures trigger colliders and distance tracking on the controller bodies
   - Grabbing the robot or ball works using standard Touch Plus grip/trigger or direct pinch

---

## 🎮 States (What robot is doing at any time)

| State | Meaning |
|---|---|
| **Idle** | Wandering around, waiting for something to happen |
| **Fetching Ball** | Walking to pick up the beer pong ball |
| **Offering Ball** | Holding ball out toward player |
| **Hurt & Angry** | Stars on head, circling player, moving away from hands |
| **Calming Down** | Transitioning from angry back to normal |

---

## 🖥️ PC Debug Keys (Editor Testing)

| Key | Action |
|---|---|
| `C` | Directly launch Cocasnap Game |
| `V` | Directly launch Beer Pong Game |
| `E` / `Enter` | Continue / Advance welcome panel |
| `Escape` / `X` | Close active panel |
| `B` / `T` | Force robot to help with nearest block (Cocasnap) |
| `F` | Force robot to fetch ball (Beer Pong) |
| `G` | Force robot Hurt / Angry reaction |
| `J` | Force robot to calm down |
| `K` | Force robot to offer ball |
| `H` | Simulate High Five |
| `P` | Simulate Poke |
| `D` | Simulate Dizzy Shake |
| `1` | Demo: Block height too high |
| `2` | Demo: Block too big / robot falls |
