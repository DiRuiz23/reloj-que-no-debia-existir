using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Reloj.Player
{
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public class PlayerMotor : MonoBehaviour
    {
        [Header("Entrada")]
        [SerializeField] InputActionReference moveAction;
        [SerializeField] InputActionReference jumpAction;

        [Header("Movimiento")]
        [SerializeField] float moveSpeed = 5f;
        [SerializeField] float lookThreshold = 0.1f;

        [Header("Salto")]
        [Tooltip("Altura maxima del salto en metros. Corto a proposito: no debe saltarse los vagones del convoy.")]
        [SerializeField, Min(0.1f)] float jumpHeight = 0.8f;
        [Tooltip("Capas que cuentan como suelo.")]
        [SerializeField] LayerMask groundMask = ~0;
        [SerializeField, Min(0.01f)] float groundCheckDistance = 0.12f;
        [Tooltip("Si se pulsa saltar un instante antes de tocar el suelo, el salto se hace igual.")]
        [SerializeField, Min(0f)] float jumpBufferSeconds = 0.12f;

        [Header("Fisica (evita deslizarse en rampas al estar quieto)")]
        [SerializeField] PhysicsMaterial movingMaterial;
        [SerializeField] PhysicsMaterial idleMaterial;

        [Header("Debug")]
        [SerializeField] bool showDebug;

        Rigidbody rb;
        CapsuleCollider col;
        Vector2 input;
        bool wasMoving;
        float lockUntil;
        float jumpBufferedUntil = -1f;

        /// <summary>
        /// true = ignora el input de movimiento (dialogos, cinematicas). El personaje se detiene
        /// y su animacion vuelve a Idle; al ponerlo en false recupera el control.
        /// </summary>
        public bool InputLocked { get; set; }

        /// <summary>Ignora el input unos segundos (p. ej. animacion de lanzamiento). No interfiere con InputLocked.</summary>
        public void LockFor(float seconds)
        {
            lockUntil = Mathf.Max(lockUntil, Time.time + seconds);
        }

        /// <summary>0 a 1: magnitud del input (stick proporcional, teclado = 1).</summary>
        public float NormalizedSpeed { get; private set; }

        /// <summary>Ultima direccion de movimiento (x, y del plano XZ). Se conserva al detenerse.</summary>
        public Vector2 LastDirection { get; private set; } = Vector2.down;

        /// <summary>true mientras los pies tocan el suelo.</summary>
        public bool IsGrounded { get; private set; }

        /// <summary>Se dispara al despegar en un salto (para el tutorial, sonido, animacion).</summary>
        public event Action Jumped;

        bool Locked => InputLocked || Time.time < lockUntil;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            col = GetComponent<CapsuleCollider>();
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            if (idleMaterial != null) col.sharedMaterial = idleMaterial;
        }

        void OnEnable()
        {
            if (moveAction != null) moveAction.action.Enable();
            if (jumpAction != null)
            {
                jumpAction.action.performed += OnJumpPerformed;
                jumpAction.action.Enable();
            }
        }

        void OnDisable()
        {
            if (moveAction != null) moveAction.action.Disable();
            if (jumpAction != null)
            {
                jumpAction.action.performed -= OnJumpPerformed;
                jumpAction.action.Disable();
            }
        }

        void OnJumpPerformed(InputAction.CallbackContext ctx)
        {
            if (Locked || Time.timeScale == 0f) return;   // sin saltar en dialogo ni en pausa
            jumpBufferedUntil = Time.time + jumpBufferSeconds;
        }

        void Update()
        {
            Vector2 raw = (!Locked && moveAction != null) ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;
            input = Vector2.ClampMagnitude(raw, 1f);
            NormalizedSpeed = input.magnitude;

            if (input.sqrMagnitude > lookThreshold * lookThreshold)
                LastDirection = input.normalized;
        }

        void FixedUpdate()
        {
            bool moving = input.sqrMagnitude > 0.0001f;
            SetMaterial(moving);

            IsGrounded = CheckGrounded();

            Vector3 v = rb.linearVelocity;
            float vy = v.y;

            if (jumpBufferedUntil >= Time.time && IsGrounded && !Locked)
            {
                jumpBufferedUntil = -1f;
                float g = Mathf.Abs(Physics.gravity.y);
                vy = Mathf.Sqrt(2f * g * jumpHeight);
                IsGrounded = false;
                Jumped?.Invoke();
            }

            rb.linearVelocity = new Vector3(input.x * moveSpeed, vy, input.y * moveSpeed);
        }

        bool CheckGrounded()
        {
            // Esfera desde el pie del capsulo hacia abajo. El capsulo propio no cuenta: el cast
            // empieza dentro de el, y los colliders que ya se solapan al inicio se ignoran.
            float r = col.radius * 0.9f;
            Vector3 feet = transform.TransformPoint(col.center) + Vector3.down * (col.height * 0.5f - col.radius);
            float dist = groundCheckDistance + col.radius * 0.1f;
            return Physics.SphereCast(feet + Vector3.up * 0.02f, r, Vector3.down, out _, dist + 0.02f,
                                      groundMask, QueryTriggerInteraction.Ignore);
        }

        void SetMaterial(bool moving)
        {
            if (moving == wasMoving) return;
            wasMoving = moving;
            col.sharedMaterial = moving ? movingMaterial : idleMaterial;
        }

        void OnGUI()
        {
            if (!showDebug || rb == null) return;
            Vector3 v = rb.linearVelocity;
            float h = new Vector2(v.x, v.z).magnitude;
            GUI.Label(new Rect(10, 10, 360, 24), $"Vel. horizontal: {h:F2}  |  Input: {input.magnitude:F2}  |  Suelo: {IsGrounded}");
        }
    }
}
