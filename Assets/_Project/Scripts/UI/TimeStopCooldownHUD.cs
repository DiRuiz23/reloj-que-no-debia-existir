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
        void LateUpdate() => Refresh();

        void Refresh()
        {
            if (controller == null || fill == null) return;

            switch (controller.State)
            {
                case TimeStopState.Ready:
                    fill.fillAmount = 1f;
                    break;
                case TimeStopState.Active:
                    // Se consume de 1 a 0 durante la parada.
                    fill.fillAmount = controller.Duration > 0f
                        ? Mathf.Clamp01(controller.RemainingActiveTime / controller.Duration)
                        : 0f;
                    break;
                default: // Recharging: 0 -> 1
                    fill.fillAmount = controller.CooldownNormalized;
                    break;
            }

            if (palette != null)
            {
                fill.color = controller.State == TimeStopState.Recharging
                    ? palette.SepiaMedio   // recargando: tono apagado
                    : palette.Acento;      // listo y activo: color de acento
            }
        }
    }
}
