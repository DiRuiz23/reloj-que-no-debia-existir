using UnityEngine;
using Reloj.Player;

namespace Reloj.Level
{
    [DefaultExecutionOrder(100)]
    public class CameraFollowXZ : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] Vector3 offset = new Vector3(0f, 12f, -9f);
        [SerializeField, Min(0.1f)] float smoothing = 8f;
        [Tooltip("Límites (x, z) del objetivo para no mostrar fuera del escenario.")]
        [SerializeField] Vector2 minXZ = new Vector2(-20f, -2f);
        [SerializeField] Vector2 maxXZ = new Vector2(21f, 9f);

        PlayerRespawn respawn;

        void Start()
        {
            if (target == null)
            {
                var m = FindFirstObjectByType<PlayerMotor>();
                if (m != null) target = m.transform;
            }
            if (target != null)
            {
                respawn = target.GetComponent<PlayerRespawn>();
                if (respawn != null) respawn.OnRespawned += Snap;
            }
            Snap();
        }

        void OnDestroy() { if (respawn != null) respawn.OnRespawned -= Snap; }

        Vector3 Desired()
        {
            Vector3 t = target.position;
            t.x = Mathf.Clamp(t.x, minXZ.x, maxXZ.x);
            t.z = Mathf.Clamp(t.z, minXZ.y, maxXZ.y);
            return t + offset;
        }

        void LateUpdate()
        {
            if (target == null) return;
            float k = 1f - Mathf.Exp(-smoothing * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, Desired(), k);
        }

        void Snap() { if (target != null) transform.position = Desired(); }
    }
}