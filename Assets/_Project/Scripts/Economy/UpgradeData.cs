using UnityEngine;
using WeaponFactory.Logic;

namespace WeaponFactory.Economy
{
    /// <summary>Inspector-editable permanent upgrade (Resources/WF/Data/Upgrades).</summary>
    [CreateAssetMenu(menuName = "Weapon Factory/Upgrade", fileName = "Upgrade_")]
    public class UpgradeData : ScriptableObject
    {
        public UpgradeDefinition definition = new UpgradeDefinition();
    }
}
