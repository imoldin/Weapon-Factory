using UnityEngine;
using WeaponFactory.Logic;

namespace WeaponFactory.Levels
{
    /// <summary>
    /// Inspector-editable level configuration. Put assets in Resources/WF/Data/Levels to override
    /// the built-in defaults (menu: Weapon Factory/Export Default Data Assets creates all 10).
    /// Adding a new level = creating a new LevelData asset with the next level number.
    /// </summary>
    [CreateAssetMenu(menuName = "Weapon Factory/Level Data", fileName = "Level_00")]
    public class LevelData : ScriptableObject
    {
        public LevelDefinition definition = new LevelDefinition();
    }
}
