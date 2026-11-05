using UnityEngine;
using Reloj.Player;

namespace Reloj.Level
{
    /// <summary>Botiquin de un solo uso: cura al jugador al entrar.</summary>
    [RequireComponent(typeof(Collider))]
    public class HealthStation : MonoBehaviour
    {
        [SerializeField, Min(1)] int healAmount = 34;
        [SerializeField] string message = "Vendas y agua limpia. Me siento mejor.";
        [SerializeField] GameObject[] hideWhenUsed;
        bool used;

        void Reset() { GetComponent<Collider>().isTrigger = true; }

        void OnTriggerEnter(Collider other)
        {
            if (used) return;
            var h = other.GetComponentInParent<PlayerHealth>();
            if (h == null || h.Current >= h.Max) return;
            used = true;
            h.Heal(healAmount);
            if (hideWhenUsed != null) foreach (var g in hideWhenUsed) if (g != null) g.SetActive(false);
            if (HintBanner.Instance != null) HintBanner.Instance.Flash(message, 2.5f);
        }
    }
}
