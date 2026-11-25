using Reloj.TimeStop;
using UnityEngine;
using UnityEngine.UI;

namespace Reloj.UI
{
    /// <summary>
    /// Indicador de la Parada Temporal (I-17). Reloj con relleno radial:
    /// Listo = lleno (acento) | Activo = se consume 1 -> 0 | Recarga = se llena 0 -> 1.
    /// </summary>
    public class TimeStopCooldownHUD : MonoBehaviour
    {
        [SerializeField] TimeStopController controller;
        [SerializeField] UIPalette palette;
        [SerializeField] Image fill;
        [SerializeField] Image background;

        TimeStopState lastState = (TimeStopState)(-1);
        float lastFill = -1f;

        void Start()
        {
            if (controller == null) controller = TimeStopController.Instance;
            if (controller == null)
            {
                Debug.LogWarning("TimeStopCooldownHUD: no hay TimeStopController en la escena.", this);
                enabled = false;
                return;
            }

            controller.OnTimeStopStart += Refresh;
            controller.OnTimeStopEnd += Refresh;
            controller.OnTimeStopReady += Refresh;

            if (background != null && palette != null)
            {
                Color c = palette.Tinta;
                c.a = 0.6f;
                background.color = c;
            }

            Refresh();
        }

        void OnDestroy()
        {
            if (controller == null) return;
            controller.OnTimeStopStart -= Refresh;
            controller.OnTimeStopEnd -= Refresh;
            controller.OnTimeStopReady -= Refresh;
        }

        // LateUpdate: el controller ya actualizo su timer en Update este mismo frame.
        void LateUpdate()
        {
            if (controller == null) return;
            if (controller.State == TimeStopState.Ready && lastState == TimeStopState.Ready) return;
            Refresh();
        }

        void Refresh()
        {
            if (controller == null || fill == null) return;

            float nextFill;
            switch (controller.State)
            {
                case TimeStopState.Ready:
                    nextFill = 1f;
                    break;
                case TimeStopState.Active:
                    // Se consume de 1 a 0 durante la parada.
                    nextFill = controller.Duration > 0f
                        ? Mathf.Clamp01(controller.RemainingActiveTime / controller.Duration)
                        : 0f;
                    break;
                default: // Recharging: 0 -> 1
                    nextFill = controller.CooldownNormalized;
                    break;
            }

            if (!Mathf.Approximately(nextFill, lastFill))
            {
                fill.fillAmount = nextFill;
                lastFill = nextFill;
            }

            if (palette != null && controller.State != lastState)
            {
                fill.color = controller.State == TimeStopState.Recharging
                    ? palette.SepiaMedio   // recargando: tono apagado
                    : palette.Acento;      // listo y activo: color de acento
            }
            lastState = controller.State;
        }
    }
}
