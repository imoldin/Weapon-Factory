using UnityEngine;
using WeaponFactory.Logic;

namespace WeaponFactory.Combat
{
    /// <summary>Inspector-editable enemy stats (Resources/WF/Data/Enemies).</summary>
    [CreateAssetMenu(menuName = "Weapon Factory/Enemy", fileName = "Enemy_")]
    public class EnemyData : ScriptableObject
    {
        public EnemyDefinition definition = new EnemyDefinition();
    }
}
