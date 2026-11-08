using UnityEngine;
using WeaponFactory.Levels;

namespace WeaponFactory.Core
{
    /// <summary>
    /// Entry point of the Gameplay scene: creates the LevelManager for the level chosen in the menu.
    /// Placed in Gameplay.unity (or spawned automatically by SceneBootstrapGuard).
    /// </summary>
    public class GameplayBootstrap : MonoBehaviour
    {
        [Tooltip("If > 0, overrides the level to play (handy for testing a level directly from the editor).")]
        public int debugLevelOverride;

        void Start()
        {
            SceneBootstrapGuard.RemoveForeignSceneObjects();
            var gm = GameManager.Instance;
            int level = debugLevelOverride > 0 ? debugLevelOverride : (gm.LevelToPlay > 0 ? gm.LevelToPlay : gm.Save.currentLevel);
            var go = new GameObject("LevelManager");
            go.AddComponent<LevelManager>().Init(level);
        }
    }
}
