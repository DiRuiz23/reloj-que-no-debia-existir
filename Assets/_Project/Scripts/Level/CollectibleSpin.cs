using UnityEngine;

namespace Reloj.Player
{
    /// <summary>Hace girar el coleccionable sobre su propio eje vertical, con un leve flote y pulso para que llame la atencion.</summary>
    public class CollectibleSpin : MonoBehaviour
    {
        [SerializeField] float degreesPerSecond = 140f;
        [SerializeField, Min(0f)] float bobAmplitude = 0.12f;
        [SerializeField, Min(0f)] float bobSpeed = 2.2f;
        [SerializeField, Range(0f, 0.3f)] float pulse = 0.08f;

        [Tooltip("Anillo de brillo que se pega al piso bajo el coleccionable (no gira).")]
        [SerializeField] Transform groundRing;
        [SerializeField, Min(0f)] float ringLift = 0.04f;

        Vector3 startPos, startScale; float phase;

        void Start()
        {
            startPos = transform.localPosition; startScale = transform.localScale;
            phase = Random.value * Mathf.PI * 2f;
        }

        void PlaceRing()
        {
            if (groundRing == null) return;
            Transform owner = transform.parent != null ? transform.parent : transform;
            Vector3 from = owner.position + Vector3.up * 0.3f;
            var hits = Physics.RaycastAll(from, Vector3.down, 8f, ~0, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue; Vector3 point = owner.position + Vector3.down * 0.85f;
            foreach (var h in hits)
            {
                if (h.collider.transform.IsChildOf(owner)) continue;
                if (h.distance < best) { best = h.distance; point = h.point; }
            }
            groundRing.position = point + Vector3.up * ringLift;
            groundRing.rotation = Quaternion.Euler(90f, 0f, 0f);
        }

        void Update()
        {
            transform.Rotate(0f, degreesPerSecond * Time.deltaTime, 0f, Space.Self);
            float s = Mathf.Sin(Time.time * bobSpeed + phase);
            transform.localPosition = startPos + Vector3.up * (s * bobAmplitude);
            transform.localScale = startScale * (1f + pulse * (s * 0.5f + 0.5f));
        }
    }
}
