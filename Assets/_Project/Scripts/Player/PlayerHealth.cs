using System;
using UnityEngine;
using Reloj.Core;

namespace Reloj.Player
{
    public class PlayerHealth : MonoBehaviour
    {
        public static PlayerHealth Instance { get; private set; }

        [SerializeField, Min(1)] int maxHealth = 100;

        public int Current { get; private set; }
        public int Max => maxHealth;
        public bool IsDead => Current <= 0;
        public float Normalized => (float)Current / maxHealth;

        /// <summary>(vida actual, vida máxima). La UI (HUD de I-20) se suscribe aquí.</summary>
        public event Action<int, int> OnHealthChanged;

        static GameStateManager State =>
            GameServices.Instance != null ? GameServices.Instance.State : null;

        void Awake()
        {
            Instance = this;
            Current = maxHealth;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // Start y no Awake: GameServices puede despertar después en la escena.
        void Start() => Publish();

        public void TakeDamage(int amount)
        {
            if (amount <= 0 || IsDead) return;
            var state = State;
            if (state != null && state.CurrentState != GameState.Jugando) return;

            Current = Mathf.Max(0, Current - amount);
            Publish();   // primero el evento (el HUD muestra 0), luego la derrota

            if (Current == 0)
            {
                if (state != null) state.Fail();
                else Debug.LogWarning("[PlayerHealth] Sin GameServices: no se puede llamar a Fail().", this);
            }
        }

        public void Heal(int amount)
        {
            if (amount <= 0 || IsDead) return;
            int next = Mathf.Min(maxHealth, Current + amount);   // nunca supera el máximo
            if (next == Current) return;                         // ya estaba al tope: no hay evento
            Current = next;
            Publish();
        }

        void Publish()
        {
            var gs = GameServices.Instance;
            if (gs != null && gs.Session != null) gs.Session.SetHealth(Current, maxHealth);
            OnHealthChanged?.Invoke(Current, maxHealth);
        }
    }
}
