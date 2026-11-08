using System;
using UnityEngine;

namespace WeaponFactory.Audio
{
    public enum SfxId
    {
        Shoot, ShootHeavy, Rocket, Explosion, Hit, Crit, Pickup, Coin, GatePositive, GateNegative,
        Evolve, Reload, EnemyDie, PlayerHurt, Victory, Defeat, UIClick, Purchase, Error, BossRoar, Whoosh,
    }

    /// <summary>
    /// Synthesises every placeholder sound effect and the background music loop at startup,
    /// so the game is fully playable with original audio and no external assets.
    /// Replace any sound by dropping an AudioClip named after the SfxId into
    /// Assets/_Project/Resources/Audio/SFX/ (e.g. "Shoot.wav"), or music into Resources/Audio/Music/MainTheme.
    /// </summary>
    public static class ProceduralAudio
    {
        const int Rate = 22050;
        static System.Random _rng = new System.Random(1234);

        static float Noise() { return (float)(_rng.NextDouble() * 2.0 - 1.0); }

        static AudioClip Make(string name, float seconds, Func<float, float> sample, float gain = 0.9f)
        {
            int n = Mathf.Max(1, Mathf.RoundToInt(seconds * Rate));
            var data = new float[n];
            float peak = 0.0001f;
            for (int i = 0; i < n; i++)
            {
                float v = sample(i / (float)Rate);
                data[i] = v;
                float a = Mathf.Abs(v);
                if (a > peak) peak = a;
            }
            float norm = gain / peak;
            // short fade-in/out to avoid clicks
            int fade = Mathf.Min(64, n / 4);
            for (int i = 0; i < n; i++)
            {
                float f = 1f;
                if (i < fade) f = i / (float)fade;
                else if (i > n - fade) f = (n - i) / (float)fade;
                data[i] = Mathf.Clamp(data[i] * norm * f, -1f, 1f);
            }
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static float Sine(float phase) { return Mathf.Sin(phase * 6.2831853f); }
        static float Square(float phase) { return Mathf.Repeat(phase, 1f) < 0.5f ? 1f : -1f; }
        static float Saw(float phase) { return Mathf.Repeat(phase, 1f) * 2f - 1f; }
        static float Tri(float phase) { float p = Mathf.Repeat(phase, 1f); return p < 0.5f ? p * 4f - 1f : 3f - p * 4f; }
        static float Env(float t, float attack, float decay) { return t < attack ? t / attack : Mathf.Exp(-(t - attack) / decay); }

        public static AudioClip Create(SfxId id)
        {
            switch (id)
            {
                case SfxId.Shoot:
                    {
                        float lp = 0f;
                        return Make("Shoot", 0.14f, t =>
                        {
                            lp += (Noise() - lp) * 0.5f;
                            float body = Square(t * Mathf.Lerp(420f, 140f, t / 0.14f)) * 0.35f;
                            return (lp * 0.8f + body) * Env(t, 0.002f, 0.035f);
                        }, 0.6f);
                    }
                case SfxId.ShootHeavy:
                    {
                        float lp = 0f;
                        return Make("ShootHeavy", 0.3f, t =>
                        {
                            lp += (Noise() - lp) * 0.25f;
                            float body = Sine(t * Mathf.Lerp(160f, 45f, t / 0.3f)) * 0.9f;
                            return (lp * 0.7f + body) * Env(t, 0.003f, 0.08f);
                        }, 0.8f);
                    }
                case SfxId.Rocket:
                    {
                        float lp = 0f;
                        return Make("Rocket", 0.6f, t =>
                        {
                            lp += (Noise() - lp) * Mathf.Lerp(0.6f, 0.08f, t / 0.6f);
                            return lp * Env(t, 0.03f, 0.25f) + Sine(t * Mathf.Lerp(90f, 220f, t / 0.6f)) * 0.25f * Env(t, 0.01f, 0.2f);
                        }, 0.75f);
                    }
                case SfxId.Explosion:
                    {
                        float lp = 0f, lp2 = 0f;
                        return Make("Explosion", 0.9f, t =>
                        {
                            lp += (Noise() - lp) * 0.12f;
                            lp2 += (lp - lp2) * 0.3f;
                            float boom = Sine(t * Mathf.Lerp(90f, 30f, t)) * Env(t, 0.005f, 0.18f);
                            return lp2 * 2.2f * Env(t, 0.004f, 0.3f) + boom;
                        }, 0.95f);
                    }
                case SfxId.Hit:
                    return Make("Hit", 0.07f, t => (Noise() * 0.6f + Square(t * 900f) * 0.4f) * Env(t, 0.001f, 0.015f), 0.45f);
                case SfxId.Crit:
                    return Make("Crit", 0.16f, t => (Square(t * 1400f) * 0.5f + Sine(t * 2100f) * 0.5f) * Env(t, 0.001f, 0.05f), 0.5f);
                case SfxId.Pickup:
                    return Make("Pickup", 0.28f, t =>
                    {
                        float f = t < 0.08f ? 660f : (t < 0.16f ? 880f : 1320f);
                        return (Tri(t * f) * 0.7f + Sine(t * f * 2f) * 0.3f) * Env(t, 0.005f, 0.12f);
                    }, 0.6f);
                case SfxId.Coin:
                    return Make("Coin", 0.22f, t =>
                    {
                        float f = t < 0.06f ? 988f : 1319f;
                        return Square(t * f) * 0.5f * Env(t, 0.002f, 0.08f);
                    }, 0.4f);
                case SfxId.GatePositive:
                    return Make("GatePositive", 0.5f, t =>
                    {
                        float f = Mathf.Lerp(440f, 1320f, Mathf.Clamp01(t / 0.25f));
                        return (Saw(t * f) * 0.3f + Sine(t * f) * 0.7f) * Env(t, 0.01f, 0.2f);
                    }, 0.6f);
                case SfxId.GateNegative:
                    return Make("GateNegative", 0.45f, t =>
                    {
                        float f = Mathf.Lerp(300f, 90f, t / 0.45f);
                        return Square(t * f) * 0.6f * Env(t, 0.01f, 0.2f) * (0.7f + 0.3f * Sine(t * 18f));
                    }, 0.5f);
                case SfxId.Evolve:
                    return Make("Evolve", 1.0f, t =>
                    {
                        int step = Mathf.Min(4, (int)(t / 0.12f));
                        float[] notes = { 523f, 659f, 784f, 1047f, 1319f };
                        float f = notes[step];
                        float tt = t - step * 0.12f;
                        return (Saw(t * f) * 0.3f + Sine(t * f) * 0.6f + Sine(t * f * 0.5f) * 0.3f) * Env(tt, 0.005f, step == 4 ? 0.4f : 0.1f);
                    }, 0.7f);
                case SfxId.Reload:
                    return Make("Reload", 0.35f, t =>
                    {
                        float click1 = t < 0.03f ? Noise() * Env(t, 0.001f, 0.008f) : 0f;
                        float t2 = t - 0.22f;
                        float click2 = t2 > 0f && t2 < 0.05f ? (Noise() * 0.6f + Square(t2 * 1800f) * 0.4f) * Env(t2, 0.001f, 0.012f) : 0f;
                        return click1 + click2;
                    }, 0.45f);
                case SfxId.EnemyDie:
                    {
                        float lp = 0f;
                        return Make("EnemyDie", 0.5f, t =>
                        {
                            lp += (Noise() - lp) * 0.2f;
                            return lp * Env(t, 0.004f, 0.15f) + Square(t * Mathf.Lerp(600f, 80f, t / 0.5f)) * 0.3f * Env(t, 0.004f, 0.2f);
                        }, 0.7f);
                    }
                case SfxId.PlayerHurt:
                    return Make("PlayerHurt", 0.3f, t => (Square(t * Mathf.Lerp(220f, 110f, t / 0.3f)) * 0.5f + Noise() * 0.4f) * Env(t, 0.002f, 0.09f), 0.6f);
                case SfxId.Victory:
                    return Make("Victory", 1.6f, t =>
                    {
                        float[] notes = { 523f, 659f, 784f, 1047f, 784f, 1047f };
                        float[] starts = { 0f, 0.15f, 0.3f, 0.45f, 0.75f, 0.9f };
                        float v = 0f;
                        for (int i = 0; i < notes.Length; i++)
                        {
                            float tt = t - starts[i];
                            if (tt < 0f) continue;
                            v += (Square(tt * notes[i]) * 0.25f + Tri(tt * notes[i]) * 0.5f) * Env(tt, 0.005f, i == notes.Length - 1 ? 0.5f : 0.14f);
                        }
                        return v;
                    }, 0.7f);
                case SfxId.Defeat:
                    return Make("Defeat", 1.3f, t =>
                    {
                        float[] notes = { 392f, 370f, 349f, 262f };
                        int step = Mathf.Min(3, (int)(t / 0.25f));
                        float tt = t - step * 0.25f;
                        return (Tri(t * notes[step]) * 0.6f + Saw(t * notes[step] * 0.5f) * 0.2f) * Env(tt, 0.01f, step == 3 ? 0.5f : 0.2f);
                    }, 0.65f);
                case SfxId.UIClick:
                    return Make("UIClick", 0.06f, t => (Sine(t * 1200f) * 0.7f + Noise() * 0.1f) * Env(t, 0.001f, 0.015f), 0.4f);
                case SfxId.Purchase:
                    return Make("Purchase", 0.45f, t =>
                    {
                        float f = t < 0.1f ? 784f : (t < 0.2f ? 1047f : 1568f);
                        return (Square(t * f) * 0.3f + Sine(t * f) * 0.6f) * Env(t, 0.003f, 0.15f);
                    }, 0.55f);
                case SfxId.Error:
                    return Make("Error", 0.25f, t => Square(t * 140f) * 0.6f * Env(t, 0.005f, 0.1f), 0.45f);
                case SfxId.BossRoar:
                    {
                        float lp = 0f;
                        return Make("BossRoar", 1.2f, t =>
                        {
                            lp += (Noise() - lp) * 0.08f;
                            float growl = Saw(t * (55f + 8f * Sine(t * 7f))) * 0.6f;
                            return (growl + lp * 1.5f) * Env(t, 0.1f, 0.5f);
                        }, 0.8f);
                    }
                case SfxId.Whoosh:
                    {
                        float lp = 0f;
                        return Make("Whoosh", 0.4f, t =>
                        {
                            float k = Mathf.Lerp(0.05f, 0.5f, Mathf.Sin(t / 0.4f * Mathf.PI));
                            lp += (Noise() - lp) * k;
                            return lp * Mathf.Sin(t / 0.4f * Mathf.PI);
                        }, 0.5f);
                    }
            }
            return Make(id.ToString(), 0.05f, t => 0f);
        }

        /// <summary>
        /// Original 8-bar electronic loop (A minor: Am - F - C - G) at 124 BPM: kick, hats, bass and arpeggio.
        /// </summary>
        public static AudioClip CreateMusic()
        {
            const float bpm = 124f;
            float beat = 60f / bpm;
            int bars = 8;
            float length = bars * 4 * beat;
            int n = Mathf.RoundToInt(length * Rate);
            var data = new float[n];

            float[][] chords =
            {
                new[] { 220.00f, 261.63f, 329.63f }, // Am
                new[] { 174.61f, 220.00f, 261.63f }, // F
                new[] { 261.63f, 329.63f, 392.00f }, // C
                new[] { 196.00f, 246.94f, 293.66f }, // G
            };
            var rng = new System.Random(77);
            float hatLp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float beatPos = t / beat;
                int bar = (int)(beatPos / 4f) % bars;
                var chord = chords[(bar / 2) % 4];
                float inBeat = beatPos - Mathf.Floor(beatPos);
                float sixteenth = beatPos * 4f;
                int sIdx = (int)sixteenth;
                float inSixteenth = sixteenth - sIdx;

                // kick on every beat
                float kt = inBeat * beat;
                float kick = Sine(kt * Mathf.Lerp(120f, 45f, Mathf.Clamp01(kt / 0.12f))) * Mathf.Exp(-kt / 0.11f) * 0.9f;

                // hats on off-beats
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                hatLp += (noise - hatLp) * 0.9f;
                float hatEnv = (sIdx % 2 == 1) ? Mathf.Exp(-(inSixteenth * beat * 0.25f) / 0.018f) : 0f;
                float hat = (noise - hatLp) * hatEnv * 0.25f;

                // bass: root on 8ths, octave on off-8ths
                float bassFreq = chord[0] * 0.5f * ((sIdx % 4 == 2) ? 2f : 1f);
                float eighthPos = Mathf.Repeat(beatPos * 2f, 1f) * beat * 0.5f;
                float bass = (Saw(t * bassFreq) * 0.5f + Sine(t * bassFreq) * 0.5f) * Mathf.Exp(-eighthPos / 0.12f) * 0.45f;

                // arpeggio 16ths
                int note = sIdx % 4 == 3 ? 1 : sIdx % 3;
                float arpFreq = chord[note] * 2f;
                float arpT = inSixteenth * beat * 0.25f;
                float arp = (Square(t * arpFreq) * 0.3f + Tri(t * arpFreq) * 0.7f) * Mathf.Exp(-arpT / 0.06f) * 0.18f;

                // soft pad
                float pad = (Sine(t * chord[0]) + Sine(t * chord[1]) + Sine(t * chord[2])) * 0.05f;

                // sidechain-ish duck from kick
                float duck = 1f - Mathf.Exp(-kt / 0.08f) * 0.5f;
                data[i] = Mathf.Clamp((kick + hat + (bass + arp + pad) * duck) * 0.55f, -1f, 1f);
            }
            var clip = AudioClip.Create("MainTheme", n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
