using UnityEngine;
using Reloj.Core;
using Reloj.Player;

namespace Reloj.Level
{
    [RequireComponent(typeof(Collider))]
    public class Checkpoint : MonoBehaviour
    {
        [SerializeField] string checkpointId = "CP_01";
        [Tooltip("Orden en el nivel: un checkpoint con orden menor no sobrescribe a uno mayor.")]
        [SerializeField] int order = 1;
        [Tooltip("Opcional: punto exacto de reaparicion (a la altura del suelo).")]
        [SerializeField] Transform respawnPoint;
        [SerializeField] GameObject activatedVisual;
        [Tooltip("Objetos extra que se activan al guardar el checkpoint (luces, etc.).")]
        [SerializeField] GameObject[] activatedVisuals;
        [SerializeField] string message = "Punto de control guardado";

        bool announced;

        void Reset() => GetComponent<Collider>().isTrigger = true;

        void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<PlayerMotor>() == null) return;
            var session = GameServices.Instance != null ? GameServices.Instance.Session : null;
            if (session == null) return;

            Vector3 pos = respawnPoint != null ? respawnPoint.position : transform.position;
            if (session.SetCheckpoint(checkpointId, pos, order)) Activate();
        }

        void Activate()
        {
            if (activatedVisual != null) activatedVisual.SetActive(true);
            if (activatedVisuals != null)
                foreach (var go in activatedVisuals)
                    if (go != null) go.SetActive(true);

            if (announced) return;
            announced = true;
            if (HintBanner.Instance != null) HintBanner.Instance.Flash(message, 2f);
        }
    }
}
