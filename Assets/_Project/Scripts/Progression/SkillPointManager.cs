using System;
using System.Collections.Generic;
using UnityEngine;
using Reloj.Core;

namespace Reloj.Progression
{
    /// <summary>Las 3 categorias de mejora (I-38). El valor numerico es el indice en SaveData.skillLevels.</summary>
    public enum SkillCategory
    {
        /// <summary>+1 s de duracion de la Parada Temporal por nivel.</summary>
        TimeStopDuration = 0,
        /// <summary>-10 % del radio de deteccion de los enemigos por nivel.</summary>
        DetectionRadius = 1,
        /// <summary>Desbloquea opciones de dialogo persuasivas (DialogueChoice.requiresPersuasion).</summary>
        Persuasion = 2,
    }

    /// <summary>
    /// Progresion del personaje (I-38). Dueno de los puntos de habilidad y de los niveles de mejora.
    ///  - Al completar un nivel de LevelSequence se otorga EXACTAMENTE 1 punto, una sola vez por nivel
    ///    (repetir el nivel no da otro). Los recolectables suman al PUNTAJE, nunca a estos puntos.
    ///  - 3 categorias, maximo 3 niveles cada una. Cada mejora cuesta 1 punto; no se puede gastar mas de lo ganado.
    ///  - Los efectos se SUMAN a los valores base de LevelDifficultyConfig (I-30): ver <see cref="SkillModifiers"/>.
    ///  - Todo se guarda en SaveData (I-34) en cuanto cambia, asi persiste entre niveles y sesiones.
    /// Vive en el prefab GameService (DontDestroyOnLoad), junto a GameSession y SaveService.
    /// </summary>
    public class SkillPointManager : MonoBehaviour
    {
        public const int MaxLevel = 3;
        public const int CategoryCount = 3;
        /// <summary>Puntos que da completar un nivel. Fijo por diseno (criterio de aceptacion de I-38).</summary>
        public const int PointsPerLevelCompleted = 1;

        public static SkillPointManager Instance { get; private set; }

        [Header("Efecto de cada nivel de mejora")]
        [Tooltip("Segundos que suma a la Parada Temporal cada nivel de la mejora (diseno: 1 s).")]
        [SerializeField, Min(0.1f)] float timeStopSecondsPerLevel = 1f;
        [Tooltip("Fraccion del radio de deteccion que se quita por nivel (diseno: 0.10 = -10 %). Con 3 niveles = -30 %.")]
        [SerializeField, Range(0.01f, 0.3f)] float detectionReductionPerLevel = 0.10f;

        readonly int[] levels = new int[CategoryCount];
        readonly List<string> rewardedLevels = new List<string>();
        int available;

        /// <summary>Se dispara cuando cambian los puntos o los niveles (el menu y los HUD se refrescan con esto).</summary>
        public event Action Changed;

        // ---------------------------------------------------------------- estado

        /// <summary>Puntos sin gastar.</summary>
        public int Available => available;

        /// <summary>Puntos ya gastados en mejoras (1 por nivel de mejora comprado).</summary>
        public int Spent
        {
            get { int s = 0; for (int i = 0; i < levels.Length; i++) s += levels[i]; return s; }
        }

        /// <summary>Puntos ganados en total (gastados + disponibles).</summary>
        public int TotalEarned => available + Spent;

        /// <summary>Puntos que dio la ULTIMA llamada a AwardLevelCompleted: 1, o 0 si ese nivel ya se habia cobrado.</summary>
        public int LastLevelAward { get; private set; }

        public int GetLevel(SkillCategory category) => levels[(int)category];
        public bool IsMaxed(SkillCategory category) => GetLevel(category) >= MaxLevel;
        public bool CanUpgrade(SkillCategory category) => available > 0 && !IsMaxed(category);
        public bool HasBeenRewarded(string sceneName) => rewardedLevels.Contains(sceneName);

        // ---------------------------------------------------------------- efectos (los lee SkillModifiers)

        public float TimeStopBonusSeconds => GetLevel(SkillCategory.TimeStopDuration) * timeStopSecondsPerLevel;

        /// <summary>1 = radio original; 0.9 / 0.8 / 0.7 con los niveles 1 / 2 / 3.</summary>
        public float DetectionRadiusMultiplier =>
            Mathf.Max(0.1f, 1f - GetLevel(SkillCategory.DetectionRadius) * detectionReductionPerLevel);

        public int PersuasionLevel => GetLevel(SkillCategory.Persuasion);

        // ---------------------------------------------------------------- ciclo de vida

        // Evita estado viejo si el Editor tiene desactivado "Reload Domain".
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[SkillPointManager] Ya hay uno activo; este se desactiva.", this);
                enabled = false;
                return;
            }
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ---------------------------------------------------------------- acciones

        /// <summary>
        /// Lo llama GameStateManager.CompleteLevel ANTES de guardar. Da 1 punto la primera vez que se completa
        /// ese nivel; las siguientes veces no da nada. Escenas fuera de LevelSequence (pruebas) tampoco dan.
        /// Devuelve los puntos otorgados (1 o 0).
        /// </summary>
        public int AwardLevelCompleted(string sceneName)
        {
            LastLevelAward = 0;
            if (string.IsNullOrEmpty(sceneName) || !LevelSequence.Contains(sceneName)) return 0;
            if (rewardedLevels.Contains(sceneName)) return 0;

            rewardedLevels.Add(sceneName);
            available += PointsPerLevelCompleted;
            LastLevelAward = PointsPerLevelCompleted;
            SyncSession();
            Changed?.Invoke();
            return LastLevelAward;
        }

        /// <summary>Gasta 1 punto en la categoria. false si no hay puntos o ya esta al maximo (no cambia nada).</summary>
        public bool TryUpgrade(SkillCategory category)
        {
            if (!CanUpgrade(category)) return false;

            available--;
            levels[(int)category]++;
            SyncSession();
            Persist();
            Changed?.Invoke();
            return true;
        }

        /// <summary>Partida nueva: puntos, mejoras y niveles cobrados a cero. No guarda (lo hace SaveService.ResetProgress).</summary>
        public void ResetAll()
        {
            available = 0;
            Array.Clear(levels, 0, levels.Length);
            rewardedLevels.Clear();
            LastLevelAward = 0;
            SyncSession();
            Changed?.Invoke();
        }

        // ---------------------------------------------------------------- guardado (I-34)

        /// <summary>Copia el estado a SaveData. Lo llama SaveService al terminar un nivel.</summary>
        public void WriteTo(SaveData data)
        {
            if (data == null) return;
            data.skillPoints = available;
            data.skillLevels = (int[])levels.Clone();
            data.skillRewardedLevels = new List<string>(rewardedLevels);
        }

        /// <summary>Restaura el estado desde SaveData (Continuar). Tolera campos faltantes o fuera de rango.</summary>
        public void LoadFrom(SaveData data)
        {
            Array.Clear(levels, 0, levels.Length);
            rewardedLevels.Clear();
            available = 0;
            LastLevelAward = 0;

            if (data != null)
            {
                available = Mathf.Max(0, data.skillPoints);
                if (data.skillLevels != null)
                    for (int i = 0; i < levels.Length && i < data.skillLevels.Length; i++)
                        levels[i] = Mathf.Clamp(data.skillLevels[i], 0, MaxLevel);
                if (data.skillRewardedLevels != null)
                    foreach (var s in data.skillRewardedLevels)
                        if (!string.IsNullOrEmpty(s) && !rewardedLevels.Contains(s)) rewardedLevels.Add(s);
            }
            SyncSession();
            Changed?.Invoke();
        }

        void Persist()
        {
            var gs = GameServices.Instance;
            if (gs == null || gs.Save == null) return;
            WriteTo(gs.Save.Data);
            gs.Save.Save();
        }

        // GameSession.SkillPoints sigue siendo el espejo "disponibles" para quien ya lo leia.
        void SyncSession()
        {
            var gs = GameServices.Instance;
            if (gs != null && gs.Session != null) gs.Session.SetSkillPoints(available);
        }

        // ---------------------------------------------------------------- textos para la UI

        public static string DisplayName(SkillCategory category)
        {
            switch (category)
            {
                case SkillCategory.TimeStopDuration: return "Parada Temporal";
                case SkillCategory.DetectionRadius: return "Sigilo";
                default: return "Persuasión";
            }
        }

        /// <summary>Descripcion del efecto en un nivel dado (0 = sin mejora).</summary>
        public string DescribeEffect(SkillCategory category, int level)
        {
            level = Mathf.Clamp(level, 0, MaxLevel);
            switch (category)
            {
                case SkillCategory.TimeStopDuration:
                    return level == 0 ? "Duracion base" : $"+{level * timeStopSecondsPerLevel:0.#} s de duracion";
                case SkillCategory.DetectionRadius:
                    return level == 0 ? "Radio de deteccion base" : $"-{Mathf.RoundToInt(level * detectionReductionPerLevel * 100f)} % de radio de deteccion";
                default:
                    return level == 0 ? "Sin opciones persuasivas" : $"Opciones persuasivas de nivel {level}";
            }
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>SOLO PRUEBAS: regala 1 punto sin completar un nivel (menu contextual del componente).</summary>
        [ContextMenu("Debug: +1 punto de habilidad")]
        void DebugGrantPoint()
        {
            available++;
            SyncSession();
            Persist();
            Changed?.Invoke();
        }
#endif
    }
}
