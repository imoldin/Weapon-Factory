using UnityEngine;
using WeaponFactory.Audio;
using WeaponFactory.Levels;
using WeaponFactory.Logic;
using WeaponFactory.Player;
using WeaponFactory.Utilities;

namespace WeaponFactory.Gates
{
    /// <summary>
    /// One half of a gate pair. Passing through it applies its effect; the sibling gate then
    /// shuts down, so the player makes exactly one choice per pair.
    /// </summary>
    public class UpgradeGate : MonoBehaviour, ITrackInteractable
    {
        public GateEffect effect;
        public GatePair pair;
        public Transform panel;
        public TextMesh valueText, captionText;
        public Color color;

        public void OnPlayerEnter(PlayerController player)
        {
            if (pair == null || pair.Used) return;
            pair.Choose(this);
            player.Weapon.ApplyGate(effect);

            bool positive = effect.IsPositive;
            Vector3 fx = transform.position + Vector3.up * 1.5f;
            VFX.Play(VFXType.Ring, fx, color, 40);
            VFX.Play(VFXType.Pickup, fx, color, 25);
            AudioManager.Play(positive ? SfxId.GatePositive : SfxId.GateNegative, 0.9f);
            if (!positive) ScreenShake.Add(0.15f);
            var lm = LevelManager.Instance;
            if (lm != null) lm.Notify(effect.FullLabel, color, true);

            // flash and burst the chosen panel
            if (panel != null)
            {
                StopPulse();
                Vector3 s = panel.localScale;
                Tween.Run(panel, 0.35f, p => panel.localScale = new Vector3(s.x * (1f + p * 0.3f), s.y * (1f + p * 0.3f), s.z), Ease.OutQuad,
                    () => panel.gameObject.SetActive(false));
            }
        }

        void StopPulse()
        {
            if (panel == null) return;
            var pulse = panel.GetComponent<Pulser>();
            if (pulse != null) pulse.enabled = false;
        }

        /// <summary>Called on the gate that was NOT chosen.</summary>
        public void Shutdown()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
            StopPulse();
            if (panel != null) Tween.Scale(panel, new Vector3(panel.localScale.x, 0.02f, panel.localScale.z), 0.3f, Ease.InQuad);
            if (valueText != null) valueText.color = new Color(1f, 1f, 1f, 0.25f);
            if (captionText != null) captionText.color = new Color(1f, 1f, 1f, 0.25f);
        }
    }
}
