using UnityEngine;
using WeaponFactory.Audio;
using WeaponFactory.Levels;
using WeaponFactory.Logic;
using WeaponFactory.Player;
using WeaponFactory.Utilities;

namespace WeaponFactory.Gates
{
    public class GatePair : MonoBehaviour
    {
        public UpgradeGate left, right;
        public bool Used { get; private set; }

        public void Choose(UpgradeGate chosen)
        {
            Used = true;
            if (left != null && left != chosen) left.Shutdown();
            if (right != null && right != chosen) right.Shutdown();
            var col = chosen.GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }
    }
}
