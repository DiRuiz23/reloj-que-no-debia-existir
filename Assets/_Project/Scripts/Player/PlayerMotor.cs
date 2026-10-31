using UnityEngine;
using UnityEngine.InputSystem;

namespace Reloj.Player
{
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public class PlayerMotor : MonoBehaviour
    {
        [Header("Entrada")]
        [SerializeField] InputActionReference moveAction;

        [Header("Movimiento")]
        [SerializeField] float moveSpeed = 5f;
        [SerializeField] float lookThreshold = 0.1f;

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
        }

        void OnDisable()
        {
            if (moveAction != null) moveAction.action.Disable();
        }

        void Update()
        {
            bool locked = InputLocked || Time.time < lockUntil;
            Vector2 raw = (!locked && moveAction != null) ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;
            input = Vector2.ClampMagnitude(raw, 1f);
            NormalizedSpeed = input.magnitude;

            if (input.sqrMagnitude > lookThreshold * lookThreshold)
                LastDirection = input.normalized;
        }

        void FixedUpdate()
        {
            bool moving = input.sqrMagnitude > 0.0001f;
            SetMaterial(moving);

            Vector3 v = rb.linearVelocity;
            rb.linearVelocity = new Vector3(input.x * moveSpeed, v.y, input.y * moveSpeed);
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
            GUI.Label(new Rect(10, 10, 320, 24), $"Vel. horizontal: {h:F2}  |  Input: {input.magnitude:F2}");
        }
    }
}
