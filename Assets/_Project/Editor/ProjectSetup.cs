using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using WeaponFactory.Combat;
using WeaponFactory.Core;
using WeaponFactory.Economy;
using WeaponFactory.Levels;
using WeaponFactory.Logic;
using WeaponFactory.Weapons;
#if UNITY_2021_2_OR_NEWER
using UnityEditor.Build;
#endif

namespace WeaponFactory.EditorTools
{
    /// <summary>
    /// One-click (and automatic on first open) project setup:
    ///  • base materials in Resources (so Standard/particle shaders are included in builds)
    ///  • MainMenu.unity and Gameplay.unity with their bootstraps, added to Build Settings
    ///  • portrait player settings, bundle id, Android IL2CPP/ARM64
    ///  • legacy Input Manager enabled (the game uses UnityEngine.Input for touch/mouse/keys)
    ///  • ScriptableObject data assets for levels, components, evolution, upgrades and enemies
    /// Everything is idempotent: existing assets are left untouched.
    /// </summary>
    [InitializeOnLoad]
    public static class ProjectSetup
    {
        const string Root = "Assets/_Project";
        const string MaterialsDir = Root + "/Resources/WF/Materials";
        const string DataDir = Root + "/Resources/WF/Data";
        const string ScenesDir = Root + "/Scenes";
        public const string MainMenuScene = ScenesDir + "/MainMenu.unity";
        public const string GameplayScene = ScenesDir + "/Gameplay.unity";
        const string AutoKey = "WeaponFactory.AutoSetupDone.v1";

        static ProjectSetup()
        {
            EditorApplication.delayCall += AutoSetup;
        }

        static void AutoSetup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            bool missing = !File.Exists(MainMenuScene) || !File.Exists(GameplayScene) || !File.Exists(MaterialsDir + "/WF_Lit.mat");
            if (!missing && EditorPrefs.GetBool(AutoKey + Application.dataPath, false)) return;
            RunSetup(false);
            EditorPrefs.SetBool(AutoKey + Application.dataPath, true);
        }

        [MenuItem("Weapon Factory/Setup Project (Scenes, Materials, Settings)", priority = 0)]
        public static void RunSetupMenu()
        {
            RunSetup(true);
            EditorUtility.DisplayDialog("Weapon Factory", "Project setup complete.\n\nScenes, materials, build settings, player settings and data assets are ready.\nOpen Assets/_Project/Scenes/MainMenu.unity and press Play.", "OK");
        }

        public static void RunSetup(bool verbose)
        {
            EnsureFolder(MaterialsDir);
            EnsureFolder(DataDir);
            EnsureFolder(ScenesDir);
            EnsureFolder(Root + "/Resources/Audio/SFX");
            EnsureFolder(Root + "/Resources/Audio/Music");
            CreateMaterials();
            CreateScenes();
            ConfigureBuildSettings();
            ConfigurePlayerSettings();
            EnableLegacyInput();
            ExportDataAssets(false);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (verbose) Debug.Log("[Weapon Factory] Setup complete.");
        }

        // ------------------------------------------------------------------ folders

        static void EnsureFolder(string path)
        {
            path = path.Replace('\\', '/');
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        // ------------------------------------------------------------------ materials

        static void CreateMaterials()
        {
            CreateMaterial("WF_Lit", m => { m.SetFloat("_Glossiness", 0.45f); m.enableInstancing = true; }, "Standard");
            CreateMaterial("WF_Glow", m =>
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", Color.white);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                m.enableInstancing = true;
            }, "Standard");
            CreateMaterial("WF_Unlit", m => m.enableInstancing = true, "Unlit/Color");
            CreateMaterial("WF_ParticleAdd", null, "Legacy Shaders/Particles/Additive", "Particles/Standard Unlit");
            CreateMaterial("WF_ParticleAlpha", null, "Legacy Shaders/Particles/Alpha Blended", "Particles/Standard Unlit");
        }

        static void CreateMaterial(string name, System.Action<Material> configure, params string[] shaders)
        {
            string path = MaterialsDir + "/" + name + ".mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) return;
            Shader shader = null;
            foreach (var s in shaders) { shader = Shader.Find(s); if (shader != null) break; }
            if (shader == null)
            {
                Debug.LogWarning("[Weapon Factory] Shader not found for " + name + " (" + string.Join(", ", shaders) + "). " +
                                 "This project targets the Built-in Render Pipeline.");
                return;
            }
            var mat = new Material(shader) { name = name };
            if (configure != null) configure(mat);
            AssetDatabase.CreateAsset(mat, path);
        }

        // ------------------------------------------------------------------ scenes

        static void CreateScenes()
        {
            CreateBootstrapScene(MainMenuScene, typeof(MainMenuBootstrap), "MainMenuBootstrap");
            CreateBootstrapScene(GameplayScene, typeof(GameplayBootstrap), "GameplayBootstrap");
        }

        static void CreateBootstrapScene(string path, System.Type bootstrap, string goName)
        {
            if (File.Exists(path))
            {
                if (SceneHasComponent(path, bootstrap)) return;
                Debug.LogWarning("[Weapon Factory] " + path + " has no " + bootstrap.Name + " - recreating it.");
                AssetDatabase.DeleteAsset(path);
            }
            // Additive + close afterwards so the user's currently open scene is never touched.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            var go = new GameObject(goName);
            go.AddComponent(bootstrap);
            SceneManager.MoveGameObjectToScene(go, scene);
            EditorSceneManager.SaveScene(scene, path);
            EditorSceneManager.CloseScene(scene, true);
        }

        static bool SceneHasComponent(string path, System.Type type)
        {
            try
            {
                var loaded = SceneManager.GetSceneByPath(path);
                bool wasLoaded = loaded.IsValid() && loaded.isLoaded;
                var scene = wasLoaded ? loaded : EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                bool found = false;
                foreach (var root in scene.GetRootGameObjects())
                    if (root.GetComponentInChildren(type, true) != null) { found = true; break; }
                if (!wasLoaded) EditorSceneManager.CloseScene(scene, true);
                return found;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[Weapon Factory] Could not inspect " + path + ": " + e.Message);
                return false;
            }
        }

        static void ConfigureBuildSettings()
        {
            var wanted = new[] { MainMenuScene, GameplayScene };
            var list = new List<EditorBuildSettingsScene>();
            foreach (var p in wanted) if (File.Exists(p)) list.Add(new EditorBuildSettingsScene(p, true));
            foreach (var s in EditorBuildSettings.scenes)
                if (System.Array.IndexOf(wanted, s.path) < 0) list.Add(s);
            EditorBuildSettings.scenes = list.ToArray();
        }

        // ------------------------------------------------------------------ player settings

        static void ConfigurePlayerSettings()
        {
            if (PlayerSettings.productName == "New Unity Project" || string.IsNullOrEmpty(PlayerSettings.productName) || PlayerSettings.productName == "Weapon-Factory")
                PlayerSettings.productName = "Weapon Factory";
            if (PlayerSettings.companyName == "DefaultCompany" || string.IsNullOrEmpty(PlayerSettings.companyName))
                PlayerSettings.companyName = "WeaponFactoryStudio";

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.defaultScreenWidth = 540;
            PlayerSettings.defaultScreenHeight = 960;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = false;

            const string id = "com.weaponfactory.game";
#if UNITY_2021_2_OR_NEWER
            if (PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android).StartsWith("com.Default") || string.IsNullOrEmpty(PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android)))
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, id);
            if (PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.iOS).StartsWith("com.Default") || string.IsNullOrEmpty(PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.iOS)))
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, id);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
#else
            if (PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android).StartsWith("com.Default") || string.IsNullOrEmpty(PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android)))
                PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, id);
            if (PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.iOS).StartsWith("com.Default") || string.IsNullOrEmpty(PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.iOS)))
                PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, id);
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
#endif
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            if ((int)PlayerSettings.Android.minSdkVersion < 23) PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)23;
            PlayerSettings.bundleVersion = string.IsNullOrEmpty(PlayerSettings.bundleVersion) || PlayerSettings.bundleVersion == "0.1" ? "1.0.0" : PlayerSettings.bundleVersion;
        }

        /// <summary>
        /// The game reads touches/mouse/keys through UnityEngine.Input (Input Manager). If the project is set to
        /// "Input System Package (New)" only, switch to "Both" so legacy input keeps working. Requires an editor restart.
        /// </summary>
        static void EnableLegacyInput()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (assets == null || assets.Length == 0) return;
            var so = new SerializedObject(assets[0]);
            var prop = so.FindProperty("activeInputHandler");
            if (prop != null && prop.intValue == 1)
            {
                prop.intValue = 2; // Both
                so.ApplyModifiedProperties();
                Debug.LogWarning("[Weapon Factory] Active Input Handling switched to 'Both'. Please restart the Unity editor.");
            }
        }

        // ------------------------------------------------------------------ data assets

        [MenuItem("Weapon Factory/Export Default Data Assets (overwrite)", priority = 20)]
        public static void ExportDataAssetsMenu()
        {
            if (!EditorUtility.DisplayDialog("Weapon Factory", "Overwrite all data assets in Resources/WF/Data with the built-in defaults?", "Overwrite", "Cancel")) return;
            ExportDataAssets(true);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static void ExportDataAssets(bool overwrite)
        {
            EnsureFolder(DataDir + "/Levels");
            EnsureFolder(DataDir + "/Components");
            EnsureFolder(DataDir + "/Upgrades");
            EnsureFolder(DataDir + "/Enemies");

            foreach (var l in LevelLibrary.CreateDefaults())
            {
                var path = DataDir + "/Levels/Level_" + l.levelNumber.ToString("00") + ".asset";
                Write<LevelData>(path, overwrite, a => a.definition = l);
            }
            foreach (var c in ComponentCatalog.CreateDefaults())
                Write<WeaponComponentData>(DataDir + "/Components/Component_" + c.type + ".asset", overwrite, a => a.definition = c);
            Write<WeaponData>(DataDir + "/WeaponEvolution.asset", overwrite, a => a.tiers = WeaponEvolution.CreateDefaults());
            foreach (var u in UpgradeCatalog.CreateDefaults())
                Write<UpgradeData>(DataDir + "/Upgrades/Upgrade_" + u.type + ".asset", overwrite, a => a.definition = u);
            foreach (var e in EnemyCatalog.CreateDefaults())
                Write<EnemyData>(DataDir + "/Enemies/Enemy_" + e.type + ".asset", overwrite, a => a.definition = e);
        }

        static void Write<T>(string path, bool overwrite, System.Action<T> fill) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null && !overwrite) return;
            if (existing != null)
            {
                fill(existing);
                EditorUtility.SetDirty(existing);
                return;
            }
            var asset = ScriptableObject.CreateInstance<T>();
            fill(asset);
            AssetDatabase.CreateAsset(asset, path);
        }

        // ------------------------------------------------------------------ utilities

        [MenuItem("Weapon Factory/Play From Main Menu", priority = 40)]
        public static void PlayFromMenu()
        {
            if (!File.Exists(MainMenuScene)) RunSetup(false);
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(MainMenuScene);
            EditorApplication.isPlaying = true;
        }

        [MenuItem("Weapon Factory/Reset Save Data (Development)", priority = 41)]
        public static void ResetSave()
        {
            string dir = Application.persistentDataPath;
            int n = 0;
            foreach (var f in new[] { "weaponfactory_save.json", "weaponfactory_save.bak", "weaponfactory_save.json.tmp" })
            {
                string p = Path.Combine(dir, f);
                if (File.Exists(p)) { File.Delete(p); n++; }
            }
            Debug.Log("[Weapon Factory] Deleted " + n + " save file(s) in " + dir);
        }

        [MenuItem("Weapon Factory/Open Save Folder", priority = 42)]
        public static void OpenSaveFolder()
        {
            EditorUtility.RevealInFinder(Application.persistentDataPath);
        }
    }
}
