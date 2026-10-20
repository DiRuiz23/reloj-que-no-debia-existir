using System.Collections.Generic;
using UnityEngine;

namespace Reloj.Core
{
    public class GameSession : MonoBehaviour
    {
        public int Score { get; private set; }
        public int LastLevelScore { get; private set; }
        public float LastLevelTime { get; private set; }

        private readonly Dictionary<string, string> _data = new();

        public void AddScore(int amount) => Score += amount;

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
            _data.Clear();
        }
    }
}