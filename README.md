# The Solaris Protocol

A first-person horror level built in Unity 6 (URP). Inspired by Stanisław Lem's *Solaris*. The player wakes alone on an abandoned research station orbiting a planet that responds to grief by materializing the dead.

## About the project

This is a student project for the Game Level Design at Dawson College.

The level focuses on atmosphere, scripted horror beats, and a single coherent narrative arc rather than open-ended gameplay. The design philosophy is that the threat is psychological rather than mechanical: the visitors cannot kill the player, but they can break them. Health and sanity are atmospheric meters that drive screen distortion and audio breakdown rather than death conditions. The player has to push through the breakdown to reach the airlock and escape.

## Story

Adam, a space scavenger, finds an abandoned research station. The station's crew is dead. As he explores, he finds personal logs (PDAs) from the dead scientists describing a planet that has been "communicating" with them — materializing dead loved ones as a form of contact, without understanding that it is destroying them.

Sophie, Adam's dead wife, appears in the bedroom. She walks, talks, and turns her head 180 degrees to stare at him. She is not real. She is the planet's attempt to reach Adam, built from his grief.

The level ends at the airlock — a sequence-input puzzle whose solution requires listening to all five PDAs and reconstructing the timeline of the station's collapse, which leads player to the final room. 
Depending on the player's current Sanoty Level, Station AI offers the player 2 options for ending his journey.

## Tech stack

- **Engine**: Unity 6 (Universal Render Pipeline)
- **Player rig**: Unity Starter Assets — First Person (Cinemachine + Input System)
- **Animation**: Humanoid avatar with Animator Controllers, IK Pass, blend tree-free locomotion
- **Post-processing**: URP Volume framework (Vignette, Chromatic Aberration, Film Grain, Lens Distortion, Color Adjustments, Bloom)
- **Audio**: AudioSource with PlayOneShot for SFX, dedicated voice source for dialogue
- **UI**: Canvas + TextMeshPro (UI and 3D variants) + UnityEvents
- **Version control**: GitHub Desktop

## Architecture

The level is built from small, single-responsibility scripts wired together via UnityEvents in the Inspector. 
Most logic lives in the wiring, not the code. This is a deliberate design choice: it makes the level designable by non-programmers and trivially debuggable by playtesting in the Editor.

### Core systems

**Player and stats**

- `PlayerStats` — tracks Health (drains on Sophie contact) and Sanity (drains on Sophie proximity + visibility). Master `DrainEnabled` switch is armed when the chase begins, so the bedroom dialogue doesn't affect stats. Exposes `DrainSanity`, `DrainHealth`, `RestoreSanity`, `RestoreHealth` for external systems (puzzle failures, scripted scares).
- `PlayerLock` — clean wrapper for disabling FirstPersonController + StarterAssetsInputs during cinematic moments.
- `CameraLookAt` — forces the player's camera to look at a target (Sophie at spawn, scripted beats). Targets `PlayerCameraRoot`, the Cinemachine follow target.

**Visitors (Sophie and the monsters)**

- `VisitorController` — drives any humanoid visitor's behavior. Walk/run toward player, wall-safety raycast prevents clipping, configurable contact damage, optional persistent chase (vanish-and-reappear when stuck or lingering too close), adaptive speed (walks by default, runs when the player sprints). Used for Sophie and the monster-visitors.
- `VisitorSpawner` — pool-based random spawner. Periodically picks an inactive visitor from the pool and repositions it at a random spawn point outside the player's immediate area. Used for the monster encounters in the later corridors.
- `VisitorStopZone` — trigger that fires on a specific GameObject's entry (Sophie hitting a doorway). Used for the bedroom door-bang sequence.
- `HeadTracker` — IK look-at + scripted 180-degree head turn with downward tilt to look directly at the player during the stare. Requires Humanoid avatar and IK Pass enabled on the Animator base layer.

**Dialogue and audio logs**

- `DialogueManager` — branched dialogue trees from `DialogueNode` ScriptableObjects. Supports skip controls (Space = skip line, Tab = fast-forward) and named completion events for wiring narrative beats.
- `AudioLogPlayer` — singleton that plays an AudioLog ScriptableObject, drives the journal UI, syncs transcript reveal to audio progress. Fires per-log start/end events (`OnLogStartedTyped`, `OnLogEndedTyped`) for systems that need to react to specific logs.
- `AudioLogTrigger` — per-PDA wrapper that exposes `Play()` and `onThisLogFinished` for narrative wiring.

**PDA collection system**

- `PDAManager` — central state for all 5 PDAs (Discovered, Listened). Auto-subscribes to AudioLogPlayer's typed events. Fires `OnStateChanged` for UI repainting.
- `PDAStatusUI` — paints 5 icon slots in the corner of the screen based on PDA state (Locked → Discovered → Listened).
- `PDAGate` — fires an event when a configurable set of PDAs have all been listened to. Used to unlock Jenkins's room after Reyes, Ahern, and Savik's PDAs are heard.

**Doors and locks**

- `SlidingDoor` — translates a door GameObject's local position by configurable direction and distance. Supports `Lock()`, `Unlock()`, `Open()`, `Close()`, `Toggle()`. Used for room doors and the sliding wall that blocks a corridor mid-level.
- `DoorShaker` — three-stage escalating bang sequence (soft thumps → harder strikes → massive impacts with dent visuals). Triggered by Sophie's pursuit beat.

**World interaction**

- `TriggerZone` — the workhorse. Configurable enter/exit events, `fireOnce`/`fireExitOnce`/`entersToSkip`/`startsArmed` flags for gating beats by player progression. Used for ambient transitions, scripted scares, room entry events.
- `Interactable` — generic "press E to interact" component. Supports both distance-based and raycast-based selection (raycast is required for clustered objects like the keypad buttons, where distance-based selection would fire all buttons at once).
- `Pickup` — single-use interactable that destroys itself on pickup. Used for the suit, keycards, and similar gating items.
- `DelayedEvent` — fires a UnityEvent after a configurable delay. Used to space scripted beats without writing custom coroutines.

**Puzzle**

- `PuzzleKeypad` + `KeypadButton` — sequence-input puzzle. Five physical buttons on a console next to the airlock. Player presses them in the order revealed by the PDAs (chronological by Mission Day). Wrong sequence resets, plays an error tone, and drains 10 sanity. Solving unlocks and opens the airlock door.

**Atmospheric effects**

- `StatVolumeDriver` — drives a URP Volume's `weight` continuously based on PlayerStats (health or sanity). At full health/sanity, weight is 0 (no effect). At zero, weight is 1 (full breakdown). Smoothly lerps in between. Drives the low-sanity vignette + chromatic aberration + lens distortion + color shift effect, and the low-health red vignette.
- `FlickerLight`, `PhoneLight`, `CorridorDarkness` — lighting effects for ambient horror.
- `SoundManager` — central audio coordinator for stingers, ambient drones, and one-shots.

### Why UnityEvent wiring instead of code references

Every system above exposes its API as a combination of small public methods and serialized UnityEvents. Wiring happens in the Inspector. This means:

- A level designer can rearrange the sequence of scripted beats without touching code.
- Each script is testable in isolation by manually invoking its public methods from a Debug button or temporary trigger.
- Debugging is visual: select any object in the Hierarchy and you can see exactly what events fire when, where they go, and what they call.

The trade-off is that complex chains can become hard to read in the Inspector (six entries on one event list). For those, named completion events from a central manager (DialogueManager's `onCompleted` callbacks per named beat) replace dense Inspector chains with named hooks.

## Gameplay features

### Bedroom segment

Sophie performs a full scripted animation chain — lying on the bed, sitting up, standing, jumping off. Branched dialogue plays through with the player able to skip or fast-forward. After dialogue, Sophie's head turns 180 degrees to face Adam with a downward tilt for a 4-second stare hold, then returns to forward. The player camera is locked to her face during this beat. Stats are not drained during this segment — the bedroom is meant to feel uncanny, not threatening yet.

### Audio log discovery

Five PDAs are placed throughout the level. Interacting with one plays its audio log, reveals its transcript in sync, and updates the PDA UI in the top-right of the screen. Each PDA fragment reveals one piece of the station's collapse. Jenkins's PDA also triggers the chase: Sophie despawns from the bedroom and reappears in front of Adam wherever he is in the station.

### The chase

Sophie spawns in front of the player, holds a standing pose for 1.5 seconds with head tracking on the player, then begins walking toward him. If the player sprints, she escalates to running. If she gets stuck behind a closed door for more than ~1.5 seconds, she vanishes and reappears in front of the player. If she lingers within 4 meters for 3 continuous seconds, she vanishes and reappears in front of the player. The persistence loop means the player cannot lose her — she is everywhere.

### The monster-visitors

After the chase begins, a second VisitorSpawner activates. Every 25–55 seconds, one of a pool of pre-placed visitor-monsters teleports to a random spawn point outside the player's immediate area and begins chasing. These visitors deal direct health damage on contact, do NOT teleport (so they CAN be lost behind a closed door), and despawn when the player escapes.

### Bars and breakdown

Health and Sanity bars sit in the bottom-left corner. They never reach zero in a "you died" sense — instead, low values trigger atmospheric effects via the URP Volume framework. Low sanity ramps in a heavy vignette, chromatic aberration, film grain, lens distortion, washed-out color, and cool white balance. Low health adds a red vignette. The effects scale continuously with the bar value, so the player sees themselves degrading in real time.

### The puzzle

A keypad next to the airlock has five buttons labeled with scientist names (REYES, AHERN, SAVIK, JENKINS, ???). The player must press them in the order the logs were recorded — Mission Day 11, 14, 15, 18, then the Birthday log last. Wrong sequence drains 10 sanity. Correct sequence unlocks the airlock.

## Setup and how to play

### Editor

1. Clone the repository.
2. Open the project in Unity 6 (URP).
3. Open the main level scene from `Assets/Scenes/`.
4. Press Play in the Editor.

### Build

1. File → Build Profiles → select Windows.
2. Build to a fresh empty folder (NOT inside the project folder).
3. Run the resulting `.exe`.

### Controls

- **WASD**: move
- **Mouse**: look
- **Left Shift**: sprint
- **Space**: skip dialogue line
- **Tab**: fast-forward dialogue
- **E**: interact (pick up PDAs, press keypad buttons, interact with the world)
- **Esc**: dismiss audio log UI

### Goal

Survive long enough to reach the airlock at the far end of the station. To unlock it, find and listen to all 5 PDAs, then enter the scientist names in chronological order at the keypad next to the airlock door.

## Architecture diagrams

The level is built around three loops:

**Narrative loop (PDAs → state → gates)**

```
Player interacts with PDA → AudioLogPlayer plays the log →
PDAManager marks Discovered (on start) and Listened (on end) →
PDAGate watches PDAManager state →
When required PDAs are Listened, PDAGate unlocks gated doors
```

**Chase loop (Sophie's pursuit)**

```
Jenkins PDA finishes → VisitorController.AppearInFrontAndChase →
Sophie spawns in front of player, pauses, begins walking →
Update() drives walk-toward-player + adaptive speed →
PersistenceLoop checks for stuck/lingering →
On stuck or linger: VanishAndReappear() →
Repositions to in front of player, resumes chase
```

**Atmosphere loop (stats → effects)**

```
PlayerStats Update() samples distance to Sophie + visibility →
Drains Health (proximity) and Sanity (proximity + LOS) →
StatVolumeDriver reads Sanity, ramps LowSanityVolume.weight →
URP Volume framework composites the effects onto the camera output
```

## Project structure

```
Assets/
├── Scenes/                  Main level scene + test scenes
├── Scripts/
│   ├── AudioLogs/           PDA + audio log system
│   ├── DialogueScripts/     DialogueManager + nodes
│   ├── Door/                SlidingDoor, DoorShaker
│   ├── Interacatables/      TriggerZone, Interactable, Pickup
│   ├── Lighting/            FlickerLight, CorridorDarkness, PhoneLight
│   ├── Managers/            GameManager, SoundManager
│   ├── Player/              PlayerStats, PlayerLock
│   ├── Puzzles/             PuzzleKeypad, KeypadButton
│   ├── Sophie/              VisitorController, VisitorSpawner
│   ├── spooky triggers/     SoundBehindPlayer, WhisperTrigger
│   ├── CameraLookAt.cs
│   ├── DelayedEvent.cs
│   ├── FaceCamera.cs
│   ├── FlashlightController.cs
│   ├── Headtracker.cs
│   ├── PerceptionHack.cs
│   ├── StatVolumeDriver.cs
│   └── VisitorSTopZone.cs
├── Prefabs/                 Reusable visitor + door + PDA prefabs
├── Settings/                URP profiles + Volume profiles
└── Art/                     Models, textures, materials
```

## Credits

**Design and scripting**: Nataliya Laptyk
**Partner**: Jonathan Reinglas

**Built on**: Unity 6, URP, Unity Starter Assets (First Person), Cinemachine, TextMeshPro
**Inspirations**: *Solaris* (Stanisław Lem, 1961), *Solaris* (Andrei Tarkovsky, 1972), *Event Horizon* (1997), *Alien: Isolation* (2014)
**Audio**: Voice work by the team. Sound effects sourced from [Freesound](https://freesound.org) under Creative Commons licenses.

## Acknowledgments

Thanks to Dawson College's Game Level Design and Scripting II course staff for the assignment framework, and to the Unity Starter Assets team for the First Person controller that this project builds on.
