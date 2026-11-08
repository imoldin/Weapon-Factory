using System;
using System.Collections.Generic;

namespace WeaponFactory.Logic
{
    public enum GateEffectType
    {
        DamagePercent = 0,     // value = percent, e.g. 50 => +50%
        FireRatePercent = 1,
        MagazineAdd = 2,       // value = flat rounds
        DamageMultiply = 3,    // value = multiplier, e.g. 2 => x2
        BarrelAdd = 4,         // value = barrels
        CritPercent = 5,       // value = percentage points
        PowerAdd = 6,          // value = evolution power
        MagazinePercent = 7,
    }

    [Serializable]
    public class GateEffect
    {
        public GateEffectType type;
        public float value;

        public GateEffect() { }
        public GateEffect(GateEffectType type, float value) { this.type = type; this.value = value; }

        public bool IsPositive
        {
            get { return type == GateEffectType.DamageMultiply ? value >= 1f : value > 0f; }
        }

        /// <summary>Big number line shown on the gate, e.g. "+50%" or "x2".</summary>
        public string ValueLabel
        {
            get
            {
                switch (type)
                {
                    case GateEffectType.DamageMultiply:
                        return "x" + FormatNumber(value);
                    case GateEffectType.DamagePercent:
                    case GateEffectType.FireRatePercent:
                    case GateEffectType.CritPercent:
                    case GateEffectType.MagazinePercent:
                        return (value >= 0 ? "+" : "") + FormatNumber(value) + "%";
                    default:
                        return (value >= 0 ? "+" : "") + FormatNumber(value);
                }
            }
        }

        /// <summary>Caption under the number, e.g. "DAMAGE".</summary>
        public string Caption
        {
            get
            {
                switch (type)
                {
                    case GateEffectType.DamagePercent:
                    case GateEffectType.DamageMultiply: return "DAMAGE";
                    case GateEffectType.FireRatePercent: return "FIRE RATE";
                    case GateEffectType.MagazineAdd:
                    case GateEffectType.MagazinePercent: return "MAGAZINE";
                    case GateEffectType.BarrelAdd: return Math.Abs(value) > 1 ? "BARRELS" : "BARREL";
                    case GateEffectType.CritPercent: return "CRIT CHANCE";
                    case GateEffectType.PowerAdd: return "UPGRADE";
                }
                return "";
            }
        }

        public string FullLabel { get { return ValueLabel + " " + Caption; } }

        static string FormatNumber(float v)
        {
            if (Math.Abs(v - Math.Round(v)) < 0.01f) return ((int)Math.Round(v)).ToString();
            return v.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>Applies the effect to run modifiers / build. Results are clamped to safe ranges.</summary>
        public void Apply(RunModifiers run, WeaponBuild build)
        {
            switch (type)
            {
                case GateEffectType.DamagePercent: run.damageMultiplier *= 1f + value / 100f; break;
                case GateEffectType.FireRatePercent: run.fireRateMultiplier *= 1f + value / 100f; break;
                case GateEffectType.MagazineAdd: run.magazineBonus += (int)Math.Round(value); break;
                case GateEffectType.MagazinePercent: run.magazineMultiplier *= 1f + value / 100f; break;
                case GateEffectType.DamageMultiply: run.damageMultiplier *= value; break;
                case GateEffectType.BarrelAdd: run.extraProjectiles += (int)Math.Round(value); break;
                case GateEffectType.CritPercent: run.critBonus += value / 100f; break;
                case GateEffectType.PowerAdd: if (build != null) build.AddPower((int)Math.Round(value)); break;
            }
            run.Clamp();
        }

        public GateEffect Clone() { return new GateEffect(type, value); }
    }

    /// <summary>A pair of side-by-side gates. The player can pass through only one.</summary>
    [Serializable]
    public class GatePairDefinition
    {
        public GateEffect left;
        public GateEffect right;
        /// <summary>If true, the generator replaces left/right with a balanced random pair.</summary>
        public bool randomize;
    }

    /// <summary>
    /// Generates fair gate pairs: never two negatives, never the same stat on both sides,
    /// and a negative is always paired with a clearly positive alternative.
    /// </summary>
    public static class GateGenerator
    {
        static readonly GateEffect[] PositivePool =
        {
            new GateEffect(GateEffectType.DamagePercent, 30),
            new GateEffect(GateEffectType.DamagePercent, 50),
            new GateEffect(GateEffectType.FireRatePercent, 20),
            new GateEffect(GateEffectType.FireRatePercent, 30),
            new GateEffect(GateEffectType.MagazineAdd, 10),
            new GateEffect(GateEffectType.MagazineAdd, 20),
            new GateEffect(GateEffectType.CritPercent, 15),
            new GateEffect(GateEffectType.CritPercent, 25),
            new GateEffect(GateEffectType.PowerAdd, 2),
            new GateEffect(GateEffectType.PowerAdd, 3),
        };

        // Rare, strong positives — unlocked by difficulty.
        static readonly GateEffect[] StrongPositivePool =
        {
            new GateEffect(GateEffectType.DamageMultiply, 2),
            new GateEffect(GateEffectType.BarrelAdd, 1),
            new GateEffect(GateEffectType.PowerAdd, 5),
        };

        static readonly GateEffect[] NegativePool =
        {
            new GateEffect(GateEffectType.DamagePercent, -30),
            new GateEffect(GateEffectType.FireRatePercent, -20),
            new GateEffect(GateEffectType.MagazinePercent, -20),
            new GateEffect(GateEffectType.BarrelAdd, -1),
            new GateEffect(GateEffectType.PowerAdd, -2),
        };

        /// <param name="difficulty">0..1 — higher means more negatives and more strong positives.</param>
        public static GatePairDefinition Generate(Random rng, float difficulty)
        {
            if (difficulty < 0f) difficulty = 0f;
            if (difficulty > 1f) difficulty = 1f;

            GateEffect a = Pick(rng, rng.NextDouble() < 0.15 + 0.25 * difficulty ? StrongPositivePool : PositivePool);
            GateEffect b;
            bool negative = rng.NextDouble() < 0.25 + 0.45 * difficulty;
            int guard = 0;
            do
            {
                b = negative ? Pick(rng, NegativePool) : Pick(rng, PositivePool);
                guard++;
            } while (Category(b) == Category(a) && guard < 20);
            if (Category(b) == Category(a)) b = new GateEffect(GateEffectType.MagazineAdd, 10);

            var pair = new GatePairDefinition();
            if (rng.Next(2) == 0) { pair.left = a; pair.right = b; }
            else { pair.left = b; pair.right = a; }
            return pair;
        }

        /// <summary>Returns a fair version of a pair (fixes two negatives / duplicates).</summary>
        public static GatePairDefinition Sanitize(GatePairDefinition pair, Random rng, float difficulty)
        {
            if (pair == null || pair.randomize || pair.left == null || pair.right == null)
                return Generate(rng, difficulty);
            if (!pair.left.IsPositive && !pair.right.IsPositive)
                pair.right = Pick(rng, PositivePool);
            if (Category(pair.left) == Category(pair.right) && pair.left.IsPositive == pair.right.IsPositive)
                pair.right = new GateEffect(GateEffectType.MagazineAdd, 10);
            return pair;
        }

        public static bool IsFair(GatePairDefinition p)
        {
            if (p == null || p.left == null || p.right == null) return false;
            if (!p.left.IsPositive && !p.right.IsPositive) return false;
            if (Category(p.left) == Category(p.right) && p.left.IsPositive == p.right.IsPositive) return false;
            return true;
        }

        static int Category(GateEffect e)
        {
            switch (e.type)
            {
                case GateEffectType.DamageMultiply:
                case GateEffectType.DamagePercent: return 0;
                case GateEffectType.MagazineAdd:
                case GateEffectType.MagazinePercent: return 2;
                default: return 10 + (int)e.type;
            }
        }

        static GateEffect Pick(Random rng, GateEffect[] pool) { return pool[rng.Next(pool.Length)].Clone(); }
    }
}
