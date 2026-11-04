using System;
using UnityEngine;
using TMPro;
using Reloj.Core;
using Reloj.Player;
using Reloj.TimeStop;

namespace Reloj.Stealth
{
    public enum EnemyState { Ignorante, Sospechoso, Alerta }

    /// <summary>
    /// Enemigo de patrulla con waypoints fijos (sin NavMesh), cono de vision y
    /// maquina de estados Ignorante -> Sospechoso -> Alerta (I-21).
    /// Hereda de TimeAffectableBase: congelado por la Parada Temporal no se mueve ni detecta.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class EnemyAI : TimeAffectableBase
    {
        /// <summary>(enemigo, estado anterior, estado nuevo)</summary>
        public static event Action<EnemyAI, EnemyState, EnemyState> OnEnemyStateChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { OnEnemyStateChanged = null; }

        [Header("Patrulla (waypoints fijos)")]
        [Tooltip("Opcional: padre cuyos hijos son los waypoints (si 'Waypoints' esta vacio).")]
        [SerializeField] Transform waypointRoot;
        [SerializeField] Transform[] waypoints;
        [SerializeField, Min(0.1f)] float speed = 2f;
        [SerializeField, Min(0f)] float waitTime = 1f;
        [SerializeField, Min(1f)] float turnSpeed = 240f;
        [SerializeField, Min(0.01f)] float arriveDistance = 0.1f;

        [Header("Vision (los carga I-30 con ApplyTuning)")]
        [SerializeField, Range(10f, 180f)] float viewAngle = 70f;
        [SerializeField, Min(0.5f)] float viewDistance = 6f;
        [SerializeField] float eyeHeight = 1.2f;
        [Tooltip("Capas que bloquean o contienen al jugador (paredes, suelo, Player). La capa propia del enemigo se excluye sola.")]
        [SerializeField] LayerMask visionMask = ~0;

        [Header("Deteccion")]
        [Tooltip("Segundos viendo al jugador antes de pasar a Alerta.")]
        [SerializeField, Min(0.1f)] float timeToAlert = 2f;
        [Tooltip("Segundos sin ver al jugador antes de volver a Ignorante.")]
        [SerializeField, Min(0f)] float forgetCooldown = 3f;
        [Tooltip("Pausa breve en Alerta para que se vea el icono antes de reaparecer.")]
        [SerializeField, Min(0f)] float alertCatchDelay = 0.35f;
        [Tooltip("Tras atrapar o reaparecer, no detecta durante este tiempo.")]
        [SerializeField, Min(0f)] float blindAfterCatch = 1.5f;
        [SerializeField] bool stopWhenSuspicious = true;

        [Header("Icono")]
        [SerializeField] TMP_Text alertIcon;

        Rigidbody body;
        Collider playerCol;
        PlayerRespawn playerRespawn;

        int index;
        float waitTimer, legTimer, legLimit;
        float suspicion, lostTimer, catchTimer, blindTimer;

        public EnemyState State { get; private set; } = EnemyState.Ignorante;
        public float ViewAngle => viewAngle;
        public float ViewDistance => viewDistance;
        public float TimeToAlert => timeToAlert;
        public float ForgetCooldown => forgetCooldown;

        // ---------- Ciclo de vida ----------
        protected override void Awake()
        {
            base.Awake();
            body = GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;

            // Nunca debe verse a si mismo ni a otros enemigos como "pared".
            visionMask &= ~(1 << gameObject.layer);

            if ((waypoints == null || waypoints.Length == 0) && waypointRoot != null)
            {
                waypoints = new Transform[waypointRoot.childCount];
                for (int i = 0; i < waypoints.Length; i++) waypoints[i] = waypointRoot.GetChild(i);
            }
            UpdateIcon();
        }

        void Start()
        {
            index = ClosestWaypoint();
            StartLeg();
            ValidateRoute();
        }

        protected override void OnDisable()
        {
            if (playerRespawn != null) playerRespawn.OnRespawned -= HandlePlayerRespawned;
            playerRespawn = null; playerCol = null;
            base.OnDisable();
        }

        /// <summary>Punto de entrada para LevelDifficultyConfig (I-30).</summary>
        public void ApplyTuning(float angle, float distance, float alertSeconds, float cooldownSeconds)
        {
            viewAngle = Mathf.Clamp(angle, 10f, 180f);
            viewDistance = Mathf.Max(0.5f, distance);
            timeToAlert = Mathf.Max(0.1f, alertSeconds);
            forgetCooldown = Mathf.Max(0f, cooldownSeconds);
        }

        // ---------- Logica ----------
        void FixedUpdate()
        {
            if (IsFrozen) return;                 // Parada Temporal: ni se mueve ni detecta
            if (!IsPlaying()) return;             // Pausa / Fin de nivel / Game Over

            float dt = Time.fixedDeltaTime;
            bool hasPlayer = ResolvePlayer();
            if (blindTimer > 0f) blindTimer -= dt;
            bool sees = hasPlayer && blindTimer <= 0f && CanSeePlayer();

            switch (State)
            {
                case EnemyState.Ignorante:
                    if (sees) { suspicion = 0f; lostTimer = 0f; SetState(EnemyState.Sospechoso); }
                    else Patrol(dt);
                    break;

                case EnemyState.Sospechoso:
                    if (sees)
                    {
                        lostTimer = 0f;
                        suspicion += dt;
                        if (suspicion >= timeToAlert) { catchTimer = alertCatchDelay; SetState(EnemyState.Alerta); }
                    }
                    else
                    {
                        suspicion = 0f;           // visibilidad continua: si se pierde, se reinicia
                        lostTimer += dt;
                        if (lostTimer >= forgetCooldown) SetState(EnemyState.Ignorante);
                    }
                    if (!stopWhenSuspicious) Patrol(dt);
                    break;

                case EnemyState.Alerta:
                    catchTimer -= dt;
                    if (catchTimer <= 0f) CatchPlayer();
                    break;
            }
        }

        static bool IsPlaying()
        {
            var s = GameServices.Instance;
            return s == null || s.State == null || s.State.CurrentState == GameState.Jugando;
        }

        void CatchPlayer()
        {
            blindTimer = blindAfterCatch;
            // Kill(true): ignora la invulnerabilidad para no quedarse atascado en Alerta.
            // Respawn lleva al ultimo checkpoint de GameSession; los flags no se tocan.
            if (playerRespawn != null) playerRespawn.Kill(true);
            else Debug.LogWarning("[EnemyAI] El jugador no tiene PlayerRespawn: no se puede reaparecer.", this);
            suspicion = 0f; lostTimer = 0f;
            SetState(EnemyState.Ignorante);
        }

        void HandlePlayerRespawned()
        {
            // El jugador murio por otra causa (convoy, caida): el enemigo olvida.
            blindTimer = blindAfterCatch;
            suspicion = 0f; lostTimer = 0f;
            SetState(EnemyState.Ignorante);
        }

        // ---------- Vision ----------
        bool CanSeePlayer()
        {
            Vector3 eye = body.position + Vector3.up * eyeHeight;
            Vector3 target = playerCol.bounds.center;

            Vector3 flat = target - eye; flat.y = 0f;
            float dist = flat.magnitude;
            if (dist > viewDistance) return false;

            if (dist > 0.01f)
            {
                Vector3 fwd = body.rotation * Vector3.forward; fwd.y = 0f;
                if (Vector3.Angle(fwd, flat) > viewAngle * 0.5f) return false;
            }

            Vector3 dir = target - eye;
            if (!Physics.Raycast(eye, dir.normalized, out RaycastHit hit, dir.magnitude + 0.1f,
                                 visionMask, QueryTriggerInteraction.Ignore)) return false;
            return hit.collider.GetComponentInParent<PlayerMotor>() != null;  // si golpea pared u otra cosa, no lo ve
        }

        bool ResolvePlayer()
        {
            if (playerCol != null) return true;
            var motor = FindFirstObjectByType<PlayerMotor>();
            if (motor == null) return false;
            playerCol = motor.GetComponent<Collider>();
            playerRespawn = motor.GetComponent<PlayerRespawn>();
            if (playerRespawn != null) playerRespawn.OnRespawned += HandlePlayerRespawned;
            return playerCol != null;
        }

        // ---------- Patrulla ----------
        void Patrol(float dt)
        {
            if (waypoints == null || waypoints.Length < 2) return;

            Vector3 pos = body.position;
            Vector3 target = waypoints[index].position; target.y = pos.y;
            Vector3 to = target - pos; to.y = 0f;

            if (waitTimer > 0f)
            {
                waitTimer -= dt;
                FaceDirection(to, dt);            // durante la pausa gira hacia el siguiente punto
                return;
            }

            legTimer += dt;
            if (to.sqrMagnitude <= arriveDistance * arriveDistance || legTimer > legLimit)
            {
                index = (index + 1) % waypoints.Length;   // ruta ciclica
                waitTimer = waitTime;
                StartLeg();
                return;
            }

            FaceDirection(to, dt);
            body.MovePosition(Vector3.MoveTowards(pos, target, speed * dt));
        }

        void StartLeg()
        {
            legTimer = 0f;
            if (waypoints == null || waypoints.Length == 0) { legLimit = 1f; return; }
            // Anti-atasco: si un tramo tarda el doble de lo esperado, se salta al siguiente punto.
            float d = Vector3.Distance(body.position, waypoints[index].position);
            legLimit = d / speed * 2f + 1f;
        }

        int ClosestWaypoint()
        {
            if (waypoints == null || waypoints.Length == 0) return 0;
            int best = 0; float bestD = float.MaxValue;
            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] == null) continue;
                float d = (waypoints[i].position - transform.position).sqrMagnitude;
                if (d < bestD) { bestD = d; best = i; }
            }
            return best;
        }

        void FaceDirection(Vector3 dir, float dt)
        {
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return;
            Quaternion t = Quaternion.LookRotation(dir.normalized, Vector3.up);
            body.MoveRotation(Quaternion.RotateTowards(body.rotation, t, turnSpeed * dt));
        }

        /// <summary>Avisa en consola si algun tramo de la ruta atraviesa una pared.</summary>
        void ValidateRoute()
        {
            if (waypoints == null || waypoints.Length < 2)
            {
                Debug.LogWarning("[EnemyAI] Necesita al menos 2 waypoints para patrullar.", this);
                return;
            }
            for (int i = 0; i < waypoints.Length; i++)
            {
                Transform a = waypoints[i], b = waypoints[(i + 1) % waypoints.Length];
                if (a == null || b == null) { Debug.LogWarning("[EnemyAI] Hay un waypoint vacio.", this); continue; }
                if (SegmentBlocked(a.position, b.position))
                    Debug.LogWarning($"[EnemyAI] El tramo {a.name} -> {b.name} atraviesa un obstaculo. Reubica los waypoints.", this);
            }
        }

        bool SegmentBlocked(Vector3 a, Vector3 b)
        {
            a.y += 0.5f; b.y += 0.5f;
            Vector3 d = b - a;
            return Physics.SphereCast(a, 0.3f, d.normalized, out _, d.magnitude, visionMask & ~LayerMask.GetMask("Player", "Ground"),
                                      QueryTriggerInteraction.Ignore);
        }

        // ---------- Estado e icono ----------
        void SetState(EnemyState next)
        {
            if (State == next) return;
            EnemyState prev = State;
            State = next;
            UpdateIcon();
            OnEnemyStateChanged?.Invoke(this, prev, next);
        }

        void UpdateIcon()
        {
            if (alertIcon == null) return;
            switch (State)
            {
                case EnemyState.Ignorante:
                    alertIcon.gameObject.SetActive(false); break;
                case EnemyState.Sospechoso:
                    alertIcon.text = "!"; alertIcon.color = new Color(1f, 0.85f, 0.2f);
                    alertIcon.gameObject.SetActive(true); break;
                case EnemyState.Alerta:
                    alertIcon.text = "!!"; alertIcon.color = new Color(1f, 0.2f, 0.2f);
                    alertIcon.gameObject.SetActive(true); break;
            }
        }

        // ---------- Gizmos ----------
        void OnDrawGizmos()
        {
            Vector3 origin = transform.position + Vector3.up * eyeHeight;
            Vector3 fwd = transform.forward; fwd.y = 0f;
            fwd = fwd.sqrMagnitude < 0.0001f ? Vector3.forward : fwd.normalized;

            Color c = new Color(0.2f, 1f, 0.3f);
            if (Application.isPlaying)
                c = State == EnemyState.Alerta ? Color.red
                  : State == EnemyState.Sospechoso ? new Color(1f, 0.85f, 0.2f) : c;
            Gizmos.color = c;

            float half = viewAngle * 0.5f;
            Vector3 prev = origin + Quaternion.AngleAxis(-half, Vector3.up) * fwd * viewDistance;
            Gizmos.DrawLine(origin, prev);
            const int seg = 24;
            for (int i = 1; i <= seg; i++)
            {
                float a = -half + viewAngle * i / seg;
                Vector3 p = origin + Quaternion.AngleAxis(a, Vector3.up) * fwd * viewDistance;
                Gizmos.DrawLine(prev, p);
                prev = p;
            }
            Gizmos.DrawLine(origin, prev);

            // Ruta
            Gizmos.color = Color.cyan;
            int n = waypoints != null && waypoints.Length > 0 ? waypoints.Length
                  : waypointRoot != null ? waypointRoot.childCount : 0;
            for (int i = 0; i < n; i++)
            {
                Transform a = (waypoints != null && waypoints.Length > 0) ? waypoints[i] : waypointRoot.GetChild(i);
                Transform b = (waypoints != null && waypoints.Length > 0) ? waypoints[(i + 1) % n] : waypointRoot.GetChild((i + 1) % n);
                if (a == null || b == null) continue;
                Gizmos.DrawSphere(a.position, 0.15f);
                Gizmos.DrawLine(a.position, b.position);
            }
        }
    }
}
