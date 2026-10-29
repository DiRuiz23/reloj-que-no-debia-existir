using UnityEngine;
using Reloj.Core;
using Reloj.Player;

namespace Reloj.Level
{
    [RequireComponent(typeof(Collider))]
    public class LevelGoal : MonoBehaviour
    {
        [SerializeField] Level01Controller level;
        [Tooltip("Flag que debe estar activo para poder terminar el nivel (vacio = sin requisito).")]
        [SerializeField] string requiredFlag = "";
        [Tooltip("Aviso (ambiguo, a modo de pista) cuando el jugador llega sin el requisito.")]
        [SerializeField] string blockedMessage = "";

        float nextMessageTime;

        void Reset() { GetComponent<Collider>().isTrigger = true; }

        void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<PlayerMotor>() == null) return;

            if (!string.IsNullOrEmpty(requiredFlag) && !DialogueFlagManager.GetFlag(requiredFlag))
            {
                if (Time.time >= nextMessageTime && HintBanner.Instance != null && !string.IsNullOrEmpty(blockedMessage))
                {
                    nextMessageTime = Time.time + 4f;
                    HintBanner.Instance.Flash(blockedMessage, 3.5f);
                }
                return;
            }

            if (level == null) level = FindFirstObjectByType<Level01Controller>();
            if (level != null) level.Complete();
        }
    }
}
