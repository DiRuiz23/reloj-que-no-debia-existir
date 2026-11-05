using System;
using System.Collections.Generic;
using UnityEngine;

namespace Reloj.Core
{
    [Serializable]
    public class ReputationEntry
    {
        /// <summary>Flag de narrativa (debe existir en Resources/Dialogues/_flags.json).</summary>
        public string flag;
        /// <summary>Puntos que suma (o resta, si es negativo) cuando el flag esta activo.</summary>
        public int points;
    }

    [Serializable]
    public class ReputationConfig
    {
        /// <summary>Flag que se activa si el puntaje alcanza el umbral (alta confianza).</summary>
        public string derivedFlag = "rep_n3_alta";
        public int threshold = 3;
        public List<ReputationEntry> entries = new List<ReputationEntry>();
    }

    /// <summary>
    /// Reputacion con los revolucionarios: suma los puntos de las decisiones de los Niveles 1 y 2
    /// (tabla en Resources/Reputation/reputation_n3.json) y activa el flag derivado
    /// <c>rep_n3_alta</c> si llega al umbral. Los dialogos lo leen con <c>altIf</c>.
    /// Llamar a <see cref="Recalculate"/> al iniciar el Nivel 3 (despues de restaurar los flags).
    /// El flag derivado no se guarda: siempre se recalcula desde las decisiones.
    /// </summary>
    public static class ReputationService
    {
        public const string ResourcesPath = "Reputation/reputation_n3";

        static ReputationConfig config;
        static bool warnedMissing;

        /// <summary>Puntaje de la ultima llamada a Recalculate.</summary>
        public static int Score { get; private set; }

        /// <summary>true si el flag derivado de alta confianza esta activo.</summary>
        public static bool IsHighTrust => config != null && DialogueFlagManager.GetFlag(config.derivedFlag);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            config = null;
            warnedMissing = false;
            Score = 0;
        }

        /// <summary>Calcula el puntaje con los flags actuales y actualiza el flag derivado. Devuelve el puntaje.</summary>
        public static int Recalculate()
        {
            if (!EnsureConfig())
            {
                Score = 0;
                return 0;
            }

            int total = 0;
            foreach (var e in config.entries)
                if (e != null && DialogueFlagManager.GetFlag(e.flag)) total += e.points;

            Score = total;
            DialogueFlagManager.SetFlag(config.derivedFlag, total >= config.threshold);
            return total;
        }

        static bool EnsureConfig()
        {
            if (config != null) return true;

            var asset = Resources.Load<TextAsset>(ResourcesPath);
            if (asset == null)
            {
                if (!warnedMissing)
                {
                    warnedMissing = true;
                    Debug.LogWarning($"[Reputation] No existe Resources/{ResourcesPath}.json: la reputacion queda en 0.");
                }
                return false;
            }

            try { config = JsonUtility.FromJson<ReputationConfig>(asset.text); }
            catch (Exception e)
            {
                Debug.LogError($"[Reputation] JSON invalido en '{ResourcesPath}': {e.Message}");
                return false;
            }
            if (config == null || string.IsNullOrEmpty(config.derivedFlag))
            {
                config = null;
                Debug.LogError($"[Reputation] '{ResourcesPath}' no define 'derivedFlag'.");
                return false;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            WarnUnknownFlags();
#endif
            return true;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // Avisa de flags mal escritos en la tabla (un typo daria 0 puntos sin ningun error).
        static void WarnUnknownFlags()
        {
            var catalog = Reloj.Dialogue.DialogueLoader.LoadFlagCatalog();
            if (catalog == null || catalog.flags.Count == 0) return;

            if (!catalog.flags.Contains(config.derivedFlag))
                Debug.LogWarning($"[Reputation] El flag derivado '{config.derivedFlag}' no esta en _flags.json.");
            foreach (var e in config.entries)
                if (e != null && !catalog.flags.Contains(e.flag))
                    Debug.LogWarning($"[Reputation] El flag '{e.flag}' de la tabla no esta en _flags.json.");
        }
#endif
    }
}
