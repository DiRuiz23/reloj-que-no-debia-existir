using System.Collections.Generic;
using UnityEngine;

namespace Reloj.Core
{
    public class GameSession : MonoBehaviour
    {
        public int Score { get; private set; }
        private readonly Dictionary<string, string> _data = new();

        public void AddScore(int amount) => Score += amount;
        public void SetData(string key, string value) => _data[key] = value;
        public string GetData(string key, string fallback = "") =>
            _data.TryGetValue(key, out var v) ? v : fallback;

        public void ResetSession()
        {
            Score = 0;
            _data.Clear();
        }
    }
}