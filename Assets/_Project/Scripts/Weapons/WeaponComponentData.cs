using UnityEngine;
using WeaponFactory.Logic;

namespace WeaponFactory.Weapons
{
    /// <summary>Inspector-editable weapon component stats (Resources/WF/Data/Components).</summary>
    [CreateAssetMenu(menuName = "Weapon Factory/Weapon Component", fileName = "Component_")]
    public class WeaponComponentData : ScriptableObject
    {
        public ComponentDefinition definition = new ComponentDefinition();
    }
}
