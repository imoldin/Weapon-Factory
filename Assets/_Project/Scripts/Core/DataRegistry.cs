using System.Collections.Generic;
using UnityEngine;
using WeaponFactory.Combat;
using WeaponFactory.Economy;
using WeaponFactory.Levels;
using WeaponFactory.Logic;
using WeaponFactory.Weapons;

namespace WeaponFactory.Core
{
    /// <summary>
    /// Loads ScriptableObject data from Resources/WF/Data and feeds it to the pure-logic catalogs.
    /// Any category without assets keeps the code defaults, so the game always has valid data.
    /// </summary>
    public static class DataRegistry
    {
        public const string DataPath = "WF/Data/";
        static readonly Dictionary<int, LevelDefinition> _levels = new Dictionary<int, LevelDefinition>();
        static bool _loaded;

        public static void Load()
        {
            _levels.Clear();
            foreach (var l in LevelLibrary.CreateDefaults()) _levels[l.levelNumber] = l;

            var levelAssets = Resources.LoadAll<LevelData>(DataPath + "Levels");
            foreach (var a in levelAssets)
                if (a != null && a.definition != null && a.definition.levelNumber > 0 && a.definition.segments != null && a.definition.waves != null && a.definition.waves.Count > 0)
                    _levels[a.definition.levelNumber] = a.definition;

            var comps = Resources.LoadAll<WeaponComponentData>(DataPath + "Components");
            if (comps.Length > 0)
            {
                var list = new List<ComponentDefinition>();
                foreach (var c in comps) if (c != null && c.definition != null) list.Add(c.definition);
                list.Sort((a, b) => a.type.CompareTo(b.type));
                ComponentCatalog.Override(list);
            }

            var evo = Resources.Load<WeaponData>(DataPath + "WeaponEvolution");
            if (evo != null) WeaponEvolution.Override(evo.tiers);

            var ups = Resources.LoadAll<UpgradeData>(DataPath + "Upgrades");
            if (ups.Length > 0)
            {
                var list = new List<UpgradeDefinition>();
                foreach (var u in ups) if (u != null && u.definition != null && u.definition.prices != null && u.definition.prices.Length > 0) list.Add(u.definition);
                list.Sort((a, b) => a.type.CompareTo(b.type));
                UpgradeCatalog.Override(list);
            }

            var enemies = Resources.LoadAll<EnemyData>(DataPath + "Enemies");
            if (enemies.Length > 0)
            {
                var list = new List<EnemyDefinition>();
                foreach (var e in enemies) if (e != null && e.definition != null) list.Add(e.definition);
                list.Sort((a, b) => a.type.CompareTo(b.type));
                EnemyCatalog.Override(list);
            }
            _loaded = true;
        }

        public static LevelDefinition GetLevel(int levelNumber)
        {
            if (!_loaded) Load();
            LevelDefinition def;
            if (_levels.TryGetValue(levelNumber, out def)) return def;
            var gen = LevelLibrary.GenerateProcedural(levelNumber);
            _levels[levelNumber] = gen;
            return gen;
        }
    }
}
