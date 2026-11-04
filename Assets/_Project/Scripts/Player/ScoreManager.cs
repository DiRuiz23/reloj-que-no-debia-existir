using System;
using UnityEngine;
using Reloj.Core;

namespace Reloj.Player
{
    public class ScoreManager : MonoBehaviour
    {
        public static ScoreManager Instance { get; private set; }

        public int Score { get; private set; }

        /// <summary>Puntaje actual del nivel.</summary>
        public event Action<int> OnScoreChanged;

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Start() => Publish();   // el puntaje empieza en 0 en cada carga del nivel

        public void Add(int points)
        {
            if (points <= 0) return;
            Score += points;
            Publish();
        }

        void Publish()
        {
            var gs = GameServices.Instance;
            if (gs != null && gs.Session != null) gs.Session.SetLevelScore(Score);
            OnScoreChanged?.Invoke(Score);
        }
    }
}
