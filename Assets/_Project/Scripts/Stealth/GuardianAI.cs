using System;
using UnityEngine;
using Reloj.Core;
using Reloj.Player;
using Reloj.TimeStop;

namespace Reloj.Stealth
{
    public enum GuardianPhase { Patrulla, Anticipacion, Ataque, Recuperacion }

    /// <summary>
    /// IA del Guardian del dia 20 (I-32). Reutiliza el sistema de enemigos de I-21: EnemyAI aporta la patrulla,
    /// el cono de vision y los estados Ignorante -> Sospechoso -> Alerta. Este componente toma el control
    /// cuando el Guardian llega a Alerta (te vio por completo) y ejecuta su UNICO ataque:
    ///
    ///   Patrulla      EnemyAI patrulla y vigila. Al llegar a Alerta, el Guardian te fija.
    ///   Anticipacion  Se queda quieto y la zona de peligro se va llenando en el suelo (minimo 1 s).
    ///   Ataque        Onda de choque instantanea: si sigues dentro de la zona, Game Over / checkpoint.
    ///   Recuperacion  Queda aturdido (sin ver ni moverse). Luego vuelve a Patrulla.
    ///
    /// Como se esquiva: la onda cubre un circulo alrededor del Guardian cuyo radio se calcula en tiempo de ejecucion como
    ///   alcance de vision + velocidad del jugador x anticipacion x margen,
    /// asi que NINGUNA carrera durante la anticipacion saca al jugador de la zona (saltar tampoco: se mide en el plano XZ
    /// y no la frenan las paredes). La Parada Temporal congela al Guardian (y su cuenta atras): mientras dura, el jugador
    /// camina fuera de la zona y el golpe cae en vacio al reanudarse el tiempo.
    ///
    /// Valores (velocidad, cono, tiempo de alerta, anticipacion, recuperacion) salen de LevelDifficultyConfig (Nivel 4).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyAI))]
    [DefaultExecutionOrder(50)]   // despues de EnemyAI.Awake, para pisar sus valores de dificultad de soldado
    public class GuardianAI : MonoBehaviour
    {
        /// <summary>Tiempo minimo de anticipacion exigido por el diseno (criterio de aceptacion de I-32).</summary>
        public const float MinAnticipationSeconds = 1f;

        /// <summary>(guardian, fase anterior, fase nueva). Util para audio y tutorial.</summary>
        public static event Action<GuardianAI, GuardianPhase, GuardianPhase> OnPhaseChanged;

        /// <summary>true si el golpe alcanzo al jugador.</summary>
        public event Action<bool> OnStrike;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { OnPhaseChanged = null; }

        [Header("Valores base (los sobreescribe LevelDifficultyConfig, I-30 / I-32)")]
        [SerializeField, Min(0.1f)] float patrolSpeed = 2.5f;
        [SerializeField, Range(10f, 180f)] float viewAngle = 80f;
        [SerializeField, Min(0.5f)] float viewDistance = 7f;
        [Tooltip("Segundos viendote antes de fijarte y anunciar el ataque.")]
        [SerializeField, Min(0.1f)] float timeToAlert = 1f;
        [Tooltip("Segundos entre el aviso y el golpe. Nunca baja de 1 s.")]
        [SerializeField, Min(1f)] float anticipationSeconds = 1.5f;
        [SerializeField, Min(0.5f)] float recoverySeconds = 3f;

        [Header("Ataque (onda de choque)")]
        [Tooltip("Duracion del destello del golpe. El dano se evalua al inicio.")]
        [SerializeField, Min(0.05f)] float strikeSeconds = 0.35f;
        [Tooltip("Cuanto mas lejos de lo que el jugador puede correr durante la anticipacion llega la onda. Mayor que 1 = no se esquiva sin Parada Temporal.")]
        [SerializeField, Range(1.1f, 2f)] float escapeMargin = 1.25f;
        [Tooltip("Colchon extra en metros para el tamano del jugador.")]
        [SerializeField, Min(0f)] float radiusPadding = 0.5f;
        [Tooltip("Velocidad del jugador si no se encuentra el PlayerMotor.")]
        [SerializeField, Min(0.1f)] float fallbackPlayerSpeed = 5f;

        [Header("Limpieza y justicia")]
        [Tooltip("true = el Guardian no anuncia el ataque mientras la Parada Temporal se esta recargando: sin ella no habria forma de esquivar.")]
        [SerializeField] bool waitForTimeStop = true;
        [Tooltip("Gracia tras salir de Recuperacion antes de volver a detectar.")]
        [SerializeField, Min(0f)] float graceAfterRecovery = 0.75f;
        [Tooltip("Gracia tras reaparecer el jugador (checkpoint) antes de volver a detectar.")]
        [SerializeField, Min(0f)] float graceAfterRespawn = 1.5f;

        EnemyAI enemy;
        PlayerMotor playerMotor;
        PlayerRespawn playerRespawn;

        float timer;
        float holdLostTimer;
        bool holding;            // Alerta, esperando a que la Parada Temporal este lista
        bool resolvingKill;
        bool warnedFairness;
        float attackRadius;

        public GuardianPhase Phase { get; private set; } = GuardianPhase.Patrulla;
        public bool IsFrozen => enemy != null && enemy.IsFrozen;
        public bool IsHolding => holding;
        public float AttackRadius => attackRadius;
        public float AnticipationSeconds => anticipationSeconds;
        public float RecoverySeconds => recoverySeconds;
        public float PatrolSpeed => patrolSpeed;
        public float ViewAngle => viewAngle;
        public float ViewDistance => viewDistance;
        public float TimeToAlert => timeToAlert;

        /// <summary>0 a 1 dentro de la fase actual (Anticipacion: 1 = golpe inminente).</summary>
        public float PhaseProgress
        {
            get
            {
                switch (Phase)
                {
                    case GuardianPhase.Anticipacion: return Mathf.Clamp01(timer / anticipationSeconds);
                    case GuardianPhase.Ataque: return Mathf.Clamp01(timer / strikeSeconds);
                    case GuardianPhase.Recuperacion: return Mathf.Clamp01(timer / recoverySeconds);
                    default: return 0f;
                }
            }
        }

        // ---------- Ciclo de vida ----------
        void Awake()
        {
            enemy = GetComponent<EnemyAI>();
            ApplyDifficulty(LevelDifficultyConfig.Current);
        }

        void OnEnable()
        {
            EnemyAI.OnEnemyStateChanged += HandleEnemyState;
            if (enemy != null) enemy.PlayerRespawned += HandlePlayerRespawned;
        }

        void OnDisable()
        {
            EnemyAI.OnEnemyStateChanged -= HandleEnemyState;
            if (enemy != null)
            {
                enemy.PlayerRespawned -= HandlePlayerRespawned;
                enemy.Suspended = false;      // nunca dejar la IA base apagada
                enemy.VisionHidden = false;
            }
        }

        /// <summary>
        /// I-32: aplica el bloque del Guardian de la dificultad del nivel (velocidad, cono, tiempo de alerta, anticipacion,
        /// recuperacion). config null = valores del prefab. La anticipacion nunca baja de 1 s.
        /// </summary>
        public void ApplyDifficulty(LevelDifficultyConfig config)
        {
            if (config != null)
            {
                patrolSpeed = config.GuardianSpeed;
                viewAngle = config.GuardianViewAngle;
                viewDistance = config.GuardianViewDistance;
                timeToAlert = config.GuardianTimeToAlert;
                anticipationSeconds = config.GuardianAnticipationSeconds;
                recoverySeconds = config.GuardianRecoverySeconds;
            }
            anticipationSeconds = Mathf.Max(MinAnticipationSeconds, anticipationSeconds);
            recoverySeconds = Mathf.Max(0.5f, recoverySeconds);

            if (enemy == null) enemy = GetComponent<EnemyAI>();
            enemy.SetSpeed(patrolSpeed);
            enemy.ApplyTuning(viewAngle, viewDistance, timeToAlert, enemy.ForgetCooldown);
        }

        // ---------- Entradas externas ----------
        void HandleEnemyState(EnemyAI who, EnemyState previous, EnemyState next)
        {
            if (who != enemy) return;
            if (Phase == GuardianPhase.Patrulla && !holding && next == EnemyState.Alerta)
            {
                // Te vio por completo: el Guardian te fija. La IA base se aparta; el ataque lo gobierna este componente.
                holding = true;
                holdLostTimer = 0f;
                enemy.Suspended = true;
            }
        }

        void HandlePlayerRespawned()
        {
            if (resolvingKill) return;   // lo provoca nuestro propio golpe: seguimos con Ataque -> Recuperacion
            ResetToPatrol();             // murio por otra causa (caida, convoy): el Guardian olvida
        }

        // ---------- Maquina de estados ----------
        void FixedUpdate()
        {
            if (enemy.IsFrozen) return;     // Parada Temporal: el Guardian y su cuenta atras se congelan
            if (!IsPlaying()) return;       // Pausa / Fin de nivel / Game Over

            float dt = Time.fixedDeltaTime;
            switch (Phase)
            {
                case GuardianPhase.Patrulla:
                    if (holding) TickHold(dt);
                    break;

                case GuardianPhase.Anticipacion:
                    timer += dt;
                    FacePlayer(dt);
                    if (timer >= anticipationSeconds) Strike();
                    break;

                case GuardianPhase.Ataque:
                    timer += dt;
                    if (timer >= strikeSeconds) EnterRecovery();
                    break;

                case GuardianPhase.Recuperacion:
                    timer += dt;
                    if (timer >= recoverySeconds) ExitRecovery();
                    break;
            }
        }

        /// <summary>Alerta sin ataque aun: mira al jugador y espera a poder anunciar el ataque con la Parada Temporal disponible.</summary>
        void TickHold(float dt)
        {
            FacePlayer(dt);

            if (enemy.CanSeePlayerNow)
            {
                holdLostTimer = 0f;
                if (CanAnnounceAttack()) BeginAnticipation();
            }
            else
            {
                holdLostTimer += dt;
                if (holdLostTimer >= enemy.ForgetCooldown) ResetToPatrol();   // lo perdio de vista: sin atascos en Alerta
            }
        }

        bool CanAnnounceAttack()
        {
            if (!waitForTimeStop) return true;
            var ts = TimeStopController.Instance;
            return ts == null || ts.State == TimeStopState.Ready;
        }

        void BeginAnticipation()
        {
            holding = false;
            attackRadius = ComputeAttackRadius();
            timer = 0f;
            SetPhase(GuardianPhase.Anticipacion);
        }

        void Strike()
        {
            timer = 0f;
            SetPhase(GuardianPhase.Ataque);

            bool hit = ResolvePlayer() && PlayerInsideZone();
            OnStrike?.Invoke(hit);

            if (!hit) return;   // esquivado: la onda cae en vacio
            if (playerRespawn == null)
            {
                Debug.LogWarning("[GuardianAI] El jugador no tiene PlayerRespawn: no se puede reaparecer.", this);
                return;
            }
            // Kill(true): ignora la invulnerabilidad. Respawn lleva al ultimo checkpoint; si la vida llega a 0, Game Over.
            resolvingKill = true;
            try { playerRespawn.Kill(true); }
            finally { resolvingKill = false; }
        }

        void EnterRecovery()
        {
            timer = 0f;
            holding = false;
            enemy.ForceState(EnemyState.Ignorante);
            enemy.VisionHidden = true;
            SetPhase(GuardianPhase.Recuperacion);
        }

        void ExitRecovery()
        {
            timer = 0f;
            enemy.VisionHidden = false;
            enemy.BlindFor(graceAfterRecovery);
            enemy.Suspended = false;
            SetPhase(GuardianPhase.Patrulla);
        }

        void ResetToPatrol()
        {
            timer = 0f;
            holding = false;
            holdLostTimer = 0f;
            enemy.VisionHidden = false;
            enemy.Suspended = false;
            enemy.BlindFor(graceAfterRespawn);
            enemy.ForceState(EnemyState.Ignorante);
            SetPhase(GuardianPhase.Patrulla);
        }

        void SetPhase(GuardianPhase next)
        {
            if (Phase == next) return;
            GuardianPhase prev = Phase;
            Phase = next;
            OnPhaseChanged?.Invoke(this, prev, next);
        }

        // ---------- Zona de ataque ----------
        /// <summary>
        /// Radio de la onda = alcance de vision + lo que el jugador puede correr durante la anticipacion x margen.
        /// El jugador esta a como mucho 'viewDistance' cuando el Guardian lo fija, asi que correr no basta para salir.
        /// </summary>
        float ComputeAttackRadius()
        {
            float escape = PlayerSpeed() * anticipationSeconds * escapeMargin;
            float radius = enemy.ViewDistance + escape + radiusPadding;
            ValidateFairness(radius);
            return radius;
        }

        /// <summary>
        /// Avisa si, en el peor caso (el jugador pegado al Guardian), la Parada Temporal mas el resto de la anticipacion
        /// no dan tiempo a correr hasta fuera de la zona. Es un error de configuracion del nivel.
        /// </summary>
        void ValidateFairness(float radius)
        {
            if (warnedFairness) return;
            var ts = TimeStopController.Instance;
            if (ts == null) return;
            float needed = radius / Mathf.Max(0.1f, PlayerSpeed());            // segundos corriendo en linea recta
            float available = anticipationSeconds + ts.Duration;               // parada + anticipacion (suman siempre, se pulse cuando se pulse)
            if (needed > available * 0.9f)
            {
                warnedFairness = true;
                Debug.LogWarning($"[GuardianAI] Con la Parada Temporal de {ts.Duration:F1} s y {anticipationSeconds:F1} s de anticipacion, "
                               + $"salir de la zona de ataque exige {needed:F1} s corriendo y solo hay {available:F1} s. Baja la anticipacion o sube la duracion de la parada.", this);
            }
        }

        bool PlayerInsideZone()
        {
            Vector3 p = playerMotor.transform.position;
            Vector3 g = transform.position;
            float dx = p.x - g.x, dz = p.z - g.z;   // plano XZ: saltar no esquiva
            return dx * dx + dz * dz <= attackRadius * attackRadius;
        }

        float PlayerSpeed() => ResolvePlayer() ? playerMotor.MoveSpeed : fallbackPlayerSpeed;

        void FacePlayer(float dt)
        {
            if (ResolvePlayer()) enemy.TurnTowards(playerMotor.transform.position, dt);
        }

        bool ResolvePlayer()
        {
            if (playerMotor != null) return true;
            playerMotor = FindFirstObjectByType<PlayerMotor>();
            if (playerMotor == null) return false;
            playerRespawn = playerMotor.GetComponent<PlayerRespawn>();
            return true;
        }

        static bool IsPlaying()
        {
            var s = GameServices.Instance;
            return s == null || s.State == null || s.State.CurrentState == GameState.Jugando;
        }

        // ---------- Gizmos ----------
        void OnDrawGizmosSelected()
        {
            float r = Application.isPlaying && attackRadius > 0f
                ? attackRadius
                : viewDistance + fallbackPlayerSpeed * Mathf.Max(MinAnticipationSeconds, anticipationSeconds) * escapeMargin + radiusPadding;
            Gizmos.color = new Color(1f, 0.2f, 0.1f, 0.9f);
            const int seg = 64;
            Vector3 c = transform.position + Vector3.up * 0.05f;
            Vector3 prev = c + new Vector3(r, 0f, 0f);
            for (int i = 1; i <= seg; i++)
            {
                float a = i * Mathf.PI * 2f / seg;
                Vector3 p = c + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                Gizmos.DrawLine(prev, p);
                prev = p;
            }
        }
    }
}
