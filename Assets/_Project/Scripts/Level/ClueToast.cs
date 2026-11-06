using UnityEngine;
using Reloj.Core;

namespace Reloj.Level
{
    /// <summary>Avisa en pantalla cuando se obtiene una pista (hasta que exista la UI del inventario, I-27).</summary>
    public class ClueToast : MonoBehaviour
    {
        [SerializeField] string prefix = "Pista guardada: ";
        void OnEnable() { ClueTracker.OnClueObtained += Show; }
        void OnDisable() { ClueTracker.OnClueObtained -= Show; }
        void Show(ClueEntry c)
        {
            if (HintBanner.Instance != null) HintBanner.Instance.Flash(prefix + c.title, 3f);
        }
    }
}
