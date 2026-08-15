# CineForge Desktop 0.5.0 user guide

This guide takes a first-time user from installation to a finished local Wan video. CineForge Desktop is a native Windows application: it does not open a browser, require ComfyUI, or send generation work to a CineForge server.

## What you need

- Windows 11 x64
- A supported NVIDIA GPU and current NVIDIA driver
- At least 42 GB free for the Wan model pack, plus space for the CineForge runtime and generated videos
- A reliable internet connection during the first installation
- One source image for image-to-video generation

The v0.5.0 release was validated on an NVIDIA GeForce RTX 4070 with 12 GB VRAM. Other NVIDIA configurations may work, but generation speed and memory behavior will vary.

## 1. Install CineForge

1. Open the [CineForge Desktop v0.5.0 release](https://github.com/thebalddudeco/CineForge/releases/tag/v0.5.0).
2. Download `CineForge-Desktop-Setup-0.5.0-win-x64.exe`.
3. Run the installer.
4. Choose English, Korean, or Japanese as the initial interface language.
5. Choose the application location. The final application folder is named `CineForge`.
6. Choose a separate storage location for `CineForge Library`.
7. Leave setup open while it downloads and verifies the matching native runtime and required Wan model pack.

The installer downloads approximately 2.0 GB for the native CineForge runtime and approximately 35.6 GB for the Wan model pack. Interrupted downloads are preserved as `.partial` files. Run setup again with the same locations to resume rather than restart them.

The application folder and CineForge Library must be separate folders. Shadowframe folders and models are never reused.

## 2. Confirm that the local engine is ready

Open CineForge Desktop and look at the instrument panel across the top of the window.

- **Connected** means the private CineForge engine started successfully.
- **GPU** identifies the NVIDIA graphics processor being used.
- **VRAM** shows current graphics-memory activity.
- **Native Wan / CUDA** confirms that generation is local and does not use ComfyUI.
- The four live traces show recent GPU/runtime activity rather than a prerecorded animation.

The **Active Wan Video Model** menu is beneath **Clip Length** in the Scene Brief. CineForge selects the first compatible installed Wan model automatically.

If the model menu is empty after installation finishes, select **Refresh Models** in the upper-right corner. If it remains empty, see [Troubleshooting](#troubleshooting).

## 3. Write the Scene Brief

The Scene Brief describes what the sequence is about. Replace the example text with concrete language.

| Field | What to enter | Example |
|---|---|---|
| Project Title | A short name for this sequence | `Midnight Exchange` |
| Principal Subject | The person, creature, vehicle, or object that must remain recognizable | `a wary courier in a charcoal overcoat` |
| Primary Action | The physical action occurring in the sequence | `moves toward a marked bench, slows, and notices danger` |
| Immediate Objective | What the subject is trying to accomplish | `reach the last train and complete a covert handoff` |
| Environment | The location, weather, time, and surrounding conditions | `an aging metropolitan platform in cold midnight rain` |
| Pressure / Obstacle | What creates danger, urgency, or resistance | `an unseen observer closes in from the opposite platform` |
| Lighting | The motivated lighting and contrast | `sodium-vapor practical light, deep negative fill, wet reflections` |
| Visual Language | Camera, film, texture, color, and finishing language | `neo-noir 35 mm thriller, natural skin, restrained grain and halation` |

Specific descriptions produce a more coherent set of shot prompts than abstract phrases such as “make it cinematic.” Describe visible actions, materials, locations, and lighting.

### Clip length and model

Keep **5 Seconds** selected for v0.5.0. The current release uses its validated five-second generation profile even though additional duration choices are visible in the menu.

Use **Active Wan Video Model** to confirm the installed Wan adapter that will generate the clip.

## 4. Lock the canonical reference

Scroll to **Lock what must not drift**. Select any of the five reference-category cards and choose the strongest source image for the shot.

Supported formats are PNG, JPEG, WebP, and BMP. A useful reference image should have:

- a clearly readable subject;
- enough detail to establish identity, clothing, surfaces, props, and environment;
- the intended color and lighting direction;
- minimal blur, compression damage, text, or watermarks;
- composition that gives the subject room to move.

After the image is imported, its local CineForge path appears in the reference panel and **Build 15-Shot Factory** becomes available.

> **v0.5.0 reference behavior:** Identity, Wardrobe, Geography, Film Look, and Props are continuity categories, but this release stores one canonical image for the generation job. Selecting another category and importing another image replaces the current canonical image; it does not add a separate fifth reference.

## 5. Build the 15-shot factory

Select **Build 15-Shot Factory** beneath the reference cards. CineForge uses the Scene Brief to create three branches:

- **Five Angles** — alternate camera positions and coverage of the same dramatic situation;
- **Five Inserts** — close details, props, hands, surfaces, and environmental evidence;
- **Story Progression** — five shots that advance the dramatic action rather than merely changing the camera.

The app moves automatically to the candidate list after planning. Use the three branch buttons above the list to switch between Angles, Inserts, and Story Progression.

Each candidate contains a shot number, title, story change, prompt summary, and its own **Generate Video** button.

## 6. Choose a candidate and generate

There is intentionally no Generate button in the Scene Brief. Generation becomes available only after the 15-shot factory exists.

1. Read candidates across all three branches.
2. Decide which candidate best advances the sequence while respecting the canonical image.
3. Select **Generate Video** on that candidate.
4. Leave CineForge open while the local Wan job runs.

CineForge sends the chosen candidate’s motion prompt, the canonical image, and the selected Wan adapter to its private local engine. The v0.5.0 validated output profile is 768 × 432 using the five-second generation path.

## 7. Read the generation monitor

The generation instrument appears over the lower-right portion of the application while a job is active.

- **Percentage and segmented progress bar** show real job progress.
- **Step** shows the current engine step and total reported steps.
- **Phase** identifies the current generation stage.
- **Elapsed** is the time already spent on the active job.
- **ETA** is the engine’s current estimate and may change as stages become faster or slower.
- **Job ID** identifies this specific local request.
- The fixed **38 × 10 dot matrix** is an active-state instrument. Its positions never move; the slow breathing opacity and restrained lime signals show that generation remains active.
- The header’s runtime traces and VRAM value continue to reflect live GPU activity.

Wan generation is computationally heavy. A progress phase can remain on screen for a while without indicating that the app has frozen. Avoid launching another GPU-intensive application during generation.

## 8. Open the finished video

When the job completes, the monitor displays **Generation Complete** and **Open Result** becomes available. Select it to open the video in the default Windows media player.

Use **Open Output Folder** in the upper-right corner at any time to open:

```text
CineForge Library/outputs/
```

The CineForge Library also contains the imported inputs, projects, models, cache, logs, and temporary working files. Uninstalling the application preserves this library and the user’s creative work.

## 9. Change the interface language

Use the three controls in the lower-left corner:

- `EN` — English
- `한` — Korean
- `日` — Japanese

The selected language is remembered for future launches. The approved language-specific fonts are packaged with the application and work offline.

## A complete first project

For a quick test, use a clear source image and this brief:

```text
Project Title: Midnight Exchange
Principal Subject: a wary courier in a charcoal overcoat carrying a leather document case
Primary Action: crosses the platform, slows near a marked bench, and recognizes danger
Immediate Objective: reach the final train and complete a covert handoff
Environment: an aging metropolitan train platform in cold midnight rain
Pressure / Obstacle: an unseen observer approaches from the opposite platform
Lighting: practical sodium-vapor light, wet reflections, deep negative fill
Visual Language: neo-noir 35 mm thriller, restrained handheld camera, natural skin, subtle grain
```

Import the reference image, build the factory, inspect all three branches, and generate one candidate. Starting with one candidate is the easiest way to confirm that the model, output location, and GPU runtime are functioning before planning a larger sequence.

## Troubleshooting

### No installed Wan model appears

1. Confirm the installer completed rather than being closed during the model download.
2. Select **Refresh Models**.
3. Confirm that the selected CineForge Library still contains `models/CineForge-Wan-2.2-I2V-A14B-FP8/`.
4. Rerun the v0.5.0 installer using the same application and library locations. Existing verified files are retained and partial downloads resume.

### The engine reports offline or GPU unavailable

1. Close and reopen CineForge.
2. Confirm that Windows detects the NVIDIA GPU and that its driver is current.
3. Avoid moving individual files out of the installed CineForge application folder.
4. Check `CineForge Library/logs/` for the latest engine error.

### Build 15-Shot Factory is disabled

Import a canonical image in **Lock what must not drift**. The build action remains disabled until a reference has been successfully copied into the CineForge Library.

### I cannot find the Generate button

Build the 15-shot factory first. **Generate Video** appears once on every candidate card in the Angles, Inserts, and Story Progression branches.

### Generation is slow

Local Wan generation is demanding, especially on 12 GB GPUs where memory-saving execution may move model blocks between system memory and VRAM. Keep CineForge open, watch the real phase/step data, and close other GPU-heavy applications.

### The installer stops or the connection drops

Run the same installer again and choose the same folders. CineForge preserves `.partial` transfers and resumes them. Do not delete partial files unless a repeated checksum failure specifically requires a clean retry.

## Privacy and responsibility

CineForge Desktop performs generation locally. Prompts, reference media, project data, and outputs remain on the user’s PC except for the installer’s explicit downloads from GitHub and the pinned CineForge Hugging Face model repository.

CineForge Desktop does not apply application-level prompt or output moderation. Users are responsible for following applicable law and respecting consent, privacy, likeness, and intellectual-property rights.
