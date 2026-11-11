using UnityEngine;
using Reloj.Player;

namespace Reloj.Stealth
{
    /// <summary>
    /// Maneja los sprites/animaciones del soldado (Idle, Walk, Aim, Shoot) a partir del estado de EnemyAI.
    /// Ignorante  -> Idle / Walk segun se mueva, mirando hacia donde camina.
    /// Sospechoso -> Aim (apunta hacia el jugador).
    /// Alerta     -> Shoot (disparo antes de reaparecer al jugador).
    /// El volteo horizontal se hace con la escala (igual que el jugador), no con flipX.
    /// La Parada Temporal pausa el Animator sola (TimeAffectableBase congela los Animators hijos).
    /// </summary>
    [DisallowMultipleComponent]
    public class EnemyVisualController : MonoBehaviour
    {
        [SerializeField] EnemyAI enemy;
        [SerializeField] Animator animator;
        [SerializeField] SpriteRenderer spriteRenderer;
        [Tooltip("Velocidad minima (m/s) para considerar que el soldado camina.")]
        [SerializeField, Min(0f)] float walkThreshold = 0.15f;

        static readonly int SpeedHash = Animator.StringToHash("Speed");
        static readonly int MoveXHash = Animator.StringToHash("MoveX");
        static readonly int MoveYHash = Animator.StringToHash("MoveY");
        static readonly int AimXHash = Animator.StringToHash("AimX");
        static readonly int AimHash = Animator.StringToHash("Aim");
        static readonly int ShootHash = Animator.StringToHash("Shoot");

        Vector3 baseScale = Vector3.one;
        Vector3 lastPos;
        Vector2 facing = Vector2.down;
        bool flip;
        EnemyState lastState = EnemyState.Ignorante;
        Transform player;

        void Reset()
        {
            enemy = GetComponentInParent<EnemyAI>();
            animator = GetComponent<Animator>();
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        void Awake()
        {
            if (enemy == null) enemy = GetComponentInParent<EnemyAI>();
            if (animator == null) animator = GetComponent<Animator>();
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

            Vector3 s = transform.localScale;
            baseScale = new Vector3(
                Mathf.Approximately(s.x, 0f) ? 1f : Mathf.Abs(s.x),
                Mathf.Approximately(s.y, 0f) ? 1f : s.y,
                Mathf.Approximately(s.z, 0f) ? 1f : s.z);
            transform.localScale = baseScale;
            if (spriteRenderer != null) spriteRenderer.flipX = false;   // el material ignora flipX

            if (enemy != null)
            {
                lastPos = enemy.transform.position;
                Vector3 f = enemy.transform.forward;
                if (new Vector2(f.x, f.z).sqrMagnitude > 0.0001f) facing = new Vector2(f.x, f.z).normalized;
            }
        }

        void Update()
        {
            if (enemy == null || animator == null || animator.runtimeAnimatorController == null) return;

            EnemyState state = enemy.State;
            bool aiming = state == EnemyState.Sospechoso;
            bool shooting = state == EnemyState.Alerta;

            // ---- Direccion que mira ----
            if ((aiming || shooting) && ResolvePlayer())
            {
                Vector3 to = player.position - enemy.transform.position;
                Vector2 d = new Vector2(to.x, to.z);
                if (d.sqrMagnitude > 0.0001f) facing = d.normalized;
            }
            else
            {
                Vector3 f = enemy.transform.forward;
                Vector2 d = new Vector2(f.x, f.z);
                if (d.sqrMagnitude > 0.0001f) facing = d.normalized;
            }

            // ---- Velocidad real (se queda en 0 si la Parada Temporal lo congela) ----
            Vector3 pos = enemy.transform.position;
            float dt = Mathf.Max(Time.deltaTime, 0.0001f);
            Vector2 delta = new Vector2(pos.x - lastPos.x, pos.z - lastPos.z);
            lastPos = pos;
            bool moving = !enemy.IsFrozen && delta.magnitude / dt > walkThreshold;

            // ---- Volteo (con histeresis para que no parpadee cerca de x = 0) ----
            if (Mathf.Abs(facing.x) > 0.05f)
            {
                if (aiming) flip = false;                               // Aim usa E/W propios
                else if (shooting) flip = facing.x < 0f;                // Shoot: todos miran a la derecha
                else flip = facing.y >= 0f ? facing.x < 0f : facing.x > 0f; // N/NE miran a la derecha, S/SW a la izquierda
            }
            else if (aiming) flip = false;

            Vector3 sc = baseScale;
            sc.x = flip ? -baseScale.x : baseScale.x;
            transform.localScale = sc;

            // ---- Animator ----
            animator.SetFloat(SpeedHash, moving ? 1f : 0f);
            animator.SetFloat(MoveXHash, Mathf.Abs(facing.x));
            animator.SetFloat(AimXHash, facing.x);
            animator.SetFloat(MoveYHash, facing.y);
            animator.SetBool(AimHash, aiming);

            if (state != lastState)
            {
                if (state == EnemyState.Alerta) animator.SetTrigger(ShootHash);
                lastState = state;
            }
        }

        bool ResolvePlayer()
        {
            if (player != null) return true;
            var motor = FindFirstObjectByType<PlayerMotor>();
            if (motor == null) return false;
            player = motor.transform;
            return true;
        }
    }
}
