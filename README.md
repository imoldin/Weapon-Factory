# WEAPON FACTORY

A 3D hyper-casual / hybrid-casual runner for mobile, built with Unity (C#, Built-in Render Pipeline, portrait 9:16).

You steer a flying weapon through a factory. Collect parts and the weapon visibly grows (longer barrels, extra barrels, scopes, lasers, drum magazines…). Pick upgrade gates, avoid saws, presses, lasers and hammers, then fight robots or a boss in the arena. Spend coins on permanent upgrades and evolve from a **Basic Pistol** to a **Rocket Launcher**.

> **Verification status (read this first)**
> This project was written in an environment **without the Unity Editor**. It has **not** been opened, played or built in Unity yet.
> What *was* verified:
> * every runtime and editor C# script compiles against Unity reference assemblies with **0 errors and 0 warnings** (`Tests/run_checks.sh`, see below);
> * the gameplay rules (stats, evolution, gates, economy, saves, 10 level configs, balance) pass **2,208 automated checks**;
> * the procedural meshes' triangle winding and the procedural UI icons were checked outside Unity.
>
> What still needs a first run in Unity: the look and feel, layout on real devices, physics trigger behaviour and the Android/iOS builds. See [Known limitations](#known-limitations).

---

## 1. Requirements

* **Unity 6 LTS** (6000.0.x), installed with **Android Build Support** and/or **iOS Build Support** modules.
* No third-party assets or packages: all models, textures, UI sprites, sounds and music are generated in code.

## 2. Opening the project

1. In Unity Hub choose **Add → Add project from disk** and select this folder (the one that contains `Assets/`, `Packages/` and `ProjectSettings/`).
2. Open it with Unity 6 LTS. If Hub asks for a different editor version (`ProjectVersion.txt` says 6000.0.23f1), just choose your installed 6000.0.x version.
3. On first import an editor script (`Assets/_Project/Editor/ProjectSetup.cs`) runs automatically:
   * creates the base materials in `Assets/_Project/Resources/WF/Materials` (so the Standard and particle shaders are included in builds);
   * checks/creates `MainMenu.unity` and `Gameplay.unity` and adds them to Build Settings;
   * sets portrait orientation, product name, bundle id `com.weaponfactory.game`, Android IL2CPP + ARM64;
   * makes sure legacy **Input Manager** input is enabled (if the project was set to *Input System (New)* only, it switches to *Both* — **restart the editor** if you see that warning);
   * exports all ScriptableObject data assets to `Assets/_Project/Resources/WF/Data`.

   You can run it again at any time via the menu **Weapon Factory → Setup Project (Scenes, Materials, Settings)**.

> If you started from a URP/HDRP template instead of this folder, materials will be pink. This project uses the **Built-in Render Pipeline** (no SRP asset).

## 3. Testing on PC (Editor or Windows build)

* Open `Assets/_Project/Scenes/MainMenu.unity` and press **Play**, or use **Weapon Factory → Play From Main Menu**.
* Set the Game view to a portrait resolution (e.g. *1080x1920* or *9:16*).
* Pressing Play in **any** scene also works: a bootstrap is spawned automatically.

| Action | Mobile | PC |
|---|---|---|
| Start a run | touch & drag | mouse drag, Space, A/D or arrows |
| Steer left / right | drag one finger | drag with the mouse, **A / D** or **← / →** |
| Fire | automatic | automatic |
| Pause | ‖ button (top-left) | ‖ button or **Esc** |

Developer tools (Editor and Development builds only): **Settings → RESET SAVE** (tap twice) and **+1000 COINS**.
Editor menu: **Weapon Factory → Reset Save Data**, **Open Save Folder**.
To jump straight to a level, open `Gameplay.unity`, select `GameplayBootstrap` and set **Debug Level Override**.

## 4. Building for Android (APK)

1. **File → Build Profiles** (Unity 6) → **Android** → *Switch Platform*.
2. Scenes `MainMenu` and `Gameplay` should already be listed (index 0 = MainMenu).
3. **Player Settings** (already configured by the setup script): orientation Portrait, IL2CPP, ARM64, min API 23. Change the *Package Name* if you want.
4. For a test APK leave *Build App Bundle (Google Play)* unchecked, then **Build** (or *Build And Run* with a USB-connected device with USB debugging enabled).
5. For Google Play: enable *Build App Bundle*, create a keystore in *Publishing Settings*, build an `.aab`.

## 5. Building for iOS

1. Needs a Mac with Xcode. **File → Build Profiles → iOS → Switch Platform**.
2. Set *Bundle Identifier* and *Signing Team ID* in Player Settings (Other Settings).
3. **Build** → open the generated Xcode project → choose your team → run on a device or archive for TestFlight.

## 6. Verifying without Unity (CI-friendly)

```bash
Tests/run_checks.sh
```

Needs the .NET 8 SDK, `curl` and `unzip`. It downloads Unity reference DLLs from NuGet (`UnityEngine.Modules 2021.3`, `Unity3D.SDK 2021.1` for UnityEditor), compiles **all runtime scripts** and **all editor scripts**, then runs `Tests/LogicTests` (unit tests and a level balance simulation).
uGUI is a Unity package without a NuGet build, so `Tests/CompileCheck/UGuiStub.cs` provides signature stubs for the uGUI members the project uses (checked against the real uGUI source). The stub is never part of the Unity project.

Balance simulation output (an "average" player who picks up 70 % of items and buys no upgrades):

```
 Lvl  Name            Tier   DPS   Enemy HP  Kill time  Limit
   1  FIRST SHIFT        3    87        40      0.5s     45s
   5  THE GUARDIAN       3   153      2904     18.9s     60s   (boss)
   9  OVERDRIVE          5   233      4424     19.0s     45s
  10  CORE MELTDOWN      5  1112      8230      7.4s     75s   (boss)
```

## 7. Project structure

```
Assets/_Project/
  Scenes/              MainMenu.unity, Gameplay.unity (each holds one bootstrap object)
  Editor/              ProjectSetup.cs – automatic setup, data export, dev menu items
  Resources/WF/        Materials + ScriptableObject data (generated by the setup script)
  Resources/Audio/     drop-in replacements for the procedural sounds (optional)
  Scripts/
    Logic/        pure C# rules (no UnityEngine): stats, evolution, gates, economy, levels, save data
    Core/         GameManager, SceneLoader (+fade), DataRegistry, bootstraps, menu weapon preview
    Player/       input (touch/mouse/keys), movement, camera, PlayerController (integrity, hits)
    Weapons/      WeaponController, WeaponAssembler (procedural models), WeaponFiring, data assets
    Collectibles/ Collectible + CollectibleSpawner (12 pickup kinds)
    Gates/        UpgradeGate, GatePair, GateManager (fair random pairs)
    Obstacles/    saws, moving saws, presses, laser fences, hammers, barriers, targets, barrels
    Combat/       Damageable, CombatManager, projectiles, EnemyController, BossController, EnemyFactory
    Levels/       LevelManager (run flow), LevelGenerator (data → track), EnvironmentBuilder (3 themes)
    Economy/      UpgradeShop, CurrencyManager, UpgradeData
    UI/           code-built uGUI: menu, shop, weapons, settings, HUD, pause, victory, defeat
    Save/         SaveManager (atomic JSON + backup)
    Audio/        AudioManager, ProceduralAudio (synthesised SFX + music loop)
    Utilities/    pooling, tweens, VFX, screen shake, procedural meshes/materials, small animators
Tests/
  LogicTests/     unit tests + balance simulation (dotnet run)
  CompileCheck/   Unity-free type check of all scripts
  run_checks.sh
```

### Architecture notes
* **Code-driven content.** Scenes contain only a bootstrap. Models, environments, UI and effects are built in code from primitive-shaped meshes. There are no binary prefabs; the factories (`WeaponAssembler`, `EnemyFactory`, `CollectibleSpawner`, `GateManager`, `Obstacle.Build*`, `EnvironmentBuilder`) play the role of prefabs. This keeps the project fully text-based and diff-able.
* **Data-driven.** Levels, components, the evolution path, upgrades and enemies are plain serializable definitions (`Scripts/Logic`). ScriptableObjects in `Resources/WF/Data` override the code defaults at runtime (`DataRegistry`).
* **Testable rules.** Everything balance-related lives in `Scripts/Logic` with no Unity dependency.
* **Decoupling.** Static `GameEvents` (coins, settings), C# events on `WeaponController` / `PlayerController`, and a scene-level `LevelManager` / `CombatManager`.
* **Mobile performance.** Shared, instancing-enabled materials cached per colour; static batching of the environment; one ParticleSystem per effect type emitting at positions; pooled projectiles, enemy shots and damage numbers; cached number strings; hit tests are segment-vs-sphere checks instead of physics raycasts; 60 FPS target; Low graphics option (no shadows, fewer particles).

### Adding a level
Duplicate `Resources/WF/Data/Levels/Level_10.asset`, set `levelNumber` to 11 and edit segments/waves in the Inspector. Segment types: Coins, ComponentItem, ComponentChoice, GatePair (set `randomize` for a fair random pair), Saw, MovingSaw, Crusher, LaserWall, Barrier, ExplosiveBarrels, Pickup, Mixed, Targets, Hammer. Levels without an asset are generated procedurally (endless mode after level 10, a boss every 5th level).
To go back to the built-in defaults: **Weapon Factory → Export Default Data Assets (overwrite)**, or delete the assets.

### Replacing placeholder audio
Put an AudioClip into `Assets/_Project/Resources/Audio/SFX/` named after an `SfxId` (`Shoot`, `ShootHeavy`, `Rocket`, `Explosion`, `Hit`, `Crit`, `Pickup`, `Coin`, `GatePositive`, `GateNegative`, `Evolve`, `Reload`, `EnemyDie`, `PlayerHurt`, `Victory`, `Defeat`, `UIClick`, `Purchase`, `Error`, `BossRoar`, `Whoosh`), and music as `Resources/Audio/Music/MainTheme`. They replace the synthesised sounds automatically.

---

## 8. Implemented features

**Core loop:** auto-run with one-finger steering (touch, mouse, keyboard) → collect parts → gates → hazards → arena combat → rewards → permanent upgrades → next level.

**Weapon system**
* 10 components (Barrel, Extended Barrel, Double Barrel, Magazine, Drum Magazine, Stock, Scope, Laser Sight, Grip, Muzzle Brake). Each has id, type, damage / fire-rate / reload / accuracy / range / crit modifiers, extra projectiles, evolution power and up to 3 levels.
* Statistics: damage, fire rate, magazine, reload, accuracy, range, crit, projectiles per shot. `Final = Tier base × component multipliers × gate multipliers × permanent multiplier` (with a soft penalty per extra projectile).
* 8-stage evolution: Basic Pistol → Modified Pistol → Double Barrel → Assault Rifle → Advanced Rifle → Minigun (spinning barrels) → Heavy Cannon (explosive shells) → Rocket Launcher (homing rockets, splash).
* **Every pickup is visible on the weapon**, and the number of visible barrels always equals projectiles per shot. Damage boosts add glowing power coils. Parts pop in with an animation; evolving shows a banner, slow motion and effects.
* Auto-aim, magazine and reload with a tilt animation, recoil, muzzle flashes, crits (yellow numbers).

**Collectibles:** components, coins, upgrade modules, damage / fire-rate boosters, upgrade boxes, repair kits; negatives: broken part, rust, damage reducer, weight penalty, explosive hazard. Each has a pickup animation, floating notification, sound and HUD update.

**Gates:** paired neon gates with large text (+50% DAMAGE, x2 DAMAGE, +1 BARREL, +20 MAGAZINE, +25% CRIT, UPGRADE +N, and negatives such as -30% DAMAGE, -20% FIRE RATE, -1 BARREL, -20% MAGAZINE). Blue = upgrade, gold = strong upgrade, red = penalty. Only one gate per pair can be taken. The random generator never produces two negatives or duplicates (tested on 2,000 random pairs).

**Hazards:** static and moving saw blades, hydraulic presses (with a floor warning), laser fences with a green safe gap, swinging hammers, breakable barriers with HP (shoot or dodge), explosive barrels (shoot for coins), shooting targets.

**Enemies:** Factory Robot (melee), Armored Robot (armour + shield), Security Drone (flying, strafes, shoots), Heavy Security (stops at range, burst fire, then charges), Factory Boss (large HP bar, fan barrage, ground shockwave with a safe gap, drone summons, enrage below 50 %, telegraphed attacks). Hit flash, knockback, health bars, damage numbers, break-apart deaths with explosions.

**Levels:** 10 hand-made levels (bosses on 5 and 10), 3 themes (Industrial Factory, Tech Laboratory, Robot Factory), rising speed/HP/hazard density, waves, arena time limit, then endless procedural levels.

**Economy:** rewards for completion, enemies, boss, weapon tier, collected coins, a flawless bonus and the coin multiplier. Shop with 6 upgrades (Damage, Fire Rate, Magazine, Starting Weapon Level, Coin Bonus, Critical) × 4–5 levels with rising prices (Damage: 100/250/500/900/1500). Buying without enough coins is blocked. 4 weapon finishes (skins) to buy and equip. Tiers unlock in the WEAPONS gallery when first reached.

**UI:** main menu (logo, rotating 3D weapon on a pedestal in a factory backdrop, PLAY, UPGRADES, WEAPONS, SETTINGS, coins, level), HUD (level, progress, coins, weapon name / tier / evolution bar, DMG and RATE chips, armour, ammo / reload, boss bar, arena timer, wave counter, notifications), pause, victory with a counting reward breakdown, defeat with reason and tips, settings (music, SFX, mute, vibration, screen shake, graphics quality). Rounded panels, shadows, press animations, click sounds and fade transitions. CanvasScaler at 1080×1920 and safe-area support.

**Audio:** 21 synthesised SFX plus an original music loop; separate music/SFX volume and mute; sound-rate limiting.

**Save:** JSON in `Application.persistentDataPath` (level, coins, upgrades, unlocked tiers, skins, settings, completed levels, stats). Atomic write and backup; corrupted files fall back to the backup or defaults (the bad file is kept as `.corrupt`). Saves on purchase, level end, settings change and app pause.

## Known limitations

* **Not yet run in Unity** (see the top of this file). Expect to tune camera distance, HUD layout, gate/obstacle spacing, effect sizes and colours after the first play session. Gameplay bugs that only show up at runtime are possible, and the type-check cannot catch them.
* uGUI was type-checked against a hand-written signature stub, and UnityEditor against the 2021.1 API. The Unity 6–specific branch of the setup script (`NamedBuildTarget`) was not compiled here.
* Hand-written scene files are minimal YAML. If Unity rejects them, the setup script detects the missing bootstrap and recreates the scenes automatically.
* Placeholder art only (primitive-based models, procedural textures, synthesised audio) — functional and readable but not final art.
* No ads, IAP, analytics or localisation. The legacy `Text` component and the built-in font are used instead of TextMeshPro, to avoid the TMP Essentials import step.
* No level-select screen: PLAY continues from the next uncompleted level.
* Performance targets (60 FPS) are designed for, not measured: no device profiling has been done.
