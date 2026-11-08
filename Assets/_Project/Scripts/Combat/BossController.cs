using UnityEngine;
using WeaponFactory.Audio;
using WeaponFactory.Levels;
using WeaponFactory.Logic;
using WeaponFactory.Player;
using WeaponFactory.Utilities;

namespace WeaponFactory.Combat
{
    /// <summary>
    /// Boss AI. Holds position at long range and cycles through special attacks:
    /// fan barrage, ground shockwave with a safe gap (move into the gap!), and drone summons.
    /// Below 50% health it becomes enraged: faster attacks and wider barrages.
    /// </summary>
    public class BossController : EnemyController
    {
        public string BossName = "FACTORY BOSS";
        public bool Enraged { get; private set; }

        int _pattern;
        bool _introDone;
        float _windup;
        int _pendingAttack = -1;

        public override void Init(EnemyDefinition def, EnemyHealth health, EnemyParts parts)
        {
            base.Init(def, health, parts);
            SpawnTimer = 1.4f;
            AttackTimer = 2.5f;
            AudioManager.Play(SfxId.BossRoar, 1f);
            ScreenShake.Add(0.5f);
        }

        protected override void Behave(float dt, PlayerController player)
        {
            if (!_introDone)
            {
                _introDone = true;
                var lm = LevelManager.Instance;
                if (lm != null) lm.OnBossAppeared(this);
            }

            var d = Definition;
            Vector3 pos = transform.position;
            Vector3 pp = player.transform.position;
            float targetZ = pp.z + d.preferredDistance;
            pos.z = Mathf.MoveTowards(pos.z, targetZ, d.moveSpeed * 2f * dt);
            float sway = Mathf.Sin(AliveTime * (Enraged ? 0.9f : 0.55f)) * 2.6f;
            pos.x = Mathf.MoveTowards(pos.x, sway, 1.5f * dt);
            transform.position = pos;

            if (!Enraged && Health.HealthFraction < 0.5f)
            {
                Enraged = true;
                AudioManager.Play(SfxId.BossRoar, 1f, 0f, 1.2f);
                ScreenShake.Add(0.4f);
                if (Parts.core != null) Parts.core.GetComponent<MeshRenderer>().sharedMaterial = GameAssets.Glow(new Color(1f, 0.1f, 0.1f), 4f);
                var lm = LevelManager.Instance;
                if (lm != null) lm.Notify("BOSS ENRAGED!", new Color(1f, 0.25f, 0.3f), true);
            }

            // telegraph (wind-up) before each attack so the player can react
            if (_pendingAttack >= 0)
            {
                _windup -= dt;
                if (_windup <= 0f)
                {
                    Execute(_pendingAttack, player);
                    _pendingAttack = -1;
                }
                return;
            }

            AttackTimer -= dt;
            if (AttackTimer <= 0f)
            {
                AttackTimer = d.attackInterval * (Enraged ? 0.65f : 1f);
                _pendingAttack = NextPattern();
                _windup = 0.45f;
                VFX.Play(VFXType.Ring, Health.AimPoint, new Color(1f, 0.3f, 0.7f), 20);
            }
        }

        int NextPattern()
        {
            // 0 barrage, 1 shockwave, 2 summon
            int[] cycle = { 0, 1, 0, 2, 1, 0 };
            int p = cycle[_pattern % cycle.Length];
            _pattern++;
            if (p == 2 && CombatManager.Instance != null && CombatManager.Instance.ActiveEnemyCount > 3) p = 0;
            return p;
        }

        void Execute(int pattern, PlayerController player)
        {
            switch (pattern)
            {
                case 0:
                    {
                        int shots = Enraged ? 7 : 5;
                        float spread = Enraged ? 34f : 26f;
                        for (int i = 0; i < shots; i++)
                        {
                            float a = Mathf.Lerp(-spread, spread, shots == 1 ? 0.5f : i / (float)(shots - 1));
                            Shoot(player, 9.5f, a);
                        }
                        break;
                    }
                case 1:
                    {
                        float gap = Random.Range(-2.8f, 2.8f);
                        BossShockwave.Spawn(transform.parent, transform.position.z - 1.5f, gap, Enraged ? 9f : 7f, Definition.rangedDamage * 2f);
                        AudioManager.Play(SfxId.Explosion, 0.8f, 0.05f, 0.7f);
                        ScreenShake.Add(0.35f);
                        VFX.Play(VFXType.Smoke, transform.position, new Color(0.4f, 0.3f, 0.45f, 0.8f), 20, 2f);
                        if (Parts.body != null) Tween.Punch(Parts.body, 0.15f, 0.3f);
                        break;
                    }
                case 2:
                    {
                        var lm = LevelManager.Instance;
                        if (lm != null)
                        {
                            lm.SpawnEnemy(EnemyType.Drone, transform.position + new Vector3(-2.5f, 1.5f, -1f));
                            lm.SpawnEnemy(EnemyType.Drone, transform.position + new Vector3(2.5f, 1.5f, -1f));
                            lm.Notify("DRONES DEPLOYED!", new Color(0.4f, 0.9f, 1f));
                        }
                        break;
                    }
            }
        }

        public override void Die()
        {
            if (Dead) return;
            base.Die();
            // big finale: chain explosions + the boss takes its minions with it
            for (int i = 0; i < 5; i++)
            {
                Vector3 p = transform.position + new Vector3(Random.Range(-2f, 2f), Random.Range(0.5f, 3.5f), Random.Range(-1f, 1f));
                Tween.Delay(0.12f * i, () => VFX.Explosion(p, 2f, new Color(1f, 0.3f, 0.8f)));
            }
            ScreenShake.Add(0.6f);
            var lm = LevelManager.Instance;
            if (lm != null) lm.KillAllEnemies();
        }
    }
}
