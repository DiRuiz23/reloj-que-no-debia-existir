using UnityEngine;
using Reloj.Player;
using Reloj.TimeStop;

namespace Reloj.Level
{
    /// <summary>
    /// Reja de engranes (I-22): permanece cerrada y solo cede mientras la Parada Temporal
    /// esta activa. Tras la parada se mantiene abierta un instante (gracia) y nunca se cierra
    /// encima del jugador. Es el punto del Nivel 2 que SOLO se supera con la Parada.
    /// </summary>
    public class TimeStopGate : MonoBehaviour
    {
        [Tooltip("Parte visual que sube al abrirse (los barrotes).")]
        [SerializeField] Transform bars;
        [Tooltip("Colliders que bloquean el paso mientras esta cerrada.")]
        [SerializeField] Collider[] blockers;
        [SerializeField, Min(0.1f)] float openHeight = 3.2f;
        [SerializeField, Min(0.1f)] float slideSpeed = 9f;
        [Tooltip("Segundos que sigue abierta despues de que termina la Parada.")]
        [SerializeField, Min(0f)] float graceSeconds = 0.6f;
        [Tooltip("Zona (local) que debe estar libre de jugador para poder cerrarse.")]
        [SerializeField] Vector3 clearanceCenter = new Vector3(0f, 1.2f, 0f);
        [SerializeField] Vector3 clearanceHalfExtents = new Vector3(0.9f, 1.4f, 2.2f);

        Vector3 closedLocalPos;
        bool open;
        float closeAt = -1f;

        public bool IsOpen => open;

        void Start()
        {
            if (bars != null) closedLocalPos = bars.localPosition;
            Apply(false, true);
        }

        void Update()
        {
            if (TimeStopController.IsStopped)
            {
                if (!open) Apply(true, false);
                closeAt = -1f;
            }
            else if (open)
            {
                if (closeAt < 0f) closeAt = Time.time + graceSeconds;
                if (Time.time >= closeAt && !PlayerInside())
                {
                    Apply(false, false);
                    closeAt = -1f;
                }
            }

            if (bars != null)
            {
                Vector3 target = closedLocalPos + (open ? Vector3.up * openHeight : Vector3.zero);
                bars.localPosition = Vector3.MoveTowards(bars.localPosition, target, slideSpeed * Time.deltaTime);
            }
        }

        void Apply(bool value, bool instant)
        {
            open = value;
            if (blockers != null)
                foreach (var c in blockers) if (c != null) c.enabled = !value;
            if (instant && bars != null)
                bars.localPosition = closedLocalPos + (value ? Vector3.up * openHeight : Vector3.zero);
        }

        bool PlayerInside()
        {
            Vector3 center = transform.TransformPoint(clearanceCenter);
            var hits = Physics.OverlapBox(center, clearanceHalfExtents, transform.rotation, ~0, QueryTriggerInteraction.Ignore);
            foreach (var h in hits)
                if (h.GetComponentInParent<PlayerMotor>() != null) return true;
            return false;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.6f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(clearanceCenter, clearanceHalfExtents * 2f);
        }
    }
}
