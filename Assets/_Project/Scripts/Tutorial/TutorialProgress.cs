using System.Collections.Generic;
using UnityEngine;
using Reloj.Core;

namespace Reloj.Tutorial
{
    /// <summary>
    /// Estado del tutorial del Nivel 1.
    ///  - Pasos hechos: en memoria. Sobreviven a "Reiniciar" y a recargar la escena, asi que no se repiten
    ///    indicaciones que la persona ya cumplio dentro de la misma sesion.
    ///  - Flag <c>tutorial_completado</c>: persistente (PlayerPrefs hasta que exista SaveService real, I-34) y
    ///    reflejado en DialogueFlagManager para que el guardado/narrativa lo vean.
    /// </summary>
    public static class TutorialProgress
    {
        public const string FlagName = "tutorial_completado";
        const string PrefsKey = "tutorial_completado";

        static readonly HashSet<string> registered = new HashSet<string>();
        static readonly HashSet<string> done = new HashSet<string>();

        // Evita estado viejo si el Editor tiene desactivado "Reload Domain".
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            registered.Clear();
            done.Clear();
        }

        /// <summary>true si la persona ya completo el tutorial en alguna partida anterior.</summary>
        public static bool Completed =>
            PlayerPrefs.GetInt(PrefsKey, 0) == 1 || DialogueFlagManager.GetFlag(FlagName);

        public static bool IsStepDone(string stepId) => done.Contains(stepId);

        /// <summary>Cada zona se registra al nacer; asi sabemos cuando estan todas cumplidas.</summary>
        public static void Register(string stepId) { if (!string.IsNullOrEmpty(stepId)) registered.Add(stepId); }
        public static void Unregister(string stepId) { registered.Remove(stepId); }

        public static void MarkStepDone(string stepId)
        {
            if (string.IsNullOrEmpty(stepId)) return;
            done.Add(stepId);
            if (registered.Count > 0 && done.IsSupersetOf(registered)) MarkCompleted();
        }

        /// <summary>Se llama al cumplir todos los pasos o al terminar el nivel.</summary>
        public static void MarkCompleted()
        {
            if (!Completed)
            {
                PlayerPrefs.SetInt(PrefsKey, 1);
                PlayerPrefs.Save();
            }
            SyncFlag();
        }

        /// <summary>Vuelve a poner el flag en DialogueFlagManager (Level01Controller lo limpia al iniciar).</summary>
        public static void SyncFlag()
        {
            if (Completed) DialogueFlagManager.SetFlag(FlagName, true);
        }

        /// <summary>Borra todo el progreso. Para probar con una persona nueva (menu Reloj/Tutorial).</summary>
        public static void ResetAll()
        {
            PlayerPrefs.DeleteKey(PrefsKey);
            PlayerPrefs.Save();
            done.Clear();
            DialogueFlagManager.SetFlag(FlagName, false);
        }
    }
}
