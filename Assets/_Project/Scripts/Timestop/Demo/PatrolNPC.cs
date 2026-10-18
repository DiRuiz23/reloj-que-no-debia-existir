using UnityEngine;

namespace Reloj.TimeStop
{
    [RequireComponent(typeof(Rigidbody))]
    public class PatrolNPC : TimeAffectableBase
    {
        [SerializeField] Transform[] waypoints;
        [SerializeField] float speed = 2f;
        [SerializeField] float waitTime = 1f;
        [SerializeField] float arriveDistance = 0.05f;

        Rigidbody body;
        int index;
        float waitTimer;

        protected override void Awake()
        {
            base.Awake();
            body = GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
        }

        void FixedUpdate()
        {
            if (TimeStopController.IsStopped) return;
            if (waypoints == null || waypoints.Length < 2) return;

            if (waitTimer > 0f)
            {
                waitTimer -= Time.deltaTime; // en FixedUpdate equivale al paso fijo
                return;
            }

            Vector3 target = waypoints[index].position;
            target.y = body.position.y;

            Vector3 next = Vector3.MoveTowards(body.position, target, speed * Time.deltaTime);
            body.MovePosition(next);

            if ((next - target).sqrMagnitude <= arriveDistance * arriveDistance)
            {
                index = (index + 1) % waypoints.Length;
                waitTimer = waitTime;
            }
        }
    }
}