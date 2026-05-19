# Medivoria

Medieval fantasy 2D **Tile Connect (Onet / Pao Pao)** puzzle game built with **Unity 6**.

## How to play
- Find pairs of matching icons
- Connect them with a path of **≤ 2 turns**
- Clear all pairs before time runs out
- Levels 5+ add tile gravity (top / bottom)

## Controls (New Input System)
| Input | Action |
|-------|--------|
| Mouse / touch | Select tile |
| **H** | Hint |
| **R** | Refresh (shuffle) |
| **P** / **Esc** | Pause |
| **M** | Mute |

Asset: `Assets/Project/Resources/MedivoriaGame.inputactions`  
Runtime: `GameInputController`

## Course criteria (summary)

| Topic | Implementation |
|-------|----------------|
| **Save (JSON)** | `GameRunPersistence` → `medivoria_run.json` |
| **Input System** | `MedivoriaGame.inputactions` + `GameInputController` |
| **Patterns** | **Observer** `GameEvents` / `GameAudioFeedback`; **Singleton** managers |
| **Unity Feature: VFX & Postprocessing** | `TileSandBurstEffect` (match VFX); `GameScenePresentation` (URP Vignette, Color Adjustments, Bloom + UI vignette) |
| **Clean code (SRP)** | `GameTimerPresenter`, `GameModalUiBuilder`, `GameScenePresentation` |
| **Polish / procedural UI** | `ProceduralBoardFrame`, `MedievalHudButtons`, `MedievalAtmosphere`, candle timer |

## Tech
- Unity 6, URP 2D
- C#

## Git branches
`main`, `develop`, `master` on GitHub.

## Build
1. Open **File → Build Profiles**
2. Select macOS or Windows
3. Scenes: `MainMenu`, `GameScene`
4. **Build**
