using UnityEngine;
using WeaponFactory.Audio;
using WeaponFactory.Levels;
using WeaponFactory.Logic;
using WeaponFactory.Player;
using WeaponFactory.Utilities;

namespace WeaponFactory.Collectibles
{
    /// <summary>
    /// A pickup on the track. On contact it applies its effect to the weapon, plays a fly-to-weapon
    /// animation, sparkles, sounds and shows a floating notification.
    /// </summary>
    public class Collectible : MonoBehaviour, ITrackInteractable
    {
        public PickupKind kind;
        public ComponentType component;
        public int value = 1;
        public Transform visual;

        bool _collected;

        public bool IsNegative { get { return (int)kind >= 20; } }

        public void OnPlayerEnter(PlayerController player)
        {
            if (_collected) return;
            _collected = true;
            var col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
            Apply(player);
            PlayCollectAnimation(player);
        }

        void Apply(PlayerController player)
        {
            var lm = LevelManager.Instance;
            var w = player.Weapon;
            Vector3 fx = transform.position + Vector3.up * 0.6f;
            switch (kind)
            {
                case PickupKind.Component:
                    {
                        var def = ComponentCatalog.Get(component);
                        int before = w.Build.GetLevel(component);
                        int lvl = w.AddComponent(component);
                        Color c = GameAssets.Hex(def.colorHex);
                        string msg = lvl > before ? (lvl > 1 ? def.displayName + " LV" + lvl : "+" + def.displayName) : def.displayName + " MAX > +POWER";
                        Notify(msg, c);
                        AudioManager.Play(SfxId.Pickup, 0.8f);
                        VFX.Play(VFXType.Pickup, fx, c, 22);
                        break;
                    }
                case PickupKind.Coin:
                    if (lm != null) lm.AddRunCoins(value);
                    AudioManager.Play(SfxId.Coin, 0.45f);
                    VFX.Play(VFXType.Pickup, fx, new Color(1f, 0.85f, 0.2f), 6);
                    break;
                case PickupKind.UpgradeModule:
                    w.AddPower(Mathf.Max(1, value));
                    Notify("+" + Mathf.Max(1, value) + " POWER", new Color(0.4f, 1f, 0.55f));
                    AudioManager.Play(SfxId.Pickup, 0.8f, 0.05f, 1.15f);
                    VFX.Play(VFXType.Pickup, fx, new Color(0.4f, 1f, 0.55f), 22);
                    break;
                case PickupKind.DamageBooster:
                    w.ModifyDamage(0.2f);
                    Notify("+20% DAMAGE", new Color(1f, 0.35f, 0.3f));
                    AudioManager.Play(SfxId.GatePositive, 0.7f);
                    VFX.Play(VFXType.Pickup, fx, new Color(1f, 0.35f, 0.3f), 22);
                    break;
                case PickupKind.FireRateBooster:
                    w.ModifyFireRate(0.15f);
                    Notify("+15% FIRE RATE", new Color(1f, 0.8f, 0.2f));
                    AudioManager.Play(SfxId.GatePositive, 0.7f);
                    VFX.Play(VFXType.Pickup, fx, new Color(1f, 0.8f, 0.2f), 22);
                    break;
                case PickupKind.UpgradeBox:
                    {
                        var types = (ComponentType[])System.Enum.GetValues(typeof(ComponentType));
                        var t = types[Random.Range(0, types.Length)];
                        w.AddPower(3);
                        w.AddComponent(t);
                        if (lm != null) lm.AddRunCoins(15);
                        Notify("UPGRADE BOX! +" + ComponentCatalog.Get(t).displayName, new Color(1f, 0.85f, 0.25f));
                        AudioManager.Play(SfxId.Purchase, 0.9f);
                        VFX.Play(VFXType.Confetti, fx, Color.white, 30);
                        VFX.Play(VFXType.Ring, fx, new Color(1f, 0.85f, 0.25f), 30);
                        break;
                    }
                case PickupKind.RepairKit:
                    player.Repair(Mathf.Max(20, value));
                    Notify("+" + Mathf.Max(20, value) + " ARMOR", new Color(0.4f, 1f, 0.6f));
                    AudioManager.Play(SfxId.Pickup, 0.8f, 0.05f, 0.9f);
                    VFX.Play(VFXType.Pickup, fx, new Color(0.4f, 1f, 0.6f), 20);
                    break;

                // ---------- negatives ----------
                case PickupKind.BrokenPart:
                    {
                        ComponentType lost;
                        if (w.LoseRandomComponent(out lost)) Notify("BROKEN " + ComponentCatalog.Get(lost).displayName + "!", new Color(1f, 0.3f, 0.3f));
                        else { w.ModifyDamage(-0.1f); Notify("BROKEN PART -10% DMG", new Color(1f, 0.3f, 0.3f)); }
                        Negative(fx);
                        break;
                    }
                case PickupKind.RustedPart:
                    w.ModifyDamage(-0.1f);
                    Notify("RUSTED -10% DAMAGE", new Color(0.85f, 0.45f, 0.25f));
                    Negative(fx);
                    break;
                case PickupKind.DamageReducer:
                    w.ModifyDamage(-0.2f);
                    Notify("-20% DAMAGE", new Color(1f, 0.3f, 0.3f));
                    Negative(fx);
                    break;
                case PickupKind.WeightPenalty:
                    w.ModifyFireRate(-0.15f);
                    Notify("TOO HEAVY -15% RATE", new Color(1f, 0.3f, 0.3f));
                    Negative(fx);
                    break;
                case PickupKind.ExplosiveHazard:
                    VFX.Explosion(transform.position + Vector3.up * 0.5f, 1.3f, new Color(1f, 0.4f, 0.1f));
                    AudioManager.Play(SfxId.Explosion, 0.9f);
                    player.TakeHit(25f, "Blown up by an explosive hazard", true, true);
                    Notify("BOOM! -25 ARMOR", new Color(1f, 0.3f, 0.3f));
                    break;
            }
        }

        static void Negative(Vector3 fx)
        {
            AudioManager.Play(SfxId.GateNegative, 0.8f);
            VFX.Play(VFXType.Smoke, fx, new Color(0.3f, 0.3f, 0.3f, 0.8f), 10);
            ScreenShake.Add(0.12f);
        }

        static void Notify(string text, Color color)
        {
            var lm = LevelManager.Instance;
            if (lm != null) lm.Notify(text, color);
        }

        void PlayCollectAnimation(PlayerController player)
        {
            if (kind == PickupKind.ExplosiveHazard) { gameObject.SetActive(false); return; }
            var t = visual != null ? visual : transform;
            var bob = GetComponentInChildren<Bobber>();
            if (bob != null) bob.enabled = false;
            Vector3 start = t.position;
            Vector3 startScale = t.localScale;
            Tween.Run(this, 0.28f, p =>
            {
                if (player == null) return;
                Vector3 target = player.HitCenter + Vector3.up * 0.2f;
                t.position = Vector3.Lerp(start, target, p) + Vector3.up * Mathf.Sin(p * Mathf.PI) * 0.8f;
                t.localScale = startScale * Mathf.Lerp(1.2f, 0.05f, p);
            }, Ease.InQuad, () => gameObject.SetActive(false));
        }
    }
}
