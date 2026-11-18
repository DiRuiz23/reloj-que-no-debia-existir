using UnityEngine;
using Reloj.Core;
using Reloj.Player;

namespace Reloj.Level
{
    /// <summary>
    /// Trigger que activa un flag de narrativa/progreso la primera vez que el jugador entra (Nivel 4).
    /// Alimenta LevelObjectives (el objetivo avanza al activarse el flag) y puede mostrar un aviso breve.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class FlagTriggerZone : MonoBehaviour
    {
        [Tooltip("Flag a activar (debe estar en Resources/Dialogues/_flags.json).")]
        [SerializeField] string flag = "";
        [Tooltip("Aviso opcional al activarse (vacio = ninguno).")]
        [SerializeField] string message = "";
        [SerializeField, Min(0.5f)] float messageSeconds = 3f;

        void Reset() => GetComponent<Collider>().isTrigger = true;

        void OnTriggerEnter(Collider other)
        {
            if (string.IsNullOrEmpty(flag)) return;
            if (other.GetComponentInParent<PlayerMotor>() == null) return;
            if (DialogueFlagManager.GetFlag(flag)) return;

            DialogueFlagManager.SetFlag(flag, true);
            if (!string.IsNullOrEmpty(message) && HintBanner.Instance != null)
                HintBanner.Instance.Flash(message, messageSeconds);
        }
    }
}
