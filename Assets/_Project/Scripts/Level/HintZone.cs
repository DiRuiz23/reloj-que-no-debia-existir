using UnityEngine;
using UnityEngine.InputSystem;
using Reloj.Core;
using Reloj.Dialogue;
using Reloj.Player;
using Reloj.TimeStop;

namespace Reloj.Level
{
    // Zona que muestra una pista sobre la Parada Temporal mientras el jugador esta dentro.
    // Opcionalmente lanza un aviso breve (ambiguo) la primera vez que se entra, si falta un flag.
    [RequireComponent(typeof(Collider))]
    public class HintZone : MonoBehaviour
    {
        [SerializeField] InputActionReference timeStopAction;
        [SerializeField] string readyMessage = "Pulsa {0} para detener el tiempo";
        [SerializeField] string rechargingMessage = "La Parada se recarga... {0}%";
        [Tooltip("Si es true, deja de mostrar la pista de uso una vez que el jugador ya uso la Parada.")]
        [SerializeField] bool hideAfterFirstUse = true;
        [Tooltip("Si es true, solo avisa mientras la Parada se esta recargando.")]
        [SerializeField] bool onlyWhenRecharging;

        [Header("Aviso al entrar (opcional)")]
        [SerializeField] string flashOnEnter = "";
        [Tooltip("El aviso solo sale si este flag NO esta activo (vacio = siempre).")]
        [SerializeField] string flashIfFlagMissing = "";
        [SerializeField, Min(0.5f)] float flashSeconds = 3.5f;

        bool inside;
        bool flashed;

        void Reset() { GetComponent<Collider>().isTrigger = true; }

        void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<PlayerMotor>() == null) return;
            inside = true;
            if (!flashed && !string.IsNullOrEmpty(flashOnEnter)
                && (string.IsNullOrEmpty(flashIfFlagMissing) || !DialogueFlagManager.GetFlag(flashIfFlagMissing))
                && HintBanner.Instance != null)
            {
                flashed = true;
                HintBanner.Instance.Flash(flashOnEnter, flashSeconds);
            }
        }

        void OnTriggerExit(Collider other)
        {
            if (other.GetComponentInParent<PlayerMotor>() == null) return;
            inside = false;
            if (HintBanner.Instance != null) HintBanner.Instance.Hide(this);
        }

        void OnDisable()
        {
            inside = false;
            if (HintBanner.Instance != null) HintBanner.Instance.Hide(this);
        }

        void Update()
        {
            if (!inside) return;
            var banner = HintBanner.Instance;
            var ctl = TimeStopController.Instance;
            if (banner == null || ctl == null) return;

            if (DialogueRunner.IsRunning) { banner.Hide(this); return; }

            string st = ctl.State.ToString();
            if (st == "Active") { banner.Hide(this); return; }

            if (st == "Recharging")
            {
                int pct = Mathf.RoundToInt(Mathf.Clamp01(ctl.CooldownNormalized) * 100f);
                banner.Show(this, string.Format(rechargingMessage, pct));
                return;
            }

            if (onlyWhenRecharging || (hideAfterFirstUse && HintBanner.TimeStopUsed)) { banner.Hide(this); return; }
            banner.Show(this, string.Format(readyMessage, KeyLabel()));
        }

        string KeyLabel()
        {
            if (timeStopAction == null || timeStopAction.action == null) return "Q";
            return timeStopAction.action.GetBindingDisplayString().Replace(" | ", " / ");
        }
    }
}
