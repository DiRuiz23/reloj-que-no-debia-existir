using System;
using System.Collections.Generic;
using UnityEngine;
using Reloj.Progression;

namespace Reloj.Core
{
    /// <summary>
    /// Guardado (I-34). Serializa SaveData a JSON y lo guarda con PlayerPrefs (clave unica), asi funciona
    /// igual en PC, WebGL y Android. No usa rutas de archivos propias.
    ///  - El progreso (nivel desbloqueado, flags, pistas, puntos de habilidad) se captura al TERMINAR un nivel.
    ///  - Save() tambien se llama al salir al menu: conserva ese progreso y las opciones.
    ///  - Un guardado vacio, corrupto o de version desconocida se ignora con un aviso en consola.
    /// </summary>
    public class SaveService : MonoBehaviour
    {
        public const int CurrentVersion = 2;
        const string PrefsKey = "reloj.save";

        SaveData _data;
        bool _loaded;

        /// <summary>Datos en memoria (nunca null). I-36 lee/escribe aqui las opciones y luego llama a Save().</summary>
        public SaveData Data
        {
            get
            {
                EnsureLoaded();
                if (_data == null) _data = new SaveData();
                return _data;
            }
        }

        /// <summary>true si existe un guardado valido con al menos un nivel terminado.</summary>
        public bool HasSaveGame
        {
            get
            {
                EnsureLoaded();
                return _data != null && _data.hasProgress;
            }
        }

        /// <summary>true si el jugador ya llego a alguno de los finales (lee del guardado, sin necesitar instancia).</summary>
        public static bool GameCompleted
        {
            get
            {
                var d = ReadFromPrefs();
                return d != null && d.gameCompleted;
            }
        }

        /// <summary>Id del ultimo final alcanzado (Ending_A / Ending_B), o vacio.</summary>
        public static string CompletedEndingId
        {
            get
            {
                var d = ReadFromPrefs();
                return d != null && d.endingId != null ? d.endingId : "";
            }
        }

        // ---------------------------------------------------------------- lectura

        void EnsureLoaded()
        {
            if (!_loaded) Load();
        }

        /// <summary>Relee el guardado desde PlayerPrefs. Devuelve false si no hay uno valido (nunca lanza).</summary>
        public bool Load()
        {
            _loaded = true;
            _data = ReadFromPrefs();
            return _data != null;
        }

        static SaveData ReadFromPrefs()
        {
            if (!PlayerPrefs.HasKey(PrefsKey)) return null;

            string json = PlayerPrefs.GetString(PrefsKey, "");
            if (string.IsNullOrWhiteSpace(json))
            {
                Debug.LogWarning("[SaveService] El guardado esta vacio: se ignora.");
                return null;
            }

            try
            {
                var d = JsonUtility.FromJson<SaveData>(json);
                if (d == null) throw new FormatException("JSON sin contenido");
                if (d.saveVersion < 1 || d.saveVersion > CurrentVersion)
                    throw new FormatException($"saveVersion no soportado ({d.saveVersion})");
                Migrate(d);
                Sanitize(d);
                return d;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveService] Guardado corrupto o ilegible, se ignora: {e.Message}");
                return null;
            }
        }

        // Aqui se agregan los pasos de migracion cuando cambie el formato:
        //   if (d.saveVersion < 2) { ...convertir...; d.saveVersion = 2; }
        static void Migrate(SaveData d)
        {
            // v1 -> v2 (I-38): se agregan skillLevels y skillRewardedLevels. JsonUtility los deja por defecto
            // en un guardado v1 (sin mejoras, ningun nivel cobrado) y Sanitize los normaliza: no hace falta convertir nada.
            d.saveVersion = CurrentVersion;
        }

        // Deja el objeto en un estado seguro aunque el JSON tenga campos faltantes o fuera de rango.
        static void Sanitize(SaveData d)
        {
            if (d.bestScores == null) d.bestScores = new List<LevelScoreEntry>();
            if (d.flags == null) d.flags = new List<FlagEntry>();
            if (d.clueIds == null) d.clueIds = new List<string>();
            if (d.options == null) d.options = new OptionsData();
            if (d.endingId == null) d.endingId = "";

            d.bestScores.RemoveAll(e => e == null || string.IsNullOrEmpty(e.level));
            d.flags.RemoveAll(f => f == null || string.IsNullOrEmpty(f.key));
            d.clueIds.RemoveAll(string.IsNullOrEmpty);

            d.unlockedLevel = Mathf.Clamp(d.unlockedLevel, 0, LevelSequence.Order.Length - 1);
            d.skillPoints = Mathf.Max(0, d.skillPoints);
            if (d.skillLevels == null || d.skillLevels.Length != SkillPointManager.CategoryCount)
            {
                var fixedLevels = new int[SkillPointManager.CategoryCount];
                if (d.skillLevels != null)
                    for (int i = 0; i < fixedLevels.Length && i < d.skillLevels.Length; i++) fixedLevels[i] = d.skillLevels[i];
                d.skillLevels = fixedLevels;
            }
            for (int i = 0; i < d.skillLevels.Length; i++) d.skillLevels[i] = Mathf.Clamp(d.skillLevels[i], 0, SkillPointManager.MaxLevel);
            if (d.skillRewardedLevels == null) d.skillRewardedLevels = new List<string>();
            d.skillRewardedLevels.RemoveAll(string.IsNullOrEmpty);
            d.options.musicVolume = Mathf.Clamp01(d.options.musicVolume);
            d.options.sfxVolume = Mathf.Clamp01(d.options.sfxVolume);
            if (d.options.inputBindingsJson == null) d.options.inputBindingsJson = "";
        }

        // ---------------------------------------------------------------- escritura

        /// <summary>Escribe los datos actuales en PlayerPrefs.</summary>
        public void Save() => WriteToPrefs(Data);

        static void WriteToPrefs(SaveData d)
        {
            d.saveVersion = CurrentVersion;
            d.savedAtUtc = DateTime.UtcNow.ToString("o");
            try
            {
                PlayerPrefs.SetString(PrefsKey, JsonUtility.ToJson(d));
                PlayerPrefs.Save();   // en WebGL fuerza el volcado a IndexedDB
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveService] No se pudo guardar: {e.Message}");
            }
        }

        /// <summary>Lo llama EndingController: marca la partida como completada y guarda.</summary>
        public void MarkGameCompleted(string endingId)
        {
            var d = Data;
            d.gameCompleted = true;
            d.endingId = endingId ?? "";
            Save();
        }

        /// <summary>
        /// Igual que MarkGameCompleted pero sin instancia (escena de final abierta directo, sin GameServices).
        /// Lee el guardado actual de PlayerPrefs (si es valido), lo marca y lo escribe.
        /// </summary>
        public static void WriteCompletion(string endingId)
        {
            var d = ReadFromPrefs() ?? new SaveData();
            d.gameCompleted = true;
            d.endingId = endingId ?? "";
            WriteToPrefs(d);
        }

        /// <summary>
        /// Lo llama GameStateManager.CompleteLevel: desbloquea el siguiente nivel, actualiza el puntaje maximo,
        /// captura flags/pistas/puntos de habilidad y guarda. Escenas fuera de LevelSequence (pruebas) no guardan.
        /// </summary>
        public void RegisterLevelComplete(string sceneName, int score)
        {
            int index = Array.IndexOf(LevelSequence.Order, sceneName);
            if (index < 0)
            {
                Debug.Log($"[SaveService] '{sceneName}' no esta en LevelSequence: no se guarda progreso.");
                return;
            }

            var d = Data;
            d.hasProgress = true;
            d.unlockedLevel = Mathf.Max(d.unlockedLevel, Mathf.Min(index + 1, LevelSequence.Order.Length - 1));

            var entry = d.bestScores.Find(e => e.level == sceneName);
            if (entry == null) d.bestScores.Add(new LevelScoreEntry { level = sceneName, bestScore = Mathf.Max(0, score) });
            else if (score > entry.bestScore) entry.bestScore = score;

            CaptureProgress(d);
            Save();
        }

        public int GetBestScore(string sceneName)
        {
            var e = Data.bestScores.Find(x => x.level == sceneName);
            return e != null ? e.bestScore : 0;
        }

        static void CaptureProgress(SaveData d)
        {
            d.flags.Clear();
            foreach (var kv in DialogueFlagManager.Snapshot())
                d.flags.Add(new FlagEntry { key = kv.Key, value = kv.Value });

            var gs = GameServices.Instance;
            d.clueIds = (gs != null && gs.Clues != null) ? gs.Clues.GetIds() : new List<string>();
            // I-38: puntos sin gastar + niveles de mejora + niveles ya cobrados.
            if (gs != null && gs.SkillPoints != null) gs.SkillPoints.WriteTo(d);
            else d.skillPoints = (gs != null && gs.Session != null) ? gs.Session.SkillPoints : 0;
        }

        // ---------------------------------------------------------------- partida nueva / continuar

        /// <summary>Partida nueva: borra el progreso pero conserva las opciones del jugador.</summary>
        public void ResetProgress()
        {
            var options = Data.options;
            _data = new SaveData { options = options };
            var gsReset = GameServices.Instance;
            if (gsReset != null && gsReset.SkillPoints != null) gsReset.SkillPoints.ResetAll();   // I-38
            _loaded = true;
            Save();
        }

        /// <summary>
        /// Continuar: relee el guardado, restaura flags, pistas y puntos de habilidad y devuelve la escena
        /// del nivel desbloqueado. false si no hay un guardado valido (no cambia nada).
        /// </summary>
        public bool TryContinue(out string sceneName)
        {
            sceneName = null;
            if (!Load() || !_data.hasProgress) return false;

            var flags = new Dictionary<string, bool>();
            foreach (var f in _data.flags) flags[f.key] = f.value;
            DialogueFlagManager.Restore(flags);

            var gs = GameServices.Instance;
            if (gs != null)
            {
                if (gs.Clues != null) gs.Clues.RestoreIds(_data.clueIds);
                if (gs.Session != null)
                {
                    gs.Session.ResetSession();
                    gs.Session.SetSkillPoints(_data.skillPoints);
                }
            }

            if (gs != null && gs.SkillPoints != null) gs.SkillPoints.LoadFrom(_data);   // I-38 (despues de ResetSession)

            sceneName = LevelSequence.Order[_data.unlockedLevel];
            return true;
        }
    }
}
