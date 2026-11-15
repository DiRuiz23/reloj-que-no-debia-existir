using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;

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
        Resume,
        Jump,
        Pickup,
        Damage
    }

    /// <summary>
    /// Reproduce musica, ambiente y efectos de sonido de la escena.
    /// Es por escena (no persiste): cada nivel tiene su propio AudioManager con su musica.
    /// El audio normal se pausa con AudioListener.pause (Pausa); los sonidos de UI siguen sonando.
    /// Las fuentes se enrutan al AudioMixer (grupos Music y SFX); los volumenes del jugador se
    /// controlan con los parametros expuestos MusicVol y SfxVol.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public const string MusicVolParam = "MusicVol";
        public const string SfxVolParam = "SfxVol";
        const string MusicPrefKey = "audio.musicVol";
        const string SfxPrefKey = "audio.sfxVol";

        public static AudioManager Instance { get; private set; }

        [Header("Mixer (MainMixer: grupos Master / Music / SFX)")]
        [SerializeField] AudioMixer mixer;

        [Header("WebGL")]
        [Tooltip("Los navegadores bloquean el audio hasta que el jugador interactua. En WebGL la musica empieza tras el primer clic, tecla o toque. Activalo para probar ese comportamiento en el Editor.")]
        [SerializeField] bool requireGestureInEditor;

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
        [SerializeField] AudioClip jump;
        [SerializeField] AudioClip pickup;
        [SerializeField] AudioClip damage;

        [Header("Tic-tac (suena mientras algun enemigo esta Sospechoso)")]
        [SerializeField] AudioClip tickTock;
        [SerializeField, Range(0f, 1f)] float tickTockVolume = 0.6f;
        [SerializeField, Min(0.01f)] float tickTockFade = 0.15f;

        [Header("Volumenes (balance interno; el volumen del jugador va por el Mixer)")]
        [SerializeField, Range(0f, 1f)] float musicVolume = 0.45f;
        [SerializeField, Range(0f, 1f)] float windVolume = 0.35f;
        [SerializeField, Range(0f, 1f)] float rumbleVolume = 0.3f;
        [SerializeField, Range(0f, 1f)] float sfxVolume = 0.8f;
        [SerializeField, Min(0f)] float musicFadeIn = 2.5f;

        [Header("Efecto de Parada Temporal")]
        [Tooltip("Frecuencia de corte del filtro de la musica mientras el tiempo esta detenido.")]
        [SerializeField] float stoppedLowpassHz = 700f;
        [Range(0f, 1f)] [SerializeField] float stoppedMusicDuck = 0.6f;

        AudioSource musicSrc, windSrc, rumbleSrc, uiSrc, tickSrc;
        AudioSource[] sfxPool;
        AudioLowPassFilter musicFilter;
        AudioMixerGroup musicGroup, sfxGroup;
        int poolIndex;

        // El primer gesto del jugador se recuerda entre escenas: solo el menu tiene que esperarlo.
        static bool gestureReceived;
        bool waitingForGesture;

        // Estado de mezcla (se suaviza en Update).
        bool timeStopped;
        bool musicFadingOut;
        float musicFadeOutSeconds = 1.5f;
        bool suspicious;
        float musicMul = 0f;       // 0..1 (fundido de entrada / salida)
        float rumbleMul = 1f;
        float tickMul = 0f;
        float duckCurrent = 1f;
        float lowpassCurrent = 22000f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; gestureReceived = false; }

        void Awake()
        {
            Instance = this;

            if (mixer != null)
            {
                var m = mixer.FindMatchingGroups("Music");
                if (m != null && m.Length > 0) musicGroup = m[0];
                var s = mixer.FindMatchingGroups("SFX");
                if (s != null && s.Length > 0) sfxGroup = s[0];
            }

            musicSrc = CreateSource("Music", true, false, musicGroup);
            musicFilter = musicSrc.gameObject.AddComponent<AudioLowPassFilter>();
            musicFilter.cutoffFrequency = 22000f;
            windSrc = CreateSource("Ambience_Wind", true, false, musicGroup);
            rumbleSrc = CreateSource("Ambience_Rumble", true, false, musicGroup);
            uiSrc = CreateSource("SFX_UI", false, true, sfxGroup);   // suena aun en pausa
            tickSrc = CreateSource("SFX_TickTock", true, false, sfxGroup);

            sfxPool = new AudioSource[8];
            for (int i = 0; i < sfxPool.Length; i++)
                sfxPool[i] = CreateSource("SFX_" + i, false, false, sfxGroup);
        }

        void Start()
        {
            ApplySavedVolumes();

            bool needsGesture = Application.platform == RuntimePlatform.WebGLPlayer || requireGestureInEditor;
            if (needsGesture && !gestureReceived) waitingForGesture = true;   // la musica empieza tras el primer clic
            else StartAllLoops();
        }

        void StartAllLoops()
        {
            musicMul = 0f;   // el fundido de entrada arranca ahora
            StartLoop(musicSrc, music);
            StartLoop(windSrc, ambienceWind);
            StartLoop(rumbleSrc, ambienceRumble);
            StartLoop(tickSrc, tickTock);
        }

        static bool AnyGesture()
        {
            if (Mouse.current != null && (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame)) return true;
            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) return true;
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) return true;
            var pad = Gamepad.current;
            if (pad != null && (pad.buttonSouth.wasPressedThisFrame || pad.startButton.wasPressedThisFrame)) return true;
            return false;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        AudioSource CreateSource(string name, bool loop, bool ignoreListenerPause, AudioMixerGroup group)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var s = go.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.loop = loop;
            s.spatialBlend = 0f;   // 2D
            s.ignoreListenerPause = ignoreListenerPause;
            if (group != null) s.outputAudioMixerGroup = group;
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
        public void FadeOutMusic(float seconds = 1.5f)
        {
            musicFadeOutSeconds = Mathf.Max(0.05f, seconds);
            musicFadingOut = true;
        }

        /// <summary>true mientras al menos un enemigo esta en estado Sospechoso: activa el tic-tac.</summary>
        public void SetSuspicious(bool value) { suspicious = value; }

        /// <summary>Volumen de la musica del jugador (0 a 1). Se guarda y se aplica al Mixer (MusicVol).</summary>
        public void SetMusicVolume(float value01)
        {
            value01 = Mathf.Clamp01(value01);
            PlayerPrefs.SetFloat(MusicPrefKey, value01);
            ApplyMixer(MusicVolParam, value01);
        }

        /// <summary>Volumen de efectos del jugador (0 a 1). Se guarda y se aplica al Mixer (SfxVol).</summary>
        public void SetSfxVolume(float value01)
        {
            value01 = Mathf.Clamp01(value01);
            PlayerPrefs.SetFloat(SfxPrefKey, value01);
            ApplyMixer(SfxVolParam, value01);
        }

        public static float SavedMusicVolume => PlayerPrefs.GetFloat(MusicPrefKey, 1f);
        public static float SavedSfxVolume => PlayerPrefs.GetFloat(SfxPrefKey, 1f);

        void ApplySavedVolumes()
        {
            ApplyMixer(MusicVolParam, SavedMusicVolume);
            ApplyMixer(SfxVolParam, SavedSfxVolume);
        }

        void ApplyMixer(string param, float value01)
        {
            if (mixer == null) return;
            // 0 -> -80 dB (silencio); 1 -> 0 dB. Escala logaritmica para que el slider se sienta lineal.
            float db = value01 <= 0.0001f ? -80f : Mathf.Log10(value01) * 20f;
            mixer.SetFloat(param, db);
        }

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
                case Sfx.Jump: PlayClip(jump, 0.6f, 0.95f, 1.05f); break;
                case Sfx.Pickup: PlayClip(pickup, 0.8f, 0.97f, 1.03f); break;
                case Sfx.Damage: PlayClip(damage, 0.9f); break;
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

            if (waitingForGesture && AnyGesture())
            {
                gestureReceived = true;
                waitingForGesture = false;
                StartAllLoops();
            }

            // Musica: fundido de entrada, y de salida si se pidio.
            float musicTarget = musicFadingOut ? 0f : 1f;
            float rate = musicFadingOut ? 1f / musicFadeOutSeconds : (musicFadeIn > 0f ? 1f / musicFadeIn : 1000f);
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

            // Tic-tac: sube y baja rapido segun haya enemigos Sospechosos.
            tickMul = Mathf.MoveTowards(tickMul, suspicious ? 1f : 0f, dt / tickTockFade);
            tickSrc.volume = tickTockVolume * sfxVolume * tickMul;
        }
    }
}
