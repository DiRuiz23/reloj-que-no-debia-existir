using UnityEngine;
using Reloj.Core;

namespace Reloj.Level
{
    // Activa o desactiva objetos segun un flag de narrativa (por ejemplo, el brillo de un objeto
    // que aun no se ha tomado). Opcionalmente avisa en pantalla cuando el flag se activa.
    public class FlagToggle : MonoBehaviour
    {
        [SerializeField] string flag = "telegrama_dia20_leido";
        [SerializeField] GameObject[] activeWhenSet;
        [SerializeField] GameObject[] activeWhenNotSet;
        [Tooltip("Aviso breve cuando el flag se activa durante la partida (vacio = ninguno).")]
        [SerializeField] string messageWhenSet = "";

        void OnEnable() { DialogueFlagManager.OnFlagChanged += HandleChanged; }
        void OnDisable() { DialogueFlagManager.OnFlagChanged -= HandleChanged; }
        void Start() { Apply(DialogueFlagManager.GetFlag(flag)); }

        void HandleChanged(string key, bool value)
        {
            if (key != flag) return;
            Apply(value);
            if (value && !string.IsNullOrEmpty(messageWhenSet) && HintBanner.Instance != null)
                HintBanner.Instance.Flash(messageWhenSet, 2.5f);
        }

        void Apply(bool isSet)
        {
            SetAll(activeWhenSet, isSet);
            SetAll(activeWhenNotSet, !isSet);
        }

        static void SetAll(GameObject[] list, bool active)
        {
            if (list == null) return;
            foreach (var go in list)
                if (go != null) go.SetActive(active);
        }
    }
}
