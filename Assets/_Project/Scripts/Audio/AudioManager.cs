using System.Collections;
using UnityEngine;

namespace Reloj.Sound
{
    public enum Sfx
    {
        Footstep,
        TimeStopStart,
        TimeStopEnd,
        TimeStopReady,
        Checkpoint,
        Death,
        Respawn,
        Goal,
        DialogueOpen,
        DialogueClose,
        UiClick,
        Pause,
        Resume
    }

    /// <summary>
    /// Reproduce musica, ambiente y efectos de sonido de la escena.
    /// Es por escena (no persiste): cada nivel tiene su propio AudioManager con su musica.
    /// El audio normal se pausa con AudioListener.pause (Pausa); los sonidos de UI siguen sonando.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Musica y ambiente")]
        [SerializeField] AudioClip music;
        [SerializeField] AudioClip ambienceWind;
        [SerializeField] AudioClip ambienceRumble;

        [Header("Efectos (arrastra otro clip para reemplazar el de relleno)")]
        [SerializeField] AudioClip[] footsteps;
        [SerializeField] AudioClip timeStopStart;
        [SerializeField] AudioClip timeStopEnd;
        [SerializeField] AudioClip timeStopReady;
        [SerializeField] AudioClip checkpoint;
        [SerializeField] AudioClip death;
        [SerializeField] AudioClip respawn;
        [SerializeField] AudioClip goal;
        [SerializeField] AudioClip dialogueOpen;
        [SerializeField] AudioClip dialogueClose;
        [SerializeField] AudioClip uiClick;
        [SerializeField] AudioClip pause;
        [SerializeField] AudioClip resume;

        [Header("Volumenes")]
        [SerializeField, Range(0f, 1f)] float musicVolume = 0.45f;
        [SerializeField, Range(0f, 1f)] float windVolume = 0.35f;
        [SerializeField, Range(0f, 1f)] float rumbleVolume = 0.3f;
        [SerializeField, Range(0f, 1f)] float sfxVolume = 0.8f;
        [SerializeField, Min(0f)] float musicFadeIn = 2.5f;

        [Header("Efecto de Parada Temporal")]
        [Tooltip("Frecuencia de corte del filtro de la musica mientras el tiempo esta detenido.")]
        [SerializeField] float stoppedLowpassHz = 700f;
        [Range(0f, 1f)] [SerializeField] float stoppedMusicDuck = 0.6f;

        AudioSource musicSrc, windSrc, rumbleSrc, uiSrc;
        AudioSource[] sfxPool;
        AudioLowPassFilter musicFilter;
        int poolIndex;

        // Estado de mezcla (se suaviza en Update).
        bool timeStopped;
        bool musicFadingOut;
        float musicMul = 0f;       // 0..1 (fundido de entrada / salida)
        float rumbleMul = 1f;
        float duckCurrent = 1f;
        float lowpassCurrent = 22000f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        void Awake()
        {
            Instance = this;

            musicSrc = CreateSource("Music", true, false);
            musicFilter = musicSrc.gameObject.AddComponent<AudioLowPassFilter>();
            musicFilter.cutoffFrequency = 22000f;
            windSrc = CreateSource("Ambience_Wind", true, false);
            rumbleSrc = CreateSource("Ambience_Rumble", true, false);
            uiSrc = CreateSource("SFX_UI", false, true);   // suena aun en pausa

            sfxPool = new AudioSource[8];
            for (int i = 0; i < sfxPool.Length; i++)
                sfxPool[i] = CreateSource("SFX_" + i, false, false);
        }

        void Start()
        {
            StartLoop(musicSrc, music);
            StartLoop(windSrc, ambienceWind);
            StartLoop(rumbleSrc, ambienceRumble);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        AudioSource CreateSource(string name, bool loop, bool ignoreListenerPause)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var s = go.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.loop = loop;
            s.spatialBlend = 0f;   // 2D
            s.ignoreListenerPause = ignoreListenerPause;
            return s;
        }

        static void StartLoop(AudioSource s, AudioClip clip)
        {
            if (clip == null) return;
            s.clip = clip;
            s.volume = 0f;
            s.Play();
        }

        // ------------------------------------------------------------------ API

        public void SetTimeStopped(bool value) { timeStopped = value; }

        /// <summary>Baja la musica hasta silencio (fin de nivel / game over).</summary>
        public void FadeOutMusic() { musicFadingOut = true; }

        public void Play(Sfx id)
        {
            switch (id)
            {
                case Sfx.Footstep:
                    if (footsteps == null || footsteps.Length == 0) return;
                    PlayClip(footsteps[Random.Range(0, footsteps.Length)], 0.55f, 0.9f, 1.1f);
                    break;
                case Sfx.TimeStopStart: PlayClip(timeStopStart, 1f); break;
                case Sfx.TimeStopEnd: PlayClip(timeStopEnd, 0.9f); break;
                case Sfx.TimeStopReady: PlayClip(timeStopReady, 0.7f); break;
                case Sfx.Checkpoint: PlayClip(checkpoint, 0.9f); break;
                case Sfx.Death: PlayClip(death, 1f); break;
                case Sfx.Respawn: PlayClip(respawn, 0.7f); break;
                case Sfx.Goal: PlayClip(goal, 1f); break;
                case Sfx.DialogueOpen: PlayClip(dialogueOpen, 0.7f, 0.97f, 1.03f); break;
                case Sfx.DialogueClose: PlayClip(dialogueClose, 0.7f, 0.97f, 1.03f); break;
                case Sfx.UiClick: PlayUi(uiClick, 0.8f); break;
                case Sfx.Pause: PlayUi(pause, 0.8f); break;
                case Sfx.Resume: PlayUi(resume, 0.8f); break;
            }
        }

        void PlayClip(AudioClip clip, float volume, float pitchMin = 1f, float pitchMax = 1f)
        {
            if (clip == null) return;
            var s = sfxPool[poolIndex];
            poolIndex = (poolIndex + 1) % sfxPool.Length;
            s.pitch = Random.Range(pitchMin, pitchMax);
            s.PlayOneShot(clip, volume * sfxVolume);
        }

        void PlayUi(AudioClip clip, float volume)
        {
            if (clip == null) return;
            uiSrc.pitch = 1f;
            uiSrc.PlayOneShot(clip, volume * sfxVolume);
        }

        // ------------------------------------------------------------------ mezcla

        void Update()
        {
            float dt = Time.unscaledDeltaTime;

            // Musica: fundido de entrada, y de salida si se pidio.
            float musicTarget = musicFadingOut ? 0f : 1f;
            float rate = musicFadingOut ? 1f / 1.5f : (musicFadeIn > 0f ? 1f / musicFadeIn : 1000f);
            musicMul = Mathf.MoveTowards(musicMul, musicTarget, rate * dt);

            // Parada Temporal: la musica se "apaga" (filtro paso bajo + menos volumen) y el traqueteo calla.
            float duck = timeStopped ? (1f - stoppedMusicDuck) : 1f;
            float targetHz = timeStopped ? stoppedLowpassHz : 22000f;
            lowpassCurrent = Mathf.Lerp(lowpassCurrent, targetHz, 1f - Mathf.Exp(-6f * dt));
            musicFilter.cutoffFrequency = Mathf.Clamp(lowpassCurrent, 200f, 22000f);

            duckCurrent = Mathf.Lerp(duckCurrent, duck, 1f - Mathf.Exp(-5f * dt));
            musicSrc.volume = musicVolume * musicMul * duckCurrent;

            windSrc.volume = Mathf.MoveTowards(windSrc.volume, windVolume * (musicFadingOut ? 0.4f : 1f), dt * 0.3f);

            rumbleMul = Mathf.MoveTowards(rumbleMul, timeStopped ? 0f : 1f, dt * (timeStopped ? 4f : 1.2f));
            rumbleSrc.volume = rumbleVolume * rumbleMul * (musicFadingOut ? 0.4f : 1f);
        }
    }
}
