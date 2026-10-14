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

        void Reset()
        {
            motor = GetComponentInParent<PlayerMotor>();
            animator = GetComponent<Animator>();
            spriteRenderer = GetComponent<SpriteRenderer>();
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
            if (spriteRenderer != null && Mathf.Abs(dir.x) > 0.01f)
                spriteRenderer.flipX = dir.x < 0f;
        }
    }
}