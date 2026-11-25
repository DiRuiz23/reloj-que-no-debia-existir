using System.Collections.Generic;
using UnityEngine;

namespace Reloj.Core
{
    public class GameSession : MonoBehaviour
    {
        public int Score { get; private set; }
        public int LastLevelScore { get; private set; }
        public float LastLevelTime { get; private set; }

        // --- Vida, puntaje y tiempo del nivel en curso (los escriben PlayerHealth, ScoreManager y LevelTimer) ---
        public int CurrentHealth { get; private set; }
        public int MaxHealth { get; private set; }
        public int LevelScore { get; private set; }
        public float LevelTime { get; private set; }

        public void SetHealth(int current, int max) { CurrentHealth = current; MaxHealth = max; }
        public void SetLevelScore(int score) => LevelScore = score;
        public void SetLevelTime(float time) => LevelTime = time;

        // --- Puntos de habilidad (I-34: se guardan en SaveData) ---
        public int SkillPoints { get; private set; }
        public void SetSkillPoints(int value) => SkillPoints = Mathf.Max(0, value);
        public void AddSkillPoints(int amount) => SetSkillPoints(SkillPoints + amount);

        private readonly Dictionary<string, string> _data = new();
        private readonly Dictionary<string, int> _levelScores = new();

        public void AddScore(int amount) => Score += amount;

        public void RecordLevelScore(string sceneName, int levelScore)
        {
            if (string.IsNullOrEmpty(sceneName)) return;
            _levelScores[sceneName] = Mathf.Max(0, levelScore);
            RecalculateTotalScore();
        }

        public int GetScoreExcluding(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return Score;
            int sum = 0;
            foreach (var kv in _levelScores)
                if (kv.Key != sceneName) sum += kv.Value;
            return sum;
        }

        public void RestoreLevelScores(IEnumerable<LevelScoreEntry> entries)
        {
            _levelScores.Clear();
            if (entries != null)
            {
                foreach (var e in entries)
                    if (e != null && !string.IsNullOrEmpty(e.level) && e.bestScore > 0)
                        _levelScores[e.level] = e.bestScore;
            }
            RecalculateTotalScore();
        }

        private void RecalculateTotalScore()
        {
            int total = 0;
            foreach (var v in _levelScores.Values) total += v;
            Score = total;
        }

        // Lo llama GameStateManager.CompleteLevel. La pantalla de I-12 lee estos valores.
        public void SetLevelResult(int score, float time)
        {
            LastLevelScore = score;
            LastLevelTime = time;
        }

        public void SetData(string key, string value) => _data[key] = value;
        public string GetData(string key, string fallback = "") =>
            _data.TryGetValue(key, out var v) ? v : fallback;

        public void ResetSession()
        {
            Score = 0;
            LastLevelScore = 0;
            LastLevelTime = 0f;
            CurrentHealth = 0;
            MaxHealth = 0;
            LevelScore = 0;
            LevelTime = 0f;
            SkillPoints = 0;
            _data.Clear();
            _levelScores.Clear();
            ClearCheckpoint();
        }

        public bool HasCheckpoint { get; private set; }
        public Vector3 CheckpointPosition { get; private set; }
        public string CheckpointId { get; private set; } = "";
        public int CheckpointOrder { get; private set; } = -1;

        /// Guarda el checkpoint solo si no es anterior al actual (evita retroceder).
        public bool SetCheckpoint(string id, Vector3 position, int order)
        {
            if (HasCheckpoint && order < CheckpointOrder) return false;
            HasCheckpoint = true;
            CheckpointId = id;
            CheckpointPosition = position;
            CheckpointOrder = order;
            return true;
        }

        public void ClearCheckpoint()
        {
            HasCheckpoint = false;
            CheckpointId = "";
            CheckpointPosition = Vector3.zero;
            CheckpointOrder = -1;
        }
    }
}
