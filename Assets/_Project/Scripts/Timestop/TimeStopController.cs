using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Reloj.Core;

namespace Reloj.TimeStop
{
    public enum TimeStopState { Ready, Active, Recharging }

    public class TimeStopController : MonoBehaviour
    {
        public static TimeStopController Instance { get; private set; }

        /// <summary>true mientras el tiempo esta detenido. Lo consultan los scripts de patrulla.</summary>
        public static bool IsStopped { get; private set; }

        static readonly List<ITimeAffectable> affectables = new List<ITimeAffectable>();
        static readonly List<ITimeAffectable> buffer = new List<ITimeAffectable>();

        [Header("Entrada")]
        [SerializeField] InputActionReference timestopAction;

        [Header("Tiempos (segundos)")]
        [Tooltip("Duracion de la parada. Diseno: 3 a 5 s.")]
        [SerializeField, Min(0.1f)] float duration = 4f;
        [Tooltip("Tiempo de recarga. Diseno: 8 a 10 s.")]
        [SerializeField, Min(0.1f)] float cooldown = 9f;

        [Header("Debug")]
        [SerializeField] bool showDebug = true;

        float timer;

        public event Action OnTimeStopStart;
        public event Action OnTimeStopEnd;
        public event Action OnTimeStopReady;

        public TimeStopState State { get; private set; } = TimeStopState.Ready;

        /// <summary>Progreso de carga: 1 = listo, 0 = recien empezo a recargar (y durante la parada).</summary>
        public float CooldownNormalized
        {
            get
            {
                switch (State)
                {
                    case TimeStopState.Ready: return 1f;
                    case TimeStopState.Recharging: return Mathf.Clamp01(timer / cooldown);
                    default: return 0f;
                }
            }
        }

        public float RemainingActiveTime =>
            State == TimeStopState.Active ? Mathf.Max(0f, duration - timer) : 0f;

        public float Duration => duration;

        // Evita estado viejo si el Editor tiene desactivado "Reload Domain".
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            affectables.Clear();
            buffer.Clear();
            IsStopped = false;
            Instance = null;
        }

        public static void Register(ITimeAffectable a)
        {
            if (a == null || affectables.Contains(a)) return;
            affectables.Add(a);
            if (IsStopped) a.Freeze(); // aparecio durante la parada
        }

        public static void Unregister(ITimeAffectable a)
        {
            affectables.Remove(a);
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
                Debug.LogWarning("Hay mas de un TimeStopController en la escena.", this);
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void OnEnable()
        {
            if (timestopAction == null) return;
            timestopAction.action.performed += OnTimestopPerformed;
            timestopAction.action.Enable();
        }

        void OnDisable()
        {
            if (timestopAction != null)
            {
                timestopAction.action.performed -= OnTimestopPerformed;
                timestopAction.action.Disable();
            }
            if (State == TimeStopState.Active) EndTimeStop(); // nunca dejar objetos congelados
        }

        void OnTimestopPerformed(InputAction.CallbackContext ctx)
        {
            TryStartTimeStop();
        }

        public bool TryStartTimeStop()
        {
            // I-11: sin Parada Temporal en Pausa / FinDelNivel / GameOver.
            var services = GameServices.Instance;
            if (services != null && services.State != null
                && services.State.CurrentState != GameState.Jugando) return false;

            // En Activo o Recargando no hace nada y NO toca el temporizador.
            if (State != TimeStopState.Ready) return false;

            State = TimeStopState.Active;
            timer = 0f;
            IsStopped = true;
            SetFrozen(true);
            OnTimeStopStart?.Invoke();
            return true;
        }

        void Update()
        {
            // Time.deltaTime: con Pausa (timeScale 0, I-11) los temporizadores tambien se detienen.
            switch (State)
            {
                case TimeStopState.Active:
                    timer += Time.deltaTime;
                    if (timer >= duration) EndTimeStop();
                    break;

                case TimeStopState.Recharging:
                    timer += Time.deltaTime;
                    if (timer >= cooldown)
                    {
                        State = TimeStopState.Ready;
                        timer = 0f;
                        OnTimeStopReady?.Invoke();
                    }
                    break;
            }
        }

        void EndTimeStop()
        {
            IsStopped = false;
            SetFrozen(false);
            State = TimeStopState.Recharging;
            timer = 0f;
            OnTimeStopEnd?.Invoke();
        }

        static void SetFrozen(bool frozen)
        {
            // Copia: un afectable puede desactivarse al congelarse y modificar la lista.
            buffer.Clear();
            buffer.AddRange(affectables);
            for (int i = 0; i < buffer.Count; i++)
            {
                if (frozen) buffer[i].Freeze();
                else buffer[i].Unfreeze();
            }
        }

        void OnGUI()
        {
            if (!showDebug) return;
            GUI.Label(new Rect(10, 40, 420, 24),
                $"TimeStop: {State}  |  Activo restante: {RemainingActiveTime:F1}s  |  Carga: {CooldownNormalized:F2}");
        }
    }
}