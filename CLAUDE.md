# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Unity 6 thesis/diploma project ("Diplom"). Currently a fresh project template — `Assets/Scripts/` is empty and `Diplom.sln` contains no C# projects yet. The first time C# scripts are added under `Assets/`, the Unity editor regenerates `Diplom.sln` and the `Assembly-CSharp*.csproj` files; do not hand-edit those, they are derived artifacts.

- **Unity Editor**: `6000.4.7f1` (pinned in `ProjectSettings/ProjectVersion.txt` — open via Unity Hub at the matching version).
- **Render pipeline**: URP 17.4.0, configured for **2D** (`Assets/Settings/Renderer2D.asset`, `UniversalRP.asset`, `Lit2DSceneTemplate.scenetemplate`).
- **Input**: new Input System (`com.unity.inputsystem`). The starter action map is `Assets/InputSystem_Actions.inputactions` (Player + UI maps with Move/Look/Attack/Interact). Wire gameplay scripts to this asset rather than the legacy `Input` API.
- **Backend**: Unity Gaming Services Cloud Code (`com.unity.services.cloudcode`) is included — when server-side modules/scripts are added, put them in a `~`-suffixed folder (e.g. `Module~/`, mirroring the package's own samples) so Unity ignores them in the Asset Database.
- **Multiplayer**: `com.unity.multiplayer.center` is installed but no networking transport (Netcode/Mirror) has been chosen yet. Confirm the intended transport with the user before scaffolding networked code.
- **Visual Scripting**: `com.unity.visualscripting` is included, but no graphs exist yet. Default to C# scripts unless the user explicitly asks for visual scripting.

The sole scene is `Assets/Scenes/SampleScene.unity` (referenced by `EditorBuildSettings.asset`).

## Working in this project

Almost all operations are driven through the Unity Editor GUI, not the shell. Claude cannot launch the Editor itself — when an action requires the Editor (entering Play mode, baking lighting, building a player, generating .sln/.csproj from new scripts), ask the user to perform it and report back.

**Source files**: Every asset in `Assets/` has a sibling `.meta` file containing its GUID. When you create/rename/delete an asset via the filesystem (not the Editor), update or remove the matching `.meta` too — otherwise Unity reimports and reissues GUIDs, breaking scene/prefab references. Prefer letting the user do moves/renames inside the Editor.

**Do not commit / do not edit**: `Library/`, `Temp/`, `Logs/`, `UserSettings/` are local caches and per-user state. `Library/PackageCache/` contains read-only package sources — useful to read for API reference, never to modify.

**Headless builds / CI** (only if the user sets this up): the pattern is
```
& "<Unity install>\Unity.exe" -batchmode -quit -projectPath . -executeMethod <Class.Method> -logFile -
```
There is no build script in the repo today, so this requires the user to first author a static `BuildPlayer` method under `Assets/Editor/`.

**Tests**: `com.unity.test-framework` is installed but no test assemblies exist yet. Tests are run from the Editor's *Test Runner* window (Window → General → Test Runner), or headlessly via `Unity.exe -runTests -testPlatform EditMode|PlayMode -testResults <path>`. A test `.asmdef` needs `"optionalUnityReferences": ["TestAssemblies"]` (and references to `UnityEngine.TestRunner` / `UnityEditor.TestRunner`) so Unity treats it as a test assembly.

## Conventions to apply when adding code

- Put gameplay scripts under `Assets/Scripts/<Feature>/`. Once multiple features exist, give each its own `.asmdef` so iteration-time compiles stay fast and dependencies stay explicit.
- Read input by generating a C# class from `InputSystem_Actions.inputactions` (Inspector → "Generate C# Class") or by subscribing to `PlayerInput` events — do not poll `UnityEngine.Input`.
- This is a **2D URP** project: use `Sprite Renderer` + 2D lights, not 3D `MeshRenderer`/built-in lights. Shaders must target URP (Shader Graph or `Universal Render Pipeline/2D/...`).
