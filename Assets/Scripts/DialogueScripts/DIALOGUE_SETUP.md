# Dialogue System Setup Guide

Five new scripts for the bedroom scene branching dialogue:

| Script | Purpose |
|---|---|
| `DialogueLine.cs` | Data: speaker name, text, audio clip |
| `DialogueChoice.cs` | Data: button text, Adam's reply, Sophie's response, next node |
| `DialogueNode.cs` | ScriptableObject: one turn of conversation |
| `DialogueManager.cs` | The controller. Plays nodes in sequence |
| `DialogueUI.cs` | The UI. Shows subtitles + 3 buttons |

## Part 1 — Install TextMeshPro (if not already)

The UI uses TextMeshPro. In Unity 6:

1. **Window → TextMeshPro → Import TMP Essential Resources**.
2. Click **Import**. Wait.

If you've already used TMP in your project, skip this.

## Part 2 — Drop in the scripts

Unzip `DialogueScripts.zip` into `Assets/Scripts/`. Wait for compilation. Console should be clean.

## Part 3 — Build the dialogue UI canvas

This is a one-time setup per scene.

### Step 1: Create the Canvas
1. **GameObject → UI → Canvas**. Name it `DialogueCanvas`.
2. Inside it, **GameObject → UI → Panel**. Name it `SubtitlePanel`. Position at the bottom: anchor to bottom-stretch, height ~140px, semi-transparent black background (color: 0,0,0, alpha 180).
3. Inside SubtitlePanel:
   - **GameObject → UI → Text - TextMeshPro**. Name it `SpeakerName`. Position at top of panel, font size 18, color white, bold.
   - **GameObject → UI → Text - TextMeshPro**. Name it `SubtitleText`. Below SpeakerName, font size 22, color white. Set text alignment to top-center.

### Step 2: Create the choices panel
1. Inside DialogueCanvas, create another **Panel**. Name it `ChoicesPanel`. Anchor to center-right (or wherever you want choices to appear). Make it ~400px wide, ~300px tall.
2. Add a **Vertical Layout Group** component to ChoicesPanel (Component → Layout → Vertical Layout Group). Set spacing to 10.
3. Inside ChoicesPanel, create three **Button - TextMeshPro** objects. Name them `Choice1`, `Choice2`, `Choice3`.
4. Each button: set height to 60, width to flex.
5. Each button has a TextMeshProUGUI child — that's the label.

### Step 3: Wire up DialogueUI script
1. Select the `DialogueCanvas`. Add Component → `Dialogue UI`.
2. Drag references in the Inspector:
   - **Subtitle Panel** → SubtitlePanel
   - **Speaker Name Text** → SpeakerName TMP
   - **Subtitle Text** → SubtitleText TMP
   - **Choices Panel** → ChoicesPanel
   - **Choice Buttons** → drag Choice1, Choice2, Choice3 (size: 3)
   - **Choice Button Labels** → drag the TMP text inside each button (size: 3)
3. **Unlock Cursor For Choices** — leave checked.

### Step 4: Set up the DialogueManager GameObject
1. Create empty GameObject named `DialogueManager`.
2. Add Component → `Dialogue Manager`.
3. Add Component → `Audio Source`. Uncheck Play On Awake. Uncheck Loop.
4. In DialogueManager Inspector:
   - **Dialogue UI** → drag DialogueCanvas
   - **Voice Source** → drag the AudioSource you just added
   - **Player Controller** → drag your `PlayerCapsule`'s `FirstPersonController` script (the one from Starter Assets)
   - **Player Look Controller** → optional, drag the `StarterAssetsInputs` if you want to disable camera too

## Part 4 — Build the dialogue tree assets

Now create one ScriptableObject per turn of conversation. We have 6 turns (5 with branches + 1 forced final).

### Step 1: Create the folder
1. In Project window, create folder `Assets/Dialogue/`.

### Step 2: Create the nodes
For each of the 6 turns:
1. Right-click in `Assets/Dialogue/` → **Create → Solaris → Dialogue Node**.
2. Name them: `Turn1_Recognition`, `Turn2_FirstCrack`, `Turn3_HowDidYouGetHere`, `Turn4_TheRadiator`, `Turn5_Unspeakable`, `Turn6_Forgiveness`.

### Step 3: Fill in Turn 1 (Recognition)
Click `Turn1_Recognition`. In the Inspector:

**Sophie Opening Lines** (size 2):
- Element 0:
  - Speaker Name: `Sophie`
  - Text: `Adam. There you are.`
  - Audio Clip: drag `sophie_01_adam_there_you_are.wav` (later)
  - Display Duration: 3
- Element 1:
  - Speaker Name: `Sophie`
  - Text: `You look like you've seen a ghost. Are you alright?`
  - Audio Clip: drag `sophie_02_seen_a_ghost.wav` (later)

**Choices** (size 3):
- Element 0 (Push back):
  - Button Text: `What are you doing here?`
  - Adam Reply: Speaker `Adam`, Text: `What are you doing here?`, Audio: `adam_t1_a.wav`
  - Sophie Response: Speaker `Sophie`, Text: `What do you mean what am I doing here? I was looking for you.`, Audio: `sophie_t1_response_a.wav`
  - Next Node: drag `Turn2_FirstCrack`
- Element 1 (Play along):
  - Button Text: `I'm fine. Just tired.`
  - Adam Reply: Speaker `Adam`, Text: `I'm fine. Just tired.`, Audio: `adam_t1_b.wav`
  - Sophie Response: Speaker `Sophie`, Text: `You don't look fine. I was looking for you.`, Audio: `sophie_t1_response_b.wav`
  - Next Node: drag `Turn2_FirstCrack`
- Element 2 (Crack):
  - Button Text: `What…`
  - Adam Reply: Speaker `Adam`, Text: `What…`, Audio: `adam_t1_c.wav`
  - Sophie Response: Speaker `Sophie`, Text: `I was looking for you. You said you'd come.`, Audio: `sophie_t1_response_c.wav`
  - Next Node: drag `Turn2_FirstCrack`

Repeat the pattern for Turns 2 through 5.

### Step 4: Turn 6 (Forgiveness — forced, no choices)
Click `Turn6_Forgiveness`. In the Inspector:

**Sophie Opening Lines** (size 5 — Adam's lines mixed in here for forced delivery):
- Element 0: Speaker `Adam`, Text: `Because you're not real. And because I'm sorry. I was always going to tell you I was sorry.`
- Element 1: Speaker `Sophie`, Text: `Sorry for what?`
- Element 2: Speaker `Sophie`, Text: `It's alright. Whatever it is. I forgive you.`
- Element 3: Speaker `Sophie`, Text: `Why don't you show me around? It's so beautiful here.`

**Choices** — size 0 (no buttons, this turn auto-ends).

**Is End Node** → check this.
**Completion Event Name** → type `BedroomDialogueComplete`. This name fires the next scene beat.

## Part 5 — Wire it to a TriggerZone

1. Create a TriggerZone at the spot in the bedroom where Sophie should "appear" (probably near the audio log on the nightstand).
2. On its **On Player Enter** event:
   - Drag the audio log object → call `Pickup.Collect` (so the log plays first, optional but recommended).
   - Drag the Visitor (Sophie's GameObject) → call `VisitorController.Appear`.
   - Drag DialogueManager → call `DialogueManager.StartDialogue` → drag `Turn1_Recognition` into the parameter slot.

## Part 6 — Hook the dialogue end to the next scene beat

In the DialogueManager Inspector, find **Named Completion Events**:
1. Set size to 1.
2. Element 0:
   - Event Name: `BedroomDialogueComplete`
   - Action: drag whatever should happen next — e.g. unlock the escape door, or trigger the wrongness/head-turn beat.

When Turn 6 finishes, the manager fires this event. The dialogue ends and the next beat begins.

## Voice Recording Plan (Real Voice)

Recording schedule for the week:

### Day 1: Record Sophie's fixed lines (~6 lines)
- Turn 1 opening (2 lines)
- Turn 2 opening
- Turn 3 opening + the "I was just here" malfunction
- Turn 4 — the radiator memory monologue
- Turn 5 opening — "Why won't you look at me?"
- Turn 6 — sorry / forgiveness (3 lines)

### Day 2: Record Adam's lines (~5 fixed)
- Turn 4 — "I never told anyone that" / "I know"
- Turn 5 — "I can't"
- Turn 6 — "Because you're not real. And because I'm sorry."
- A few interjections.

### Day 3: Record branched variants (~18 lines)
- 3 Adam responses × 5 turns = 15 short lines (some are 1–3 words like "What…")
- 3 Sophie response variants × 4 turns = 12 lines

If you run out of time recording, just use the same Sophie response for all three player choices in a given turn. Less ideal, but workable.

### Recording tips
- Use a phone in a small carpeted room (closet works perfectly).
- Voice Memos (iOS) or Easy Voice Recorder (Android).
- Record each line 3 times: warm/natural, flatter, with a tiny pause halfway.
- Save as WAV if possible (Audacity can convert).
- Name files clearly: `sophie_t1_open_01.wav`, `adam_t5_c.wav`.

### Editing
- Drop into **Audacity** (free).
- Trim silence at start/end.
- Light EQ: reduce frequencies above 6kHz on Sophie's lines for a slight "she's not really there" quality.
- Export as WAV or MP3.

## Common pitfalls

**Player can move during dialogue.** Make sure you dragged the correct FirstPersonController script into the Player Controller field. The dialogue will disable it during conversation.

**Cursor doesn't unlock to click buttons.** Check Unlock Cursor For Choices is on in DialogueUI. After dialogue ends, cursor should re-lock automatically.

**Choices appear but Sophie's audio keeps playing.** This is by design — Sophie's response audio plays before choices appear. If you want immediate choices, set the audio clip to null and rely on subtitles only.

**Audio doesn't play.** Check the AudioSource has Play On Awake unchecked, that the clip is assigned, and that the AudioSource is referenced in DialogueManager.Voice Source.
