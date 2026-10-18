using UnityEngine;

namespace Reloj.TimeStop
{
    [RequireComponent(typeof(Rigidbody))]
    public class SlidingBarrier : TimeAffectableBase
    {
        [SerializeField] Vector3 travel = new Vector3(6f, 0f, 0f);
        [SerializeField] float speed = 2f;
        [Tooltip("rad/s. Solo para probar la velocidad angular; dejar en 0 en uso normal.")]
        [SerializeField] Vector3 spin = Vector3.zero;

        Rigidbody body;
        Vector3 start;
        Vector3 dir;
        float length;
        bool forward = true;

        protected override void Awake()
        {
            base.Awake();
            body = GetComponent<Rigidbody>();
            body.useGravity = false;
            body.linearDamping = 0f;
            body.angularDamping = 0f;
            body.mass = 50f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.constraints = spin == Vector3.zero ? RigidbodyConstraints.FreezeRotation : RigidbodyConstraints.None;
        }

        void Start()
        {
            start = transform.position;
            length = travel.magnitude;
            dir = travel.normalized;
            body.linearVelocity = dir * speed;
            body.angularVelocity = spin;
        }

        void FixedUpdate()
        {
            if (TimeStopController.IsStopped) return; // no tocar velocidad de un kinematic

            float along = Vector3.Dot(body.position - start, dir);
            if (forward && along >= length)
            {
                forward = false;
                body.linearVelocity = -dir * speed;
            }
            else if (!forward && along <= 0f)
            {
                forward = true;
                body.linearVelocity = dir * speed;
            }
        }
    }
}