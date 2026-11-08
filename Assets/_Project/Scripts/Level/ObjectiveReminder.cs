using UnityEngine;
using Reloj.Core;
using Reloj.Player;

namespace Reloj.Level
{
    /// <summary>
    /// Recordatorio de objetivo: al entrar el jugador, si NINGUNO de 'doneFlags' esta activo,
    /// muestra 'message' (ej. 'Antes de continuar debo hablar con Lupe'). Pensado para los checkpoints.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ObjectiveReminder : MonoBehaviour
    {
        [SerializeField] string[] doneFlags;
        [TextArea, SerializeField] string message;
        [SerializeField, Min(0.5f)] float seconds = 4.5f;
        [SerializeField, Min(0f)] float cooldown = 6f;
        float next;

        void Reset() { GetComponent<Collider>().isTrigger = true; }

        void OnTriggerEnter(Collider other)
        {
            if (Time.time < next || other.GetComponentInParent<PlayerMotor>() == null) return;
            if (doneFlags != null)
                foreach (var f in doneFlags) if (DialogueFlagManager.GetFlag(f)) return;
            next = Time.time + cooldown;
            if (HintBanner.Instance != null) HintBanner.Instance.Flash(message, seconds);
        }
    }
}
