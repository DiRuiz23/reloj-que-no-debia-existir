using UnityEngine;
using Reloj.Core;
using Reloj.Player;

namespace Reloj.Level
{
    /// <summary>
    /// Bloqueo condicionado por flags. Esta ABIERTO si CUALQUIERA de 'openIfAnyFlag' esta activo.
    /// Cerrado: activa 'activeWhenClosed' (colliders/visuales del bloqueo). Abierto: activa 'activeWhenOpen'.
    /// Si el jugador toca la zona cerrada (trigger opcional) muestra 'blockedMessage'.
    /// </summary>
    public class FlagGate : MonoBehaviour
    {
        [SerializeField] string[] openIfAnyFlag;
        [SerializeField] GameObject[] activeWhenClosed;
        [SerializeField] GameObject[] activeWhenOpen;
        [SerializeField] string blockedMessage = "";
        [SerializeField] string openedMessage = "";
        [Tooltip("Trigger opcional (en este objeto) para mostrar el aviso al acercarse con la puerta cerrada.")]
        [SerializeField] bool useTriggerForMessage = true;

        float nextMsg;
        public bool IsOpen { get; private set; }

        void OnEnable() { DialogueFlagManager.OnFlagChanged += OnChanged; }
        void OnDisable() { DialogueFlagManager.OnFlagChanged -= OnChanged; }
        void Start() { Apply(false); }

        void OnChanged(string key, bool value)
        {
            if (System.Array.IndexOf(openIfAnyFlag, key) >= 0) Apply(true);
        }

        void Apply(bool announce)
        {
            bool open = false;
            if (openIfAnyFlag != null)
                foreach (var f in openIfAnyFlag) if (DialogueFlagManager.GetFlag(f)) { open = true; break; }
            bool changed = open != IsOpen;
            IsOpen = open;
            Set(activeWhenClosed, !open);
            Set(activeWhenOpen, open);
            if (announce && changed && open && !string.IsNullOrEmpty(openedMessage) && HintBanner.Instance != null)
                HintBanner.Instance.Flash(openedMessage, 3f);
        }

        static void Set(GameObject[] list, bool v)
        {
            if (list == null) return;
            foreach (var g in list) if (g != null) g.SetActive(v);
        }

        void OnTriggerEnter(Collider other)
        {
            if (!useTriggerForMessage || IsOpen || string.IsNullOrEmpty(blockedMessage)) return;
            if (other.GetComponentInParent<PlayerMotor>() == null || Time.time < nextMsg) return;
            nextMsg = Time.time + 4f;
            if (HintBanner.Instance != null) HintBanner.Instance.Flash(blockedMessage, 3.5f);
        }
    }
}
