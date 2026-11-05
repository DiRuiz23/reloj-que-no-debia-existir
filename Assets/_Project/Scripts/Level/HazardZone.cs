using UnityEngine;
using Reloj.Player;

namespace Reloj.Level
{
    /// <summary>Zona que lastima al jugador (nopales). Trigger alrededor de la planta; dano por tick, sin matar de golpe.</summary>
    [RequireComponent(typeof(Collider))]
    public class HazardZone : MonoBehaviour
    {
        [SerializeField, Min(1)] int damage = 8;
        [SerializeField, Min(0.2f)] float tickSeconds = 1f;
        [SerializeField] string message = "Las espinas del nopal duelen.";
        [Tooltip("Empuje (m/s) alejandolo del centro al lastimarlo.")]
        [SerializeField, Min(0f)] float push = 4f;
        float next;

        void Reset() { GetComponent<Collider>().isTrigger = true; }

        void OnTriggerStay(Collider other)
        {
            if (Time.time < next) return;
            var health = other.GetComponentInParent<PlayerHealth>();
            if (health == null) return;
            next = Time.time + tickSeconds;
            health.TakeDamage(damage);
            var rb = health.GetComponent<Rigidbody>();
            if (rb != null && push > 0f)
            {
                Vector3 d = rb.position - transform.position; d.y = 0f;
                rb.linearVelocity = d.normalized * push;
            }
            if (HintBanner.Instance != null && !string.IsNullOrEmpty(message)) HintBanner.Instance.Flash(message, 1.5f);
        }
    }
}
