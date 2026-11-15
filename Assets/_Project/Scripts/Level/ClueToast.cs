using UnityEngine;
using Reloj.Clues;

namespace Reloj.Level
{
    /// <summary>Avisa en pantalla cuando se obtiene una pista nueva (I-27). Escucha a ClueInventory.</summary>
    public class ClueToast : MonoBehaviour
    {
        [SerializeField] string prefix = "Pista guardada: ";
        [SerializeField, Min(0.5f)] float seconds = 3f;

        ClueInventory inv;

        void OnEnable() { Bind(); }
        void Start() { Bind(); }          // por si GameServices aun no existia en OnEnable
        void OnDisable() { Unbind(); }

        void Bind()
        {
            var current = ClueInventory.Current;
            if (current == inv || current == null) return;
            Unbind();
            inv = current;
            inv.OnClueAdded += Show;
        }

        void Unbind()
        {
            if (inv != null) inv.OnClueAdded -= Show;
            inv = null;
        }

        void Show(ClueData c)
        {
            if (HintBanner.Instance != null) HintBanner.Instance.Flash(prefix + c.Title, seconds);
        }
    }
}
