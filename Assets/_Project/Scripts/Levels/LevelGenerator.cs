using UnityEngine;
using WeaponFactory.Collectibles;
using WeaponFactory.Gates;
using WeaponFactory.Logic;
using WeaponFactory.Obstacles;

namespace WeaponFactory.Levels
{
    /// <summary>
    /// Turns a data-only LevelDefinition into track objects. Each segment type has a small builder;
    /// segments are laid out back-to-back along +Z. New levels only need new data, not new code.
    /// </summary>
    public static class LevelGenerator
    {
        public const float LaneLimit = 3.3f;

        /// <returns>Z position where the arena begins.</returns>
        public static float Build(LevelDefinition def, Transform parent)
        {
            var rng = new System.Random(def.seed);
            float z = LevelDefinition.StartPadding;
            foreach (var seg in def.segments)
            {
                float len = seg.Length;
                BuildSegment(seg, parent, z, len, rng, def);
                z += len;
            }
            return z + LevelDefinition.EndPadding;
        }

        static float Lane(float x) { return Mathf.Clamp(x, -LaneLimit, LaneLimit); }

        static void CoinLine(Transform parent, float x, float z0, int count, float spacing = 1.6f)
        {
            for (int i = 0; i < count; i++)
                CollectibleSpawner.Spawn(parent, PickupKind.Coin, new Vector3(Lane(x), 0f, z0 + i * spacing), ComponentType.Barrel, 2);
        }

        static void BuildSegment(SegmentDefinition s, Transform parent, float z, float len, System.Random rng, LevelDefinition def)
        {
            float mid = z + len * 0.5f;
            switch (s.type)
            {
                case SegmentType.Empty:
                    break;

                case SegmentType.Coins:
                    CoinLine(parent, s.x, z + 2f, Mathf.Max(1, s.count));
                    break;

                case SegmentType.ComponentItem:
                    CoinLine(parent, s.x, z + 1f, 3, 1.4f);
                    CollectibleSpawner.Spawn(parent, PickupKind.Component, new Vector3(Lane(s.x), 0f, mid + 1.5f), s.component);
                    break;

                case SegmentType.ComponentChoice:
                    CollectibleSpawner.Spawn(parent, PickupKind.Component, new Vector3(-2.2f, 0f, mid), s.component);
                    CollectibleSpawner.Spawn(parent, PickupKind.Component, new Vector3(2.2f, 0f, mid), s.component2);
                    break;

                case SegmentType.GatePair:
                    GateManager.BuildPair(parent, mid, s.gate, rng, def.gateDifficulty);
                    break;

                case SegmentType.Saw:
                    Obstacle.BuildSaw(parent, new Vector3(Lane(s.x), 0f, mid));
                    CoinLine(parent, -Mathf.Sign(s.x == 0f ? 1f : s.x) * 2.2f, mid - 2f, 3);
                    break;

                case SegmentType.MovingSaw:
                    Obstacle.BuildMovingSaw(parent, new Vector3(0f, 0f, mid), s.value <= 0f ? 1f : s.value);
                    break;

                case SegmentType.Crusher:
                    Obstacle.BuildCrusher(parent, new Vector3(Mathf.Clamp(s.x, -2.6f, 2.6f), 0f, mid));
                    CoinLine(parent, s.x, mid - 5f, 3, 1.2f); // risky coins in front of the press
                    break;

                case SegmentType.LaserWall:
                    Obstacle.BuildLaserWall(parent, new Vector3(0f, 0f, mid), s.x);
                    CoinLine(parent, s.x, mid - 3f, 4, 1.5f);
                    break;

                case SegmentType.Hammer:
                    Obstacle.BuildHammer(parent, new Vector3(0f, 0f, mid), s.value <= 0f ? 1f : s.value);
                    break;

                case SegmentType.Barrier:
                    {
                        float bx = s.x == 0f ? 2.25f : Mathf.Sign(s.x) * 2.25f;
                        BreakableBarrier.Build(parent, new Vector3(bx, 0f, mid + 2f), Mathf.Max(5, Mathf.RoundToInt(s.value)));
                        CoinLine(parent, -bx, mid - 2f, 4, 1.4f);
                        break;
                    }

                case SegmentType.ExplosiveBarrels:
                    ExplosiveBarrel.Build(parent, new Vector3(Lane(s.x), 0f, mid));
                    CoinLine(parent, s.x >= 0f ? -2.2f : 2.2f, mid - 2f, 4, 1.3f);
                    break;

                case SegmentType.Pickup:
                    CollectibleSpawner.Spawn(parent, s.pickup, new Vector3(Lane(s.x), 0f, mid), s.component, PickupValue(s));
                    break;

                case SegmentType.Mixed:
                    {
                        float gx = s.x == 0f ? 2.2f : Lane(s.x);
                        CollectibleSpawner.Spawn(parent, s.pickup, new Vector3(gx, 0f, mid), s.component, PickupValue(s));
                        CollectibleSpawner.Spawn(parent, s.pickup2, new Vector3(-gx, 0f, mid), s.component, 1);
                        break;
                    }

                case SegmentType.Targets:
                    {
                        int n = Mathf.Clamp(s.count, 1, 5);
                        int hp = Mathf.Max(5, Mathf.RoundToInt(s.value));
                        for (int i = 0; i < n; i++)
                        {
                            float x = n == 1 ? 0f : Mathf.Lerp(-3.4f, 3.4f, i / (float)(n - 1));
                            TargetDummy.Build(parent, new Vector3(x, 0f, mid + (i % 2) * 1.5f), hp);
                        }
                        break;
                    }
            }
        }

        static int PickupValue(SegmentDefinition s)
        {
            if (s.value > 0f) return Mathf.RoundToInt(s.value);
            switch (s.pickup)
            {
                case PickupKind.UpgradeModule: return 2;
                case PickupKind.RepairKit: return 30;
                default: return 1;
            }
        }
    }
}
