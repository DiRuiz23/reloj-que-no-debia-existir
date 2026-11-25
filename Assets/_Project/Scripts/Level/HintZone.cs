using UnityEngine;
using UnityEngine.InputSystem;
using Reloj.Core;
using Reloj.Dialogue;
using Reloj.Player;
using Reloj.TimeStop;
using Reloj.Tutorial;

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
        [Tooltip("Lo que se muestra en {0} con pantalla tactil (hasta que I-37 agregue bindings tactiles).")]
        [SerializeField] string touchLabel = "el boton de Parada";
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
        int lastPct = -1;
        string lastRechargeText;
        bool hasReadyScheme;
        InputScheme lastReadyScheme;
        string cachedReadyText;

        void Reset() { GetComponent<Collider>().isTrigger = true; }

        void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<PlayerMotor>() == null) return;
            inside = true;
            if (timeStopAction != null) timeStopAction.action.Enable();
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

            switch (ctl.State)
            {
                case TimeStopState.Active:
                    banner.Hide(this);
                    return;

                case TimeStopState.Recharging:
                    int pct = Mathf.RoundToInt(Mathf.Clamp01(ctl.CooldownNormalized) * 100f);
                    if (pct != lastPct || lastRechargeText == null)
                    {
                        lastPct = pct;
                        lastRechargeText = string.Format(rechargingMessage, pct);
                    }
                    banner.Show(this, lastRechargeText);
                    return;

                default:
                    if (onlyWhenRecharging || (hideAfterFirstUse && HintBanner.TimeStopUsed)) { banner.Hide(this); return; }
                    var scheme = InputDeviceTracker.Current;
                    if (!hasReadyScheme || scheme != lastReadyScheme || cachedReadyText == null)
                    {
                        hasReadyScheme = true;
                        lastReadyScheme = scheme;
                        cachedReadyText = string.Format(readyMessage, KeyLabel());
                    }
                    banner.Show(this, cachedReadyText);
                    return;
            }
        }

        // Tecla/boton real del dispositivo que la persona esta usando (teclado, gamepad o tactil).
        string KeyLabel()
        {
            if (timeStopAction == null || timeStopAction.action == null) return "[Q]";
            return InputPrompt.Label(timeStopAction.action, InputDeviceTracker.Current, touchLabel);
        }
    }
}
