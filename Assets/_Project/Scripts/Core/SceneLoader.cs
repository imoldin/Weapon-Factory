using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WeaponFactory.UI;

namespace WeaponFactory.Core
{
    public enum GameScene { MainMenu, Gameplay }

    /// <summary>
    /// Scene transitions with a fade. Loads real scenes when they are in Build Settings;
    /// otherwise creates an empty runtime scene and spawns the matching bootstrap,
    /// so the game works even before the editor setup has added scenes to the build.
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        static SceneLoader _instance;
        CanvasGroup _fade;
        bool _busy;
        int _runtimeSceneCounter;

        public static bool IsLoading { get { return _instance != null && _instance._busy; } }

        public static string SceneName(GameScene s) { return s == GameScene.MainMenu ? "MainMenu" : "Gameplay"; }

        void Awake()
        {
            _instance = this;
            BuildFadeOverlay();
        }

        void BuildFadeOverlay()
        {
            var go = new GameObject("FadeCanvas");
            go.transform.SetParent(transform, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            go.AddComponent<CanvasScaler>();
            go.AddComponent<GraphicRaycaster>();
            _fade = go.AddComponent<CanvasGroup>();
            var img = new GameObject("Fade").AddComponent<Image>();
            img.transform.SetParent(go.transform, false);
            img.color = new Color(0.05f, 0.06f, 0.09f, 1f);
            var rt = img.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            _fade.alpha = 1f;
            _fade.blocksRaycasts = false;
            StartCoroutine(FadeTo(0f, 0.45f));
        }

        public static void Load(GameScene scene)
        {
            if (_instance == null || _instance._busy) return;
            _instance.StartCoroutine(_instance.DoLoad(scene));
        }

        IEnumerator FadeTo(float target, float duration)
        {
            float start = _fade.alpha, t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                _fade.alpha = Mathf.Lerp(start, target, t / duration);
                yield return null;
            }
            _fade.alpha = target;
        }

        IEnumerator DoLoad(GameScene scene)
        {
            _busy = true;
            _fade.blocksRaycasts = true;
            yield return FadeTo(1f, 0.25f);
            Time.timeScale = 1f;
            UIFactory.ClearCaches();

            string name = SceneName(scene);
            if (Application.CanStreamedLevelBeLoaded(name))
            {
                var op = SceneManager.LoadSceneAsync(name, LoadSceneMode.Single);
                while (!op.isDone) yield return null;
            }
            else
            {
                // Fallback when the scene is not in Build Settings: build it at runtime.
                var old = SceneManager.GetActiveScene();
                var created = SceneManager.CreateScene(name + "_Runtime" + (++_runtimeSceneCounter));
                SceneManager.SetActiveScene(created);
                if (old.IsValid() && old.isLoaded)
                {
                    var op = SceneManager.UnloadSceneAsync(old);
                    while (op != null && !op.isDone) yield return null;
                }
                SceneBootstrapGuard.Spawn(scene);
            }

            yield return null; // let bootstraps build the scene
            yield return FadeTo(0f, 0.35f);
            _fade.blocksRaycasts = false;
            _busy = false;
        }
    }

    /// <summary>Guarantees each loaded scene has a bootstrap that builds its content.</summary>
    public static class SceneBootstrapGuard
    {
        public static void Ensure(Scene scene)
        {
            if (scene.IsValid() && scene.isLoaded)
                foreach (var root in scene.GetRootGameObjects())
                    if (root.GetComponentInChildren<MainMenuBootstrap>(true) != null || root.GetComponentInChildren<GameplayBootstrap>(true) != null)
                        return;
            bool gameplay = scene.name.Contains("Gameplay");
            Spawn(gameplay ? GameScene.Gameplay : GameScene.MainMenu);
        }

        public static void Spawn(GameScene scene)
        {
            var go = new GameObject(scene == GameScene.MainMenu ? "MainMenuBootstrap" : "GameplayBootstrap");
            if (scene == GameScene.MainMenu) go.AddComponent<MainMenuBootstrap>();
            else go.AddComponent<GameplayBootstrap>();
        }

        /// <summary>
        /// Removes cameras/lights/event systems that a template scene might contain (e.g. Unity's
        /// SampleScene) so they do not conflict with the procedurally built ones.
        /// </summary>
        public static void RemoveForeignSceneObjects()
        {
            // Walk the active scene's roots (avoids FindObjectsByType, whose sort-mode overload is obsolete in Unity 6.x).
            var roots = SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var root in roots)
            {
                if (root == null) continue;
                foreach (var cam in root.GetComponentsInChildren<Camera>(true))
                    if (cam != null) Object.DestroyImmediate(cam.gameObject);
            }
            foreach (var root in roots)
            {
                if (root == null) continue;
                foreach (var light in root.GetComponentsInChildren<Light>(true))
                    if (light != null && light.type == LightType.Directional) Object.DestroyImmediate(light.gameObject);
                foreach (var es in root.GetComponentsInChildren<UnityEngine.EventSystems.EventSystem>(true))
                    if (es != null) Object.DestroyImmediate(es.gameObject);
            }
        }
    }
}
