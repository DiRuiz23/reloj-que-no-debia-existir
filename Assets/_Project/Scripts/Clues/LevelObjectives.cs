using System;
using UnityEngine;
using Reloj.Core;

namespace Reloj.Clues
{
    /// <summary>
    /// Objetivos del nivel (I-27). Va una vez por escena de gameplay. Lista ordenada de pasos: el objetivo
    /// actual es el primer paso cuyo flag de cumplimiento aun NO esta activo; al activarse ese flag, pasa
    /// al siguiente. El HUD (<see cref="ObjectiveHUD"/>) escucha <see cref="OnObjectiveChanged"/>.
    /// </summary>
    [DefaultExecutionOrder(-50)]   // despues de Level01Controller (limpia flags en Awake), antes del HUD
    public class LevelObjectives : MonoBehaviour
    {
        [Serializable]
        public class Step
        {
            [TextArea(1, 3)] public string text;
            [Tooltip("Flag que, al activarse, cumple este objetivo (ej. 'n2_hablo_con_lupe').")]
            public string completeFlag;
        }

        public static LevelObjectives Instance { get; private set; }

        [SerializeField] Step[] steps = new Step[0];
        [Tooltip("Texto cuando todos los objetivos estan cumplidos (vacio = el HUD oculta el objetivo).")]
        [SerializeField] string allDoneText = "";

        /// <summary>(texto actual, true si cambio por cumplir un objetivo durante el juego)</summary>
        public event Action<string, bool> OnObjectiveChanged;

        public string CurrentText { get; private set; } = "";
        public bool AllDone { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Instance = null;

        void Awake() { Instance = this; }

        void OnEnable() { DialogueFlagManager.OnFlagChanged += HandleFlag; }
        void OnDisable() { DialogueFlagManager.OnFlagChanged -= HandleFlag; }
        void OnDestroy() { if (Instance == this) Instance = null; }

        void Start() { Evaluate(false); }

        void HandleFlag(string flag, bool value)
        {
            if (steps == null) return;
            foreach (var s in steps)
                if (s != null && s.completeFlag == flag) { Evaluate(true); return; }
        }

        void Evaluate(bool advanced)
        {
            string text = allDoneText;
            bool done = true;
            if (steps != null)
                foreach (var s in steps)
                {
                    if (s == null || string.IsNullOrEmpty(s.completeFlag)) continue;
                    if (!DialogueFlagManager.GetFlag(s.completeFlag)) { text = s.text; done = false; break; }
                }

            if (text == CurrentText && done == AllDone) return;
            CurrentText = text ?? "";
            AllDone = done;
            OnObjectiveChanged?.Invoke(CurrentText, advanced);
        }
    }
}
