using System;
using System.Collections.Generic;

namespace Reloj.Core
{
    // Formato del guardado (I-34). JsonUtility no serializa Dictionary: por eso son listas.
    // Si cambias la estructura, sube SaveService.CurrentVersion y agrega un paso en SaveService.Migrate.

    [Serializable]
    public class FlagEntry
    {
        public string key;
        public bool value;
    }

    [Serializable]
    public class LevelScoreEntry
    {
        /// <summary>Nombre de la escena Gameplay (ej. "Level01_Gameplay").</summary>
        public string level;
        public int bestScore;
    }

    /// <summary>Opciones del jugador. I-36 las lee y escribe a traves de SaveService.Data.options.</summary>
    [Serializable]
    public class OptionsData
    {
        [UnityEngine.Range(0f, 1f)] public float musicVolume = 1f;
        [UnityEngine.Range(0f, 1f)] public float sfxVolume = 1f;
        /// <summary>-1 = no definido (se usa el nivel de calidad por defecto).</summary>
        public int qualityLevel = -1;
        /// <summary>Overrides de controles (InputActionAsset.SaveBindingOverridesAsJson). Vacio = por defecto.</summary>
        public string inputBindingsJson = "";
    }

    [Serializable]
    public class SaveData
    {
        public int saveVersion = SaveService.CurrentVersion;

        /// <summary>true cuando se termino al menos un nivel. Sin esto, "Continuar" no se habilita.</summary>
        public bool hasProgress;

        /// <summary>Indice (0-based) en LevelSequence.Order del nivel al que lleva "Continuar".</summary>
        public int unlockedLevel;

        public List<LevelScoreEntry> bestScores = new List<LevelScoreEntry>();
        public List<FlagEntry> flags = new List<FlagEntry>();
        public List<string> clueIds = new List<string>();
        public int skillPoints;
        public OptionsData options = new OptionsData();

        public string savedAtUtc = "";
    }
}
