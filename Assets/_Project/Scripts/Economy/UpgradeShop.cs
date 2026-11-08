using WeaponFactory.Audio;
using WeaponFactory.Core;
using WeaponFactory.Logic;

namespace WeaponFactory.Economy
{
    /// <summary>
    /// Permanent upgrade purchases. Rules live in Logic/Purchases (unit tested); this class applies
    /// them to the live save, persists, plays feedback and notifies the UI.
    /// </summary>
    public static class UpgradeShop
    {
        public static Purchases.Result Buy(UpgradeType type)
        {
            var gm = GameManager.Instance;
            var result = Purchases.TryBuyUpgrade(gm.Save, type);
            Finish(gm, result);
            return result;
        }

        public static Purchases.Result BuySkin(string skinId)
        {
            var gm = GameManager.Instance;
            var result = Purchases.TryBuySkin(gm.Save, skinId);
            Finish(gm, result);
            return result;
        }

        public static void EquipSkin(string skinId)
        {
            var gm = GameManager.Instance;
            if (!gm.Save.ownedSkins.Contains(skinId)) return;
            gm.Save.equippedSkin = skinId;
            gm.SaveSystem.Save();
            AudioManager.Play(SfxId.UIClick, 0.8f);
        }

        static void Finish(GameManager gm, Purchases.Result result)
        {
            if (result == Purchases.Result.Success)
            {
                gm.SaveSystem.Save();
                gm.NotifyCoinsChanged();
                AudioManager.Play(SfxId.Purchase, 0.9f);
                gm.Vibrate();
            }
            else
            {
                AudioManager.Play(SfxId.Error, 0.7f);
            }
        }

        public static int LevelOf(UpgradeType type) { return GameManager.Instance.Save.GetUpgradeLevel(type); }
    }
}
