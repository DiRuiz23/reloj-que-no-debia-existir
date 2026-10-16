using Reloj.Controls;
using Reloj.Core;
using UnityEngine;

namespace Reloj.Player
{
    /// <summary>
    /// TEMPORAL (I-05): simula los parámetros que publicará I-04.
    /// Eliminar cuando el control real del jugador esté integrado.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class AnimatorTestDriver : MonoBehaviour
    {
        [SerializeField] private float _moveSpeed = 2f;
        [Tooltip("¿El sprite de lado del sheet mira a la derecha?")]
        [SerializeField] private bool _sheetSideFacesRight = true;

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int MoveXHash = Animator.StringToHash("MoveX");
        private static readonly int MoveYHash = Animator.StringToHash("MoveY");
        private static readonly int InteractHash = Animator.StringToHash("Interact");

        private Animator _animator;
        private SpriteRenderer _renderer;
        private PlayerControls _controls;
        private Vector2 _lastDirection = Vector2.down;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _renderer = GetComponent<SpriteRenderer>();
            _controls = new PlayerControls();
        }

        private void OnEnable() { _controls.Gameplay.Enable(); }
        private void OnDisable() { _controls.Gameplay.Disable(); }
        private void OnDestroy() { _controls.Dispose(); }

        private void Update()
        {
            Vector2 input = _controls.Gameplay.Move.ReadValue<Vector2>();
            bool isMoving = input.sqrMagnitude > 0.01f;

            if (isMoving)
            {
                // Dirección dominante (sin diagonales) que se conserva al detenerse.
                _lastDirection = Mathf.Abs(input.x) >= Mathf.Abs(input.y)
                    ? new Vector2(Mathf.Sign(input.x), 0f)
                    : new Vector2(0f, Mathf.Sign(input.y));

                Vector3 step = new Vector3(_lastDirection.x, 0f, _lastDirection.y);
                transform.position += step * (_moveSpeed * Time.deltaTime);
            }

            // La izquierda reutiliza el lado derecho volteado (o al revés, según el sheet).
            _renderer.flipX = _sheetSideFacesRight ? _lastDirection.x < 0f
                                                   : _lastDirection.x > 0f;

            _animator.SetFloat(SpeedHash, isMoving ? 1f : 0f);
            _animator.SetFloat(MoveXHash, _lastDirection.x);
            _animator.SetFloat(MoveYHash, _lastDirection.y);

            if (_controls.Gameplay.Interact.WasPressedThisFrame())
                _animator.SetTrigger(InteractHash);
        }
    }
}