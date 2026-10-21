using UnityEngine;

namespace Reloj.Core
{
    /// <summary>
    /// Herramienta de prueba (I-11): imprime en consola cada cambio de flag y consulta su valor
    /// con DialogueFlagManager.GetFlag, igual que haria cualquier otro script del juego.
    /// </summary>
    public class DialogueFlagLogger : MonoBehaviour
    {
        [Tooltip("Flag que se consulta con GetFlag cada vez que algun flag cambia.")]
        [SerializeField] string watchedFlag = "pista_dia20_obtenida";

        void OnEnable() => DialogueFlagManager.OnFlagChanged += OnChanged;
        void OnDisable() => DialogueFlagManager.OnFlagChanged -= OnChanged;

        void OnChanged(string key, bool value)
        {
            Debug.Log($"[Flags] '{key}' = {value} | GetFlag('{watchedFlag}') = {DialogueFlagManager.GetFlag(watchedFlag)}");
        }
    }
}
