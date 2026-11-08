using UnityEngine;
using WeaponFactory.Levels;
using WeaponFactory.Logic;
using WeaponFactory.Player;
using WeaponFactory.UI;
using WeaponFactory.Utilities;
using WeaponFactory.Weapons;

namespace WeaponFactory.Core
{
    /// <summary>
    /// Builds the main menu scene: factory backdrop, camera, rotating 3D weapon preview on a pedestal
    /// and the menu UI. Placed in MainMenu.unity (or spawned automatically).
    /// </summary>
    public class MainMenuBootstrap : MonoBehaviour
    {
        public MenuPreview Preview { get; private set; }

        void Start()
        {
            SceneBootstrapGuard.RemoveForeignSceneObjects();
            var gm = GameManager.Instance;
            var save = gm.Save;
            int lvl = Mathf.Max(1, save.currentLevel);
            var theme = DataRegistry.GetLevel(lvl).theme;

            Light sun;
            var pal = EnvironmentBuilder.BuildMenuBackdrop(theme, out sun);

            var camRig = CameraFollow.Create(pal.background);
            camRig.enabled = false;
            camRig.transform.position = new Vector3(0f, 2.6f, -5.6f);
            camRig.transform.rotation = Quaternion.LookRotation(new Vector3(0f, 1.75f, 0f) - camRig.transform.position);
            camRig.Cam.fieldOfView = Mathf.Clamp(2f * Mathf.Atan(Mathf.Tan(30f * Mathf.Deg2Rad) / Mathf.Max(0.3f, camRig.Cam.aspect)) * Mathf.Rad2Deg, 50f, 80f);

            Preview = MenuPreview.Create(new Vector3(0f, 1.6f, 0f));
            int startTier = WeaponEvolution.TierForPower(gm.Bonuses.startingPower);
            Preview.Show(Mathf.Max(startTier, save.highestTierUnlocked), gm.EquippedSkin);

            MainMenuUI.Build(this);
        }
    }
}
