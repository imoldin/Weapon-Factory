using UnityEngine;

namespace WeaponFactory.Audio
{
    /// <summary>
    /// Persistent audio system: one looping music source + a small round-robin pool of SFX sources.
    /// Separate music / SFX volumes and a global mute. Rapid-fire sounds are rate-limited.
    /// Custom clips placed in Resources/Audio/SFX/&lt;SfxId&gt; and Resources/Audio/Music/MainTheme
    /// automatically replace the procedural placeholders.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        const int PoolSize = 12;
        AudioSource _music;
        AudioSource[] _sfx;
        int _next;
        AudioClip[] _clips;
        float[] _lastPlayed;
        float _musicVolume = 0.6f, _sfxVolume = 0.85f;
        bool _muted;

        public static AudioManager Create(Transform parent)
        {
            var go = new GameObject("AudioManager");
            go.transform.SetParent(parent, false);
            var am = go.AddComponent<AudioManager>();
            am.Init();
            return am;
        }

        void Init()
        {
            Instance = this;
            _music = gameObject.AddComponent<AudioSource>();
            _music.loop = true;
            _music.playOnAwake = false;
            _music.spatialBlend = 0f;
            _music.priority = 0;

            _sfx = new AudioSource[PoolSize];
            for (int i = 0; i < PoolSize; i++)
            {
                var s = gameObject.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.spatialBlend = 0f;
                _sfx[i] = s;
            }

            int n = System.Enum.GetValues(typeof(SfxId)).Length;
            _clips = new AudioClip[n];
            _lastPlayed = new float[n];
            for (int i = 0; i < n; i++)
            {
                var id = (SfxId)i;
                var custom = Resources.Load<AudioClip>("Audio/SFX/" + id);
                _clips[i] = custom != null ? custom : ProceduralAudio.Create(id);
                _lastPlayed[i] = -10f;
            }

            var music = Resources.Load<AudioClip>("Audio/Music/MainTheme");
            _music.clip = music != null ? music : ProceduralAudio.CreateMusic();
        }

        public void ApplySettings(float musicVolume, float sfxVolume, bool muted)
        {
            _musicVolume = Mathf.Clamp01(musicVolume);
            _sfxVolume = Mathf.Clamp01(sfxVolume);
            _muted = muted;
            _music.volume = _muted ? 0f : _musicVolume * 0.55f;
            if (!_music.isPlaying && _music.clip != null) _music.Play();
        }

        static float MinInterval(SfxId id)
        {
            switch (id)
            {
                case SfxId.Shoot: return 0.045f;
                case SfxId.Hit: return 0.035f;
                case SfxId.Coin: return 0.04f;
                case SfxId.Crit: return 0.06f;
                case SfxId.Explosion: return 0.08f;
                default: return 0.02f;
            }
        }

        public static void Play(SfxId id, float volume = 1f, float pitchJitter = 0.06f, float pitch = 1f)
        {
            var inst = Instance;
            if (inst == null || inst._muted || inst._sfxVolume <= 0.001f) return;
            int i = (int)id;
            float now = Time.unscaledTime;
            if (now - inst._lastPlayed[i] < MinInterval(id)) return;
            inst._lastPlayed[i] = now;

            var src = inst._sfx[inst._next];
            inst._next = (inst._next + 1) % PoolSize;
            src.pitch = pitch * (1f + Random.Range(-pitchJitter, pitchJitter));
            src.PlayOneShot(inst._clips[i], Mathf.Clamp01(volume) * inst._sfxVolume);
        }

        /// <summary>Lowers the music briefly (e.g. on victory jingle).</summary>
        public void DuckMusic(float seconds)
        {
            if (_muted) return;
            StopAllCoroutines();
            StartCoroutine(Duck(seconds));
        }

        System.Collections.IEnumerator Duck(float seconds)
        {
            float target = _musicVolume * 0.55f;
            _music.volume = target * 0.25f;
            yield return new WaitForSecondsRealtime(seconds);
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime;
                _music.volume = Mathf.Lerp(target * 0.25f, _muted ? 0f : target, t);
                yield return null;
            }
        }
    }
}
