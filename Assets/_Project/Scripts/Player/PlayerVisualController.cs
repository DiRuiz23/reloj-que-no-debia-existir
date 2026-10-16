using UnityEngine;

namespace Reloj.Player
{
    public class PlayerVisualController : MonoBehaviour
    {
        [SerializeField] PlayerMotor motor;
        [SerializeField] Animator animator;
        [SerializeField] SpriteRenderer spriteRenderer;

        static readonly int SpeedHash = Animator.StringToHash("Speed");
        static readonly int MoveXHash = Animator.StringToHash("MoveX");
        static readonly int MoveYHash = Animator.StringToHash("MoveY");

        Vector3 baseScale = Vector3.one;

        void Reset()
        {
            motor = GetComponentInParent<PlayerMotor>();
            animator = GetComponent<Animator>();
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        void Awake()
        {
            Vector3 s = transform.localScale;
            // Si la escala llega en 0 (o invalida), usa 1 para que el sprite nunca desaparezca.
            baseScale = new Vector3(
                Mathf.Approximately(s.x, 0f) ? 1f : Mathf.Abs(s.x),
                Mathf.Approximately(s.y, 0f) ? 1f : s.y,
                Mathf.Approximately(s.z, 0f) ? 1f : s.z);
            transform.localScale = baseScale;
            // El volteo se hace con la escala, no con flipX (el Shader Graph del material ignora flipX).
            if (spriteRenderer != null) spriteRenderer.flipX = false;
        }

        void Update()
        {
            if (motor == null) return;
            Vector2 dir = motor.LastDirection;

            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.SetFloat(SpeedHash, motor.NormalizedSpeed);
                animator.SetFloat(MoveXHash, dir.x);
                animator.SetFloat(MoveYHash, dir.y);
            }

            // Se voltea solo cuando hay componente horizontal; al ir arriba/abajo conserva el ultimo lado.
            if (Mathf.Abs(dir.x) > 0.01f)
            {
                Vector3 s = baseScale;
                s.x = dir.x < 0f ? -baseScale.x : baseScale.x;
                transform.localScale = s;
            }
        }
    }
}
