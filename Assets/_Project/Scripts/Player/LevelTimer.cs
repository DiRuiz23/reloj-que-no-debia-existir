using System;
using UnityEngine;
using Reloj.Core;

namespace Reloj.Player
{
    public class LevelTimer : MonoBehaviour
    {
        public static LevelTimer Instance { get; private set; }

        public float Elapsed { get; private set; }
        public bool IsRunning { get; private set; } = true;

        /// <summary>Segundos transcurridos. Se emite una vez por segundo entero, no cada frame.</summary>
        public event Action<float> OnTimeChanged;

        int lastSecond = -1;

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Start() => Publish();

        void Update()
        {
            if (!IsRunning) return;

            // Solo cuenta en Jugando. Pausa, Fin del Nivel y Game Over lo detienen
            // (además, con timeScale 0 Time.deltaTime ya es 0).
            var gs = GameServices.Instance;
            if (gs != null && gs.State != null && gs.State.CurrentState != GameState.Jugando) return;

            Elapsed += Time.deltaTime;
            if ((int)Elapsed != lastSecond) Publish();
        }

        /// <summary>Congela el reloj y guarda el tiempo exacto (lo llama Level01Controller al terminar).</summary>
        public void Stop()
        {
            if (!IsRunning) return;
            IsRunning = false;
            Publish();
        }

        void Publish()
        {
            lastSecond = (int)Elapsed;
            var gs = GameServices.Instance;
            if (gs != null && gs.Session != null) gs.Session.SetLevelTime(Elapsed);
            OnTimeChanged?.Invoke(Elapsed);
        }

        /// <summary>"mm:ss", mismo formato que LevelCompletePanel.</summary>
        public static string Format(float seconds)
        {
            var t = TimeSpan.FromSeconds(Mathf.Max(0f, seconds));
            return $"{(int)t.TotalMinutes:00}:{t.Seconds:00}";
        }
    }
}
