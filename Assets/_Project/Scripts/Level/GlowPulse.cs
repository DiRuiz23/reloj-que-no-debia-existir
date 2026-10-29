using UnityEngine;

namespace Reloj.Level
{
    // Pulso suave de escala para marcadores brillantes (meta, checkpoint activo).
    public class GlowPulse : MonoBehaviour
    {
        [SerializeField, Range(0f, 0.5f)] float amount = 0.1f;
        [SerializeField, Min(0.1f)] float speed = 2f;

        Vector3 baseScale;

        void Awake() { baseScale = transform.localScale; }

        void Update()
        {
            transform.localScale = baseScale * (1f + Mathf.Sin(Time.time * speed) * amount);
        }
    }
}
