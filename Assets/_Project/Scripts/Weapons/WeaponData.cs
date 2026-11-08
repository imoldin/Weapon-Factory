using System.Collections.Generic;
using UnityEngine;
using WeaponFactory.Logic;

namespace WeaponFactory.Weapons
{
    /// <summary>
    /// Inspector-editable weapon evolution path (8 tiers, Resources/WF/Data/WeaponEvolution).
    /// Power thresholds must be strictly increasing.
    /// </summary>
    [CreateAssetMenu(menuName = "Weapon Factory/Weapon Evolution", fileName = "WeaponEvolution")]
    public class WeaponData : ScriptableObject
    {
        public List<WeaponTierDefinition> tiers = new List<WeaponTierDefinition>();
    }
}
