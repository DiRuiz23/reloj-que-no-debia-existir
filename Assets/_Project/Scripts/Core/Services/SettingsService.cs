using System;
using System.Collections;
using System.Collections.Generic;
using Reloj.Tutorial;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;

namespace Reloj.Core
{
    public enum RebindOutcome { Applied, Cancelled, Conflict, Unavailable }

    public readonly struct RebindResult
    {
        public readonly RebindOutcome outcome;
        /// <summary>Tecla/boton elegido (texto legible). Solo en Applied y Conflict.</summary>
        public readonly string key;
        /// <summary>Nombre (es) de la accion que ya usaba esa tecla. Solo en Conflict.</summary>
        public readonly string conflictAction;

        public RebindResult(RebindOutcome outcome, string key = null, string conflictAction = null)
        {
            this.outcome = outcome; this.key = key; this.conflictAction = conflictAction;
        }
    }

    /// <summary>
    /// Opciones del jugador (I-36). Vive en GameServices (persiste entre escenas).
    ///  - Volumen: AudioMixer (MusicVol / SfxVol), 0 = silencio total, conversion log a decibelios.
    ///  - Calidad: QualitySettings.SetQualityLevel.
    ///  - Controles: overrides del InputActionAsset (PerformInteractiveRebinding) guardados como JSON.
    /// Todo se guarda en SaveService.Data.options (I-34) y se aplica al arrancar el juego.
    /// </summary>
    public class SettingsService : MonoBehaviour
    {
        public const string MusicVolParam = "MusicVol";
        public const string SfxVolParam = "SfxVol";
        public const float DefaultVolume = 1f;
        const float SaveDelaySeconds = 0.6f;
        const float RebindTimeoutSeconds = 15f;

        public static SettingsService Instance { get; private set; }

        [SerializeField] AudioMixer mixer;
        [Tooltip("Asset PlayerControls.inputactions (el mismo que usan las InputActionReference).")]
        [SerializeField] InputActionAsset controls;
        [Tooltip("Calidad por defecto en PC. En WebGL y movil se usa la Baja (indice 0).")]
        [SerializeField] int defaultQualityStandalone = 2;

        bool _dirty;
        float _saveAt;

        InputActionRebindingExtensions.RebindingOperation _op;
        readonly List<InputAction> _pausedActions = new List<InputAction>();
        string _snapshot;
        Coroutine _reenableRoutine;
        static bool _warnedMissingMixer;

        public InputActionAsset Controls => controls;
        public bool IsRebinding => _op != null;

        /// <summary>Se dispara cuando cambian los valores desde fuera del panel (Restablecer, carga).</summary>
        public event Action Changed;
        /// <summary>Se dispara cuando cambia alguna tecla/boton.</summary>
        public event Action BindingsChanged;
        /// <summary>Evento estatico global para etiquetas fuera del panel (InputPromptLabel).</summary>
        public static event Action AnyBindingsChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Instance = null;
            AnyBindingsChanged = null;
            _warnedMissingMixer = false;
        }

        SaveService Save => GameServices.Instance != null && GameServices.Instance.Save != null
            ? GameServices.Instance.Save
            : GetComponentInParent<GameServices>()?.Save;

        OptionsData Options => Save != null
            ? Save.Data.options
            : _fallback ?? (_fallback = new OptionsData());
        OptionsData _fallback;

        public float MusicVolume => Mathf.Clamp01(Options.musicVolume);
        public float SfxVolume => Mathf.Clamp01(Options.sfxVolume);

        public int DefaultQualityLevel
        {
            get
            {
                bool low = Application.isMobilePlatform || Application.platform == RuntimePlatform.WebGLPlayer;
                return ClampQuality(low ? 0 : defaultQualityStandalone);
            }
        }

        public int QualityLevel
        {
            get
            {
                int q = Options.qualityLevel;
                return ClampQuality(q < 0 ? DefaultQualityLevel : q);
            }
        }

        static int ClampQuality(int q) => Mathf.Clamp(q, 0, Mathf.Max(0, QualitySettings.names.Length - 1));

        // ------------------------------------------------------------------ ciclo de vida

        void Awake()
        {
            if (Instance != null && Instance != this) return;   // duplicado al volver a Boot
            Instance = this;
            ApplyAll();
        }

        void Start()
        {
            // El Mixer a veces ignora SetFloat durante Awake: se repite un frame despues.
            if (Instance == this) ApplyAudio();
        }

        void OnDestroy()
        {
            if (Instance == this) { CancelRebind(true); Flush(); Instance = null; }
        }

        void Update()
        {
            if (_dirty && Time.unscaledTime >= _saveAt) Flush();
        }

        // Android puede cerrar la app sin avisar y WebGL cierra la pestana: se guarda al perder el foco.
        void OnApplicationPause(bool paused) { if (paused) Flush(); }
        void OnApplicationFocus(bool focus) { if (!focus) Flush(); }
        void OnApplicationQuit() { Flush(); }

        /// <summary>Aplica volumen, calidad y controles guardados.</summary>
        public void ApplyAll()
        {
            Application.targetFrameRate = 60;
            ApplyAudio();
            QualitySettings.SetQualityLevel(QualityLevel, true);
            LoadBindings(Options.inputBindingsJson);
        }

        public void ApplyAudio()
        {
            ApplyMixer(MusicVolParam, MusicVolume);
            ApplyMixer(SfxVolParam, SfxVolume);
        }

        // ------------------------------------------------------------------ guardado

        void MarkDirty() { _dirty = true; _saveAt = Time.unscaledTime + SaveDelaySeconds; }

        /// <summary>Guarda ya si hay cambios pendientes (el panel lo llama al cerrar).</summary>
        public void Flush()
        {
            if (!_dirty) return;
            _dirty = false;
            var save = Save;
            if (save != null) save.Save();
        }

        // ------------------------------------------------------------------ audio

        public void SetMusicVolume(float value01)
        {
            Options.musicVolume = Mathf.Clamp01(value01);
            ApplyMixer(MusicVolParam, Options.musicVolume);
            MarkDirty();
        }

        public void SetSfxVolume(float value01)
        {
            Options.sfxVolume = Mathf.Clamp01(value01);
            ApplyMixer(SfxVolParam, Options.sfxVolume);
            MarkDirty();
        }

        /// <summary>0 -> -80 dB (silencio total); 1 -> 0 dB. Escala logaritmica.</summary>
        void ApplyMixer(string param, float value01)
        {
            if (mixer == null)
            {
                if (!_warnedMissingMixer)
                {
                    _warnedMissingMixer = true;
                    Debug.LogWarning("[SettingsService] Falta asignar el AudioMixer (MainMixer) en SettingsService.", this);
                }
                return;
            }
            float db = value01 <= 0.0001f ? -80f : Mathf.Log10(value01) * 20f;
            mixer.SetFloat(param, db);
        }

        // ------------------------------------------------------------------ calidad

        public void SetQuality(int level)
        {
            level = ClampQuality(level);
            Options.qualityLevel = level;
            QualitySettings.SetQualityLevel(level, true);
            Application.targetFrameRate = 60;
            _dirty = true;
            Flush();
        }

        // ------------------------------------------------------------------ controles

        void LoadBindings(string json)
        {
            if (controls == null) return;
            try
            {
                if (string.IsNullOrEmpty(json)) controls.RemoveAllBindingOverrides();
                else controls.LoadBindingOverridesFromJson(json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SettingsService] Controles guardados ilegibles, se usan los de fabrica: {e.Message}");
                controls.RemoveAllBindingOverrides();
                Options.inputBindingsJson = "";
            }
            AnyBindingsChanged?.Invoke();
        }

        static readonly Dictionary<string, string> ActionNames = new Dictionary<string, string>
        {
            { "Move", "Mover" }, { "Jump", "Saltar" }, { "Interact", "Interactuar" },
            { "Timestop", "Parada Temporal" }, { "Pause", "Pausa" },
        };

        public static string DisplayName(string actionName) =>
            ActionNames.TryGetValue(actionName, out var n) ? n : actionName;

        /// <summary>Si la accion se puede reasignar con ese dispositivo (movimiento de mando y tactil no).</summary>
        public bool CanRebind(string actionName, InputScheme scheme)
        {
            if (controls == null || scheme == InputScheme.Touch) return false;
            var action = controls.FindAction(actionName, false);
            if (action == null) return false;
            return FindTargets(action, InputPrompt.GroupOf(scheme)).Count > 0;
        }

        // Indices de binding a reasignar: el unico binding del grupo, o las 4 partes del primer compuesto (WASD).
        static List<int> FindTargets(InputAction action, string group)
        {
            var list = new List<int>();
            var b = action.bindings;
            for (int i = 0; i < b.Count; i++)
            {
                if (b[i].isComposite)
                {
                    if (group != "Keyboard") continue;
                    for (int j = i + 1; j < b.Count && b[j].isPartOfComposite; j++) list.Add(j);
                    return list;   // solo el primer compuesto
                }
                if (b[i].isPartOfComposite) continue;
                if (string.Equals(action.expectedControlType, "Vector2", StringComparison.OrdinalIgnoreCase)) continue;
                if (!InGroup(b[i].groups, group)) continue;
                list.Add(i);
                return list;
            }
            return list;
        }

        static bool InGroup(string groups, string group)
        {
            if (string.IsNullOrEmpty(groups)) return false;
            foreach (var g in groups.Split(';'))
                if (g.Length > 0 && g.StartsWith(group, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        static string PartName(string part)
        {
            switch (part)
            {
                case "up": return "Arriba";
                case "down": return "Abajo";
                case "left": return "Izquierda";
                case "right": return "Derecha";
                default: return part;
            }
        }

        /// <summary>
        /// Reasigna una accion pidiendo al jugador la nueva tecla/boton.
        /// <paramref name="onPrompt"/> recibe el texto a mostrar mientras espera; <paramref name="onDone"/> el resultado.
        /// Una tecla ya usada por otra accion se rechaza (Conflict) y no cambia nada.
        /// </summary>
        public void StartRebind(string actionName, InputScheme scheme, Action<RebindResult> onDone, Action<string> onPrompt = null)
        {
            if (IsRebinding) return;
            var action = controls != null ? controls.FindAction(actionName, false) : null;
            string group = InputPrompt.GroupOf(scheme);
            var targets = action != null && scheme != InputScheme.Touch ? FindTargets(action, group) : new List<int>();
            if (targets.Count == 0) { onDone?.Invoke(new RebindResult(RebindOutcome.Unavailable)); return; }

            _snapshot = controls.SaveBindingOverridesAsJson();

            // Las acciones se apagan mientras se reasigna (si no, Esc dispararia Pausa, etc.).
            ReenablePausedNow();
            foreach (var a in controls) if (a.enabled) _pausedActions.Add(a);
            controls.Disable();

            RebindStep(action, targets, 0, scheme, group, onDone, onPrompt);
        }

        void RebindStep(InputAction action, List<int> targets, int step, InputScheme scheme, string group,
                        Action<RebindResult> onDone, Action<string> onPrompt)
        {
            if (step >= targets.Count)
            {
                string display = targets.Count > 1
                    ? InputPrompt.Label(action, scheme, null, false, false)
                    : DisplayKey(action.bindings[targets[0]].effectivePath);
                Finish(true);
                onDone?.Invoke(new RebindResult(RebindOutcome.Applied, display));
                return;
            }

            int index = targets[step];
            var binding = action.bindings[index];
            bool pad = scheme == InputScheme.Gamepad;
            string what = binding.isPartOfComposite ? PartName(binding.name) + $" ({step + 1}/{targets.Count})" : DisplayName(action.name);
            onPrompt?.Invoke(pad ? $"Pulsa un botón: {what}  (B cancela)" : $"Pulsa una tecla: {what}  (Esc cancela)");

            string device = pad ? "<Gamepad>" : "<Keyboard>";
            _op = action.PerformInteractiveRebinding(index)
                .WithControlsHavingToMatchPath(device)
                .WithControlsExcluding("<Keyboard>/anyKey")
                .WithExpectedControlType("Button")
                .WithCancelingThrough(pad ? "<Gamepad>/buttonEast" : "<Keyboard>/escape")
                .WithTimeout(RebindTimeoutSeconds)
                .OnMatchWaitForAnother(0.1f)
                .OnCancel(_ =>
                {
                    Finish(false);
                    onDone?.Invoke(new RebindResult(RebindOutcome.Cancelled));
                })
                .OnComplete(op =>
                {
                    string path = action.bindings[index].effectivePath;
                    string other = FindConflict(action, index, path, op.selectedControl);
                    if (other != null)
                    {
                        Finish(false);   // restaura todo: nada queda a medias
                        onDone?.Invoke(new RebindResult(RebindOutcome.Conflict, DisplayKey(path), other));
                        return;
                    }
                    op.Dispose();
                    _op = null;
                    RebindStep(action, targets, step + 1, scheme, group, onDone, onPrompt);
                })
                .Start();
        }

        // Nombre (es) de la otra accion que ya usa ese control, o null. Cuentan tambien las partes de Mover.
        string FindConflict(InputAction edited, int editedIndex, string path, InputControl selectedControl)
        {
            foreach (var a in controls)
            {
                var b = a.bindings;
                for (int i = 0; i < b.Count; i++)
                {
                    if (a == edited && i == editedIndex) continue;
                    if (b[i].isComposite) continue;
                    if (string.Equals(b[i].effectivePath, path, StringComparison.OrdinalIgnoreCase)
                        || (selectedControl != null && !string.IsNullOrEmpty(b[i].effectivePath)
                            && InputControlPath.Matches(b[i].effectivePath, selectedControl)))
                    {
                        // Dos partes del mismo Mover tampoco pueden repetirse.
                        return DisplayName(a.name);
                    }
                }
            }
            return null;
        }

        static string DisplayKey(string path)
        {
            string s = InputControlPath.ToHumanReadableString(path, InputControlPath.HumanReadableStringOptions.OmitDevice);
            return InputPrompt.Localize(s);
        }

        /// <summary>Cierra la operacion. Si no se acepto, vuelve a los controles que habia antes de empezar.</summary>
        void Finish(bool keep, bool immediateReenable = false)
        {
            if (_op != null) { _op.Dispose(); _op = null; }

            if (keep)
            {
                Options.inputBindingsJson = controls.SaveBindingOverridesAsJson();
                _dirty = true;
                Flush();
            }
            else if (_snapshot != null)
            {
                controls.LoadBindingOverridesFromJson(_snapshot);
            }
            _snapshot = null;

            if (immediateReenable || !isActiveAndEnabled) ReenablePausedNow();
            else _reenableRoutine = StartCoroutine(ReenablePausedNextFrame());

            if (keep)
            {
                BindingsChanged?.Invoke();
                AnyBindingsChanged?.Invoke();
            }
        }

        void ReenablePausedNow()
        {
            if (_reenableRoutine != null) { StopCoroutine(_reenableRoutine); _reenableRoutine = null; }
            foreach (var a in _pausedActions) a.Enable();
            _pausedActions.Clear();
        }

        IEnumerator ReenablePausedNextFrame()
        {
            yield return null;
            _reenableRoutine = null;
            foreach (var a in _pausedActions) a.Enable();
            _pausedActions.Clear();
        }

        public void CancelRebind(bool immediateReenable = false)
        {
            if (_op == null) return;
            // Dispose() de una operacion en curso no llama a OnCancel: se restaura a mano.
            Finish(false, immediateReenable);
        }

        // ------------------------------------------------------------------ restablecer

        /// <summary>Boton "Restablecer": volumen, calidad y controles vuelven a los valores por defecto.</summary>
        public void ResetToDefaults()
        {
            CancelRebind(true);
            var o = Options;
            o.musicVolume = DefaultVolume;
            o.sfxVolume = DefaultVolume;
            o.qualityLevel = -1;
            o.inputBindingsJson = "";
            ApplyAll();
            _dirty = true;
            Flush();   // se guarda de inmediato
            Changed?.Invoke();
            BindingsChanged?.Invoke();
            AnyBindingsChanged?.Invoke();
        }
    }
}
