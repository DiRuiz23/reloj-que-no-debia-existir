using UnityEngine;
using Reloj.Player;
using Reloj.TimeStop;

namespace Reloj.Level
{
    // Vagon de un convoy que cruza la zona jugable en bucle (eje Z).
    // En movimiento mata al jugador al tocarlo; congelado por la Parada Temporal
    // es un obstaculo solido pero inofensivo.
    [RequireComponent(typeof(Rigidbody))]
    public class ConvoyWagon : TimeAffectableBase
    {
        [Header("Movimiento")]
        [Tooltip("Velocidad en m/s. Negativa = el convoy avanza en sentido contrario.")]
        [SerializeField] float speed = 9f;
        [Tooltip("Limites del bucle. Largo del bucle = N vagones x (largo + hueco).")]
        [SerializeField] float loopMinZ = -24.8f;
        [SerializeField] float loopMaxZ = 24.8f;

        [Header("Visibilidad")]
        [Tooltip("El vagon se oculta cuando su centro sale de este rango (fuera del mapa jugable).")]
        [SerializeField] float visibleMinZ = -8.2f;
        [SerializeField] float visibleMaxZ = 13.4f;

        Rigidbody body;
        Renderer[] renderers;
        bool visible = true;

        protected override void Awake()
        {
            base.Awake();
            body = GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            renderers = GetComponentsInChildren<Renderer>();
        }

        void FixedUpdate()
        {
            if (IsFrozen) return;

            Vector3 p = body.position;
            p.z += speed * Time.fixedDeltaTime;
            float length = loopMaxZ - loopMinZ;
            bool wrapped = false;
            if (speed >= 0f && p.z > loopMaxZ) { p.z -= length; wrapped = true; }       // vuelve al inicio (fuera de la zona jugable)
            else if (speed < 0f && p.z < loopMinZ) { p.z += length; wrapped = true; }   // convoy en sentido contrario

            if (wrapped)
            {
                body.position = p;
                transform.position = p;
            }
            else body.MovePosition(p);
        }

        void LateUpdate()
        {
            float z = transform.position.z;
            bool shouldShow = z >= visibleMinZ && z <= visibleMaxZ;
            if (shouldShow == visible) return;
            visible = shouldShow;
            for (int i = 0; i < renderers.Length; i++)
                if (renderers[i] != null) renderers[i].enabled = visible;
        }

        void OnCollisionEnter(Collision c) { TryKill(c); }
        void OnCollisionStay(Collision c) { TryKill(c); }

        void TryKill(Collision c)
        {
            if (IsFrozen) return;   // congelado = inofensivo
            var respawn = c.collider.GetComponentInParent<PlayerRespawn>();
            if (respawn != null) respawn.Kill();
        }
    }
}
